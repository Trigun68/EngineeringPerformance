using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Projects;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Import;

internal sealed partial class PhysicalVolumeExcelImportService
{
    public Task<DataImportResultDto> IngestPhysicalVolumesAsync(
        int projectId,
        int versionId,
        IReadOnlyList<PhysicalVolumeIngestItem> items,
        bool isReplace,
        string description,
        ImportSourceMetadata source,
        CancellationToken cancellationToken = default) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(
            _contextFactory,
            context => IngestPhysicalVolumesCoreAsync(
                context, projectId, versionId, items, isReplace, description, source),
            cancellationToken);

    private async Task<DataImportResultDto> IngestPhysicalVolumesCoreAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        IReadOnlyList<PhysicalVolumeIngestItem> items,
        bool isReplace,
        string description,
        ImportSourceMetadata source)
    {
        if (isReplace)
            await ImportHelpers.ClearPhysicalVolumeDataForVersionAsync(context, projectId, versionId);

        var iteration = await ImportIterationRecorder.CreateIterationAsync(
            context, projectId, versionId, DataImportType.PhysicalVolume, description, source);

        await PhysicalVolumeCatalogBinding.EnsureCatalogTypeConfiguredAsync(context, versionId);
        var isKfo = await PhysicalVolumeCatalogBinding.IsKfoVersionAsync(context, versionId);
        var physicalVolumeRows = isKfo
            ? await context.PhysicalVolumesKfo.AsNoTracking()
                .Select(pv => new PvCatalogRow(pv.Id, pv.Code, pv.Name, pv.ClassifierId, pv.ParentId, pv.Coefficient, pv.IsAssessment))
                .ToListAsync()
            : await context.PhysicalVolumesBer.AsNoTracking()
                .Select(pv => new PvCatalogRow(pv.Id, pv.Code, pv.Name, pv.ClassifierId, pv.ParentId, pv.Coefficient, pv.IsAssessment))
                .ToListAsync();
        var catalogName = isKfo ? "PhysicalVolumesKfo" : "PhysicalVolumesBer";
        var physicalVolumes = ImportHelpers.BuildCodeLookup(physicalVolumeRows, catalogName, _logger);
        var pvById = physicalVolumeRows.ToDictionary(pv => pv.Id);

        var existingByKey = isReplace
            ? new Dictionary<(int CatalogId, string FacilityCode, string FacilityName, string WorkId), ProjectPhysicalVolume>()
            : (await context.ProjectPhysicalVolumes
                    .Where(x => x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId)
                    .ToListAsync())
                .GroupBy(x => (
                    CatalogId: x.PhysicalVolumeKfoId ?? x.PhysicalVolumeBerId ?? 0,
                    FacilityCode: x.FacilityCode ?? string.Empty,
                    FacilityName: x.FacilityName ?? string.Empty,
                    WorkId: x.WorkId ?? string.Empty))
                .ToDictionary(g => g.Key, g => g.First());

        var newEntities = new List<ProjectPhysicalVolume>();
        var rowErrors = new List<ImportRowErrorDto>();
        var skippedNoCode = 0;
        var skippedNotInCatalog = 0;
        var imported = 0;
        var importTimestamp = iteration.ImportedAt;

        for (var i = 0; i < items.Count; i++)
        {
            var rowNumber = i + 1;
            var item = items[i];
            if (!TryResolveIngestCatalogRow(item, physicalVolumes, pvById, out var catalogRow))
            {
                if (string.IsNullOrWhiteSpace(item.EpCode) && item.PhysicalVolumeId is null)
                {
                    skippedNoCode++;
                    rowErrors.Add(new ImportRowErrorDto(rowNumber, "epCode or physicalVolumeId is required."));
                }
                else
                {
                    skippedNotInCatalog++;
                    rowErrors.Add(new ImportRowErrorDto(
                        rowNumber,
                        $"Catalog volume '{item.EpCode ?? item.PhysicalVolumeId?.ToString()}' not found for this version."));
                }

                continue;
            }

            var draft = new PhysicalVolumeImportDraft
            {
                Quantity = item.Quantity,
                FacilityCode = item.FacilityCode,
                FacilityName = item.FacilityName,
                BindingType = item.BindingType,
                DocumentSet = item.DocumentSet,
                CatalogId = item.CatalogId,
                Tag = item.Tag,
                WorkId = item.WorkId,
                WorkName = item.WorkName,
                WorkComment = item.WorkComment,
                EstimationBasis = item.EstimationBasis,
                CatalogCode = catalogRow.Code,
                CatalogName = catalogRow.Name,
            };
            draft.ApplyTruncation();

            var key = (
                CatalogId: catalogRow.Id,
                FacilityCode: draft.FacilityCode ?? string.Empty,
                FacilityName: draft.FacilityName ?? string.Empty,
                WorkId: draft.WorkId ?? string.Empty);

            if (!isReplace && existingByKey.TryGetValue(key, out var existing))
            {
                draft.ApplyTo(existing);
                PhysicalVolumeCatalogBinding.AssignCatalogId(existing, catalogRow.Id, isKfo);
                PhysicalVolumeCatalogBinding.FillCatalogSnapshot(
                    existing, catalogRow.Coefficient, catalogRow.IsAssessment, catalogRow.ClassifierId);
                existing.ModifiedAt = DateTime.UtcNow;
                imported++;
                continue;
            }

            var entity = draft.ToNewEntity(projectId, versionId, catalogRow.Id, isKfo, importTimestamp, catalogRow);
            newEntities.Add(entity);
            existingByKey[key] = entity;
            imported++;
        }

        await ImportHelpers.BackfillPhysicalVolumeClassifiersFromAncestorsAsync(
            context,
            newEntities.Select(e => e.PhysicalVolumeKfoId ?? e.PhysicalVolumeBerId).Where(id => id.HasValue).Select(id => id!.Value),
            pvById,
            isKfo,
            _logger);

        if (newEntities.Count > 0)
            context.ProjectPhysicalVolumes.AddRange(newEntities);

        iteration.RecordCount = imported;
        await context.SaveChangesAsync();

        var details =
            $"Импортировано строк: {imported}." +
            (skippedNoCode + skippedNotInCatalog > 0
                ? $"{Environment.NewLine}Пропущено без шифра: {skippedNoCode}. Не найдено в каталоге: {skippedNotInCatalog}."
                : string.Empty);

        return new DataImportResultDto(
            true,
            imported,
            null,
            iteration.Id,
            details,
            rowErrors.Count > 0 ? rowErrors : null);
    }

    private static bool TryResolveIngestCatalogRow(
        PhysicalVolumeIngestItem item,
        ILookup<string, PvCatalogRow> physicalVolumes,
        IReadOnlyDictionary<int, PvCatalogRow> pvById,
        out PvCatalogRow catalogRow)
    {
        if (item.PhysicalVolumeId is int id && pvById.TryGetValue(id, out catalogRow))
            return true;

        if (!string.IsNullOrWhiteSpace(item.EpCode) && ImportHelpers.TryResolvePhysicalVolume(physicalVolumes, item.EpCode, out catalogRow))
            return true;

        catalogRow = default;
        return false;
    }
}
