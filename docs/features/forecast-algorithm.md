# Алгоритм прогнозирования трудозатрат

**Дата обновления:** 2026-08-24  
Реализация: `IPerformanceCalculationEngine`, `LaborIntensityCalculator`, `ForecastHoursAggregator`, `ProjectsPageGridBuilder.BuildPlanPerformance`  
Архитектура: [_doc/architecture/hours-and-z-ssot.md](../../_doc/architecture/hours-and-z-ssot.md)

---

## Параметрические нормы N(V)

Норма производительности задаётся формулой `N(V)` (8 типов в `FormulaTemplates.cs`). По умолчанию после миграции — линейная: `N(V) = max(0, a − b·V)`, где `a = Performance`, `b = 0`.

Отклонение — мультипликативное: `σ = StandardDeviation / Performance` (хранится в `DeviationCoefficientsJson`).

### Прогнозная трудоёмкость (LI)

```
LI(z) = N · (1 + σ · z) · V_eff
```

`N` берётся по сценарию версии (см. ниже). `z` — `ZBase` / `ZOptimistic` / `ZPessimistic` из `ProjectVersionPlanningParameters` (входы в часы, не колонка оценки).

Только строки ФО с оценкой «Да». Оборудование: `N(V_eq)×z` без группировки сценария.

`Estimate` и legacy `Performance`/`StandardDeviation` — для log-модели Z-оценки; прогноз LI использует параметрическую модель.

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
| `ProjectEquipments` | Оборудование версии + `LaborIntensity*` (без группировки сценария) |
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
| **2026-08-24** | **текущий** | Часы плана: persist ФО+оборудование, GET только из БД; Z плана = `CalculatePerformance(stored)`; факт = `PlannedHours` |
| 2026-08-23 | — | Live N(V)×сценарий на вкладках (снято 2026-08-24) |
