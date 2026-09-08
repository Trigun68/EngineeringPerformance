using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace EngineeringPerformance.Infrastructure.Import;

internal sealed partial class PhysicalVolumeExcelImportService
{
    /// <summary>Типичный шифр ЕП в «Список работ»: 01-01-001-01 (как в справочнике).</summary>
    private static readonly Regex CatalogEpCodePattern = new(
        @"^\d{2}-\d{2}-\d{3}-\d{2}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Тот же шифр с допуском по длине сегментов (поиск колонки в файле).</summary>
    private static readonly Regex CatalogEpCodeLoosePattern = new(
        @"^\d{1,2}-\d{1,2}-\d{1,4}-\d{1,4}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Первый шифр внутри текста ячейки (например «13-06-001-01 — Установка…»).</summary>
    private static readonly Regex CatalogEpCodeEmbeddedPattern = new(
        @"\d{1,2}-\d{1,2}-\d{1,4}-\d{1,4}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Шифр КФО2 в каталоге (см. миграции KFO2): KQ.02.160.</summary>
    private static readonly Regex CatalogKfoEpCodeLoosePattern = new(
        @"^[A-Z]{2}\.\d{2}\.\d{3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Первый шифр КФО2 внутри ячейки (код и наименование в одной колонке).</summary>
    private static readonly Regex CatalogKfoEpCodeEmbeddedPattern = new(
        @"\b[A-Z]{2}\.\d{2}\.\d{3}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

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

        return best ?? ImportHelpers.GetFirstWorksheetWithData(workbook);
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
            // В файлах «Список работ» между типом привязки и комплектом документов
            // есть колонки оборудования — относительные сдвиги e-3/e-2/e-1 ломают FBS.
            // Перекрываем по заголовкам (как у импорта оборудования).
            columnMap = ApplySpisokRabotHeaderOverrides(worksheet, headerRow, columnMap);
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
        var catalogColumns = new List<int>();
        for (var c = 1; c <= lastCol; c++)
        {
            var text = ImportHelpers.GetWorksheetCellString(worksheet, headerRow, c);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (IsSpisokCatalogHeaderText(text))
            {
                catalogColumns.Add(c);
            }
        }

        if (catalogColumns.Count == 0)
        {
            return null;
        }

        if (catalogColumns.Count == 1)
        {
            return catalogColumns[0];
        }

        // В «Список работ» v2 две колонки «ID по каталогу» (оборудование и работы).
        // Выбираем ту, где в данных больше шифров БЕР/КФО.
        var firstDataRow = headerRow + 1;
        var lastRow = Math.Min(used.LastRow().RowNumber(), firstDataRow + 8000);
        var bestCol = catalogColumns[0];
        var bestHits = -1;
        foreach (var col in catalogColumns)
        {
            var hits = 0;
            for (var r = firstDataRow; r <= lastRow; r++)
            {
                var cellText = ImportHelpers.GetWorksheetCellString(worksheet, r, col);
                if (CellTextLooksLikeCatalogWorkCode(cellText?.Trim() ?? string.Empty))
                {
                    hits++;
                }
            }

            if (hits > bestHits)
            {
                bestHits = hits;
                bestCol = col;
            }
        }

        return bestCol;
    }

    /// <summary>
    /// Ищет колонку «Кол-во по BoQ» / «Количество» в строке заголовка.
    /// Возвращает номер колонки или <c>null</c>, если не найдено.
    /// </summary>
    private static int? TryFindQuantityColumnIndex(IXLWorksheet worksheet, int headerRow)
        => TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("кол-во", StringComparison.OrdinalIgnoreCase)
                || t.Contains("BoQ", StringComparison.OrdinalIgnoreCase)
                || t.Contains("количество", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Подставляет колонки FBS / комплекта / привязки / работы по тексту шапки,
    /// чтобы не зависеть от числа колонок оборудования между ними.
    /// </summary>
    private static PhysicalVolumeColumnMap ApplySpisokRabotHeaderOverrides(
        IXLWorksheet worksheet,
        int headerRow,
        PhysicalVolumeColumnMap map)
    {
        var facility = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("FBS", StringComparison.OrdinalIgnoreCase));
        var binding = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("Тип привязки", StringComparison.OrdinalIgnoreCase));
        var documentSet = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("Комплект документов", StringComparison.OrdinalIgnoreCase));
        var workId = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("ID работы", StringComparison.OrdinalIgnoreCase));
        var workName = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("Наименование работы", StringComparison.OrdinalIgnoreCase));
        var workComment = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("Комментарий к работе", StringComparison.OrdinalIgnoreCase));
        var estimation = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Contains("Базис оценки", StringComparison.OrdinalIgnoreCase));
        // «TAG» работ (не «TAG оборудования»).
        var tag = TryFindHeaderColumnIndex(
            worksheet,
            headerRow,
            static t => t.Equals("TAG", StringComparison.OrdinalIgnoreCase)
                || (t.Contains("TAG", StringComparison.OrdinalIgnoreCase)
                    && !t.Contains("оборуд", StringComparison.OrdinalIgnoreCase)));

        return map with
        {
            FacilitySource = facility ?? map.FacilitySource,
            BindingType = binding ?? map.BindingType,
            DocumentSet = documentSet ?? map.DocumentSet,
            WorkId = workId ?? map.WorkId,
            WorkName = workName ?? map.WorkName,
            WorkComment = workComment ?? map.WorkComment,
            EstimationBasis = estimation ?? map.EstimationBasis,
            Tag = tag ?? map.Tag,
        };
    }

    private static int? TryFindHeaderColumnIndex(
        IXLWorksheet worksheet,
        int headerRow,
        Func<string, bool> match)
    {
        var used = worksheet.RangeUsed();
        if (used is null)
        {
            return null;
        }

        var lastCol = Math.Min(used.LastColumn().ColumnNumber(), 40);
        for (var c = 1; c <= lastCol; c++)
        {
            var text = ImportHelpers.GetWorksheetCellString(worksheet, headerRow, c);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (match(text.Trim()))
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
                var t = ImportHelpers.GetWorksheetCellString(ws, r, c);
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
            var t = ImportHelpers.GetWorksheetCellString(ws, r, bestCol);
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

    /// <summary>Совпадение с шифром БЕР (01-01-001-01) или КФО2 (KQ.02.160) в ячейке или внутри текста.</summary>
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
                var text = ImportHelpers.GetWorksheetCellString(worksheet, r, c);
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
        var v = ImportHelpers.NormalizeCatalogCode(cellText);
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
}

internal readonly struct PhysicalVolumeColumnMap
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

