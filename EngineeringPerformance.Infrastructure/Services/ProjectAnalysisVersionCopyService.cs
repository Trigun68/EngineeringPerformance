using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Копирование версии анализа с данными в одной транзакции.
/// Подробности в <see cref="IProjectAnalysisVersionCopyService"/> и docs/issues/ISS-004-version-copy.md.
/// </summary>
public sealed class ProjectAnalysisVersionCopyService : IProjectAnalysisVersionCopyService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectAnalysisVersionCopyService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<int> CopyVersionAsync(
        int sourceVersionId,
        string newVersionName,
        string? newDescription,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newVersionName))
        {
            throw new ArgumentException("Имя новой версии обязательно.", nameof(newVersionName));
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var source = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == sourceVersionId, cancellationToken)
            ?? throw new InvalidOperationException($"ProjectAnalysisVersion with Id={sourceVersionId} not found.");

        // Транзакция: либо вся структура копии создана, либо ничего.
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        // 1. Создаём саму версию.
        var copy = new ProjectAnalysisVersion
        {
            ProjectId = source.ProjectId,
            VersionName = newVersionName.Trim(),
            Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim(),
            Direction = source.Direction,
            PhysicalVolumeTypeId = source.PhysicalVolumeTypeId,
            CreatedAt = DateTime.UtcNow,
            IsArchived = false,
        };
        context.ProjectAnalysisVersions.Add(copy);
        await context.SaveChangesAsync(cancellationToken);

        // 2. ProjectPhysicalVolume — копируем всегда (Plan и Fact).
        var sourcePvs = await context.ProjectPhysicalVolumes
            .AsNoTracking()
            .Where(pv => pv.ProjectAnalysisVersionId == sourceVersionId)
            .ToListAsync(cancellationToken);

        foreach (var pv in sourcePvs)
        {
            context.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume
            {
                ProjectId = pv.ProjectId,
                ProjectAnalysisVersionId = copy.Id,
                PhysicalVolumeId = pv.PhysicalVolumeId,
                Quantity = pv.Quantity,
                FacilityCode = pv.FacilityCode,
                FacilityName = pv.FacilityName,
                BindingType = pv.BindingType,
                DocumentSet = pv.DocumentSet,
                CatalogId = pv.CatalogId,
                Tag = pv.Tag,
                WorkId = pv.WorkId,
                WorkName = pv.WorkName,
                WorkComment = pv.WorkComment,
                EstimationBasis = pv.EstimationBasis,
                CatalogCode = pv.CatalogCode,
                CatalogName = pv.CatalogName,
                LaborHoursPerUnit = pv.LaborHoursPerUnit,
                MachineHoursPerUnit = pv.MachineHoursPerUnit,
                TotalMachineHours = pv.TotalMachineHours,
                Status = pv.Status,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = null,
            });
        }

        // 3. ProjectLaborIntensity — только для Fact-версий (на Plan PLI бессмысленны).
        if (source.Direction == AnalysisDirection.Fact)
        {
            var sourceLis = await context.ProjectLaborIntensities
                .AsNoTracking()
                .Where(li => li.ProjectAnalysisVersionId == sourceVersionId)
                .ToListAsync(cancellationToken);

            foreach (var li in sourceLis)
            {
                context.ProjectLaborIntensities.Add(new ProjectLaborIntensity
                {
                    ProjectId = li.ProjectId,
                    ProjectAnalysisVersionId = copy.Id,
                    DocumentationCode = li.DocumentationCode,
                    WorkDirectionId = li.WorkDirectionId,
                    LaborIntensityId = li.LaborIntensityId,
                    PhysicalVolumeId = li.PhysicalVolumeId,
                    PlannedHours = li.PlannedHours,
                    ActualHours = li.ActualHours,
                    Notes = li.Notes,
                    Brand = li.Brand,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = null,
                });
            }
        }

        // 4. DataImportIteration — копируем 1-в-1 (вариант «б» из ADR-обсуждения):
        //    те же ImportedAt, SourceFileHash, SourceFileName, RecordCount, DataType, ImportedByUserId.
        //    Цель: новая версия должна содержать буквальную аудит-историю источника, без подмены
        //    меток времени и пользователя.
        var sourceIterations = await context.DataImportIterations
            .AsNoTracking()
            .Where(it => it.ProjectAnalysisVersionId == sourceVersionId)
            .ToListAsync(cancellationToken);

        foreach (var it in sourceIterations)
        {
            context.DataImportIterations.Add(new DataImportIteration
            {
                ProjectId = it.ProjectId,
                ProjectAnalysisVersionId = copy.Id,
                DataType = it.DataType,
                ImportedAt = it.ImportedAt,
                Description = it.Description,
                RecordCount = it.RecordCount,
                ImportedByUserId = it.ImportedByUserId,
                SourceFileName = it.SourceFileName,
                SourceFileHash = it.SourceFileHash,
            });
        }

        // AnalysisVersionSnapshot НЕ копируем (вариант «б»): пересоздадутся при первом расчёте
        // PerformanceService по актуальным нормам IIC. Замораживать снимок на момент копии бессмысленно —
        // копия фактически новая версия с CreatedAt = сейчас, и снимок должен соответствовать этому моменту.

        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return copy.Id;
    }
}
