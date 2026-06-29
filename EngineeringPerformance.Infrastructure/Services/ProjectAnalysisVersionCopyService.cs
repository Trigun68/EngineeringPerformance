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
            throw new ArgumentException("Имя новой версии обязательно.", nameof(newVersionName));

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var source = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == sourceVersionId, cancellationToken)
            ?? throw new InvalidOperationException($"ProjectAnalysisVersion with Id={sourceVersionId} not found.");

        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

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
                PhysicalVolumeKfoId = pv.PhysicalVolumeKfoId,
                PhysicalVolumeBerId = pv.PhysicalVolumeBerId,
                ParentId = pv.ParentId,
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
                    PhysicalVolumeKfoId = li.PhysicalVolumeKfoId,
                    PhysicalVolumeBerId = li.PhysicalVolumeBerId,
                    ProjectPhysicalVolumeId = li.ProjectPhysicalVolumeId,
                    PlannedHours = li.PlannedHours,
                    ActualHours = li.ActualHours,
                    Notes = li.Notes,
                    Brand = li.Brand,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = null,
                });
            }
        }

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

        var activeNorms = await context.IicWorkDirectionNorms
            .AsNoTracking()
            .Where(n => n.EffectiveFrom <= copy.CreatedAt
                        && (n.EffectiveTo == null || n.EffectiveTo >= copy.CreatedAt))
            .ToListAsync(cancellationToken);

        var pickedByDirection = activeNorms
            .GroupBy(n => n.WorkDirectionId)
            .Select(g => g.OrderByDescending(n => n.EffectiveFrom).First());

        foreach (var norm in pickedByDirection)
        {
            context.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = copy.Id,
                WorkDirectionId = norm.WorkDirectionId,
                NormId = norm.Id,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return copy.Id;
    }
}
