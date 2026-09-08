using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

public sealed partial class ProjectExcelDataImportService : IProjectImportService
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
    /// Ключ слияния строк при импорте ФО: одна позиция каталога + объект + WorkId
    /// = одна запись в <c>ProjectPhysicalVolumes</c>.
    /// Соответствует расширенному уникальному индексу:
    /// <c>(ProjectAnalysisVersionId, ProjectId, PhysicalVolumeId, FacilityCode, FacilityName, WorkId)</c>.
    /// </summary>
    private readonly record struct PvRowKey(
        int PhysicalVolumeId,
        string FacilityCode,
        string FacilityName,
        string WorkId);

    private readonly record struct PvCatalogRow(
        int Id, string Code, string Name, int? ClassifierId, int? ParentId, decimal Coefficient, bool? IsAssessment);

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<ProjectExcelDataImportService> _logger;

    private async Task<DataImportResultDto> ImportFromFileAsync(
        int projectId,
        int versionId,
        DataImportType dataType,
        string filePath,
        string description,
        ImportSourceMetadata source,
        bool addMissingLaborToCatalog = false)
    {
        if (!File.Exists(filePath))
        {
            return new DataImportResultDto(false, 0, $"File not found: {filePath}", 0);
        }

        try
        {
            return dataType switch
            {
                DataImportType.PhysicalVolume => await ImportPhysicalVolumesAsync(projectId, versionId, filePath, description, source),
                DataImportType.LaborIntensity => await ImportLaborIntensitiesAsync(projectId, versionId, filePath, description, source, addMissingLaborToCatalog),
                DataImportType.Equipment => await ImportEquipmentAsync(projectId, versionId, filePath, description, source),
                _ => new DataImportResultDto(false, 0, $"Unsupported import type: {dataType}", 0),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel import failed for project {ProjectId}, version {VersionId}, type {DataType}", projectId, versionId, dataType);
            return new DataImportResultDto(false, 0, FormatImportExceptionMessage(ex), 0);
        }
    }

    private Task<IReadOnlyList<DataImportIterationInfoDto>> GetImportIterationsCoreAsync(
        int projectId, int versionId, DataImportType dataType, CancellationToken cancellationToken = default) =>
        ImportIterationRecorder.GetImportIterationsCoreAsync(
            _contextFactory, projectId, versionId, dataType, cancellationToken);

    /// <summary>
    /// Единая точка создания итерации импорта: описание, происхождение файла и автор
    /// сохраняются вместе с данными в одной транзакции.
    /// </summary>
    private static Task<DataImportIteration> CreateIterationAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        DataImportType dataType,
        string description,
        ImportSourceMetadata source) =>
        ImportIterationRecorder.CreateIterationAsync(
            context, projectId, versionId, dataType, description, source);

    /// <summary>
    /// EnableRetryOnFailure требует оборачивать пользовательские транзакции в CreateExecutionStrategy.
    /// </summary>
    private Task<T> ExecuteWithRetryableTransactionAsync<T>(
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(_contextFactory, action, cancellationToken);

    private Task<int> DeleteIterationCoreAsync(
        int projectId, int versionId, DataImportType dataType, DateTime importedAt, CancellationToken cancellationToken = default) =>
        ImportIterationRecorder.DeleteIterationCoreAsync(
            _contextFactory,
            projectId,
            versionId,
            dataType,
            importedAt,
            DeleteImportedRecordsAsync,
            cancellationToken);

    private static string? GetCellValue(IXLRangeRow row, int columnNumber)
    {
        return GetWorksheetCellString(row.Worksheet, row.RowNumber(), columnNumber);
    }

    /// <summary>
    /// Возвращает текст ячейки; для объединённых областей — значение из первой ячейки диапазона (как в Excel).
    /// </summary>
    private static string? GetWorksheetCellString(IXLWorksheet sheet, int rowNumber, int columnNumber)
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
    private static string? CellValueToString(IXLCell cell)
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

    private static IXLWorksheet GetFirstWorksheetWithData(XLWorkbook workbook)
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

    private static decimal ParseDecimal(string? value)
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

    private static string FormatImportExceptionMessage(Exception ex)
    {
        // Log full exception server-side (already logged at call site).
        // Return a generic message to avoid leaking DB schema, constraint names,
        // connection details, or other internal information to the caller.
        return "Ошибка импорта данных. Обратитесь к администратору.";
    }

    /// <summary>
    /// Диспетчер удаления строк данных по типу импорта. Остаётся здесь: Delete*RecordsAsync
    /// живут в других partial и недоступны из <see cref="ImportIterationRecorder"/>.
    /// </summary>
    private static async Task<int> DeleteImportedRecordsAsync(AppDbContext context, DataImportType dataType, int projectId, int versionId, DateTime importedAt)
    {
        return dataType switch
        {
            DataImportType.PhysicalVolume => await DeletePhysicalVolumeRecordsAsync(context, projectId, versionId, importedAt),
            DataImportType.LaborIntensity => await DeleteLaborIntensityRecordsAsync(context, projectId, versionId, importedAt),
            DataImportType.Equipment => await DeleteEquipmentRecordsAsync(context, projectId, versionId, importedAt),
            _ => 0,
        };
    }
}
