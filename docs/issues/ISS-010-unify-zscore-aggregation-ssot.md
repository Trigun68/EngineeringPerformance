# ISS-010: Унификация агрегации Z-score (завершение SSOT движка)

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** Architecture / Analytics / SSOT
**Приоритет:** P0 (High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §2 (A-03, A-04), §4 (C-01)

## Контекст
Взвешенный Z реализован параллельно в четырёх местах с разной семантикой:
- `PerformanceCalculationEngine` / pipeline — взвешивание по часам (SoT).
- `ZTrendEngine.AggregateSameDay` (стр. 84-103) — uniform/hours по настройке, своя реализация Σ(Z×w)/Σw.
- `DetailFactSummaryTableBuilder` (стр. 92-93, 162-164) — Z×fact-часы в статическом билдере.
- `GroupAnalyticsService` (стр. 93-102) — Z×часы по проектам группы.
- `VersionCompareMerge.AverageNullable` (стр. 71-75) — **среднее арифметическое Z без весов** (compare «Детально») — расходится с взвешенным подходом.

Дополнительно `ForecastCalculation.ResolvePlanningZ` вызывается напрямую из сервисов в обход фасада: `ProjectsPageGridBuilder` (7 сайтов), `ProjectForecastService` (2), `PerformanceReportService` (1).

## Решение (направление)
1. Зафиксировать единый подход к агрегации Z (по умолчанию — взвешивание по часам) в доке + ADR при необходимости.
2. Вынести примитив взвешенного среднего и агрегации выборок в `IPerformanceCalculationEngine`; свести четыре пути к нему.
3. Экспонировать разрешение planning-Z через фасад движка; убрать прямые `ForecastCalculation.*` из сервисов.
4. Параметрический тест: одни данные → одинаковый Z во всех представлениях (тренд, compare, групповой анализ, сводные таблицы).

## Acceptance Criteria
- [ ] Нет самостоятельных реализаций Σ(Z×w)/Σw вне движка.
- [ ] Compare «Детально» использует взвешенный Z (или задокументировано осознанное исключение).
- [ ] Нет прямых вызовов `ForecastCalculation` из Infrastructure/Application сервисов.
- [ ] Согласованность подтверждена тестом; `dotnet test` зелёный.

## Файлы
- `backend/EngineeringPerformance.Application/Services/PerformanceCalculationEngine.cs` (+ интерфейс)
- `backend/EngineeringPerformance.Application/Analytics/ZTrendEngine.cs`
- `backend/EngineeringPerformance.Application/Analytics/DetailFactSummaryTableBuilder.cs`
- `backend/EngineeringPerformance.Infrastructure/Analytics/GroupAnalyticsService.cs`
- `backend/EngineeringPerformance.Infrastructure/Analytics/VersionCompareMerge.cs`
- `backend/EngineeringPerformance.Infrastructure/Analytics/ProjectsPageGridBuilder.cs`, `ProjectForecastService.cs`, `PerformanceReportService.cs`

## Связанные
- ISS-007 (SSOT движка), `performance-calculation-engine-ssot.mdc`
