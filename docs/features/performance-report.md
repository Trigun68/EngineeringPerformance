# Отчёт производительности проекта (вкладка «Отчет»)

**Дата:** 2026-08-24  
**Код:** `PerformanceReportService`, `PerformanceReportController`, `client/src/features/projects/report/`

## Назначение

Визуальный отчёт по инженерной производительности проекта: средневзвешенный Z плана и факта, шкала интерпретации, динамика по версиям, top-3 ± дисциплин, дерево «Направление», PDF.

Архитектура часов и Z: [_doc/architecture/hours-and-z-ssot.md](../../_doc/architecture/hours-and-z-ssot.md).

## Резолв версий (внутреннее правило)

1. **План-метрики:** версия факта с `IsPrimary`; если нет — версия плана с `IsPrimary`; иначе latest Fact → latest Plan.
2. **Факт-метрики:** только Fact-direction (`IsPrimary` → latest). Часы — **Σ PlannedHours только по строкам с `WorkDirectionId`**. `ActualHours` в KPI и Z не входит. Неотнесённые не входят в выделенные часы; 100% неотнесённых → «—».
3. Объём в дереве: факт, иначе план (в UI не комментируется).
4. Трудоёмкость плана = GrandTotals прогноза (сохранённые `LaborIntensity*` ФО и оборудования). Z плана = `BuildPlanPerformance` → `CalculatePerformance` (не штамп ZBase). Свёртка KPI — `ComputeWeightedZFromPerformanceRows`.

UI не передаёт `planVersionId` / `factVersionId`.

## API

| Метод | Путь | Право |
|-------|------|--------|
| GET | `/api/projects/{projectId}/performance-report` | `Reports` Read |
| GET | `/api/projects/{projectId}/performance-report/export.pdf` | `Reports` Read |

Ответ: блоки `plan` / `fact`, `versionHistory`, `accuracy`, `insights`, merged `directions`, `normSourcesUsed`.

Z-математика только через `IPerformanceCalculationEngine`. Факт: `LaborHoursKind.Planned`.

## UI

- 5 KPI: крупные заголовки, значения тёмно-синие (`--ep-report-metric`).
- Шкала: план сверху, факт снизу.
- Дерево «Направление»: тёмная шапка; объём целым без ед.; произв. план/факт — 1 знак после запятой.
- Insights: top-3 по |Z| × subtree hours. Доля бара — только факт. часы (без fallback на план).
- Шапка: имена версий плана и факта. Шкала −3…+3 жирнее. График динамики: ось X = `createdAt`, план синий / факт красный, смена `normSnapshotKey` светлее.
