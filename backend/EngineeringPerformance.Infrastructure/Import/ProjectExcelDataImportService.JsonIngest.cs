using System.Security.Cryptography;
using System.Text;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Projects;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Import;

public sealed partial class ProjectExcelDataImportService
{
    public async Task<DataImportResultDto> IngestPhysicalVolumesAsync(
        int projectId,
        int versionId,
        PhysicalVolumeIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanImportAsync(projectId, cancellationToken);
        if (!TryParseIngestMode(request.Mode, out var isReplace))
            return new DataImportResultDto(false, 0, "Invalid mode. Use replace or upsert.", 0);

        var items = request.Items ?? [];
        if (isReplace && items.Count == 0 && !request.AllowEmptyReplace)
        {
            return new DataImportResultDto(
                false,
                0,
                "Replace with empty items is not allowed. Pass allowEmptyReplace=true to clear the version.",
                0);
        }

        var source = CreateJsonIngestSource(request.Description, items.Count);
        var result = await ExecuteWithRetryableTransactionAsync(
            context => IngestPhysicalVolumesCoreAsync(
                context, projectId, versionId, items, isReplace, request.Description ?? "json-ingest", source),
            cancellationToken);

        if (result.Success)
            await _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, cancellationToken);

        return result;
    }

    public async Task<DataImportResultDto> IngestEquipmentAsync(
        int projectId,
        int versionId,
        EquipmentIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanImportAsync(projectId, cancellationToken);
        if (!TryParseIngestMode(request.Mode, out var isReplace))
            return new DataImportResultDto(false, 0, "Invalid mode. Use replace or upsert.", 0);

        var items = request.Items ?? [];
        if (isReplace && items.Count == 0 && !request.AllowEmptyReplace)
        {
            return new DataImportResultDto(
                false,
                0,
                "Replace with empty items is not allowed. Pass allowEmptyReplace=true to clear the version.",
                0);
        }

        var source = CreateJsonIngestSource(request.Description, items.Count);
        var result = await ExecuteWithRetryableTransactionAsync(
            context => IngestEquipmentCoreAsync(
                context, projectId, versionId, items, isReplace, request.Description ?? "json-ingest", source),
            cancellationToken);

        if (result.Success)
            await _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, cancellationToken);

        return result;
    }

    public async Task<DataImportResultDto> IngestLaborIntensitiesAsync(
        int projectId,
        int versionId,
        LaborIntensityIngestRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanImportAsync(projectId, cancellationToken);
        if (!TryParseIngestMode(request.Mode, out var isReplace))
            return new DataImportResultDto(false, 0, "Invalid mode. Use replace or upsert.", 0);

        var items = request.Items ?? [];
        if (isReplace && items.Count == 0 && !request.AllowEmptyReplace)
        {
            return new DataImportResultDto(
                false,
                0,
                "Replace with empty items is not allowed. Pass allowEmptyReplace=true to clear the version.",
                0);
        }

        var source = CreateJsonIngestSource(request.Description, items.Count);
        var result = await ExecuteWithRetryableTransactionAsync(
            context => IngestLaborIntensitiesCoreAsync(
                context,
                projectId,
                versionId,
                items,
                isReplace,
                request.AddMissingLaborToCatalog,
                request.Description ?? "json-ingest",
                source),
            cancellationToken);

        if (result.Success)
            await _analyticsCache.InvalidateProjectAsync(projectId, cancellationToken);

        return result;
    }

    public async Task<LaborImportPreviewDto> PreviewLaborIngestAsync(
        IReadOnlyList<string> documentTypeNames,
        CancellationToken cancellationToken = default)
    {
        var names = (documentTypeNames ?? [])
            .Select(x => x?.Trim() ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToList();

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var li in await context.LaborIntensities.AsNoTracking().ToListAsync(cancellationToken))
        {
            var key = (li.DocumentTypeName ?? string.Empty).Trim();
            if (key.Length > 0)
                known.Add(key);
        }

        var unmatched = names
            .Where(n => !known.Contains(n))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return new LaborImportPreviewDto(unmatched, names.Count);
    }

    private async Task EnsureCanImportAsync(int projectId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (!await _permissionService.CanEditAsync(userId, PermissionType.ProjectDataUpload, cancellationToken))
            throw new UnauthorizedAccessException("User cannot upload project data.");
        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Edit, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot edit project {projectId}.");
    }

    private ImportSourceMetadata CreateJsonIngestSource(string? description, int itemCount)
    {
        var userId = _currentUser.UserId;
        var payload = $"{description ?? "json-ingest"}|{itemCount}|{DateTime.UtcNow:O}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return new ImportSourceMetadata("json-ingest.json", hash, userId);
    }

    private static bool TryParseIngestMode(string? mode, out bool isReplace)
    {
        if (string.IsNullOrWhiteSpace(mode) || mode.Equals("replace", StringComparison.OrdinalIgnoreCase))
        {
            isReplace = true;
            return true;
        }

        if (mode.Equals("upsert", StringComparison.OrdinalIgnoreCase))
        {
            isReplace = false;
            return true;
        }

        isReplace = true;
        return false;
    }

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
            await ClearPhysicalVolumeDataForVersionAsync(context, projectId, versionId);

        var iteration = await CreateIterationAsync(
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
        var physicalVolumes = BuildCodeLookup(physicalVolumeRows, catalogName);
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

        await BackfillPhysicalVolumeClassifiersFromAncestorsAsync(
            context,
            newEntities.Select(e => e.PhysicalVolumeKfoId ?? e.PhysicalVolumeBerId).Where(id => id.HasValue).Select(id => id!.Value),
            pvById,
            isKfo);

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

        if (!string.IsNullOrWhiteSpace(item.EpCode) && TryResolvePhysicalVolume(physicalVolumes, item.EpCode, out catalogRow))
            return true;

        catalogRow = default;
        return false;
    }

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
            await ClearEquipmentDataForVersionAsync(context, projectId, versionId);

        var iteration = await CreateIterationAsync(
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
            await ClearLaborIntensityDataForVersionAsync(context, projectId, versionId);

        var iteration = await CreateIterationAsync(
            context, projectId, versionId, DataImportType.LaborIntensity, description, source);

        var laborCatalogRows = await context.LaborIntensities.ToListAsync();
        var laborByDocumentType = new Dictionary<string, LaborIntensity>(StringComparer.OrdinalIgnoreCase);
        foreach (var li in laborCatalogRows)
        {
            var key = (li.DocumentTypeName ?? string.Empty).Trim();
            if (key.Length == 0)
                continue;
            if (!laborByDocumentType.ContainsKey(key) || (laborByDocumentType[key].IsArchived && !li.IsArchived))
                laborByDocumentType[key] = li;
        }

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
