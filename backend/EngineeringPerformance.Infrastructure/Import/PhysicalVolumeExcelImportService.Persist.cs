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

internal sealed partial class PhysicalVolumeExcelImportService
{
    public Task<DataImportResultDto> ImportPhysicalVolumesAsync(
        int projectId,
        int versionId,
        string filePath,
        string description,
        ImportSourceMetadata source) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(
            _contextFactory,
            context => ImportPhysicalVolumesCoreAsync(context, projectId, versionId, filePath, description, source));

    private async Task<DataImportResultDto> ImportPhysicalVolumesCoreAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        string filePath,
        string description,
        ImportSourceMetadata source)
    {
        // Режим «замена»: удаляем все предыдущие данные ФО для данной версии проекта,
        // чтобы каждый импорт был полным пересозданием без дублей и без сворачивания строк.
        await ImportHelpers.ClearPhysicalVolumeDataForVersionAsync(context, projectId, versionId);

        var iteration = await ImportIterationRecorder.CreateIterationAsync(
            context,
            projectId,
            versionId,
            DataImportType.PhysicalVolume,
            description,
            source);

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
            var candidates = EnumerateCandidateEpCodesFromRow(row, columnMap, ImportHelpers.GetCellValue).ToList();
            if (candidates.Count == 0)
            {
                skippedNoEpCodeRows.Add((row.RowNumber(), ImportHelpers.ParseDecimal(ImportHelpers.GetCellValue(row, columnMap.Quantity))));
                continue;
            }

            PvCatalogRow? catalogRow = null;
            foreach (var candidate in candidates)
            {
                if (ImportHelpers.TryResolvePhysicalVolume(physicalVolumes, candidate, out var resolved))
                {
                    catalogRow = resolved;
                    break;
                }
            }

            if (catalogRow is null)
            {
                skippedNotInCatalogRows.Add((row.RowNumber(), candidates[0], ImportHelpers.ParseDecimal(ImportHelpers.GetCellValue(row, columnMap.Quantity))));
                _logger.LogWarning(
                    "Physical volume with code '{Code}' not found in catalog, skipping",
                    candidates[0]);

                continue;
            }

            var draft = PhysicalVolumeImportDraft.FromRow(row, columnMap, ImportHelpers.ParseDecimal, ImportHelpers.GetCellValue, catalogRow.Value);
            draft.ApplyTruncation();
            newEntities.Add(draft.ToNewEntity(projectId, versionId, catalogRow.Value.Id, isKfo, importTimestamp, catalogRow.Value));
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
        await ImportHelpers.BackfillPhysicalVolumeClassifiersFromAncestorsAsync(
            context,
            newEntities.Select(e => e.PhysicalVolumeKfoId ?? e.PhysicalVolumeBerId).Where(id => id.HasValue).Select(id => id!.Value).Distinct(),
            pvById,
            isKfo,
            _logger);

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
        parts.Add("Каждая строка файла сопоставляется с записью каталога по полю Code (шифр работы: БЕР вида 01-01-001-01 или КФО2 вида KQ.02.160).");
        parts.Add("Если много строк «без распознанного шифра», проверьте, что в ожидаемых колонках есть именно такие коды (а не только наименование), и что открыт лист «Список работ» с колонкой «Код по каталогу» / «Шифр по каталогу».");
        parts.Add("Если в базе только демонстрационные коды (например FO-001), а в файле — полные шифры БЕР или КФО2, совпадений не будет, пока в каталог не загружен полный перечень работ.");
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
        parts.Add("Проверьте перечисленные строки в файле «Список работ»: в колонке «ID по каталогу» / «Шифр работ» должен быть код БЕР (01-01-001-01) или КФО2 (KQ.02.160). После заполнения шифров повторите импорт.");

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
}
