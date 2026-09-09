# ISS-013: Декомпозиция god-классов backend

**Статус:** 🟠 Частично (2026-09-09)
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
- Excel ФО / оборудование / трудоёмкость вынесены в отдельные сервисы; фасад `ProjectExcelDataImportService` только делегирует.
- 2026-09-09: с `IPerformanceCalculationEngine` сняты мёртвые фасады; тесты Application переведены на `RecalculatePlanHours` / `ComputeForecastHours` / `ComputeWeightedZ*`; неиспользуемый `engine` убран со stored-hours путей `ProjectsPageGridBuilder`. `ForecastHoursAggregator` остаётся в дереве без вызовов из движка.

## Что осталось
1. GridBuilder → отдельные типы `ForecastGridBuilder` / `FactGridBuilder` / `CompareGridBuilder` (сейчас already partial `.Forecast` / `.Fact` / `.Compare` + stored-hours helpers).
2. Export writers по доменам (не второй PDF-движок).
3. Резать `VisualReportService` / крупные parse-partials до ≲600 строк.
4. Удалить или спрятать неиспользуемый `ForecastHoursAggregator`, если не понадобится тестам формул.

AC «файл ≲600» **не закрыт**. Поведение покрыто тестами Import / GridBuilder / persist.

## Связанные
- ISS-009, ISS-010, ISS-015; `performance-calculation-engine-ssot.mdc`
