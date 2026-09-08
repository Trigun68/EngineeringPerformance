using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class PerformanceService : IPerformanceService
{
    private readonly AppDbContext _context;
    private readonly IPerformanceCalculationEngine _calculationEngine;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectVisibilityService _visibility;
    private List<IicWorkDirection>? _workDirectionsCache;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public PerformanceService(
        AppDbContext context,
        IPerformanceCalculationEngine calculationEngine,
        ICurrentUserService currentUser,
        IProjectVisibilityService visibility)
    {
        _context = context;
        _calculationEngine = calculationEngine;
        _currentUser = currentUser;
        _visibility = visibility;
    }

    public Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default) =>
        GetPerformanceDataAsync(projectId, versionId, LaborHoursKind.Planned, cancellationToken);

    public async Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(
        int projectId,
        int versionId,
        LaborHoursKind hoursKind,
        CancellationToken cancellationToken = default)
    {
        // GAP WPF: defense-in-depth — проверка ProjectVisibility с уровнем Read
        // (дополняет [RequireProjectAccess] на контроллере; без учёта Level в WPF данные утекали).
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Read))
            throw new UnauthorizedAccessException($"User {userId} cannot read project {projectId}.");

        // GAP: один scoped DbContext на запрос (раньше — до 5 параллельных factory-контекстов).
        var versionExists = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .AnyAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);

        var workDirections = await GetWorkDirectionsAsync(cancellationToken);

        var physicalVolumes = await _context.ProjectPhysicalVolumes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhysicalVolumeKfo).Include(x => x.PhysicalVolumeBer)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var equipmentRows = await _context.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var laborIntensities = await _context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection;
        if (versionExists)
        {
            var snapshots = await _context.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => s.ProjectAnalysisVersionId == versionId)
                .Include(s => s.Norm)
                .ToListAsync(cancellationToken);

            normsByDirection = snapshots
                .Where(s => s.Norm is not null)
                .GroupBy(s => s.WorkDirectionId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var norm = g.First().Norm!;
                        return NormSnapshotFactory.FromEntity(norm);
                    });
        }
        else
        {
            normsByDirection = new Dictionary<int, NormCalculationSnapshot>();
        }

        var directionById = workDirections.ToDictionary(d => d.Id);

        var input = new PerformanceCalculationInput(
            VersionExists: versionExists,
            WorkDirections: workDirections
                .Select(d => new WorkDirectionCalculationNode(
                    d.Id,
                    d.ParentId,
                    d.Name,
                    d.Code,
                    IicWorkDirectionDisplayUnit.ResolveUnitName(d, directionById)))
                .ToList(),
            PhysicalVolumes: physicalVolumes
                .Where(pv => pv.GetCatalogVolumeId().HasValue)
                .Select(pv => new PhysicalVolumeCalculationLine(
                    pv.GetClassifierId(),
                    pv.GetIsAssessment() == true,
                    pv.Quantity,
                    pv.GetCoefficient()))
                .ToList(),
            Equipment: equipmentRows
                .Select(e => new EquipmentCalculationLine(
                    e.Equipment?.ClassifierId,
                    e.Equipment?.IsAssessment == true,
                    e.Quantity,
                    e.Equipment?.Coefficient ?? 1m))
                .ToList(),
                    LaborIntensities: laborIntensities
                .Select(li => new LaborIntensityCalculationLine(
                    li.WorkDirectionId,
                    hoursKind == LaborHoursKind.Actual ? li.ActualHours : li.PlannedHours))
                .ToList(),
            NormsByDirection: normsByDirection,
            RollHoursToFirstLevel: true);

        // GAP: математика только в PerformanceCalculationEngine, не в LINQ/циклах сервиса.
        var rows = _calculationEngine.CalculatePerformance(input);
        return ProjectsPageGridBuilder.ApplyFormattedWorkDirectionNames(rows, directionById);
    }

    private async Task<List<IicWorkDirection>> GetWorkDirectionsAsync(CancellationToken cancellationToken)
    {
        if (_workDirectionsCache is not null)
            return _workDirectionsCache;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_workDirectionsCache is not null)
                return _workDirectionsCache;

            _workDirectionsCache = await _context.IicWorkDirections
                .AsNoTracking()
                .Include(d => d.Unit)
                .Where(d => !d.IsArchived)
                .ToListAsync(cancellationToken);
            return _workDirectionsCache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
