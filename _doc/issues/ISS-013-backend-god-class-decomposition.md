# ISS-013: Декомпозиция god-классов backend

**Статус:** 🟠 Частично (2026-09-08)
**Категория:** Refactoring / SRP
**Приоритет:** P1 (High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §3

## Контекст
После цикла mixed `V_g` / DocSet / persist цифры:

| Файл | Строк (2026-09-08) |
|------|---------------------|
| `PhysicalVolumeExcelImportService.Parse.cs` | ~668 |
| `ProjectsPageGridBuilder.cs` | 1288 |
| `VisualReportService.cs` | 1143 |
| `ProjectExportService.cs` | 1028 |
| `ProjectExcelDataImportService.JsonIngest.cs` | 635 |
| Остальные partials импорта | ≤571 |

Импорт уже 6 partials; публичный `IProjectImportService` сохранён.

## Что сделано в этом цикле
- Поведение (mixed `V_g`, плановые объёмы, DocSet) закреплено тестами **до** механической нарезки.
- VisualReportService / FactNormAnalysisService **не** резали в том же заходе.
- WP-B4 stage 1: Excel ФО вынесен в `PhysicalVolumeExcelImportService`; общие хелперы — `ImportHelpers`.

## Что осталось
1. Equipment / LaborIntensity / JsonIngest — следующие стадии нарезки импорта.
2. GridBuilder → `ForecastGridBuilder` / `FactGridBuilder` / `CompareGridBuilder`.
3. Export writers по доменам (не второй PDF-движок).

AC «файл ≲600» **не закрыт**. Поведение покрыто тестами Import / GridBuilder / persist.

## Связанные
- ISS-009, ISS-010, ISS-015; `performance-calculation-engine-ssot.mdc`
