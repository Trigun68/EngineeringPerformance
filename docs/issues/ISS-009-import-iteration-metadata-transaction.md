# ISS-009: Метаданные итерации импорта пишутся вне транзакции импорта

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** DataImport / Transactions
**Приоритет:** P0 (Medium-High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §2 (A-06)

## Контекст
`ProjectExcelDataImportService.ImportProjectDataAsync` (`ProjectExcelDataImportService.Api.cs:52-96`) коммитит импорт, затем `EnrichIterationMetadataAsync` (стр. 149-165) открывает **новый** `AppDbContext` через `IDbContextFactory` и отдельным `SaveChangesAsync` дописывает `SourceFileHash`, `SourceFileName`, `ImportedByUserId`.

При сбое между коммитом и enrich итерация остаётся без аудит-метаданных (хэш/файл/пользователь) — дедупликация по хэшу и аудит «кто загрузил» молча деградируют.

## Решение (направление)
- Писать метаданные в той же транзакции, что и данные итерации (передать hash/fileName/userId в `ImportFromFileAsync` / записать до коммита), либо
- явный outbox/retry с логированием сбоя enrich.

## Acceptance Criteria
- [ ] Метаданные итерации атомарны с данными (одна транзакция) или есть гарантированный retry + alert.
- [ ] Тест: сбой enrich → нет «осиротевшей» итерации без метаданных.
- [ ] `dotnet test` зелёный.

## Файлы
- `backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.Api.cs`
- `backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.cs`

## Связанные
- ISS-006 (импорт LI), отчёт аудита
