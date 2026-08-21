# Отчёт производительности проекта (вкладка «Отчет»)

**Дата:** 2026-08-20  
**Код:** `PerformanceReportService`, `PerformanceReportController`, `client/src/features/projects/report/`

## Назначение

Визуальный отчёт по инженерной производительности проекта: средневзвешенный Z плана и факта, шкала интерпретации, динамика по версиям, top-3 ± дисциплин, дерево «Направление», PDF.

## Резолв версий (внутреннее правило)

1. **План-метрики** (Z план, труд план, произв. план, норма/σ/Z в дереве): версия факта с `IsPrimary`; если нет — версия плана с `IsPrimary`; иначе latest Fact → latest Plan.
2. **Факт-метрики** (Z факт, труд факт): только Fact-direction (`IsPrimary` → latest). Часы — **Σ ActualHours только по строкам с `WorkDirectionId`**. Неотнесённые не входят в KPI; 100% неотнесённых → «—». Подстановки плана в insights нет.
3. Объём в дереве: факт, иначе план (в UI не комментируется).
4. Трудоёмкость плана = GrandTotals прогноза (live N(V)×Z по ФО и оборудованию, ByEachValue). Z листа = ZBase параметров планирования; родитель и KPI — `ComputeWeightedZ` по часам. Не direction-volume × ZBase.

UI не передаёт `planVersionId` / `factVersionId`.

## API

| Метод | Путь | Право |
|-------|------|--------|
| GET | `/api/projects/{projectId}/performance-report` | `Reports` Read |
| GET | `/api/projects/{projectId}/performance-report/export.pdf` | `Reports` Read |

Ответ: блоки `plan` / `fact`, `versionHistory`, `accuracy`, `insights`, merged `directions`, `normSourcesUsed`.

Z-математика только через `IPerformanceService` / `PerformanceCalculationEngine` (`LaborHoursKind.Planned` | `Actual`).

## UI

- 5 KPI: крупные заголовки, значения тёмно-синие (`--ep-report-metric`).
- Шкала: план сверху, факт снизу.
- Дерево «Направление»: тёмная шапка; объём целым без ед.; произв. план/факт — 1 знак после запятой.
- Insights: top-3 по |Z| × subtree hours. Доля бара — только факт. часы (без fallback на план).
- Шапка: имена версий плана и факта. Шкала −3…+3 жирнее. График динамики: ось X = `createdAt`, план синий / факт красный, смена `normSnapshotKey` светлее.
