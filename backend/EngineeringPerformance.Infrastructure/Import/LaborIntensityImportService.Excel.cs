using ClosedXML.Excel;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

internal sealed partial class LaborIntensityImportService
{
    public async Task<LaborImportPreviewDto> PreviewLaborImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new LaborImportPreviewDto(Array.Empty<string>(), 0);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var laborCatalogRows = await context.LaborIntensities.AsNoTracking().ToListAsync();
        var knownDocTypes = LaborCatalogLookup.KnownKeys(laborCatalogRows);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = GetLaborIntensityImportWorksheet(workbook);
        if (worksheet.RangeUsed() is null)
        {
            return new LaborImportPreviewDto(Array.Empty<string>(), 0);
        }

        if (!TryResolveLaborIntensityImportLayout(worksheet, out var layout))
        {
            return new LaborImportPreviewDto(Array.Empty<string>(), 0);
        }

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var unmatched = new SortedSet<string>(StringComparer.Ordinal);
        var totalDataRows = 0;

        for (var rowNumber = layout.FirstDataRowNumber; rowNumber <= lastRowNumber; rowNumber++)
        {
            if (!LaborIntensityImportRowHasAnyData(worksheet, rowNumber, layout))
            {
                continue;
            }

            var documentTypeName = ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.DocumentTypeColumn)?.Trim();
            if (string.IsNullOrWhiteSpace(documentTypeName))
            {
                continue;
            }

            totalDataRows++;
            if (!knownDocTypes.Contains(documentTypeName))
            {
                unmatched.Add(documentTypeName);
            }
        }

        return new LaborImportPreviewDto(unmatched.ToList(), totalDataRows);
    }

    public Task<DataImportResultDto> ImportLaborIntensitiesAsync(
        int projectId,
        int versionId,
        string filePath,
        string description,
        ImportSourceMetadata source,
        bool addMissingToCatalog) =>
        ImportTransactionRunner.ExecuteWithRetryableTransactionAsync(
            _contextFactory,
            context =>
            ImportLaborIntensitiesCoreAsync(context, projectId, versionId, filePath, description, source, addMissingToCatalog));

    private async Task<DataImportResultDto> ImportLaborIntensitiesCoreAsync(
        AppDbContext context,
        int projectId,
        int versionId,
        string filePath,
        string description,
        ImportSourceMetadata source,
        bool addMissingToCatalog)
    {
        // Удаляем все предыдущие данные трудоёмкости для этой версии (стратегия: очистка + вставка)
        await ImportHelpers.ClearLaborIntensityDataForVersionAsync(context, projectId, versionId);

        var iteration = await ImportIterationRecorder.CreateIterationAsync(
            context,
            projectId,
            versionId,
            DataImportType.LaborIntensity,
            description,
            source);

        // Устойчивая связь — LaborIntensityId. WorkDirectionId на PLI — только ключ
        // уникального индекса SQL, не SoT расчёта: факт читает регистр as-of CreatedAt.
        var laborCatalogRows = await context.LaborIntensities.ToListAsync();
        var laborByDocumentType = LaborCatalogLookup.BuildMap(laborCatalogRows);
        var folderIdsByName = LaborCatalogLookup.FolderIdsByName(laborCatalogRows);

        var importTimestamp = iteration.ImportedAt;
        // Накапливаем строки для массовой вставки (без группировок — построчно, как в импорте ФО)
        var newEntities = new List<ProjectLaborIntensity>();
        var laborFileRowsAccepted = 0;
        var rowsWithoutCatalogMatch = 0;
        var rowsWithoutDirection = 0;
        var newCatalogEntriesCreated = 0;
        var unmatchedDocTypes = new SortedSet<string>(StringComparer.Ordinal);
        var docTypesWithoutDirection = new SortedSet<string>(StringComparer.Ordinal);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = GetLaborIntensityImportWorksheet(workbook);

        if (worksheet.RangeUsed() is null)
        {
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            return new DataImportResultDto(true, 0, null, iteration.Id);
        }

        if (!TryResolveLaborIntensityImportLayout(worksheet, out var layout))
        {
            throw new InvalidOperationException(
                "Не удалось определить структуру файла трудоёмкости. Ожидаются колонки: «Наименование проектного направления», «Наименование типа документа TF», «Сумма Плановая трудоемкость, чч».");
        }

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        // Опциональное авто-пополнение справочника «Трудоёмкость» отсутствующими
        // «Типами документа» (ответ пользователя «Да» на запрос из UI). Новые позиции
        // создаются корневыми (без Parent), без привязки к IIC и норм — это
        // промежуточные карточки, которые после импорта администратор каталога
        // может отредактировать (проставить направление/нормы) через справочник.
        if (addMissingToCatalog)
        {
            var docTypesInFile = new SortedSet<string>(StringComparer.Ordinal);
            var directionByDocType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var rowNumber = layout.FirstDataRowNumber; rowNumber <= lastRowNumber; rowNumber++)
            {
                if (!LaborIntensityImportRowHasAnyData(worksheet, rowNumber, layout))
                {
                    continue;
                }

                var name = ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.DocumentTypeColumn)?.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    docTypesInFile.Add(name);
                    if (!directionByDocType.ContainsKey(name) && layout.WorkDirectionColumn > 0)
                    {
                        var direction = ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.WorkDirectionColumn)?.Trim();
                        if (!string.IsNullOrWhiteSpace(direction))
                            directionByDocType[name] = direction;
                    }
                }
            }

            var created = 0;
            foreach (var docType in docTypesInFile)
            {
                if (laborByDocumentType.ContainsKey(docType))
                {
                    continue;
                }

                var code = docType.Length <= 100 ? docType : docType[..100];
                var name = docType.Length <= 500 ? docType : docType[..500];
                int? parentId = null;
                if (directionByDocType.TryGetValue(docType, out var folderName)
                    && folderIdsByName.TryGetValue(folderName, out var folderId))
                {
                    parentId = folderId;
                }

                var newCatalogItem = new LaborIntensity
                {
                    Code = code,
                    Name = name,
                    DocumentTypeName = name,
                    ParentId = parentId,
                    IsArchived = false,
                    SortOrder = 0,
                    CreatedAt = importTimestamp,
                };
                context.LaborIntensities.Add(newCatalogItem);
                laborByDocumentType[docType] = newCatalogItem;
                created++;
            }

            if (created > 0)
            {
                // SaveChangesAsync — чтобы у новых LaborIntensity появились Id, иначе
                // ProjectLaborIntensity.LaborIntensityId будет привязан к 0.
                await context.SaveChangesAsync();
                _logger.LogInformation(
                    "Labor import: auto-created {Count} catalog entries for unmatched DocumentTypeName",
                    created);
            }

            newCatalogEntriesCreated = created;
        }

        // Pivot/итоги-строки в файлах TF: общие суммы без типа документа. Пропускаем.
        for (var rowNumber = layout.FirstDataRowNumber; rowNumber <= lastRowNumber; rowNumber++)
        {
            if (!LaborIntensityImportRowHasAnyData(worksheet, rowNumber, layout))
            {
                continue;
            }

            var documentTypeName = ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.DocumentTypeColumn)?.Trim();
            if (string.IsNullOrWhiteSpace(documentTypeName))
            {
                continue;
            }

            // Разрешаем тип документа через справочник «Трудоёмкость» (LaborIntensityId).
            // WorkDirectionId на строке — только ключ уникального индекса, не SoT факта.
            LaborIntensity? laborItem = null;
            laborByDocumentType.TryGetValue(documentTypeName, out laborItem);

            if (laborItem is null)
            {
                rowsWithoutCatalogMatch++;
                unmatchedDocTypes.Add(documentTypeName);
            }
            else if (laborItem.IicWorkDirectionId is null)
            {
                rowsWithoutDirection++;
                docTypesWithoutDirection.Add(documentTypeName);
            }

            var brand = layout.BrandColumn > 0
                ? ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.BrandColumn)?.Trim()
                : null;
            var plannedHours = ImportHelpers.ParseDecimal(ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.PlannedHoursColumn));
            var actualHours = layout.ActualHoursColumn > 0
                ? ImportHelpers.ParseDecimal(ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.ActualHoursColumn))
                : 0m;
            var notes = layout.NotesColumn > 0
                ? ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, layout.NotesColumn)?.Trim()
                : null;

            // DocumentationCode формируем из (DocumentTypeName, Brand): одинаковый тип документа
            // для разных «марок» (3U0000-ОП-АТХ1 vs 4L0000-…) — это разные строки в файле и в БД.
            var documentationCode = string.IsNullOrWhiteSpace(brand)
                ? documentTypeName
                : $"{documentTypeName}|{brand}";

            // SQL Server unique-индекс на nvarchar — ограничение длины ~450 символов
            // (DocumentationCode настроен как 450, см. ProjectLaborIntensityConfiguration).
            // ISS-006: ранее лимит был 100 и схлопывал разные «тип+марка» по truncate
            // (270+ строк имели префикс >100 символов, ~2 830 ч терялось при merge).
            if (documentationCode.Length > 450)
            {
                documentationCode = documentationCode[..450];
            }

            var entity = new ProjectLaborIntensity
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                DocumentationCode = documentationCode,
                // Ключ уникального индекса; расчёт читает регистр as-of даты версии.
                WorkDirectionId = laborItem?.IicWorkDirectionId,
                LaborIntensityId = laborItem?.Id,
                PlannedHours = plannedHours,
                ActualHours = actualHours,
                Brand = brand,
                Notes = notes,
                CreatedAt = importTimestamp,
            };

            newEntities.Add(entity);
            laborFileRowsAccepted++;
        }

        var docSetBinder = new DocSetImportBinder(context);
        foreach (var entity in newEntities)
            entity.DocSetId = await docSetBinder.ResolveAsync(entity.DocumentationCode);

        // Массовая вставка всех строк без группировок (построчно, как в импорте ФО)
        if (newEntities.Count > 0)
        {
            context.ProjectLaborIntensities.AddRange(newEntities);
            await context.SaveChangesAsync();
        }

        iteration.RecordCount = newEntities.Count;
        await context.SaveChangesAsync();

        var details = new List<string>();
        if (newCatalogEntriesCreated > 0)
        {
            details.Add($"В справочник «Трудоёмкость» добавлено {newCatalogEntriesCreated} новых позиций из файла (без привязки к направлению IIC и без норм — проверьте карточки в справочнике).");
        }
        if (rowsWithoutCatalogMatch > 0)
        {
            details.Add($"Для {rowsWithoutCatalogMatch} строк файла «Тип документа» не найден в справочнике «Трудоёмкость»: записи сохранены без привязки (WorkDirectionId/LaborIntensityId = NULL).");
            details.Add("Не найдены в справочнике (" + unmatchedDocTypes.Count + "):" + Environment.NewLine
                        + string.Join(Environment.NewLine, unmatchedDocTypes));
        }
        if (rowsWithoutDirection > 0)
        {
            details.Add($"Для {rowsWithoutDirection} строк позиция справочника «Трудоёмкость» найдена, но у неё не задано направление работ IIC: записи сохранены без привязки к направлению.");
            details.Add("Без привязки к направлению (" + docTypesWithoutDirection.Count + "):" + Environment.NewLine
                        + string.Join(Environment.NewLine, docTypesWithoutDirection));
        }

        var detailsText = details.Count > 0 ? string.Join(Environment.NewLine + Environment.NewLine, details) : null;
        return new DataImportResultDto(true, newEntities.Count, null, iteration.Id, detailsText);
    }


    /// <summary>Описание раскладки колонок в листе трудоёмкости после авто-детекции по заголовку.</summary>
    private readonly record struct LaborIntensityImportLayout(
        int FirstDataRowNumber,
        int WorkDirectionColumn,
        int DocumentTypeColumn,
        int PlannedHoursColumn,
        int ActualHoursColumn,
        int BrandColumn,
        int NotesColumn);

    /// <summary>
    /// Определяет строку заголовка и индексы колонок в листе трудоёмкости.
    /// Поддержан формат «Труд проектов» (с заголовками «Наименование типа документа TF» и
    /// «Наименование проектного направления»). Для совместимости со старыми шаблонами без
    /// заголовков возвращает «легаси»-раскладку: A=шифр, C=марка, D=направление, E=план, F=факт, G=примечание.
    /// </summary>
    private static bool TryResolveLaborIntensityImportLayout(
        IXLWorksheet worksheet,
        out LaborIntensityImportLayout layout)
    {
        layout = default;
        var used = worksheet.RangeUsed();
        if (used is null)
        {
            return false;
        }

        var firstRow = used.FirstRow().RowNumber();
        var lastSearchRow = Math.Min(used.LastRow().RowNumber(), firstRow + 30);
        var lastCol = Math.Min(used.LastColumn().ColumnNumber(), 40);

        for (var r = firstRow; r <= lastSearchRow; r++)
        {
            int directionCol = -1, docTypeCol = -1, plannedCol = -1, actualCol = -1, brandCol = -1, notesCol = -1;
            var headerHits = 0;

            for (var c = 1; c <= lastCol; c++)
            {
                var text = ImportHelpers.GetWorksheetCellString(worksheet, r, c);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                // Нормализуем «ё» → «е» для устойчивого сравнения русскоязычных заголовков
                var t = text.Trim().Replace('ё', 'е').Replace('Ё', 'Е');
                if (directionCol < 0 && t.Contains("проектного направления", StringComparison.OrdinalIgnoreCase))
                {
                    directionCol = c;
                    headerHits++;
                }
                else if (docTypeCol < 0 && (
                    t.Contains("тип документа", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("типа документа", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("вида документов", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("вид документов", StringComparison.OrdinalIgnoreCase)))
                {
                    docTypeCol = c;
                    headerHits++;
                }
                else if (plannedCol < 0 && (
                    t.Contains("плановая трудоемкость", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("плановой трудоем", StringComparison.OrdinalIgnoreCase)
                    || (t.Contains("плановой", StringComparison.OrdinalIgnoreCase)
                        && t.Contains("труд", StringComparison.OrdinalIgnoreCase))))
                {
                    plannedCol = c;
                    headerHits++;
                }
                else if (actualCol < 0 && (
                    t.Contains("фактически списано", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("фактическая трудоемкость", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("фактической работы", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("фактической трудоем", StringComparison.OrdinalIgnoreCase)))
                {
                    actualCol = c;
                    headerHits++;
                }
                else if (brandCol < 0 && t.Equals("марка", StringComparison.OrdinalIgnoreCase))
                {
                    brandCol = c;
                    headerHits++;
                }
                else if (notesCol < 0 && t.Contains("примечан", StringComparison.OrdinalIgnoreCase))
                {
                    notesCol = c;
                    headerHits++;
                }
            }

            // Минимально достаточный набор: направление + тип документа + плановые часы.
            if (directionCol > 0 && docTypeCol > 0 && plannedCol > 0)
            {
                layout = new LaborIntensityImportLayout(
                    FirstDataRowNumber: r + 1,
                    WorkDirectionColumn: directionCol,
                    DocumentTypeColumn: docTypeCol,
                    PlannedHoursColumn: plannedCol,
                    ActualHoursColumn: actualCol > 0 ? actualCol : -1,
                    BrandColumn: brandCol > 0 ? brandCol : -1,
                    NotesColumn: notesCol > 0 ? notesCol : -1);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Выбирает лист импорта трудоёмкости. Приоритет — лист с именем «Труд проектов»;
    /// затем имена со словом «труд»; иначе первый непустой лист.
    /// </summary>
    private static IXLWorksheet GetLaborIntensityImportWorksheet(XLWorkbook workbook)
    {
        IXLWorksheet? exact = null;
        IXLWorksheet? partial = null;

        foreach (var ws in workbook.Worksheets)
        {
            if (ws.RangeUsed() is null)
            {
                continue;
            }

            if (ws.Name.Equals("Труд проектов", StringComparison.OrdinalIgnoreCase))
            {
                exact = ws;
                break;
            }

            if (partial is null && ws.Name.Contains("труд", StringComparison.OrdinalIgnoreCase))
            {
                partial = ws;
            }
        }

        return exact ?? partial ?? ImportHelpers.GetFirstWorksheetWithData(workbook);
    }

    /// <summary>Проверяет, есть ли в строке импорта трудоёмкости хоть одно значимое поле.</summary>
    private static bool LaborIntensityImportRowHasAnyData(IXLWorksheet worksheet, int rowNumber, LaborIntensityImportLayout layout)
    {
        Span<int> cols = stackalloc int[]
        {
            layout.WorkDirectionColumn,
            layout.DocumentTypeColumn,
            layout.PlannedHoursColumn,
            layout.ActualHoursColumn,
            layout.BrandColumn,
            layout.NotesColumn,
        };

        foreach (var c in cols)
        {
            if (c <= 0)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(ImportHelpers.GetWorksheetCellString(worksheet, rowNumber, c)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ISS-006: схлопывает строки трудоёмкости по натуральному ключу
    /// (DocumentationCode, WorkDirectionId) с суммированием плановых и фактических часов.
    /// Строки с пустым WorkDirectionId возвращаются отдельным списком — фильтрованный
    /// уникальный индекс пропускает их вне merge.
    /// Public для модульного тестирования (см. DataImportServiceTests).
    /// </summary>
    public static (
        Dictionary<(string DocumentationCode, int WorkDirectionId), ProjectLaborIntensity> Merged,
        List<ProjectLaborIntensity> Unmatched)
        MergeLaborRowsByNaturalKey(IEnumerable<ProjectLaborIntensity> rows)
    {
        var merged = new Dictionary<(string DocumentationCode, int WorkDirectionId), ProjectLaborIntensity>();
        var unmatched = new List<ProjectLaborIntensity>();

        foreach (var entity in rows)
        {
            if (entity.WorkDirectionId is int wdId)
            {
                var key = (entity.DocumentationCode, wdId);
                if (merged.TryGetValue(key, out var existing))
                {
                    existing.PlannedHours += entity.PlannedHours;
                    existing.ActualHours += entity.ActualHours;
                }
                else
                {
                    merged[key] = entity;
                }
            }
            else
            {
                unmatched.Add(entity);
            }
        }

        return (merged, unmatched);
    }

}
