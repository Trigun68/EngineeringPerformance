using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class DataImportService : IDataImportService
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

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(IDbContextFactory<AppDbContext> contextFactory, ILogger<DataImportService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<DataImportResult> ImportFromFileAsync(int projectId, int versionId, DataImportType dataType, string filePath, string description, bool addMissingLaborToCatalog = false)
    {
        if (!File.Exists(filePath))
        {
            return new DataImportResult(false, 0, $"File not found: {filePath}", 0);
        }

        try
        {
            return dataType switch
            {
                DataImportType.PhysicalVolume => await ImportPhysicalVolumesAsync(projectId, versionId, filePath, description),
                DataImportType.LaborIntensity => await ImportLaborIntensitiesAsync(projectId, versionId, filePath, description, addMissingLaborToCatalog),
                _ => new DataImportResult(false, 0, $"Unsupported import type: {dataType}", 0),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel import failed for project {ProjectId}, version {VersionId}, type {DataType}", projectId, versionId, dataType);
            return new DataImportResult(false, 0, FormatImportExceptionMessage(ex), 0);
        }
    }

    public async Task<LaborImportPreview> PreviewLaborImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new LaborImportPreview(Array.Empty<string>(), 0);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var laborCatalogRows = await context.LaborIntensities.AsNoTracking().ToListAsync();
        var knownDocTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var li in laborCatalogRows)
        {
            var key = (li.DocumentTypeName ?? string.Empty).Trim();
            if (key.Length > 0)
            {
                knownDocTypes.Add(key);
            }
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = GetLaborIntensityImportWorksheet(workbook);
        if (worksheet.RangeUsed() is null)
        {
            return new LaborImportPreview(Array.Empty<string>(), 0);
        }

        if (!TryResolveLaborIntensityImportLayout(worksheet, out var layout))
        {
            return new LaborImportPreview(Array.Empty<string>(), 0);
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

            var documentTypeName = GetWorksheetCellString(worksheet, rowNumber, layout.DocumentTypeColumn)?.Trim();
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

        return new LaborImportPreview(unmatched.ToList(), totalDataRows);
    }

    public Task<DataImportResult> ImportFromApiAsync(int projectId, int versionId, DataImportType dataType, string apiEndpoint, string description)
    {
        return Task.FromResult(new DataImportResult(false, 0, "API import not yet implemented", 0));
    }

    public async Task<IReadOnlyList<DataImportIterationInfo>> GetImportIterationsAsync(int projectId, int versionId, DataImportType dataType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DataImportIterations
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId
                     && i.ProjectAnalysisVersionId == versionId
                     && i.DataType == dataType)
            .OrderByDescending(i => i.ImportedAt)
            .Select(i => new DataImportIterationInfo(i.ImportedAt, i.RecordCount, i.Description, i.SourceFileName))
            .ToListAsync();
    }

    public async Task<int> DeleteIterationAsync(int projectId, int versionId, DataImportType dataType, DateTime importedAt)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var iteration = await context.DataImportIterations
            .FirstOrDefaultAsync(i => i.ProjectId == projectId
                                   && i.ProjectAnalysisVersionId == versionId
                                   && i.DataType == dataType
                                   && i.ImportedAt == importedAt);

        if (iteration is null)
        {
            return 0;
        }

        var deletedRecords = await DeleteImportedRecordsAsync(context, dataType, projectId, versionId, importedAt);

        context.DataImportIterations.Remove(iteration);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
        return deletedRecords;
    }

    private async Task<DataImportResult> ImportPhysicalVolumesAsync(int projectId, int versionId, string filePath, string description)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

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

        var physicalVolumeRows = await context.PhysicalVolumes.AsNoTracking().ToListAsync();
        var physicalVolumes = BuildCodeLookup(physicalVolumeRows, "PhysicalVolumes");

        var mergedByVolumeId = new Dictionary<int, PhysicalVolumeImportDraft>();

        using var workbook = new XLWorkbook(filePath);
        var worksheet = GetPhysicalVolumeImportWorksheet(workbook);

        if (!TryResolvePhysicalVolumeImportLayout(worksheet, out var columnMap, out var firstDataRowNumber))
        {
            await transaction.CommitAsync();
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            var emptySheetDetails =
                "Импортировано строк: 0." + Environment.NewLine + Environment.NewLine
                + "Выбранный лист не содержит заполненного диапазона (нет данных для чтения).";
            return new DataImportResult(true, 0, null, iteration.Id, emptySheetDetails);
        }

        var rows = EnumeratePhysicalVolumeDataRows(worksheet, firstDataRowNumber).ToList();
        if (rows.Count == 0)
        {
            await transaction.CommitAsync();
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            var noDataRowsDetails =
                "Импортировано строк: 0." + Environment.NewLine + Environment.NewLine
                + "После строки заголовка не найдено ни одной строки с данными. Проверьте, что открыт нужный лист «Список работ» и что в файле есть шапка с текстом вроде «ID по каталогу» / «Код по каталогу».";
            return new DataImportResult(true, 0, null, iteration.Id, noDataRowsDetails);
        }

        var importTimestamp = iteration.ImportedAt;
        var skippedNoEpCode = 0;
        var skippedNotInCatalog = 0;

        foreach (var row in rows)
        {
            var candidates = EnumerateCandidateEpCodesFromRow(row, columnMap, GetCellValue).ToList();
            if (candidates.Count == 0)
            {
                skippedNoEpCode++;
                continue;
            }

            PhysicalVolume? physicalVolume = null;
            foreach (var candidate in candidates)
            {
                if (TryResolvePhysicalVolume(physicalVolumes, candidate, out physicalVolume))
                {
                    break;
                }
            }

            if (physicalVolume is null)
            {
                skippedNotInCatalog++;
                _logger.LogWarning(
                    "Physical volume with code '{Code}' not found in catalog, skipping",
                    candidates[0]);

                continue;
            }

            var pvId = physicalVolume.Id;
            var draft = PhysicalVolumeImportDraft.FromRow(row, columnMap, ParseDecimal, GetCellValue, physicalVolume);

            if (mergedByVolumeId.TryGetValue(pvId, out var existing))
            {
                existing.MergeFrom(draft);
            }
            else
            {
                mergedByVolumeId[pvId] = draft;
            }
        }

        if (mergedByVolumeId.Count == 0)
        {
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            var catalogCount = physicalVolumes.Count;
            var zeroDetails = BuildPhysicalVolumeZeroImportDetails(
                rows.Count,
                catalogCount,
                skippedNoEpCode,
                skippedNotInCatalog);
            return new DataImportResult(true, 0, null, iteration.Id, zeroDetails);
        }

        var pvById = physicalVolumeRows.ToDictionary(pv => pv.Id);
        await BackfillPhysicalVolumeClassifiersFromAncestorsAsync(
            context,
            mergedByVolumeId.Keys,
            pvById);

        var volumeIds = mergedByVolumeId.Keys.ToList();
        var existingInDb = await context.ProjectPhysicalVolumes
            .Where(p => p.ProjectId == projectId
                     && p.ProjectAnalysisVersionId == versionId
                     && volumeIds.Contains(p.PhysicalVolumeId))
            .ToDictionaryAsync(p => p.PhysicalVolumeId);

        foreach (var (physicalVolumeId, draft) in mergedByVolumeId)
        {
            draft.ApplyTruncation();

            if (existingInDb.TryGetValue(physicalVolumeId, out var entity))
            {
                draft.ApplyTo(entity);
                // CreatedAt синхронизируется с ImportedAt итерации, чтобы удаление этой
                // итерации (фильтр по pv.CreatedAt == importedAt) корректно откатывало
                // и вновь созданные, и обновлённые строки — иначе при повторном импорте
                // образуется «итерация-фантом»: часть записей не привязывается к ней.
                entity.CreatedAt = importTimestamp;
                entity.ModifiedAt = importTimestamp;
            }
            else
            {
                var newEntity = draft.ToNewEntity(projectId, versionId, physicalVolumeId, importTimestamp);
                context.ProjectPhysicalVolumes.Add(newEntity);
            }
        }

        await context.SaveChangesAsync();

        var recordsImported = mergedByVolumeId.Count;
        iteration.RecordCount = recordsImported;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new DataImportResult(true, recordsImported, null, iteration.Id);
    }

    private static string BuildPhysicalVolumeZeroImportDetails(
        int dataRowCount,
        int catalogCodeCount,
        int skippedNoEpCode,
        int skippedNotInCatalog)
    {
        return string.Join(
            Environment.NewLine,
            "Импортировано строк: 0.",
            string.Empty,
            $"Строк данных в файле (после заголовка): {dataRowCount}.",
            $"Уникальных кодов в справочнике PhysicalVolumes: {catalogCodeCount}.",
            $"Строк без распознанного шифра ЕП в ожидаемых колонках: {skippedNoEpCode}.",
            $"Строк, где шифр не найден в справочнике: {skippedNotInCatalog}.",
            string.Empty,
            "Каждая строка файла сопоставляется с записью каталога по полю Code (шифр работы: БЭР вида 01-01-001-01 или КФО2 вида KQ.02.160).",
            "Если много строк «без распознанного шифра», проверьте, что в ожидаемых колонках есть именно такие коды (а не только наименование), и что открыт лист «Список работ» с колонкой «Код по каталогу» / «Шифр по каталогу».",
            "Если в базе только демонстрационные коды (например FO-001), а в файле — полные шифры БЭР или КФО2, совпадений не будет, пока в каталог не загружен полный перечень работ.",
            "Проверьте справочник «Физические объёмы» в приложении или выполните миграцию с полными данными каталога.");
    }

    private async Task<DataImportResult> ImportLaborIntensitiesAsync(int projectId, int versionId, string filePath, string description, bool addMissingToCatalog)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var iteration = new DataImportIteration
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            DataType = DataImportType.LaborIntensity,
            Description = description,
            SourceFileName = Path.GetFileName(filePath),
            ImportedAt = DateTime.UtcNow,
        };
        context.DataImportIterations.Add(iteration);
        await context.SaveChangesAsync();

        // Справочник «Трудоёмкость» — единственный источник связи:
        // (1) DocumentTypeName из Excel-строки проекта → LaborIntensity,
        // (2) LaborIntensity.IicWorkDirectionId → ProjectLaborIntensity.WorkDirectionId.
        // Никакого авто-создания направлений (TF-NNN) или каталога из файла проекта
        // больше не происходит: это приводило к появлению мусорных записей
        // (направлений/типов документа), не согласованных с главным справочником.
        var laborCatalogRows = await context.LaborIntensities.ToListAsync();
        var laborByDocumentType = new Dictionary<string, LaborIntensity>(StringComparer.OrdinalIgnoreCase);
        foreach (var li in laborCatalogRows)
        {
            var key = (li.DocumentTypeName ?? string.Empty).Trim();
            if (key.Length == 0)
            {
                continue;
            }

            // При коллизиях DocumentTypeName приоритет у первой встретившейся «активной» (не архив) записи.
            if (!laborByDocumentType.ContainsKey(key) || (laborByDocumentType[key].IsArchived && !li.IsArchived))
            {
                laborByDocumentType[key] = li;
            }
        }

        var importTimestamp = iteration.ImportedAt;
        // Staged: накапливаем сущности до первого SaveChanges, чтобы сохранить идентичный паттерн с импортом ФО.
        // Ключ уникальности в памяти — (DocumentationCode, WorkDirectionId) с поддержкой NULL направления.
        var stagedRows = new List<ProjectLaborIntensity>();
        var laborFileRowsAccepted = 0;
        var rowsWithoutCatalogMatch = 0;
        var rowsWithoutDirection = 0;
        var newCatalogEntriesCreated = 0;
        // Имена, не получившие соответствия. Отдельные множества для разных причин:
        // (а) типа документа нет в справочнике; (б) есть, но без привязки к IIC.
        // Возвращаем их в details, чтобы пользователь мог скопировать список и завести
        // недостающие позиции в каталоге без поиска по логам/Excel.
        var unmatchedDocTypes = new SortedSet<string>(StringComparer.Ordinal);
        var docTypesWithoutDirection = new SortedSet<string>(StringComparer.Ordinal);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = GetLaborIntensityImportWorksheet(workbook);

        if (worksheet.RangeUsed() is null)
        {
            await transaction.CommitAsync();
            iteration.RecordCount = 0;
            await context.SaveChangesAsync();
            return new DataImportResult(true, 0, null, iteration.Id);
        }

        if (!TryResolveLaborIntensityImportLayout(worksheet, out var layout))
        {
            await transaction.RollbackAsync();
            return new DataImportResult(false, 0,
                "Не удалось определить структуру файла трудоёмкости. Ожидаются колонки: «Наименование проектного направления», «Наименование типа документа TF», «Сумма Плановая трудоемкость, чч».",
                iteration.Id);
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
            for (var rowNumber = layout.FirstDataRowNumber; rowNumber <= lastRowNumber; rowNumber++)
            {
                if (!LaborIntensityImportRowHasAnyData(worksheet, rowNumber, layout))
                {
                    continue;
                }

                var name = GetWorksheetCellString(worksheet, rowNumber, layout.DocumentTypeColumn)?.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    docTypesInFile.Add(name);
                }
            }

            var created = 0;
            foreach (var docType in docTypesInFile)
            {
                if (laborByDocumentType.ContainsKey(docType))
                {
                    continue;
                }

                // Code: ограничение длины 100 (см. LaborIntensityConfiguration). В качестве
                // шифра берём сам «Тип документа» — это наиболее узнаваемая подсказка для
                // последующего ручного редактирования. Уникальность Code не требуется.
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

            var documentTypeName = GetWorksheetCellString(worksheet, rowNumber, layout.DocumentTypeColumn)?.Trim();
            if (string.IsNullOrWhiteSpace(documentTypeName))
            {
                continue;
            }

            // Разрешаем тип документа через справочник «Трудоёмкость». Направление берём из
            // найденной позиции каталога (LaborIntensity.IicWorkDirectionId). Если тип не найден
            // или в каталоге нет направления — строка принимается с NULL-привязкой, но
            // фиксируется счётчиком для последующей диагностики в интерфейсе.
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
                ? GetWorksheetCellString(worksheet, rowNumber, layout.BrandColumn)?.Trim()
                : null;
            var plannedHours = ParseDecimal(GetWorksheetCellString(worksheet, rowNumber, layout.PlannedHoursColumn));
            var actualHours = layout.ActualHoursColumn > 0
                ? ParseDecimal(GetWorksheetCellString(worksheet, rowNumber, layout.ActualHoursColumn))
                : 0m;
            var notes = layout.NotesColumn > 0
                ? GetWorksheetCellString(worksheet, rowNumber, layout.NotesColumn)?.Trim()
                : null;

            // DocumentationCode формируем из (DocumentTypeName, Brand): одинаковый тип документа
            // для разных «марок» (3U0000-ОП-АТХ1 vs 4L0000-…) — это разные строки в файле и в БД.
            var documentationCode = string.IsNullOrWhiteSpace(brand)
                ? documentTypeName
                : $"{documentTypeName}|{brand}";

            // SQL Server unique-индекс на nvarchar — ограничение длины ~450 символов
            // (DocumentationCode настроен как 100, см. ProjectLaborIntensityConfiguration).
            // Подстраховка от превышения — обрезаем (теряется уникальность только в патологических случаях).
            if (documentationCode.Length > 100)
            {
                documentationCode = documentationCode[..100];
            }

            var entity = new ProjectLaborIntensity
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                DocumentationCode = documentationCode,
                WorkDirectionId = laborItem?.IicWorkDirectionId,
                LaborIntensityId = laborItem?.Id,
                PlannedHours = plannedHours,
                ActualHours = actualHours,
                Brand = brand,
                Notes = notes,
                CreatedAt = importTimestamp,
            };

            stagedRows.Add(entity);
            laborFileRowsAccepted++;
        }

        // Схлопываем дубли по натуральному ключу (DocumentationCode, WorkDirectionId).
        // Строки с WorkDirectionId = NULL не схлопываются (уникальный индекс фильтрованный),
        // поэтому каждую такую запись оставляем как отдельную — иначе потеряем данные проекта.
        var mergedByNaturalKey = new Dictionary<(string DocumentationCode, int WorkDirectionId), ProjectLaborIntensity>();
        var unmatchedRows = new List<ProjectLaborIntensity>();
        foreach (var entity in stagedRows)
        {
            if (entity.WorkDirectionId is int wdId)
            {
                mergedByNaturalKey[(entity.DocumentationCode, wdId)] = entity;
            }
            else
            {
                unmatchedRows.Add(entity);
            }
        }

        var totalToPersist = mergedByNaturalKey.Count + unmatchedRows.Count;

        if (mergedByNaturalKey.Count > 0)
        {
            var existingInDb = await context.ProjectLaborIntensities
                .Where(li => li.ProjectId == projectId
                          && li.ProjectAnalysisVersionId == versionId
                          && li.WorkDirectionId != null)
                .ToDictionaryAsync(li => (li.DocumentationCode, li.WorkDirectionId!.Value));

            foreach (var entity in mergedByNaturalKey.Values)
            {
                var key = (entity.DocumentationCode, entity.WorkDirectionId!.Value);
                if (existingInDb.TryGetValue(key, out var tracked))
                {
                    tracked.PlannedHours = entity.PlannedHours;
                    tracked.ActualHours = entity.ActualHours;
                    tracked.Brand = entity.Brand;
                    tracked.Notes = entity.Notes;
                    tracked.LaborIntensityId = entity.LaborIntensityId;
                    // Как при импорте физ. объёмов: привязываем строку к итерации для отката.
                    tracked.CreatedAt = importTimestamp;
                    tracked.ModifiedAt = importTimestamp;
                }
                else
                {
                    context.ProjectLaborIntensities.Add(entity);
                }
            }
        }

        foreach (var entity in unmatchedRows)
        {
            context.ProjectLaborIntensities.Add(entity);
        }

        if (totalToPersist > 0)
        {
            await context.SaveChangesAsync();
        }

        iteration.RecordCount = totalToPersist;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        var details = new List<string>();
        if (newCatalogEntriesCreated > 0)
        {
            details.Add($"В справочник «Трудоёмкость» добавлено {newCatalogEntriesCreated} новых позиций из файла (без привязки к направлению IIC и без норм — проверьте карточки в справочнике).");
        }
        if (laborFileRowsAccepted > 0 && laborFileRowsAccepted != totalToPersist)
        {
            details.Add($"В базу записано {totalToPersist} строк (одинаковые «тип документа + марка» в одной версии схлопываются до одной записи).");
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
        return new DataImportResult(true, totalToPersist, null, iteration.Id, detailsText);
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
                var text = GetWorksheetCellString(worksheet, r, c);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var t = text.Trim();
                if (directionCol < 0 && t.Contains("проектного направления", StringComparison.OrdinalIgnoreCase))
                {
                    directionCol = c;
                    headerHits++;
                }
                else if (docTypeCol < 0 && (
                    t.Contains("тип документа", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("типа документа", StringComparison.OrdinalIgnoreCase)))
                {
                    docTypeCol = c;
                    headerHits++;
                }
                else if (plannedCol < 0 && t.Contains("плановая трудоемкость", StringComparison.OrdinalIgnoreCase))
                {
                    plannedCol = c;
                    headerHits++;
                }
                else if (actualCol < 0 && (
                    t.Contains("фактически списано", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("фактическая трудоемкость", StringComparison.OrdinalIgnoreCase)))
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

        return exact ?? partial ?? GetFirstWorksheetWithData(workbook);
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

            if (!string.IsNullOrWhiteSpace(GetWorksheetCellString(worksheet, rowNumber, c)))
            {
                return true;
            }
        }

        return false;
    }

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
            columnMap = PhysicalVolumeColumnMap.FromEpCatalogColumn(epCol);
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
        /// </summary>
        public static PhysicalVolumeColumnMap FromEpCatalogColumn(int epCol)
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
                Quantity = e + 11,
                EstimationBasis = e + 12,
                SplitFacilityFromFbs = true,
            };
        }
    }

    /// <summary>
    /// Несколько строк каталога могут иметь один <see cref="PhysicalVolume.Code"/> (разные
    /// <see cref="PhysicalVolume.VolumeTypeId"/>). Старый словарь оставлял только первую строку
    /// из выборки БД — часто без ClassifierId. Импорт выбирает кандидата явно.
    /// </summary>
    private ILookup<string, PhysicalVolume> BuildCodeLookup(
        IReadOnlyList<PhysicalVolume> rows,
        string catalogName)
    {
        var pairs = new List<(string Key, PhysicalVolume Pv)>();
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
                "Catalog {Catalog}: {Count} code keys map to multiple rows (e.g. КФО/БЭР); import picks row with ClassifierId, then longest Code",
                catalogName,
                duplicateGroups);
        }

        return pairs.ToLookup(p => p.Key, p => p.Pv, StringComparer.OrdinalIgnoreCase);
    }

    private static PhysicalVolume? PickBestPhysicalVolumeCandidate(IReadOnlyList<PhysicalVolume> candidates)
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
            .ThenBy(c => c.VolumeTypeId)
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
        IReadOnlyDictionary<int, PhysicalVolume> pvByIdSnapshot)
    {
        var updated = 0;
        foreach (var pvId in importedPhysicalVolumeIds.Distinct())
        {
            if (!pvByIdSnapshot.TryGetValue(pvId, out var snap))
            {
                continue;
            }

            if (snap.ClassifierId.HasValue)
            {
                continue;
            }

            var resolved = ResolveClassifierIdFromAncestorChain(pvId, pvByIdSnapshot);
            if (!resolved.HasValue)
            {
                continue;
            }

            var tracked = await context.PhysicalVolumes.FirstOrDefaultAsync(p => p.Id == pvId);
            if (tracked is null || tracked.ClassifierId.HasValue)
            {
                continue;
            }

            tracked.ClassifierId = resolved.Value;
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
        IReadOnlyDictionary<int, PhysicalVolume> byId)
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
        ILookup<string, PhysicalVolume> physicalVolumes,
        string raw,
        out PhysicalVolume? physicalVolume)
    {
        foreach (var key in ExpandEpCodeLookupKeys(raw))
        {
            if (!physicalVolumes.Contains(key))
            {
                continue;
            }

            var list = physicalVolumes[key].ToList();
            physicalVolume = PickBestPhysicalVolumeCandidate(list);
            if (physicalVolume is not null)
            {
                return true;
            }
        }

        physicalVolume = null;
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

        return 0m;
    }

    private static string FormatImportExceptionMessage(Exception ex)
    {
        var inner = ex.InnerException;
        if (ex is DbUpdateException && inner is not null)
        {
            return $"{inner.Message} ({inner.GetType().Name})";
        }

        if (inner is not null)
        {
            return $"{ex.Message} — {inner.Message}";
        }

        return ex.Message;
    }

    private static async Task<int> DeleteImportedRecordsAsync(AppDbContext context, DataImportType dataType, int projectId, int versionId, DateTime importedAt)
    {
        return dataType switch
        {
            DataImportType.PhysicalVolume => await DeletePhysicalVolumeRecordsAsync(context, projectId, versionId, importedAt),
            DataImportType.LaborIntensity => await DeleteLaborIntensityRecordsAsync(context, projectId, versionId, importedAt),
            _ => 0,
        };
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

    private static async Task<int> DeleteLaborIntensityRecordsAsync(AppDbContext context, int projectId, int versionId, DateTime importedAt)
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

    /// <summary>
    /// Aggregates rows that share the same catalog code (same <see cref="PhysicalVolume"/> id) before save.
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
            PhysicalVolume catalogEntry)
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

        public ProjectPhysicalVolume ToNewEntity(int projectId, int versionId, int physicalVolumeId, DateTime createdAt)
        {
            return new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = versionId,
                PhysicalVolumeId = physicalVolumeId,
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
