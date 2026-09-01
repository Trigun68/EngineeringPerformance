# Алгоритм прогнозирования трудозатрат

**Дата обновления:** 2026-09-01  
Реализация: `IPerformanceCalculationEngine`, `LaborIntensityCalculator`, `ForecastHoursAggregator`, `ProjectsPageGridBuilder.BuildPlanPerformance`  
Архитектура: [_doc/architecture/hours-and-z-ssot.md](../../_doc/architecture/hours-and-z-ssot.md)

---

## Параметрические нормы N(V)

Норма производительности задаётся формулой `N(V)` (12 типов в `FormulaTemplates.cs`). По умолчанию после миграции — линейная: `N(V) = max(0, a − b·V)`, где `a = Performance`, `b = 0`.

Отклонение по умолчанию — мультипликативное: `σ_rel = StandardDeviation / Performance` (хранится в `DeviationCoefficientsJson`).

### Прогнозная трудоёмкость (LI)

Мультипликативная модель (Linear и остальные, кроме логнормального распределения):

```
LI(z) = N · (1 + σ_rel · z) · V_eff
```

**Логнормальное распределение** (`FormulaType.KimCurve`, `DeviationModelType.Lognormal`): производительность не зависит от V (`N = P = a`), σ — в лог-пространстве:

```
μ = ln(P)
P(z) = e^(μ + z · σ)
LI(z) = P(z) · V_eff
```

При z = 0 часы совпадают с Constant: `P · V`. Существующие Linear-нормы не пересчитываются.

`N` берётся по сценарию версии (см. ниже). `z` — `ZBase` / `ZOptimistic` / `ZPessimistic` из `ProjectVersionPlanningParameters` (входы в часы, не колонка оценки).

Только строки ФО и оборудования с оценкой «Да». Оборудование в том же `V_g`, что ФО (`ByProjectIicGroup` / `ByObjectIicGroup`); `ByEachValue` — `N(V_i)` на линии. GET читает сохранённые колонки.

`Estimate` и `StandardDeviation` — для log-модели Z-оценки (`Z = (ln(P_факт) − μ) / σ`). Для логнормального распределения `Estimate = μ`, `StandardDeviation = σ` (лог); ветка Z в движке не нужна.

Линейная формула `ФО × (Performance + Z × StdDev)` на web-пути не применяется.

---

## Сценарии планирования (`PlanningScenario`)

| Сценарий | Алгоритм |
|---|---|
| `ByEachValue` | `N_i = N(V_i)` на строке |
| `ByProjectIicGroup` | `V_g = ΣV_i` по группе IIC на проект, `N_g = N(V_g)`, доля `V_i/V_g` |
| `ByObjectIicGroup` | то же внутри объекта (`FacilityCode`) |

Пересчёт в БД: `POST .../versions/{id}/recalculate` (и те же триггеры: импорт, CRUD ФО/оборудования, смена Z/сценария, нормы ИИС). Гриды читают сохранённые `LaborIntensity*`.

---

## Источники данных

| Источник | Описание |
|---|---|
| `ProjectPhysicalVolumes` | ФО версии + `LaborIntensity*`; GET суммирует колонки, только `IsAssessment = true` |
| `ProjectEquipments` | Оборудование версии + `LaborIntensity*` (в том же `V_g`, что ФО) |
| `AnalysisVersionSnapshots` | Снимок норм на дату версии (persist) |
| `ProjectVersionPlanningParameters` | Z-параметры по группе IIC (persist) |
| `ProjectLaborIntensity.PlannedHours` | Часы **факта**; не fallback плана |

---

## Правила расчёта

- Z-параметры планирования — входы в `LI(z)`, не штамп колонки Z.
- Оценка «Нет» не входит в `V_g` и не получает LI.
- При отсутствии сохранённых часов на листе дерева отчёта прогноза — 0 (без live N(V) и без PLI).
- Для нелистовых узлов часы = сумма листьев (уже сохранённых по сценарию на persist).
- Z-оценка плана: `CalculatePerformance` от сохранённых часов выбранного варианта; свёртка `Σ(Z×часы)/Σ(часы)`.
- Применяется на вкладках «Итого», «По объектам», «Детально» (`ForecastPanel`, `PlanningTab`), сравнении, отчёте, аналитике Plan/Forecast.
- Табличный экспорт PDF/Excel: [forecast-export.md](forecast-export.md).

---

## История изменений алгоритма

| Дата | Версия | Изменение |
|---|---|---|
| до 2026-05-06 | — | Лог-нормальное пространство: `base = ФО × exp(Estimate)` |
| 2026-05-06 | — | Линейный алгоритм: `ФО × (Performance + Z × StdDev)` |
| 2026-06-08 | — | Параметрические нормы: `LI = N(V)·(1+σ·z)·V_eff`; сценарии планирования |
| **2026-08-31** | **текущий** | Тип «Логнормальное распределение» (`KimCurve`): `LI(z) = e^(μ+zσ)·V`; Z без новой ветки |
| **2026-08-24** | — | Часы плана: persist ФО+оборудование, GET только из БД; Z плана = `CalculatePerformance(stored)`; факт = `PlannedHours` |
| 2026-08-23 | — | Live N(V)×сценарий на вкладках (снято 2026-08-24) |
