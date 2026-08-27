# ISS-010: Унификация агрегации Z-score (завершение SSOT движка)

**Статус:** ✅ Closed (2026-08-23)
**Категория:** Architecture / Analytics / SSOT
**Приоритет:** P0 (High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §2 (A-03, A-04), §4 (C-01)

## Контекст
Взвешенный Z был реализован параллельно с разной семантикой (uniform, среднее арифметическое, штамп ZBase).

## Решение (сделано)
1. Единая свёртка: `Σ(Z×hours)/Σ(hours)` в `WeightedZAggregator` / `IPerformanceCalculationEngine.ComputeWeightedZ`.
2. Тренд: `ZTrendEngine.AggregateSameDay` всегда по часам; `uniform` снят с API и UI.
3. Compare «Детально»: взвешенный Z, без arithmetic fallback.
4. Групповой анализ: `engine.ComputeWeightedZ`.
5. План: `BuildPlanPerformance` → `CalculatePerformance(live hours)`, не штамп ZBase.
6. Факт: только `PlannedHours`.
7. Документ SoT: `_doc/architecture/hours-and-z-ssot.md`.

## Acceptance Criteria
- [x] Нет самостоятельных реализаций Σ(Z×w)/Σw вне движка (кроме потокового `WeightedZAggregator` как примитива движка).
- [x] Compare «Детально» использует взвешенный Z.
- [x] Нет прямых `ForecastCalculation` для свёртки Z из Infrastructure-сервисов отчёта.
- [x] Согласованность: unit-тесты Compare / Report / ZTrend / PlanningScenario; `dotnet test`.

## Файлы
- `PerformanceCalculationEngine`, `ZTrendEngine`, `GroupAnalyticsService`, `VersionCompareMerge`
- `ProjectsPageGridBuilder.BuildPlanPerformance`, `PerformanceReportService`, `FactAssessmentPipeline`

## Связанные
- ISS-007 (SSOT движка), `performance-calculation-engine-ssot.mdc`
- `_doc/architecture/hours-and-z-ssot.md`
