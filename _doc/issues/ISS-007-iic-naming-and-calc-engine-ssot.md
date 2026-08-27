# ISS-007 — Формат имён IIC и SSOT движка расчётов

**Дата:** 2026-07-22 · **Обновлено:** 2026-07-23  
**Статус:** выполнено (2026-07-23)  
**Правила:** `.cursor/rules/iic-group-naming.mdc`, `.cursor/rules/performance-calculation-engine-ssot.mdc`  
**Стек:** только `backend/` + `client/`. WPF (`EngineeringPerformance.App`) **вне плана** — исторический источник, не эталон и не целевой код.

## 1. Единый формат имён групп IIC

**Целевой формат:** `код — наименование (ед.)`  
**Хелперы:** `FormatCodeNameWithResolvedUnit` / `formatIisNorm`

| P | Место | Статус |
|---|--------|--------|
| High | `ProjectForecastService` Name/HierarchyPath | done |
| High | `IicWorkDirectionNormService` | done |
| High | `HierarchicalSelect` / DirectoryPage parent IIC | done |
| High | `createPerformanceColumns` + `getDirectionName` | done |
| High | `PlanningTab` / `ForecastPage` | done |
| Med | `DetailForecastSummaryTableBuilder` | done |
| Med | `DirectoryFormFields` | done |
| Med | `FormatClassifierName` aligned with formatter | done |
| Low | `AssessmentTab` | done |

---

## 2. Единый движок расчётов

| P | Место | Статус |
|---|--------|--------|
| Critical | `BuildFactGrids` → `CalculateFactAssessment`; `FactAssessmentRowsCalculator` удалён | done |
| High | LI recalc через `IPerformanceCalculationEngine.RecalculateLaborIntensities` | done |
| High | Forecast hours через `ComputePpvForecastHours` / `AggregateForecastHoursByDirection` | done |
| High | Weighted Z через `ComputeWeightedZFromPerformanceRows` | done |
| Med | `EquipmentVolumeAggregator` / forecast tree rollup | deferred (display/merge helpers) |
| Low | `AppendPerformanceTotals` | n/a (сумма DTO) |

### API движка

`CalculatePerformance` · `CalculateFactAssessment` · `ComputeForecastHours` · `ComputeForecastBaseHours` · `RecalculateLaborIntensities` · `ComputePpvForecastHours` · `AggregateForecastHoursByDirection` · `ComputeWeightedZFromPerformanceRows`

Внутренние хелперы (`LaborIntensityCalculator`, `ForecastHoursAggregator`, `WeightedZScoreCalculator`, `ForecastCalculation`) — только для движка и unit-тестов формул; Infrastructure вызывает **только** `IPerformanceCalculationEngine`.

---

## 3. Порядок выполнения

1. ~~P1~~ Fact → движок  
2. ~~P2~~ Имена IIC  
3. ~~P3~~ LI + forecast facade  
4. ~~P4~~ Weighted Z facade  

Остаток (опционально): свернуть `EquipmentVolumeAggregator` и parent rollup полностью внутрь движка; сделать хелперы `internal`.
