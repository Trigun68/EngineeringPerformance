using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Строка каталога ФО (КФО или БЕР) для сопоставления при Excel/JSON импорте.</summary>
internal readonly record struct PvCatalogRow(
    int Id, string Code, string Name, int? ClassifierId, int? ParentId, decimal Coefficient, bool? IsAssessment);

/// <summary>
/// Общие хелперы импорта: чтение ячеек Excel (ФО/оборудование/трудоёмкость)
/// и привязка каталога ФО (Excel + JSON ingest).
/// </summary>
internal static class ImportHelpers
{
    public static string? GetCellValue(IXLRangeRow row, int columnNumber)
    {
        return GetWorksheetCellString(row.Worksheet, row.RowNumber(), columnNumber);
    }

    /// <summary>
    /// Возвращает текст ячейки; для объединённых областей — значение из первой ячейки диапазона (как в Excel).
    /// </summary>
    public static string? GetWorksheetCellString(IXLWorksheet sheet, int rowNumber, int columnNumber)
    {
        var cell = sheet.Cell(rowNumber, columnNumber);
        if (!cell.IsEmpty())
        {
            return CellValueToString(cell);
        }

        if (cell.IsMerged())
        {
            var merged = cell.MergedRange();
            var first = merged.FirstCell();
            if (!first.IsEmpty())
            {
                return CellValueToString(first);
            }
        }

        return null;
    }

    /// <summary>
    /// ClosedXML 0.104+: cell values are <see cref="XLCellValue"/>; <see cref="IXLCell.GetValue{T}"/> throws on mismatch.
    /// </summary>
    public static string? CellValueToString(IXLCell cell)
    {
        var v = cell.Value;
        if (v.IsBlank)
        {
            return null;
        }

        if (v.IsText)
        {
            return v.GetText().Trim();
        }

        if (v.IsNumber)
        {
            return v.GetNumber().ToString(CultureInfo.InvariantCulture);
        }

        if (v.IsBoolean)
        {
            return v.GetBoolean().ToString();
        }

        if (v.IsDateTime)
        {
            return v.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return v.ToString(CultureInfo.InvariantCulture);
    }

    public static IXLWorksheet GetFirstWorksheetWithData(XLWorkbook workbook)
    {
        foreach (var ws in workbook.Worksheets)
        {
            if (ws.RangeUsed() is not null)
            {
                return ws;
            }
        }

        return workbook.Worksheets.Count > 0
            ? workbook.Worksheet(1)
            : throw new InvalidOperationException("В книге Excel нет листов с данными.");
    }

    public static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var normalized = value.Trim()
            .Replace('\u00A0', ' ')
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');

        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
        {
            return (decimal)dbl;
        }

        throw new InvalidOperationException($"\u041D\u0435 \u0443\u0434\u0430\u043B\u043E\u0441\u044C \u0440\u0430\u0437\u043E\u0431\u0440\u0430\u0442\u044C \u0447\u0438\u0441\u043B\u043E\u0432\u043E\u0435 \u0437\u043D\u0430\u0447\u0435\u043D\u0438\u0435: '{value}'.");
    }

    /// <summary>
    /// Несколько строк каталога могут иметь один код. Импорт выбирает кандидата явно.
    /// </summary>
    public static ILookup<string, PvCatalogRow> BuildCodeLookup(
        IReadOnlyList<PvCatalogRow> rows,
        string catalogName,
        ILogger logger)
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
            logger.LogInformation(
                "Catalog {Catalog}: {Count} code keys map to multiple rows; import picks row with ClassifierId, then longest Code",
                catalogName,
                duplicateGroups);
        }

        return pairs.ToLookup(p => p.Key, p => p.Pv, StringComparer.OrdinalIgnoreCase);
    }

    public static PvCatalogRow? PickBestPhysicalVolumeCandidate(IReadOnlyList<PvCatalogRow> candidates)
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

    public static string? NormalizeCatalogCode(string? code)
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

    public static bool TryResolvePhysicalVolume(
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

    public static IEnumerable<string> ExpandEpCodeLookupKeys(string raw)
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
    /// Для позиций сида без «Техас» в Excel ClassifierId остаётся NULL, хотя у родительских
    /// групп в каталоге связь с ИИС уже задана — поднимаем её при импорте, чтобы аналитика
    /// и колонка «Классификатор» были заполнены.
    /// </summary>
    public static async Task BackfillPhysicalVolumeClassifiersFromAncestorsAsync(
        AppDbContext context,
        IEnumerable<int> importedPhysicalVolumeIds,
        IReadOnlyDictionary<int, PvCatalogRow> pvByIdSnapshot,
        bool isKfo,
        ILogger logger)
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
            logger.LogInformation(
                "Physical volume import: set ClassifierId from ancestor chain for {Count} catalog rows",
                updated);
            await context.SaveChangesAsync();
        }
    }

    public static int? ResolveClassifierIdFromAncestorChain(
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

    public static async Task ClearPhysicalVolumeDataForVersionAsync(AppDbContext context, int projectId, int versionId)
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

    public static async Task<int> DeletePhysicalVolumeRecordsAsync(AppDbContext context, int projectId, int versionId, DateTime importedAt)
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
    /// Нормализует строковое поле для использования в качестве части ключа словаря:
    /// обрезает до <paramref name="maxLength"/> символов и заменяет <c>null</c> пустой строкой.
    /// Совпадает с тем, что сохранится в БД после <c>ApplyTruncation()</c>.
    /// </summary>
    public static string TruncateKey(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public static async Task ClearEquipmentDataForVersionAsync(AppDbContext context, int projectId, int versionId)
    {
        var existing = await context.ProjectEquipments
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        if (existing.Count > 0)
            context.ProjectEquipments.RemoveRange(existing);

        var existingIterations = await context.DataImportIterations
            .Where(i => i.ProjectId == projectId
                     && i.ProjectAnalysisVersionId == versionId
                     && i.DataType == DataImportType.Equipment)
            .ToListAsync();

        if (existingIterations.Count > 0)
            context.DataImportIterations.RemoveRange(existingIterations);

        if (existing.Count > 0 || existingIterations.Count > 0)
            await context.SaveChangesAsync();
    }

    public static async Task<int> DeleteEquipmentRecordsAsync(AppDbContext context, int projectId, int versionId, DateTime importedAt)
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

    /// <summary>Удаляет все записи трудоёмкости и итерации импорта для указанной версии проекта.</summary>
    public static async Task ClearLaborIntensityDataForVersionAsync(AppDbContext context, int projectId, int versionId)
    {
        var existing = await context.ProjectLaborIntensities
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync();
        if (existing.Count > 0)
            context.ProjectLaborIntensities.RemoveRange(existing);

        var existingIterations = await context.DataImportIterations
            .Where(i => i.ProjectId == projectId
                     && i.ProjectAnalysisVersionId == versionId
                     && i.DataType == DataImportType.LaborIntensity)
            .ToListAsync();
        if (existingIterations.Count > 0)
            context.DataImportIterations.RemoveRange(existingIterations);

        if (existing.Count > 0 || existingIterations.Count > 0)
            await context.SaveChangesAsync();
    }

    public static async Task<int> DeleteLaborIntensityRecordsAsync(AppDbContext context, int projectId, int versionId, DateTime importedAt)
    {
        var records = await context.ProjectLaborIntensities
            .Where(li => li.ProjectId == projectId
                      && li.ProjectAnalysisVersionId == versionId
                      && li.CreatedAt == importedAt)
            .ToListAsync();

        if (records.Count > 0)
        {
            context.ProjectLaborIntensities.RemoveRange(records);
            await context.SaveChangesAsync();
        }

        return records.Count;
    }

    public static bool TryParseIngestMode(string? mode, out bool isReplace)
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

    /// <summary>null — можно продолжать ingest; иначе готовый ответ с ошибкой.</summary>
    public static DataImportResultDto? ValidateJsonIngest(
        string? mode,
        int itemCount,
        bool allowEmptyReplace,
        out bool isReplace)
    {
        if (!TryParseIngestMode(mode, out isReplace))
            return new DataImportResultDto(false, 0, "Invalid mode. Use replace or upsert.", 0);

        if (isReplace && itemCount == 0 && !allowEmptyReplace)
        {
            return new DataImportResultDto(
                false,
                0,
                "Replace with empty items is not allowed. Pass allowEmptyReplace=true to clear the version.",
                0);
        }

        return null;
    }

    public static ImportSourceMetadata CreateJsonIngestSource(int? userId, string? description, int itemCount)
    {
        var payload = $"{description ?? "json-ingest"}|{itemCount}|{DateTime.UtcNow:O}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return new ImportSourceMetadata("json-ingest.json", hash, userId);
    }

    public static async Task<string> SaveTempFileAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".xlsx";

        var tempPath = Path.Combine(Path.GetTempPath(), $"ep-import-{Guid.NewGuid():N}{ext}");
        await using var fs = File.Create(tempPath);
        if (stream.CanSeek)
            stream.Position = 0;
        await stream.CopyToAsync(fs, cancellationToken);
        return tempPath;
    }

    public static string ComputeFileSha256Hex(string filePath)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(filePath);
        return Convert.ToHexString(sha.ComputeHash(fs));
    }

    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Временный файл — не критично при сбое удаления.
        }
    }
}
