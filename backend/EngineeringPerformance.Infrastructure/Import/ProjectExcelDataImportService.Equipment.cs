using System.Globalization;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

public sealed partial class ProjectExcelDataImportService
{
    private Task<DataImportResultDto> ImportEquipmentAsync(int projectId, int versionId, string filePath, string description) =>
        ExecuteWithRetryableTransactionAsync(context =>
            ImportEquipmentCoreAsync(context, projectId, versionId, filePath, description));

    private async Task<DataImportResultDto> ImportEquipmentCoreAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        string filePath,
        string description)
    {
        // Паритет WPF: полная замена данных оборудования версии перед импортом.
        await ClearEquipmentDataForVersionAsync(context, projectId, versionId);

        var iteration = new DataImportIteration
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            DataType = DataImportType.Equipment,
            Description = description,
            SourceFileName = Path.GetFileName(filePath),
            ImportedAt = DateTime.UtcNow,
        };
        context.DataImportIterations.Add(iteration);
        await context.SaveChangesAsync();

        // ClosedXML: using предотвращает утечки памяти Workbook/Worksheet.
        using var workbook = new XLWorkbook(filePath);
        var worksheet = GetEquipmentImportWorksheet(workbook);

        if (!TryResolveEquipmentImportLayout(worksheet, out var columnMap, out var firstDataRowNumber))
        {
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            return new DataImportResultDto(
                true,
                0,
                null,
                iteration.Id,
                "Импортировано строк: 0." + Environment.NewLine + Environment.NewLine
                + "Выбранный лист не содержит шапки «Наименование по каталогу» или данных для чтения.");
        }

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? firstDataRowNumber - 1;
        // Все проектные строки (для доноров) + lookup текущего проекта.
        var catalogRows = await context.Equipments.AsNoTracking()
            .Where(e => e.ProjectId != null)
            .ToListAsync();
        var units = await context.UnitsOfMeasurement.AsNoTracking().ToListAsync();
        var (catalogLookup, ambiguousKeys, duplicateKeyCount) =
            EquipmentImportCatalogMatcher.BuildCatalogLookup(catalogRows, projectId, _logger);
        if (duplicateKeyCount > 0)
        {
            _logger.LogWarning(
                "Equipment import: project {ProjectId} catalog contains {Count} ambiguous keys (Name + UnitId); affected rows will be skipped",
                projectId,
                duplicateKeyCount);
        }

        var pendingCatalogCreates = new List<Equipment>();
        var autoCreatedCount = 0;
        var copiedFromOtherProjectCount = 0;
        var skippedDuplicateCount = 0;
        var skippedDuplicateRows = new List<(int RowNumber, string Name, string Unit)>();
        var importTimestamp = iteration.ImportedAt;
        var newEntities = new List<ProjectEquipment>();
        var dictionaryValidator = new EquipmentImportDictionaryValidator(_dictionaryService);
        var dictionaryColumnFlags = new EquipmentImportDictionaryValidator.EquipmentDictionaryColumnFlags(
            columnMap.HasUnitIdColumn,
            columnMap.HasDirectionIdColumn,
            columnMap.HasNormTypeColumn);
        var dictionaryErrors = new List<ImportRowErrorDto>();

        for (var rowNumber = firstDataRowNumber; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (!EquipmentImportRowHasData(row, columnMap))
                continue;

            var catalogName = GetWorksheetCellString(worksheet, rowNumber, columnMap.CatalogName)?.Trim();
            if (string.IsNullOrWhiteSpace(catalogName))
                continue;

            var dictionaryRefs = ParseEquipmentDictionaryRefs(row, columnMap);
            var validationError = await dictionaryValidator.ValidateRowAsync(
                rowNumber,
                dictionaryRefs,
                dictionaryColumnFlags);
            if (validationError is not null)
            {
                dictionaryErrors.Add(validationError);
                continue;
            }

            var rawUnitName = row.Cell(columnMap.UnitDimension).GetString()?.Trim() ?? string.Empty;
            var unitId = dictionaryRefs.UnitId
                         ?? EquipmentUnitNameMatcher.ResolveUnitId(rawUnitName, units);

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
                skippedDuplicateCount++;
                skippedDuplicateRows.Add((rowNumber, catalogName, rawUnitName));
                _logger.LogWarning(
                    "Equipment import row {Row}: skipped duplicate catalog key Name='{Name}', Unit='{Unit}' (UnitId={UnitId})",
                    rowNumber,
                    catalogName,
                    rawUnitName,
                    unitId);
                continue;
            }

            if (resolveResult.Created)
            {
                if (resolveResult.CopiedFromOtherProject)
                    copiedFromOtherProjectCount++;
                else
                    autoCreatedCount++;
            }

            // DirectionId из Excel перекрывает только stub; у копии с донора уже есть ClassifierId.
            if (dictionaryRefs.DirectionId is int directionId
                && (!resolveResult.CopiedFromOtherProject || resolveResult.Equipment.ClassifierId is null))
            {
                resolveResult.Equipment.ClassifierId = directionId;
            }

            newEntities.Add(BuildProjectEquipmentFromImportRow(
                row, columnMap, projectId, versionId, resolveResult.Equipment, importTimestamp));
        }

        if (newEntities.Count == 0)
        {
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            var emptyDetails = EquipmentImportCatalogMatcher.BuildImportSummary(
                0,
                autoCreatedCount,
                skippedDuplicateCount,
                skippedDuplicateRows,
                copiedFromOtherProjectCount);
            return new DataImportResultDto(
                true,
                0,
                null,
                iteration.Id,
                emptyDetails,
                dictionaryErrors);
        }

        if (pendingCatalogCreates.Count > 0)
            context.Equipments.AddRange(pendingCatalogCreates);

        context.ProjectEquipments.AddRange(newEntities);
        iteration.RecordCount = newEntities.Count;
        await context.SaveChangesAsync();

        var details = EquipmentImportCatalogMatcher.BuildImportSummary(
            newEntities.Count,
            autoCreatedCount,
            skippedDuplicateCount,
            skippedDuplicateRows,
            copiedFromOtherProjectCount);

        var rowErrors = dictionaryErrors
            .Concat(skippedDuplicateRows.Select(s => new ImportRowErrorDto(
                s.RowNumber,
                $"Duplicate catalog key: {s.Name} ({s.Unit})")))
            .ToList();

        return new DataImportResultDto(
            true,
            newEntities.Count,
            null,
            iteration.Id,
            details,
            rowErrors.Count > 0 ? rowErrors : null);
    }

    private static bool EquipmentImportRowHasData(IXLRow row, EquipmentColumnMap map)
    {
        return !string.IsNullOrWhiteSpace(row.Cell(map.CatalogName).GetString())
               || !string.IsNullOrWhiteSpace(row.Cell(map.Quantity).GetString());
    }

    private static ProjectEquipment BuildProjectEquipmentFromImportRow(
        IXLRow row,
        EquipmentColumnMap map,
        int projectId,
        int versionId,
        Equipment equipment,
        DateTime importTimestamp)
    {
        var fbsSource = row.Cell(map.FacilitySource).GetString()?.Trim();
        var (facilityCode, facilityName) = SplitEquipmentFbs(fbsSource);

        var entity = new ProjectEquipment
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            CreatedAt = importTimestamp,
            Quantity = ParseDecimal(row.Cell(map.Quantity).GetString()),
            QuantityInWork = ParseNullableDecimal(row.Cell(map.QuantityInWork).GetString()),
            PositionNumber = TruncateEquipmentField(row.Cell(map.PositionNumber).GetString(), 50),
            FacilityCode = facilityCode,
            FacilityName = facilityName,
            DocumentSet = TruncateEquipmentField(row.Cell(map.DocumentSet).GetString(), 100),
            DiagramNumber = TrimEquipmentField(row.Cell(map.DiagramNumber).GetString()),
            ShortName = TrimEquipmentField(row.Cell(map.ShortName).GetString()),
            CatalogName = TrimEquipmentField(row.Cell(map.CatalogName).GetString()),
            TypeMark = TrimEquipmentField(row.Cell(map.TypeMark).GetString()),
            CatalogId = TruncateEquipmentField(row.Cell(map.CatalogId).GetString(), 100),
            Tag = TruncateEquipmentField(row.Cell(map.Tag).GetString(), 100),
            FullEquipmentId = TruncateEquipmentField(row.Cell(map.FullEquipmentId).GetString(), 100),
            SupplyStandard = TrimEquipmentField(row.Cell(map.SupplyStandard).GetString()),
            OlReference = TrimEquipmentField(row.Cell(map.OlReference).GetString()),
            EstimationBasis = TrimEquipmentField(row.Cell(map.EstimationBasis).GetString()),
        };

        // Существующая номенклатура из справочника — только FK; иначе EF пытается INSERT с явным Id.
        if (equipment.Id > 0)
            entity.EquipmentId = equipment.Id;
        else
            entity.Equipment = equipment;

        return entity;
    }

    private static (string? Code, string? Name) SplitEquipmentFbs(string? fbs)
    {
        if (string.IsNullOrWhiteSpace(fbs))
            return (null, null);

        var idx = fbs.IndexOf(" - ", StringComparison.Ordinal);
        if (idx <= 0)
        {
            var t = fbs.Trim();
            return (TruncateEquipmentField(t, 100), TrimEquipmentField(t));
        }

        return (
            TruncateEquipmentField(fbs[..idx].Trim(), 100),
            TrimEquipmentField(fbs[(idx + 3)..].Trim()));
    }

    private static string? TrimEquipmentField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static string? TruncateEquipmentField(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return ParseDecimal(value);
    }


    private static IXLWorksheet GetEquipmentImportWorksheet(XLWorkbook workbook)
    {
        IXLWorksheet? best = null;
        long bestScore = -1;

        foreach (var ws in workbook.Worksheets)
        {
            if (ws.RangeUsed() is null)
                continue;

            var score = ScoreWorksheetForEquipmentImport(ws);
            if (score > bestScore)
            {
                bestScore = score;
                best = ws;
            }
        }

        return best ?? GetFirstWorksheetWithData(workbook);
    }

    private static long ScoreWorksheetForEquipmentImport(IXLWorksheet ws)
    {
        var used = ws.RangeUsed();
        if (used is null)
            return -1;

        long score = (long)used.RowCount() * used.ColumnCount();

        if (TryFindEquipmentHeaderRow(ws, out _))
            score += 50_000_000L;

        var name = ws.Name;
        if (name.Contains("оборуд", StringComparison.OrdinalIgnoreCase))
            score += 5_000_000L;

        return score;
    }

    private static bool TryResolveEquipmentImportLayout(
        IXLWorksheet worksheet,
        out EquipmentColumnMap columnMap,
        out int firstDataRowNumber)
    {
        columnMap = default;
        firstDataRowNumber = 4;

        if (worksheet.RangeUsed() is null)
            return false;

        if (!TryFindEquipmentHeaderRow(worksheet, out var headerRow))
            return false;

        if (!TryBuildEquipmentColumnMap(worksheet, headerRow, out columnMap))
            return false;

        firstDataRowNumber = headerRow + 1;
        return true;
    }

    private static bool TryFindEquipmentHeaderRow(IXLWorksheet worksheet, out int headerRow)
    {
        headerRow = 0;
        var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 0, 15);

        for (var row = 1; row <= lastRow; row++)
        {
            var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (var col = 1; col <= lastCol; col++)
            {
                var text = GetWorksheetCellString(worksheet, row, col);
                if (text is not null
                    && text.Contains("Наименование по каталогу", StringComparison.OrdinalIgnoreCase))
                {
                    headerRow = row;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryBuildEquipmentColumnMap(IXLWorksheet worksheet, int headerRow, out EquipmentColumnMap map)
    {
        map = default;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var col = 1; col <= lastCol; col++)
        {
            var text = GetWorksheetCellString(worksheet, headerRow, col)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            columns.TryAdd(text, col);
        }

        if (!columns.TryGetValue("Наименование по каталогу", out var catalogNameCol))
            return false;

        map = new EquipmentColumnMap
        {
            RowId = FindEquipmentColumn(columns, "Id", -1),
            PositionNumber = FindEquipmentColumn(columns, "№ позиции в основном списке", 1),
            FacilitySource = FindEquipmentColumn(columns, "FBS (Code and Title of Facility)", 3),
            DocumentSet = FindEquipmentColumn(columns, "Комплект документов", 4),
            DiagramNumber = FindEquipmentColumn(
                columns,
                "Диаграмма No / PFD No / P&ID No/ Datasheet No",
                5),
            ShortName = FindEquipmentColumn(columns, "Наименование краткое", 6),
            CatalogName = catalogNameCol,
            TypeMark = FindEquipmentColumn(columns, "Тип/Марка оборудования", 8),
            CatalogId = FindEquipmentColumn(columns, "ID по каталогу", 9),
            Tag = FindEquipmentColumn(columns, "TAG", 10),
            FullEquipmentId = FindEquipmentColumn(columns, "ID обор. полный", 11),
            SupplyStandard = FindEquipmentColumn(columns, "Стандарт на поставку", 12),
            OlReference = FindEquipmentColumn(columns, "ОЛ", 13),
            Quantity = FindEquipmentColumn(columns, "Всего, (ед)", 14),
            QuantityInWork = FindEquipmentColumn(columns, "В работе, (ед)", 15),
            EstimationBasis = FindEquipmentColumn(columns, "Базис оценки", 16),
            UnitDimension = FindEquipmentColumn(columns, "Размерность", 18),
            UnitId = FindEquipmentColumn(columns, "UnitId", -1),
            DirectionId = FindEquipmentColumn(columns, "DirectionId", -1),
            NormType = FindEquipmentColumn(columns, "NormType", -1),
        };

        return true;
    }

    private static int FindEquipmentColumn(IReadOnlyDictionary<string, int> columns, string header, int fallback)
    {
        return columns.TryGetValue(header, out var col) ? col : fallback;
    }

    private static EquipmentImportDictionaryValidator.EquipmentDictionaryRefs ParseEquipmentDictionaryRefs(
        IXLRow row,
        EquipmentColumnMap map)
    {
        return new EquipmentImportDictionaryValidator.EquipmentDictionaryRefs(
            TryParseOptionalEquipmentInt(row, map.UnitId),
            TryParseOptionalEquipmentInt(row, map.DirectionId),
            map.NormType > 0 ? row.Cell(map.NormType).GetString()?.Trim() : null);
    }

    private static int? TryParseOptionalEquipmentInt(IXLRow row, int columnIndex)
    {
        if (columnIndex <= 0)
            return null;

        var text = row.Cell(columnIndex).GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : null;
    }

    private readonly struct EquipmentColumnMap
    {
        public int RowId { get; init; }

        public int PositionNumber { get; init; }
        public int FacilitySource { get; init; }
        public int DocumentSet { get; init; }
        public int DiagramNumber { get; init; }
        public int ShortName { get; init; }
        public int CatalogName { get; init; }
        public int TypeMark { get; init; }
        public int CatalogId { get; init; }
        public int Tag { get; init; }
        public int FullEquipmentId { get; init; }
        public int SupplyStandard { get; init; }
        public int OlReference { get; init; }
        public int Quantity { get; init; }
        public int QuantityInWork { get; init; }
        public int EstimationBasis { get; init; }
        public int UnitDimension { get; init; }
        public int UnitId { get; init; }
        public int DirectionId { get; init; }
        public int NormType { get; init; }

        public bool HasUnitIdColumn => UnitId > 0;
        public bool HasDirectionIdColumn => DirectionId > 0;
        public bool HasNormTypeColumn => NormType > 0;
    }

    private static async Task ClearEquipmentDataForVersionAsync(AppDbContext context, int projectId, int versionId)
    {
        var existing = await context.ProjectEquipments
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        if (existing.Count > 0)
        {
            context.ProjectEquipments.RemoveRange(existing);
        }

        var existingIterations = await context.DataImportIterations
            .Where(i => i.ProjectId == projectId
                     && i.ProjectAnalysisVersionId == versionId
                     && i.DataType == DataImportType.Equipment)
            .ToListAsync();

        if (existingIterations.Count > 0)
        {
            context.DataImportIterations.RemoveRange(existingIterations);
        }

        if (existing.Count > 0 || existingIterations.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task<int> DeleteEquipmentRecordsAsync(AppDbContext context, int projectId, int versionId, DateTime importedAt)
    {
        var records = await context.ProjectEquipments
            .Where(e => e.ProjectId == projectId
                     && e.ProjectAnalysisVersionId == versionId
                     && e.CreatedAt == importedAt)
            .ToListAsync();

        if (records.Count > 0)
        {
            context.ProjectEquipments.RemoveRange(records);
            await context.SaveChangesAsync();
        }

        return records.Count;
    }
}
