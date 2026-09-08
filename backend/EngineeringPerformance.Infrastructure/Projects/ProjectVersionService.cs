using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Common.Exceptions;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

public sealed class ProjectVersionService : IProjectVersionService
{
    private readonly AppDbContext _context;
    private readonly IProjectVersionDeepCopyService _deepCopy;
    private readonly IProjectVersionPlanningParameterService _planningParameters;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator;
    private readonly IProjectAnalyticsCache _analyticsCache;
    private readonly IUserActionLogger _actionLogger;

    public ProjectVersionService(
        AppDbContext context,
        IProjectVersionDeepCopyService deepCopy,
        IProjectVersionPlanningParameterService planningParameters,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator,
        IProjectAnalyticsCache analyticsCache,
        IUserActionLogger actionLogger)
    {
        _context = context;
        _deepCopy = deepCopy;
        _planningParameters = planningParameters;
        _recalcOrchestrator = recalcOrchestrator;
        _analyticsCache = analyticsCache;
        _actionLogger = actionLogger;
    }

    public async Task<IReadOnlyList<ProjectVersionDto>> ListAsync(int projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.ProjectId == projectId && !v.IsArchived)
            .OrderBy(v => v.SortOrder)
            .Select(Map)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectVersionDto?> GetByIdAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectAnalysisVersions.AsNoTracking()
            .Where(v => v.ProjectId == projectId && v.Id == versionId)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProjectVersionDto> CreateAsync(int projectId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default)
    {
        await ValidatePhysicalVolumeTypeAsync(request.PhysicalVolumeTypeId, cancellationToken);

        var asOf = NormalizeAsOfDate(request.AsOfDate) ?? DateTime.UtcNow;
        var hasPeers = await _context.ProjectAnalysisVersions.AsNoTracking()
            .AnyAsync(
                v => v.ProjectId == projectId
                     && v.Direction == request.Direction
                     && !v.IsArchived,
                cancellationToken);

        // Первая версия направления — всегда основная; иначе только по явному запросу.
        var makePrimary = !hasPeers || request.IsPrimary == true;

        var entity = new ProjectAnalysisVersion
        {
            ProjectId = projectId,
            Name = request.Name?.Trim() ?? request.VersionName.Trim(),
            VersionName = request.VersionName.Trim(),
            Description = request.Description,
            Direction = request.Direction,
            PhysicalVolumeTypeId = request.PhysicalVolumeTypeId,
            PlanningScenario = request.PlanningScenario ?? Domain.Norms.PlanningScenario.ByEachValue,
            ZParameterLevel = request.ZParameterLevel ?? Domain.Norms.ZParameterLevel.EachElement,
            IsPrimary = makePrimary,
            CreatedAt = asOf,
        };

        await RunInTransactionAsync(async () =>
        {
            // Filtered unique index UX_ProjectAnalysisVersions_OnePrimary проверяется per-statement:
            // сначала снимаем primary с соседей и сохраняем, затем вставляем новую основную.
            if (makePrimary && hasPeers)
            {
                await ClearPrimaryAsync(projectId, request.Direction, excludeVersionId: null, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _context.ProjectAnalysisVersions.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            await AnalysisVersionSnapshotService.PopulateSnapshotAsync(
                _context, entity.Id, entity.CreatedAt, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.VersionCreate,
            entityType: "ProjectAnalysisVersion",
            entityId: entity.Id,
            description: entity.VersionName,
            cancellationToken);

        await _planningParameters.SeedForVersionAsync(entity.Id, cancellationToken);
        await InvalidateAnalyticsAsync(projectId, cancellationToken);

        return (await GetByIdAsync(projectId, entity.Id, cancellationToken))!;
    }

    public async Task<ProjectVersionDto?> UpdateAsync(int projectId, int versionId, ProjectVersionWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectAnalysisVersions
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (entity is null)
            return null;

        if (request.RowVersion is { Length: > 0 })
            _context.Entry(entity).Property(v => v.RowVersion).OriginalValue = request.RowVersion;

        await ValidatePhysicalVolumeTypeAsync(request.PhysicalVolumeTypeId, cancellationToken);

        var catalogChanged = entity.PhysicalVolumeTypeId != request.PhysicalVolumeTypeId;
        if (catalogChanged)
            await EnsureCatalogChangeAllowedAsync(versionId, cancellationToken);

        // Сравниваем по календарному дню: UI шлёт date-only → noon UTC,
        // чтобы правка имени не сбрасывала CreatedAt и снимок.
        var asOf = NormalizeAsOfDate(request.AsOfDate);
        var createdAtChanged = asOf.HasValue
            && asOf.Value.Date != entity.CreatedAt.ToUniversalTime().Date;

        var previousScenario = entity.PlanningScenario;
        var previousDirection = entity.Direction;
        entity.Name = request.Name?.Trim() ?? request.VersionName.Trim();
        entity.VersionName = request.VersionName.Trim();
        entity.Description = request.Description;
        entity.Direction = request.Direction;
        entity.PhysicalVolumeTypeId = request.PhysicalVolumeTypeId;
        if (request.PlanningScenario.HasValue)
            entity.PlanningScenario = request.PlanningScenario.Value;
        if (request.ZParameterLevel.HasValue)
            entity.ZParameterLevel = request.ZParameterLevel.Value;
        if (createdAtChanged)
            entity.CreatedAt = asOf!.Value;
        entity.ModifiedAt = DateTime.UtcNow;
        // IsPrimary не меняется через Update — только Create / SetPrimary / Archive.

        var scenarioChanged = request.PlanningScenario.HasValue
            && request.PlanningScenario.Value != previousScenario;

        var directionChangedFromPrimary = previousDirection != request.Direction && entity.IsPrimary;

        await RunInTransactionAsync(async () =>
        {
            if (directionChangedFromPrimary)
            {
                // Смена направления: сначала снять primary и сохранить (сущность в БД ещё числится
                // primary старого направления), затем восстановить основную у старого направления.
                // Порядок важен из-за filtered unique index UX_ProjectAnalysisVersions_OnePrimary.
                entity.IsPrimary = false;
                await SaveWithConcurrencyCheckAsync(cancellationToken);
                await PromotePrimaryIfMissingAsync(projectId, previousDirection, cancellationToken);

                var hasPeers = await _context.ProjectAnalysisVersions
                    .AnyAsync(
                        v => v.ProjectId == projectId
                             && v.Direction == request.Direction
                             && !v.IsArchived
                             && v.Id != versionId,
                        cancellationToken);
                // Единственная версия нового направления — становится его основной.
                if (!hasPeers)
                    entity.IsPrimary = true;
            }

            // Снимок норм зависит от CreatedAt; смена справочника БЕР/КФО тоже требует
            // явного пересоздания (единая политика версии).
            if (catalogChanged || createdAtChanged)
            {
                await AnalysisVersionSnapshotService.RepopulateSnapshotAsync(
                    _context, entity.Id, entity.CreatedAt, cancellationToken);
            }

            await SaveWithConcurrencyCheckAsync(cancellationToken);
        }, cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.VersionEdit,
            entityType: "ProjectAnalysisVersion",
            entityId: entity.Id,
            description: entity.VersionName,
            cancellationToken);

        var recalcCount = 0;
        if (catalogChanged || createdAtChanged || scenarioChanged)
        {
            await _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, "scenario", cancellationToken);
            recalcCount = 1;
        }

        await InvalidateAnalyticsAsync(projectId, cancellationToken);

        var dto = await GetByIdAsync(projectId, versionId, cancellationToken);
        return dto is null ? null : dto with { RecalculatedVersionCount = recalcCount };
    }

    public async Task<bool> ArchiveAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectAnalysisVersions
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (entity is null)
            return false;

        var wasPrimary = entity.IsPrimary;
        var direction = entity.Direction;
        entity.IsArchived = true;
        entity.IsPrimary = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.VersionDelete,
            entityType: "ProjectAnalysisVersion",
            entityId: entity.Id,
            description: entity.VersionName,
            cancellationToken);

        if (wasPrimary)
            await PromotePrimaryIfMissingAsync(projectId, direction, cancellationToken);

        await InvalidateAnalyticsAsync(projectId, cancellationToken);
        return true;
    }

    public async Task<ProjectVersionDto?> UnarchiveAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectAnalysisVersions
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (entity is null || !entity.IsArchived)
            return null;

        entity.IsArchived = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.VersionRestore,
            entityType: "ProjectAnalysisVersion",
            entityId: entity.Id,
            description: entity.VersionName,
            cancellationToken);

        await InvalidateAnalyticsAsync(projectId, cancellationToken);

        return await GetByIdAsync(projectId, versionId, cancellationToken);
    }

    public async Task<ProjectVersionDto?> SetPrimaryAsync(int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProjectAnalysisVersions
            .FirstOrDefaultAsync(
                v => v.ProjectId == projectId && v.Id == versionId && !v.IsArchived,
                cancellationToken);
        if (entity is null)
            return null;

        if (!entity.IsPrimary)
        {
            await RunInTransactionAsync(async () =>
            {
                // Два SaveChanges: unique-индекс по primary проверяется на каждом UPDATE отдельно.
                await ClearPrimaryAsync(projectId, entity.Direction, excludeVersionId: versionId, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                entity.IsPrimary = true;
                entity.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }, cancellationToken);

            await _actionLogger.LogAsync(
                UserActionType.VersionSetPrimary,
                entityType: "ProjectAnalysisVersion",
                entityId: entity.Id,
                description: entity.VersionName,
                cancellationToken);

            await InvalidateAnalyticsAsync(projectId, cancellationToken);
        }

        return await GetByIdAsync(projectId, versionId, cancellationToken);
    }

    public async Task<ProjectVersionDto> CopyAsync(
        int projectId,
        int versionId,
        string newVersionName,
        string? newDescription = null,
        AnalysisDirection? targetDirection = null,
        CancellationToken cancellationToken = default)
    {
        var copy = await _deepCopy.CopyVersionAsync(
            projectId,
            versionId,
            newVersionName,
            newDescription,
            targetDirection,
            cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.VersionCopy,
            entityType: "ProjectAnalysisVersion",
            entityId: copy.Id,
            description: copy.VersionName,
            cancellationToken);

        await InvalidateAnalyticsAsync(projectId, cancellationToken);
        return copy;
    }

    /// <summary>
    /// Состав и признаки версий влияют на page-data и отчёт производительности,
    /// поэтому любая мутация версии сбрасывает кэш проекта (ISS-018).
    /// </summary>
    private Task InvalidateAnalyticsAsync(int projectId, CancellationToken cancellationToken) =>
        _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);

    /// <summary>
    /// Смена БЕР↔КФО при уже импортированных ФО недопустима: FK указывают на другой каталог.
    /// </summary>
    private async Task EnsureCatalogChangeAllowedAsync(int versionId, CancellationToken cancellationToken)
    {
        var hasPpv = await _context.ProjectPhysicalVolumes.AsNoTracking()
            .AnyAsync(pv => pv.ProjectAnalysisVersionId == versionId, cancellationToken);
        if (!hasPpv)
            return;

        throw new InvalidOperationException(
            "Нельзя сменить справочник физобъёмов (БЕР/КФО): для версии уже импортированы "
            + "физические объёмы. Удалите данные ФО версии или создайте новую версию с нужным справочником.");
    }

    /// <summary>
    /// Пользовательская транзакция внутри execution strategy (EnableRetryOnFailure).
    /// Если транзакция уже открыта вызывающим кодом — просто выполняем действие.
    /// </summary>
    private async Task RunInTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is not null || !_context.Database.IsRelational())
        {
            await action();
            return;
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await action();
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task ClearPrimaryAsync(
        int projectId,
        AnalysisDirection direction,
        int? excludeVersionId,
        CancellationToken cancellationToken)
    {
        var query = _context.ProjectAnalysisVersions
            .Where(v => v.ProjectId == projectId
                        && v.Direction == direction
                        && !v.IsArchived
                        && v.IsPrimary);
        if (excludeVersionId.HasValue)
            query = query.Where(v => v.Id != excludeVersionId.Value);

        var others = await query.ToListAsync(cancellationToken);
        foreach (var other in others)
        {
            other.IsPrimary = false;
            other.ModifiedAt = DateTime.UtcNow;
        }
    }

    private async Task SaveWithConcurrencyCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Версия изменена другим пользователем. Обновите данные и повторите.");
        }
    }

    private async Task PromotePrimaryIfMissingAsync(
        int projectId,
        AnalysisDirection direction,
        CancellationToken cancellationToken)
    {
        var hasPrimary = await _context.ProjectAnalysisVersions
            .AnyAsync(
                v => v.ProjectId == projectId
                     && v.Direction == direction
                     && !v.IsArchived
                     && v.IsPrimary,
                cancellationToken);
        if (hasPrimary)
            return;

        var next = await _context.ProjectAnalysisVersions
            .Where(v => v.ProjectId == projectId && v.Direction == direction && !v.IsArchived)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (next is null)
            return;

        next.IsPrimary = true;
        next.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidatePhysicalVolumeTypeAsync(int? physicalVolumeTypeId, CancellationToken cancellationToken)
    {
        if (physicalVolumeTypeId is not int typeId)
            throw new InvalidOperationException(
                "Не выбран справочник физобъёмов. Укажите КФО или БЕР для версии проекта.");

        var typeName = await _context.PhysicalVolumeTypes.AsNoTracking()
            .Where(t => t.Id == typeId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(typeName)
            || (!string.Equals(typeName, PhysicalVolumeCatalogBinding.KfoTypeName, StringComparison.Ordinal)
                && !PhysicalVolumeCatalogBinding.IsBerTypeName(typeName)))
        {
            throw new InvalidOperationException(
                "Справочник физобъёмов должен быть КФО или БЕР.");
        }
    }

    /// <summary>Нормализует AsOfDate к UTC; date-only (Kind=Unspecified) → полдень UTC.</summary>
    private static DateTime? NormalizeAsOfDate(DateTime? asOfDate)
    {
        if (asOfDate is not DateTime value)
            return null;

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Date.AddHours(12), DateTimeKind.Utc),
        };
    }

    private static System.Linq.Expressions.Expression<Func<ProjectAnalysisVersion, ProjectVersionDto>> Map =>
        v => new ProjectVersionDto(
            v.Id,
            v.ProjectId,
            v.Name,
            v.VersionName,
            v.Description,
            v.Direction,
            v.PhysicalVolumeTypeId,
            v.SortOrder,
            v.IsArchived,
            v.CreatedAt,
            v.PlanningScenario,
            v.ZParameterLevel,
            v.IsPrimary,
            0,
            v.RowVersion);
}
