using System.Globalization;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

public sealed partial class ProjectExcelDataImportService
{
    private Task<DataImportResultDto> ImportPhysicalVolumesAsync(int projectId, int versionId, string filePath, string description) =>
        ExecuteWithRetryableTransactionAsync(context =>
            ImportPhysicalVolumesCoreAsync(context, projectId, versionId, filePath, description));

    private async Task<DataImportResultDto> ImportPhysicalVolumesCoreAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        string filePath,
        string description)
    {
        // Режим «замена»: удаляем все предыдущие данные ФО для данной версии проекта,
        // чтобы каждый импорт был полным пересозданием без дублей и без сворачивания строк.
        await ClearPhysicalVolumeDataForVersionAsync(context, projectId, versionId);

        var iteration = new DataImportIteration
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            DataType = DataImportType.PhysicalVolume,
            Description = description,
            SourceFileName = Path.GetFileName(filePath),
            ImportedAt = DateTime.UtcNow,
        };
        context.DataImportIterations.Add(iteration);
        await context.SaveChangesAsync();

        await PhysicalVolumeCatalogBinding.EnsureCatalogTypeConfiguredAsync(context, versionId);

        var isKfo = await PhysicalVolumeCatalogBinding.IsKfoVersionAsync(context, versionId);
        var physicalVolumeRows = isKfo
            ? await context.PhysicalVolumesKfo.AsNoTracking()
                .Select(pv => new PvCatalogRow(pv.Id, pv.Code, pv.Name, pv.ClassifierId, pv.ParentId))
                .ToListAsync()
            : await context.PhysicalVolumesBer.AsNoTracking()
                .Select(pv => new PvCatalogRow(pv.Id, pv.Code, pv.Name, pv.ClassifierId, pv.ParentId))
                .ToListAsync();
        var catalogName = isKfo ? "PhysicalVolumesKfo" : "PhysicalVolumesBer";
        var physicalVolumes = BuildCodeLookup(physicalVolumeRows, catalogName);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = GetPhysicalVolumeImportWorksheet(workbook);

        if (!TryResolvePhysicalVolumeImportLayout(worksheet, out var columnMap, out var firstDataRowNumber))
        {
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            var emptySheetDetails =
                "Импортировано строк: 0." + Environment.NewLine + Environment.NewLine
                + "Выбранный лист не содержит заполненного диапазона (нет данных для чтения).";
            return new DataImportResultDto(true, 0, null, iteration.Id, emptySheetDetails);
        }

        var rows = EnumeratePhysicalVolumeDataRows(worksheet, firstDataRowNumber).ToList();
        if (rows.Count == 0)
        {
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            var noDataRowsDetails =
                "Импортировано строк: 0." + Environment.NewLine + Environment.NewLine
                + "После строки заголовка не найдено ни одной строки с данными. Проверьте, что открыт нужный лист «Список работ» и что в файле есть шапка с текстом вроде «ID по каталогу» / «Код по каталогу».";
            return new DataImportResultDto(true, 0, null, iteration.Id, noDataRowsDetails);
        }

        var importTimestamp = iteration.ImportedAt;
        // ISS-007: помимо счётчиков сохраняем номера строк (и Quantity, и шифр-кандидат),
        // чтобы вывести их в details. Это позволит пользователю быстро найти проблемные
        // строки в файле и дозаполнить шифр ЕП.
        var skippedNoEpCodeRows = new List<(int RowNumber, decimal Quantity)>();
        var skippedNotInCatalogRows = new List<(int RowNumber, string Code, decimal Quantity)>();
        // Каждая строка файла — отдельная запись в БД, без сворачивания.
        var newEntities = new List<ProjectPhysicalVolume>(rows.Count);

        foreach (var row in rows)
        {
            var candidates = EnumerateCandidateEpCodesFromRow(row, columnMap, GetCellValue).ToList();
            if (candidates.Count == 0)
            {
                skippedNoEpCodeRows.Add((row.RowNumber(), ParseDecimal(GetCellValue(row, columnMap.Quantity))));
                continue;
            }

            PvCatalogRow? catalogRow = null;
            foreach (var candidate in candidates)
            {
                if (TryResolvePhysicalVolume(physicalVolumes, candidate, out var resolved))
                {
                    catalogRow = resolved;
                    break;
                }
            }

            if (catalogRow is null)
            {
                skippedNotInCatalogRows.Add((row.RowNumber(), candidates[0], ParseDecimal(GetCellValue(row, columnMap.Quantity))));
                _logger.LogWarning(
                    "Physical volume with code '{Code}' not found in catalog, skipping",
                    candidates[0]);

                continue;
            }

            var draft = PhysicalVolumeImportDraft.FromRow(row, columnMap, ParseDecimal, GetCellValue, catalogRow.Value);
            draft.ApplyTruncation();
            newEntities.Add(draft.ToNewEntity(projectId, versionId, catalogRow.Value.Id, isKfo, importTimestamp));
        }

        if (newEntities.Count == 0)
        {
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            var catalogCount = physicalVolumes.Count;
            var zeroDetails = BuildPhysicalVolumeZeroImportDetails(
                rows.Count,
                catalogCount,
                skippedNoEpCodeRows,
                skippedNotInCatalogRows);
            return new DataImportResultDto(true, 0, null, iteration.Id, zeroDetails);
        }

        var pvById = physicalVolumeRows.ToDictionary(pv => pv.Id);
        await BackfillPhysicalVolumeClassifiersFromAncestorsAsync(
            context,
            newEntities.Select(e => e.PhysicalVolumeKfoId ?? e.PhysicalVolumeBerId).Where(id => id.HasValue).Select(id => id!.Value).Distinct(),
            pvById,
            isKfo);

        context.ProjectPhysicalVolumes.AddRange(newEntities);
        await context.SaveChangesAsync();

        var recordsImported = newEntities.Count;
        iteration.RecordCount = recordsImported;
        await context.SaveChangesAsync();
        // ISS-007: даже при успешном импорте показываем пропущенные строки,
        // чтобы пользователь видел, какие данные не попали в БД и мог дозаполнить
        // шифр в файле. На реальных проектах потеря 0.001% незаметна в общем итоге,
        // но конкретные «КИП-щиток без шифра» нужно разово обработать вручную.
        var successDetails = BuildPhysicalVolumeImportDetails(recordsImported, skippedNoEpCodeRows, skippedNotInCatalogRows);
        return new DataImportResultDto(true, recordsImported, null, iteration.Id, successDetails);
    }


    private static string BuildPhysicalVolumeZeroImportDetails(
        int dataRowCount,
        int catalogCodeCount,
        IReadOnlyList<(int RowNumber, decimal Quantity)> skippedNoEpCodeRows,
        IReadOnlyList<(int RowNumber, string Code, decimal Quantity)> skippedNotInCatalogRows)
    {
        var parts = new List<string>
        {
            "Импортировано строк: 0.",
            string.Empty,
            $"Строк данных в файле (после заголовка): {dataRowCount}.",
            $"Уникальных кодов в справочнике PhysicalVolumes: {catalogCodeCount}.",
            $"Строк без распознанного шифра ЕП в ожидаемых колонках: {skippedNoEpCodeRows.Count}.",
            $"Строк, где шифр не найден в справочнике: {skippedNotInCatalogRows.Count}.",
        };

        AppendSkippedRowDetails(parts, skippedNoEpCodeRows, skippedNotInCatalogRows);

        parts.Add(string.Empty);
        parts.Add("Каждая строка файла сопоставляется с записью каталога по полю Code (шифр работы: БЭР вида 01-01-001-01 или КФО2 вида KQ.02.160).");
        parts.Add("Если много строк «без распознанного шифра», проверьте, что в ожидаемых колонках есть именно такие коды (а не только наименование), и что открыт лист «Список работ» с колонкой «Код по каталогу» / «Шифр по каталогу».");
        parts.Add("Если в базе только демонстрационные коды (например FO-001), а в файле — полные шифры БЭР или КФО2, совпадений не будет, пока в каталог не загружен полный перечень работ.");
        parts.Add("Проверьте справочник «Физические объёмы» в приложении или выполните миграцию с полными данными каталога.");

        return string.Join(Environment.NewLine, parts);
    }

    /// <summary>
    /// ISS-007: details для успешного импорта PV. Если пропущенных строк нет — возвращает null
    /// (тогда UI не показывает дополнительное окно деталей). Если есть — выводит сводку
    /// и список номеров строк с Quantity, чтобы пользователь нашёл их в файле и дозаполнил шифр.
    /// </summary>
    private static string? BuildPhysicalVolumeImportDetails(
        int recordsImported,
        IReadOnlyList<(int RowNumber, decimal Quantity)> skippedNoEpCodeRows,
        IReadOnlyList<(int RowNumber, string Code, decimal Quantity)> skippedNotInCatalogRows)
    {
        if (skippedNoEpCodeRows.Count == 0 && skippedNotInCatalogRows.Count == 0)
        {
            return null;
        }

        var parts = new List<string>
        {
            $"Импортировано строк: {recordsImported}.",
            string.Empty,
            $"Пропущено строк без распознанного шифра ЕП: {skippedNoEpCodeRows.Count} (Σ Кол-во = {FormatTotalQuantity(skippedNoEpCodeRows.Sum(x => x.Quantity))}).",
            $"Пропущено строк с шифром, не найденным в справочнике: {skippedNotInCatalogRows.Count} (Σ Кол-во = {FormatTotalQuantity(skippedNotInCatalogRows.Sum(x => x.Quantity))}).",
        };

        AppendSkippedRowDetails(parts, skippedNoEpCodeRows, skippedNotInCatalogRows);

        parts.Add(string.Empty);
        parts.Add("Проверьте перечисленные строки в файле «Список работ»: в колонке «ID по каталогу» / «Шифр работ» должен быть код БЭР (01-01-001-01) или КФО2 (KQ.02.160). После заполнения шифров повторите импорт.");

        return string.Join(Environment.NewLine, parts);
    }

    private static void AppendSkippedRowDetails(
        List<string> parts,
        IReadOnlyList<(int RowNumber, decimal Quantity)> skippedNoEpCodeRows,
        IReadOnlyList<(int RowNumber, string Code, decimal Quantity)> skippedNotInCatalogRows)
    {
        // Лимит вывода списка: первые 50 строк каждого типа. На реальных проектах больше — это
        // патология файла; даже 50 хватает, чтобы заметить системную проблему. Полный список
        // остаётся в логах (LogWarning per row для not-in-catalog).
        const int maxRowsToList = 50;

        if (skippedNoEpCodeRows.Count > 0)
        {
            parts.Add(string.Empty);
            parts.Add("Строки без шифра ЕП (номер строки Excel, Кол-во по BoQ):");
            foreach (var (rowNumber, quantity) in skippedNoEpCodeRows.Take(maxRowsToList))
            {
                parts.Add($"  • строка {rowNumber}: Кол-во = {FormatQuantity(quantity)}");
            }
            if (skippedNoEpCodeRows.Count > maxRowsToList)
            {
                parts.Add($"  … и ещё {skippedNoEpCodeRows.Count - maxRowsToList} строк (см. лог приложения).");
            }
        }

        if (skippedNotInCatalogRows.Count > 0)
        {
            parts.Add(string.Empty);
            parts.Add("Строки с шифром, не найденным в справочнике (номер строки Excel, шифр, Кол-во):");
            foreach (var (rowNumber, code, quantity) in skippedNotInCatalogRows.Take(maxRowsToList))
            {
                parts.Add($"  • строка {rowNumber}: шифр «{code}», Кол-во = {FormatQuantity(quantity)}");
            }
            if (skippedNotInCatalogRows.Count > maxRowsToList)
            {
                parts.Add($"  … и ещё {skippedNotInCatalogRows.Count - maxRowsToList} строк (см. лог приложения).");
            }
        }
    }

    private static string FormatQuantity(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string FormatTotalQuantity(decimal value) =>
        value.ToString("N4", CultureInfo.InvariantCulture);


    /// <summary>
    /// Выбирает лист: приоритет у листа с шапкой «каталог» или с колонкой кодов ЕП; иначе — «Список работ» и размер.
    /// </summary>
    private static IXLWorksheet GetPhysicalVolumeImportWorksheet(XLWorkbook workbook)
    {
        IXLWorksheet? best = null;
        long bestScore = -1;

        foreach (var ws in workbook.Worksheets)
        {
            var used = ws.RangeUsed();
            if (used is null)
            {
                continue;
            }

            var score = ScoreWorksheetForPhysicalVolumeImport(ws);
            if (score > bestScore)
            {
                bestScore = score;
                best = ws;
            }
        }

        return best ?? GetFirstWorksheetWithData(workbook);
    }

    private static long ScoreWorksheetForPhysicalVolumeImport(IXLWorksheet ws)
    {
        var used = ws.RangeUsed();
        if (used is null)
        {
            return -1;
        }

        long score = (long)used.RowCount() * used.ColumnCount();

        if (TryFindSpisokRabotHeaderRow(ws, out _))
        {
            score += 50_000_000L;
        }

        if (TryDetectEpCodeColumnByData(ws, out _, out _))
        {
            score += 25_000_000L;
        }

        var name = ws.Name;
        if (name.Contains("список", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Spisok", StringComparison.OrdinalIgnoreCase))
        {
            score += 5_000_000L;
        }

        if (name.Contains("вариант", StringComparison.OrdinalIgnoreCase))
        {
            score += 500_000L;
        }

        if (name.Contains("работ", StringComparison.OrdinalIgnoreCase))
        {
            score += 100_000L;
        }

        return score;
    }

    private static bool TryResolvePhysicalVolumeImportLayout(
        IXLWorksheet worksheet,
        out PhysicalVolumeColumnMap columnMap,
        out int firstDataRowNumber)
    {
        columnMap = default;
        firstDataRowNumber = 2;

        if (worksheet.RangeUsed() is null)
        {
            return false;
        }

        if (TryFindSpisokRabotHeaderRow(worksheet, out var headerRow))
        {
            var epCol = TryFindEpCatalogColumnIndex(worksheet, headerRow) ?? 6;
            var qtyCol = TryFindQuantityColumnIndex(worksheet, headerRow);
            columnMap = PhysicalVolumeColumnMap.FromEpCatalogColumn(epCol, qtyCol);
            firstDataRowNumber = headerRow + 1;
            return true;
        }

        if (TryDetectEpCodeColumnByData(worksheet, out var epFromData, out var firstDataRow))
        {
            columnMap = PhysicalVolumeColumnMap.FromEpCatalogColumn(epFromData);
            firstDataRowNumber = firstDataRow;
            return true;
        }

        var sheetName = worksheet.Name;
        if (sheetName.Contains("список", StringComparison.OrdinalIgnoreCase)
            || sheetName.Contains("Spisok", StringComparison.OrdinalIgnoreCase)
            || sheetName.Contains("вариант", StringComparison.OrdinalIgnoreCase))
        {
            columnMap = PhysicalVolumeColumnMap.SpisokRabot;
            firstDataRowNumber = 2;
            return true;
        }

        columnMap = PhysicalVolumeColumnMap.LegacyTemplate;
        firstDataRowNumber = 2;
        return true;
    }

    private static bool IsSpisokCatalogHeaderText(string text)
    {
        var t = text.Trim();
        if (t.Length == 0)
        {
            return false;
        }

        if (t.Contains("ID по каталогу", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (t.Contains("Код по каталогу", StringComparison.OrdinalIgnoreCase)
            || t.Contains("код по каталогу", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (t.Contains("Шифр по каталогу", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (t.Contains("по каталогу", StringComparison.OrdinalIgnoreCase) && t.Length < 48)
        {
            return true;
        }

        return false;
    }

    private static int? TryFindEpCatalogColumnIndex(IXLWorksheet worksheet, int headerRow)
    {
        var used = worksheet.RangeUsed();
        if (used is null)
        {
            return null;
        }

        var lastCol = Math.Min(used.LastColumn().ColumnNumber(), 40);
        for (var c = 1; c <= lastCol; c++)
        {
            var text = GetWorksheetCellString(worksheet, headerRow, c);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (IsSpisokCatalogHeaderText(text))
            {
                return c;
            }
        }

        return null;
    }

    /// <summary>
    /// Ищет колонку «Кол-во по BoQ» / «Количество» в строке заголовка.
    /// Возвращает номер колонки или <c>null</c>, если не найдено.
    /// </summary>
    private static int? TryFindQuantityColumnIndex(IXLWorksheet worksheet, int headerRow)
    {
        var used = worksheet.RangeUsed();
        if (used is null)
        {
            return null;
        }

        var lastCol = Math.Min(used.LastColumn().ColumnNumber(), 40);
        for (var c = 1; c <= lastCol; c++)
        {
            var text = GetWorksheetCellString(worksheet, headerRow, c);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var t = text.Trim();
            if (t.Contains("кол-во", StringComparison.OrdinalIgnoreCase)
                || t.Contains("BoQ", StringComparison.OrdinalIgnoreCase)
                || t.Contains("количество", StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }

        return null;
    }

    /// <summary>
    /// Находит колонку с шифрами ЕП по данным (если шапка нестандартная или другой лист).
    /// </summary>
    private static bool TryDetectEpCodeColumnByData(IXLWorksheet ws, out int epCol, out int firstDataRow)
    {
        epCol = 6;
        firstDataRow = 2;

        var used = ws.RangeUsed();
        if (used is null)
        {
            return false;
        }

        var rStart = used.FirstRow().RowNumber();
        var rEnd = Math.Min(used.LastRow().RowNumber(), rStart + 8000);
        var lastCol = Math.Min(used.LastColumn().ColumnNumber(), 40);

        var bestCol = -1;
        var bestHits = 0;

        for (var c = 1; c <= lastCol; c++)
        {
            var hits = 0;
            for (var r = rStart; r <= rEnd; r++)
            {
                var t = GetWorksheetCellString(ws, r, c);
                if (string.IsNullOrWhiteSpace(t))
                {
                    continue;
                }

                var x = t.Trim();
                if (CellTextLooksLikeCatalogWorkCode(x))
                {
                    hits++;
                }
            }

            if (hits > bestHits)
            {
                bestHits = hits;
                bestCol = c;
            }
        }

        if (bestHits < 2 || bestCol < 1)
        {
            return false;
        }

        epCol = bestCol;

        for (var r = rStart; r <= rEnd; r++)
        {
            var t = GetWorksheetCellString(ws, r, bestCol);
            if (string.IsNullOrWhiteSpace(t))
            {
                continue;
            }

            var x = t.Trim();
            if (CellTextLooksLikeCatalogWorkCode(x))
            {
                firstDataRow = r;
                return true;
            }
        }

        return false;
    }

    /// <summary>Совпадение с шифром БЭР (01-01-001-01) или КФО2 (KQ.02.160) в ячейке или внутри текста.</summary>
    private static bool CellTextLooksLikeCatalogWorkCode(string x)
    {
        if (CatalogEpCodeLoosePattern.IsMatch(x) || CatalogEpCodePattern.IsMatch(x))
        {
            return true;
        }

        if (CatalogKfoEpCodeLoosePattern.IsMatch(x))
        {
            return true;
        }

        if (CatalogEpCodeEmbeddedPattern.IsMatch(x) || CatalogKfoEpCodeEmbeddedPattern.IsMatch(x))
        {
            return true;
        }

        return false;
    }

    private static bool TryFindSpisokRabotHeaderRow(IXLWorksheet worksheet, out int headerRowNumber)
    {
        headerRowNumber = 1;
        var range = worksheet.RangeUsed();
        if (range is null)
        {
            return false;
        }

        var firstRow = range.FirstRow().RowNumber();
        var lastRow = Math.Min(range.LastRow().RowNumber(), firstRow + 120);
        var lastCol = Math.Min(range.LastColumn().ColumnNumber(), 40);

        for (var r = firstRow; r <= lastRow; r++)
        {
            for (var c = 1; c <= lastCol; c++)
            {
                var text = GetWorksheetCellString(worksheet, r, c);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (IsSpisokCatalogHeaderText(text))
                {
                    headerRowNumber = r;
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<IXLRangeRow> EnumeratePhysicalVolumeDataRows(IXLWorksheet worksheet, int firstDataRowNumber)
    {
        var used = worksheet.RangeUsed();
        if (used is null)
        {
            yield break;
        }

        foreach (var row in used.RowsUsed())
        {
            if (row.RowNumber() < firstDataRowNumber)
            {
                continue;
            }

            yield return row;
        }
    }

    private static bool LooksLikePhysicalVolumeHeaderLabel(string epCode)
    {
        if (epCode.Equals("ID по каталогу", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (epCode.Contains("каталогу", StringComparison.OrdinalIgnoreCase) && epCode.Length < 40)
        {
            return true;
        }

        return false;
    }

    private readonly struct PhysicalVolumeColumnMap
    {
        public int EpCode { get; init; }

        public int Quantity { get; init; }

        public int FacilitySource { get; init; }

        public int WorkName { get; init; }

        public int BindingType { get; init; }

        public int DocumentSet { get; init; }

        public int CatalogId { get; init; }

        public int Tag { get; init; }

        public int WorkComment { get; init; }

        public int EstimationBasis { get; init; }

        public int WorkId { get; init; }

        public bool SplitFacilityFromFbs { get; init; }

        public static PhysicalVolumeColumnMap LegacyTemplate => new()
        {
            EpCode = 6,
            Quantity = 8,
            FacilitySource = 9,
            WorkName = 10,
            BindingType = 11,
            DocumentSet = 12,
            CatalogId = 13,
            Tag = 14,
            WorkComment = 15,
            EstimationBasis = 16,
            WorkId = 0,
            SplitFacilityFromFbs = false,
        };

        /// <summary>
        /// «Список работ» (см. <c>_dev/excel_analysis_report.md</c>, FILE 5): A–W, кол-во в колонке Q.
        /// </summary>
        public static PhysicalVolumeColumnMap SpisokRabot => new()
        {
            EpCode = 6,
            Quantity = 17,
            FacilitySource = 3,
            WorkName = 9,
            BindingType = 4,
            DocumentSet = 5,
            CatalogId = 6,
            Tag = 7,
            WorkId = 8,
            WorkComment = 10,
            EstimationBasis = 18,
            SplitFacilityFromFbs = true,
        };

        /// <summary>
        /// Сдвиг всех колонок относительно стандартной F (6): для вариантов файла с доп. колонками.
        /// <paramref name="overrideQuantityColumn"/> позволяет передать колонку «Кол-во»,
        /// найденную по заголовку, вместо расчётного смещения <c>e + 11</c>.
        /// </summary>
        public static PhysicalVolumeColumnMap FromEpCatalogColumn(int epCol, int? overrideQuantityColumn = null)
        {
            var e = epCol < 4 ? 6 : epCol;

            return new PhysicalVolumeColumnMap
            {
                EpCode = e,
                CatalogId = e,
                FacilitySource = e - 3,
                BindingType = e - 2,
                DocumentSet = e - 1,
                Tag = e + 1,
                WorkId = e + 2,
                WorkName = e + 3,
                WorkComment = e + 4,
                Quantity = overrideQuantityColumn ?? e + 11,
                EstimationBasis = e + 12,
                SplitFacilityFromFbs = true,
            };
        }
    }

    /// <summary>
    /// Несколько строк каталога могут иметь один код. Импорт выбирает кандидата явно.
    /// </summary>
    private ILookup<string, PvCatalogRow> BuildCodeLookup(
        IReadOnlyList<PvCatalogRow> rows,
        string catalogName)
    {
        var pairs = new List<(string Key, PvCatalogRow Pv)>();
        foreach (var pv in rows)
        {
            var codeKey = NormalizeCatalogCode(pv.Code);
            if (string.IsNullOrEmpty(codeKey))
            {
                continue;
            }

            pairs.Add((codeKey, pv));
        }

        var duplicateGroups = pairs
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Count(g => g.Count() > 1);
        if (duplicateGroups > 0)
        {
            _logger.LogInformation(
                "Catalog {Catalog}: {Count} code keys map to multiple rows; import picks row with ClassifierId, then longest Code",
                catalogName,
                duplicateGroups);
        }

        return pairs.ToLookup(p => p.Key, p => p.Pv, StringComparer.OrdinalIgnoreCase);
    }

    private static PvCatalogRow? PickBestPhysicalVolumeCandidate(IReadOnlyList<PvCatalogRow> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var withClassifier = candidates.Where(c => c.ClassifierId.HasValue).ToList();
        var pool = withClassifier.Count > 0 ? withClassifier : candidates;

        return pool
            .OrderByDescending(c => c.Code.Length)
            .ThenBy(c => c.Id)
            .First();
    }

    /// <summary>
    /// Для позиций сида без «Техас» в Excel ClassifierId остаётся NULL, хотя у родительских
    /// групп в каталоге связь с ИИС уже задана — поднимаем её при импорте, чтобы аналитика
    /// и колонка «Классификатор» были заполнены.
    /// </summary>
    private async Task BackfillPhysicalVolumeClassifiersFromAncestorsAsync(
        AppDbContext context,
        IEnumerable<int> importedPhysicalVolumeIds,
        IReadOnlyDictionary<int, PvCatalogRow> pvByIdSnapshot,
        bool isKfo)
    {
        var updated = 0;
        foreach (var pvId in importedPhysicalVolumeIds.Distinct())
        {
            if (!pvByIdSnapshot.TryGetValue(pvId, out var snap) || snap.ClassifierId.HasValue)
            {
                continue;
            }

            var resolved = ResolveClassifierIdFromAncestorChain(pvId, pvByIdSnapshot);
            if (!resolved.HasValue)
            {
                continue;
            }

            if (isKfo)
            {
                var tracked = await context.PhysicalVolumesKfo.FirstOrDefaultAsync(p => p.Id == pvId);
                if (tracked is null || tracked.ClassifierId.HasValue)
                    continue;
                tracked.ClassifierId = resolved.Value;
            }
            else
            {
                var tracked = await context.PhysicalVolumesBer.FirstOrDefaultAsync(p => p.Id == pvId);
                if (tracked is null || tracked.ClassifierId.HasValue)
                    continue;
                tracked.ClassifierId = resolved.Value;
            }

            updated++;
        }

        if (updated > 0)
        {
            _logger.LogInformation(
                "Physical volume import: set ClassifierId from ancestor chain for {Count} catalog rows",
                updated);
            await context.SaveChangesAsync();
        }
    }

    private static int? ResolveClassifierIdFromAncestorChain(
        int startPvId,
        IReadOnlyDictionary<int, PvCatalogRow> byId)
    {
        var visited = new HashSet<int>();
        var currentId = (int?)startPvId;
        while (currentId.HasValue && visited.Add(currentId.Value))
        {
            if (!byId.TryGetValue(currentId.Value, out var node))
            {
                break;
            }

            if (node.ClassifierId.HasValue)
            {
                return node.ClassifierId;
            }

            currentId = node.ParentId;
        }

        return null;
    }

    private static string? NormalizeCatalogCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var t = code.Trim()
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-')
            .Replace('\u2011', '-');

        return t;
    }

    private static IEnumerable<string> EnumerateCandidateEpCodesFromRow(
        IXLRangeRow row,
        PhysicalVolumeColumnMap map,
        Func<IXLRangeRow, int, string?> getCell)
    {
        var columns = new List<int>(8)
        {
            map.EpCode,
            map.EpCode + 1,
            map.EpCode + 2,
            map.EpCode + 3,
        };

        if (map.SplitFacilityFromFbs)
        {
            columns.Add(6);
            columns.Add(8);
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in columns.Distinct().OrderBy(c => c))
        {
            if (col < 1)
            {
                continue;
            }

            foreach (var code in EnumerateCatalogCodesFromCellText(getCell(row, col)))
            {
                if (seen.Add(code))
                {
                    yield return code;
                }
            }
        }
    }

    /// <summary>Одна ячейка может содержать только шифр, шифр с наименованием или нестандартные пробелы.</summary>
    private static IEnumerable<string> EnumerateCatalogCodesFromCellText(string? cellText)
    {
        var v = NormalizeCatalogCode(cellText);
        if (string.IsNullOrWhiteSpace(v) || LooksLikePhysicalVolumeHeaderLabel(v))
        {
            yield break;
        }

        var t = v.Trim();
        if (t.Length > 120)
        {
            yield break;
        }

        if (CatalogEpCodeLoosePattern.IsMatch(t))
        {
            yield return t;
            yield break;
        }

        if (CatalogKfoEpCodeLoosePattern.IsMatch(t))
        {
            yield return t;
            yield break;
        }

        var m = CatalogEpCodeEmbeddedPattern.Match(t);
        if (m.Success)
        {
            yield return m.Value;
            yield break;
        }

        var km = CatalogKfoEpCodeEmbeddedPattern.Match(t);
        if (km.Success)
        {
            yield return km.Value;
        }
    }

    private static bool TryResolvePhysicalVolume(
        ILookup<string, PvCatalogRow> physicalVolumes,
        string raw,
        out PvCatalogRow physicalVolume)
    {
        foreach (var key in ExpandEpCodeLookupKeys(raw))
        {
            if (!physicalVolumes.Contains(key))
            {
                continue;
            }

            var list = physicalVolumes[key].ToList();
            var picked = PickBestPhysicalVolumeCandidate(list);
            if (picked is not null)
            {
                physicalVolume = picked.Value;
                return true;
            }
        }

        physicalVolume = default;
        return false;
    }

    private static IEnumerable<string> ExpandEpCodeLookupKeys(string raw)
    {
        var n = NormalizeCatalogCode(raw);
        if (string.IsNullOrEmpty(n))
        {
            yield break;
        }

        yield return n;

        var parts = n.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 4 && parts.All(static p => p.Length > 0 && p.All(char.IsDigit)))
        {
            yield return $"{parts[0].PadLeft(2, '0')}-{parts[1].PadLeft(2, '0')}-{parts[2].PadLeft(3, '0')}-{parts[3].PadLeft(2, '0')}";
        }
    }

    /// <summary>
    /// Нормализует строковое поле для использования в качестве части ключа словаря:
    /// обрезает до <paramref name="maxLength"/> символов и заменяет <c>null</c> пустой строкой.
    /// Совпадает с тем, что сохранится в БД после <c>ApplyTruncation()</c>.
    /// </summary>
    private static string TruncateKey(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }


    private static async Task ClearPhysicalVolumeDataForVersionAsync(AppDbContext context, int projectId, int versionId)
    {
        var existingVolumes = await context.ProjectPhysicalVolumes
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        if (existingVolumes.Count > 0)
        {
            context.ProjectPhysicalVolumes.RemoveRange(existingVolumes);
        }

        var existingIterations = await context.DataImportIterations
            .Where(i => i.ProjectId == projectId
                     && i.ProjectAnalysisVersionId == versionId
                     && i.DataType == DataImportType.PhysicalVolume)
            .ToListAsync();

        if (existingIterations.Count > 0)
        {
            context.DataImportIterations.RemoveRange(existingIterations);
        }

        if (existingVolumes.Count > 0 || existingIterations.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task<int> DeletePhysicalVolumeRecordsAsync(AppDbContext context, int projectId, int versionId, DateTime importedAt)
    {
        var records = await context.ProjectPhysicalVolumes
            .Where(pv => pv.ProjectId == projectId
                      && pv.ProjectAnalysisVersionId == versionId
                      && pv.CreatedAt == importedAt)
            .ToListAsync();

        if (records.Count > 0)
        {
            context.ProjectPhysicalVolumes.RemoveRange(records);
            await context.SaveChangesAsync();
        }

        return records.Count;
    }

    /// <summary>
    /// Unique index: (ProjectAnalysisVersionId, ProjectId, PhysicalVolumeId).
    /// </summary>
    private sealed class PhysicalVolumeImportDraft
    {
        public const int FacilityCodeMax = 100;
        public const int FacilityNameMax = 500;
        public const int BindingTypeMax = 200;
        public const int DocumentSetMax = 100;
        public const int CatalogIdMax = 100;
        public const int TagMax = 100;
        public const int WorkIdMax = 200;
        public const int WorkNameMax = 1000;
        public const int WorkCommentMax = 500;
        public const int EstimationBasisMax = 500;
        public const int CatalogCodeMax = 100;
        public const int CatalogNameMax = 1000;

        public decimal Quantity { get; set; }

        public string? FacilityCode { get; set; }

        public string? FacilityName { get; set; }

        public string? BindingType { get; set; }

        public string? DocumentSet { get; set; }

        public string? CatalogId { get; set; }

        public string? Tag { get; set; }

        public string? WorkId { get; set; }

        public string? WorkName { get; set; }

        public string? WorkComment { get; set; }

        public string? EstimationBasis { get; set; }

        public string? CatalogCode { get; set; }

        public string? CatalogName { get; set; }

        public static PhysicalVolumeImportDraft FromRow(
            IXLRangeRow row,
            PhysicalVolumeColumnMap map,
            Func<string?, decimal> parseDecimal,
            Func<IXLRangeRow, int, string?> getCell,
            PvCatalogRow catalogEntry)
        {
            var fbsSource = getCell(row, map.FacilitySource)?.Trim();
            var rawWorkName = getCell(row, map.WorkName)?.Trim();

            string? facilityCode;
            string? facilityName;

            if (map.SplitFacilityFromFbs)
            {
                var split = SplitFbsFacilityCodeAndName(fbsSource);
                facilityCode = split.Code;
                facilityName = split.Name;
            }
            else
            {
                facilityCode = fbsSource;
                facilityName = null;
            }

            var workId = map.WorkId > 0 ? getCell(row, map.WorkId)?.Trim() : null;

            return new PhysicalVolumeImportDraft
            {
                Quantity = parseDecimal(getCell(row, map.Quantity)),
                FacilityCode = facilityCode,
                FacilityName = facilityName,
                BindingType = getCell(row, map.BindingType)?.Trim(),
                DocumentSet = getCell(row, map.DocumentSet)?.Trim(),
                CatalogId = getCell(row, map.CatalogId)?.Trim(),
                Tag = getCell(row, map.Tag)?.Trim(),
                WorkId = workId,
                WorkName = rawWorkName,
                WorkComment = getCell(row, map.WorkComment)?.Trim(),
                EstimationBasis = getCell(row, map.EstimationBasis)?.Trim(),
                CatalogCode = catalogEntry.Code,
                CatalogName = catalogEntry.Name,
            };
        }

        private static (string? Code, string? Name) SplitFbsFacilityCodeAndName(string? fbs)
        {
            if (string.IsNullOrWhiteSpace(fbs))
            {
                return (null, null);
            }

            var idx = fbs.IndexOf(" - ", StringComparison.Ordinal);
            if (idx <= 0)
            {
                var t = fbs.Trim();
                return (Truncate(t, FacilityCodeMax), Truncate(t, FacilityNameMax));
            }

            var code = fbs[..idx].Trim();
            var name = fbs[(idx + 3)..].Trim();
            return (Truncate(code, FacilityCodeMax), Truncate(name, FacilityNameMax));
        }

        public void MergeFrom(PhysicalVolumeImportDraft other)
        {
            Quantity += other.Quantity;
            FacilityCode = PreferNonEmpty(FacilityCode, other.FacilityCode);
            FacilityName = PreferNonEmpty(FacilityName, other.FacilityName);
            BindingType = PreferNonEmpty(BindingType, other.BindingType);
            DocumentSet = PreferNonEmpty(DocumentSet, other.DocumentSet);
            CatalogId = PreferNonEmpty(CatalogId, other.CatalogId);
            Tag = PreferNonEmpty(Tag, other.Tag);
            WorkId = PreferNonEmpty(WorkId, other.WorkId);
            WorkName = PreferNonEmpty(WorkName, other.WorkName);
            WorkComment = PreferNonEmpty(WorkComment, other.WorkComment);
            EstimationBasis = PreferNonEmpty(EstimationBasis, other.EstimationBasis);
            CatalogCode = PreferNonEmpty(CatalogCode, other.CatalogCode);
            CatalogName = PreferNonEmpty(CatalogName, other.CatalogName);
        }

        public void ApplyTruncation()
        {
            FacilityCode = Truncate(FacilityCode, FacilityCodeMax);
            FacilityName = Truncate(FacilityName, FacilityNameMax);
            BindingType = Truncate(BindingType, BindingTypeMax);
            DocumentSet = Truncate(DocumentSet, DocumentSetMax);
            CatalogId = Truncate(CatalogId, CatalogIdMax);
            Tag = Truncate(Tag, TagMax);
            WorkId = Truncate(WorkId, WorkIdMax);
            WorkName = Truncate(WorkName, WorkNameMax);
            WorkComment = Truncate(WorkComment, WorkCommentMax);
            EstimationBasis = Truncate(EstimationBasis, EstimationBasisMax);
            CatalogCode = Truncate(CatalogCode, CatalogCodeMax);
            CatalogName = Truncate(CatalogName, CatalogNameMax);
        }

        public void ApplyTo(ProjectPhysicalVolume entity)
        {
            entity.Quantity = Quantity;
            entity.FacilityCode = FacilityCode;
            entity.FacilityName = FacilityName;
            entity.BindingType = BindingType;
            entity.DocumentSet = DocumentSet;
            entity.CatalogId = CatalogId;
            entity.Tag = Tag;
            entity.WorkId = WorkId;
            entity.WorkName = WorkName;
            entity.WorkComment = WorkComment;
            entity.EstimationBasis = EstimationBasis;
            entity.CatalogCode = CatalogCode;
            entity.CatalogName = CatalogName;
        }

        public ProjectPhysicalVolume ToNewEntity(int projectId, int versionId, int physicalVolumeId, bool isKfo, DateTime createdAt)
        {
            var entity = new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                CreatedAt = createdAt,
                Quantity = Quantity,
                FacilityCode = FacilityCode,
                FacilityName = FacilityName,
                BindingType = BindingType,
                DocumentSet = DocumentSet,
                CatalogId = CatalogId,
                Tag = Tag,
                WorkId = WorkId,
                WorkName = WorkName,
                WorkComment = WorkComment,
                EstimationBasis = EstimationBasis,
                CatalogCode = CatalogCode,
                CatalogName = CatalogName,
            };
            PhysicalVolumeCatalogBinding.AssignCatalogId(entity, physicalVolumeId, isKfo);
            return entity;
        }

        private static string? PreferNonEmpty(string? current, string? incoming)
        {
            return string.IsNullOrWhiteSpace(incoming) ? current : incoming;
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }
    }
}
