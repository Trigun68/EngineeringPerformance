using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Import;

internal sealed partial class EquipmentImportService
{
    public Task<DataImportResultDto> IngestEquipmentAsync(
        int projectId,
        int versionId,
        IReadOnlyList<EquipmentIngestItem> items,
        bool isReplace,
        string description,
        ImportSourceMetadata source,
        CancellationToken cancellationToken = default) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(
            _contextFactory,
            context => IngestEquipmentCoreAsync(
                context, projectId, versionId, items, isReplace, description, source),
            cancellationToken);

    private async Task<DataImportResultDto> IngestEquipmentCoreAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        IReadOnlyList<EquipmentIngestItem> items,
        bool isReplace,
        string description,
        ImportSourceMetadata source)
    {
        if (isReplace)
            await ImportHelpers.ClearEquipmentDataForVersionAsync(context, projectId, versionId);

        var iteration = await ImportIterationRecorder.CreateIterationAsync(
            context, projectId, versionId, DataImportType.Equipment, description, source);

        var catalogRows = await context.Equipments.AsNoTracking()
            .Where(e => e.ProjectId != null)
            .ToListAsync();
        var units = await context.UnitsOfMeasurement.AsNoTracking().ToListAsync();
        var (catalogLookup, ambiguousKeys, _) =
            EquipmentImportCatalogMatcher.BuildCatalogLookup(catalogRows, projectId, _logger);

        var pendingCatalogCreates = new List<Equipment>();
        var drafts = new List<ProjectEquipment>();
        var rowErrors = new List<ImportRowErrorDto>();
        var importTimestamp = iteration.ImportedAt;
        var autoCreated = 0;
        var copied = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var rowNumber = i + 1;
            var item = items[i];
            Equipment? equipment = null;

            if (item.EquipmentId is int equipmentId)
            {
                equipment = catalogRows.FirstOrDefault(e => e.Id == equipmentId)
                            ?? await context.Equipments.FirstOrDefaultAsync(e => e.Id == equipmentId);
                if (equipment is null)
                {
                    rowErrors.Add(new ImportRowErrorDto(rowNumber, $"Equipment {equipmentId} not found."));
                    continue;
                }
            }
            else
            {
                var catalogName = item.CatalogName?.Trim();
                if (string.IsNullOrWhiteSpace(catalogName))
                {
                    rowErrors.Add(new ImportRowErrorDto(rowNumber, "catalogName or equipmentId is required."));
                    continue;
                }

                var unitId = item.UnitId ?? EquipmentUnitNameMatcher.ResolveUnitId(item.UnitName, units);
                var resolveResult = EquipmentImportCatalogMatcher.ResolveOrCreate(
                    projectId,
                    catalogName,
                    unitId,
                    catalogLookup,
                    ambiguousKeys,
                    catalogRows,
                    pendingCatalogCreates);

                if (resolveResult.SkippedDuplicate)
                {
                    rowErrors.Add(new ImportRowErrorDto(rowNumber, $"Duplicate catalog key: {catalogName}"));
                    continue;
                }

                if (resolveResult.Created)
                {
                    if (resolveResult.CopiedFromOtherProject)
                        copied++;
                    else
                        autoCreated++;
                }

                equipment = resolveResult.Equipment;
            }

            drafts.Add(BuildProjectEquipmentFromIngestItem(item, projectId, versionId, equipment!, importTimestamp));
        }

        if (pendingCatalogCreates.Count > 0)
            context.Equipments.AddRange(pendingCatalogCreates);

        int imported;
        if (isReplace)
        {
            if (drafts.Count > 0)
                context.ProjectEquipments.AddRange(drafts);
            imported = drafts.Count;
        }
        else
        {
            var processorRows = drafts
                .Select((draft, index) => new EquipmentImportReplaceProcessor.RowInput(index + 1, null, draft, true))
                .ToList();
            var processed = await EquipmentImportReplaceProcessor.ApplyAsync(
                context, projectId, versionId, processorRows, _logger);
            rowErrors.AddRange(processed.Errors);
            imported = processed.ProcessedCount;
        }

        iteration.RecordCount = imported;
        await context.SaveChangesAsync();

        var details = EquipmentImportCatalogMatcher.BuildImportSummary(
            imported, autoCreated, skippedDuplicateCount: 0, skippedDuplicateRows: [], copied);

        return new DataImportResultDto(
            true,
            imported,
            null,
            iteration.Id,
            details,
            rowErrors.Count > 0 ? rowErrors : null);
    }

    private static ProjectEquipment BuildProjectEquipmentFromIngestItem(
        EquipmentIngestItem item,
        int projectId,
        int versionId,
        Equipment equipment,
        DateTime importTimestamp)
    {
        var entity = new ProjectEquipment
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            CreatedAt = importTimestamp,
            Quantity = item.Quantity,
            QuantityInWork = item.QuantityInWork,
            PositionNumber = TruncateEquipmentField(item.PositionNumber, 50),
            FacilityCode = TruncateEquipmentField(item.FacilityCode, 100),
            FacilityName = TrimEquipmentField(item.FacilityName),
            DocumentSet = TruncateEquipmentField(item.DocumentSet, 100),
            DiagramNumber = TrimEquipmentField(item.DiagramNumber),
            ShortName = TrimEquipmentField(item.ShortName),
            CatalogName = TrimEquipmentField(item.CatalogName) ?? equipment.Name,
            TypeMark = TrimEquipmentField(item.TypeMark),
            CatalogId = TruncateEquipmentField(item.CatalogId, 100),
            Tag = TruncateEquipmentField(item.Tag, 100),
            FullEquipmentId = TruncateEquipmentField(item.FullEquipmentId, 100),
            SupplyStandard = TrimEquipmentField(item.SupplyStandard),
            OlReference = TrimEquipmentField(item.OlReference),
            EstimationBasis = TrimEquipmentField(item.EstimationBasis),
        };

        if (equipment.Id > 0)
            entity.EquipmentId = equipment.Id;
        else
            entity.Equipment = equipment;

        return entity;
    }
}
