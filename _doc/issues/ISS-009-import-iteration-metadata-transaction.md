# ISS-009: Метаданные итерации импорта пишутся вне транзакции импорта

**Статус:** ✅ Fixed (2026-09-08; проверено в цикле коррекции архитектуры)
**Категория:** DataImport / Transactions
**Приоритет:** P0 (Medium-High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §2 (A-06)

## Контекст (исходная проблема)
`ProjectExcelDataImportService.ImportProjectDataAsync` коммитил импорт, затем `EnrichIterationMetadataAsync` открывал **новый** `AppDbContext` через `IDbContextFactory` и отдельным `SaveChangesAsync` дописывал `SourceFileHash`, `SourceFileName`, `ImportedByUserId`.

При сбое между коммитом и enrich итерация оставалась без аудит-метаданных — дедупликация по хэшу и аудит «кто загрузил» молча деградировали.

## Как исправлено
- Метаданные (`ImportSourceMetadata`: hash, имя файла, пользователь) передаются **внутрь** импорта и записываются при создании итерации в том же `AppDbContext` и той же транзакции, что и данные:
  `backend/EngineeringPerformance.Infrastructure/Import/ImportIterationRecorder.cs` (`CreateIterationAsync` заполняет `SourceFileHash` / `SourceFileName` / `ImportedByUserId`), вызов из `PhysicalVolumeExcelImportService.Persist.cs`, `EquipmentImportService.Excel.cs`, `LaborIntensityImportService.Excel.cs` и JSON-ingest partial'ов.
- Транзакция и retry — `ImportTransactionRunner` (`Infrastructure/Import/ImportTransactionRunner.cs`); отдельного пост-коммитного enrich больше нет.
- Запись в журнал действий (`IUserActionLogger`) — после успешного commit (не влияет на атомарность данных).

## Acceptance Criteria
- [x] Метаданные итерации атомарны с данными (одна транзакция).
- [x] Нет пост-коммитного `EnrichIterationMetadataAsync` → «осиротевшая» итерация невозможна по конструкции (отдельный тест на сбой enrich не нужен).
- [x] `dotnet test` зелёный (Infrastructure.Tests `Import/*`, Api.Tests import-сценарии).

## Файлы
- `backend/EngineeringPerformance.Infrastructure/Import/ImportIterationRecorder.cs`
- `backend/EngineeringPerformance.Infrastructure/Import/ImportTransactionRunner.cs`
- `backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.Api.cs` (фасад)

## Связанные
- ISS-006 (импорт LI), ISS-013 (декомпозиция импорта), отчёт аудита
