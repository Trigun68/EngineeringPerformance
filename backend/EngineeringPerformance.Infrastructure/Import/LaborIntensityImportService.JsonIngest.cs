using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Import;

internal sealed partial class LaborIntensityImportService
{
    public Task<DataImportResultDto> IngestLaborIntensitiesAsync(
        int projectId,
        int versionId,
        IReadOnlyList<LaborIntensityIngestItem> items,
        bool isReplace,
        bool addMissingToCatalog,
        string description,
        ImportSourceMetadata source,
        CancellationToken cancellationToken = default) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(
            _contextFactory,
            context => IngestLaborIntensitiesCoreAsync(
                context,
                projectId,
                versionId,
                items,
                isReplace,
                addMissingToCatalog,
                description,
                source),
            cancellationToken);

    public async Task<LaborImportPreviewDto> PreviewLaborIngestAsync(
        IReadOnlyList<string> documentTypeNames,
        CancellationToken cancellationToken = default)
    {
        var names = (documentTypeNames ?? [])
            .Select(x => x?.Trim() ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToList();

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var known = LaborCatalogLookup.KnownKeys(
            await context.LaborIntensities.AsNoTracking().ToListAsync(cancellationToken));

        var unmatched = names
            .Where(n => !known.Contains(n))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return new LaborImportPreviewDto(unmatched, names.Count);
    }

    private async Task<DataImportResultDto> IngestLaborIntensitiesCoreAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        IReadOnlyList<LaborIntensityIngestItem> items,
        bool isReplace,
        bool addMissingToCatalog,
        string description,
        ImportSourceMetadata source)
    {
        if (isReplace)
            await ImportHelpers.ClearLaborIntensityDataForVersionAsync(context, projectId, versionId);

        var iteration = await ImportIterationRecorder.CreateIterationAsync(
            context, projectId, versionId, DataImportType.LaborIntensity, description, source);

        var laborCatalogRows = await context.LaborIntensities.ToListAsync();
        var laborByDocumentType = LaborCatalogLookup.BuildMap(laborCatalogRows);

        var importTimestamp = iteration.ImportedAt;
        if (addMissingToCatalog)
        {
            var created = 0;
            foreach (var docType in items
                         .Select(x => x.DocumentTypeName?.Trim() ?? string.Empty)
                         .Where(x => x.Length > 0)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (laborByDocumentType.ContainsKey(docType))
                    continue;

                var code = docType.Length <= 100 ? docType : docType[..100];
                var name = docType.Length <= 500 ? docType : docType[..500];
                var newCatalogItem = new LaborIntensity
                {
                    Code = code,
                    Name = name,
                    DocumentTypeName = name,
                    IsArchived = false,
                    SortOrder = 0,
                    CreatedAt = importTimestamp,
                };
                context.LaborIntensities.Add(newCatalogItem);
                laborByDocumentType[docType] = newCatalogItem;
                created++;
            }

            if (created > 0)
                await context.SaveChangesAsync();
        }

        var existingByCode = isReplace
            ? new Dictionary<string, ProjectLaborIntensity>(StringComparer.Ordinal)
            : (await context.ProjectLaborIntensities
                    .Where(x => x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId)
                    .ToListAsync())
                .GroupBy(x => x.DocumentationCode ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var docSetBinder = new DocSetImportBinder(context);
        var newEntities = new List<ProjectLaborIntensity>();
        var unmatched = new SortedSet<string>(StringComparer.Ordinal);
        var imported = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var documentTypeName = item.DocumentTypeName?.Trim();
            if (string.IsNullOrWhiteSpace(documentTypeName))
                continue;

            laborByDocumentType.TryGetValue(documentTypeName, out var laborItem);
            if (laborItem is null)
                unmatched.Add(documentTypeName);

            var brand = item.Brand?.Trim();
            var documentationCode = string.IsNullOrWhiteSpace(brand)
                ? documentTypeName
                : $"{documentTypeName}|{brand}";
            if (documentationCode.Length > 450)
                documentationCode = documentationCode[..450];

            if (!isReplace && existingByCode.TryGetValue(documentationCode, out var existing))
            {
                existing.LaborIntensityId = laborItem?.Id;
                existing.WorkDirectionId = laborItem?.IicWorkDirectionId;
                existing.PlannedHours = item.PlannedHours;
                existing.ActualHours = item.ActualHours;
                existing.Brand = brand;
                existing.Notes = item.Notes;
                existing.DocSetId = await docSetBinder.ResolveAsync(documentationCode);
                existing.ModifiedAt = DateTime.UtcNow;
                imported++;
                continue;
            }

            var entity = new ProjectLaborIntensity
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                DocumentationCode = documentationCode,
                WorkDirectionId = laborItem?.IicWorkDirectionId,
                LaborIntensityId = laborItem?.Id,
                PlannedHours = item.PlannedHours,
                ActualHours = item.ActualHours,
                Brand = brand,
                Notes = item.Notes,
                CreatedAt = importTimestamp,
            };
            entity.DocSetId = await docSetBinder.ResolveAsync(documentationCode);
            newEntities.Add(entity);
            existingByCode[documentationCode] = entity;
            imported++;
        }

        if (newEntities.Count > 0)
            context.ProjectLaborIntensities.AddRange(newEntities);

        iteration.RecordCount = imported;
        await context.SaveChangesAsync();

        var details = $"Импортировано строк: {imported}.";
        if (unmatched.Count > 0)
            details += $"{Environment.NewLine}Типы документа без справочника: {unmatched.Count}.";

        return new DataImportResultDto(true, imported, null, iteration.Id, details);
    }
}
