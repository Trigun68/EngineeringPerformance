# Алгоритм прогнозирования трудозатрат

Дата обновления: 2026-06-08  
Реализация: `FormulaTemplates`, `INormCalculator`, `IDeviationCalculator`, `ForecastCalculation.ComputeForecastHours`, `LaborIntensityCalculator`, `ProjectsPageGridBuilder`

---

## Параметрические нормы N(V)

Норма производительности задаётся формулой `N(V)` (8 типов в `FormulaTemplates.cs`). По умолчанию после миграции — линейная: `N(V) = max(0, a − b·V)`, где `a = Performance`, `b = 0`.

Отклонение для прогнозной трудоёмкости — мультипликативное: `σ = StandardDeviation / Performance` (хранится в `DeviationCoefficientsJson`).

### Прогнозная трудоёмкость (LI)

```
LI = N(V) · (1 + σ · z) · V_eff
```

где `z` — `ZBase` / `ZOptimistic` / `ZPessimistic` из `ProjectVersionPlanningParameters`.

`Estimate` и legacy `Performance`/`StandardDeviation` сохраняются для Z-score факт-оценки (log-модель); прогноз LI использует параметрическую модель независимо.

---

## Сценарии планирования (`PlanningScenario`)

| Сценарий | Алгоритм |
|---|---|
| `ByEachValue` | LI per-row по `N(V_i)`, `V_i` |
| `ByProjectIicGroup` | `V_g = ΣV_i`, `N(V_g)`, распределение `LI_g · (V_i/V_g)` |
| `ByObjectIicGroup` | то же, группировка `(FacilityCode, IicGroupId)` |

Результаты сохраняются в `ProjectPhysicalVolumes.LaborIntensityBase/Pessimistic/Optimistic`. Пересчёт: `POST .../versions/{id}/recalculate`.

---

## Источники данных

| Источник | Описание |
|---|---|
| `ProjectPhysicalVolumes` | Фактические физические объёмы версии проекта (`IsAssessment = true`) |
| `AnalysisVersionSnapshots` | Снимок норм на дату создания версии проекта (уникален по `(VersionId, WorkDirectionId)`) |
| `IicWorkDirectionNorm.Performance` | Нормативная производительность (ч/ед) |
| `IicWorkDirectionNorm.StandardDeviation` | Стандартное отклонение производительности |
| `ProjectVersionPlanningParameters` | Z-сценарии по `(VersionId, IicWorkDirectionId)`: `ZBase`, `ZOptimistic`, `ZPessimistic` |

---

## Формулы (листовые направления ИИС, оценка «Да»)

Единая структура для всех трёх сценариев; различается только Z из параметров планирования:

### Базовый сценарий
```
Базовый = ФактФО × (Performance + ZBase × StdDev)
```

### Оптимистичный сценарий
```
Оптимистичный = ФактФО × max(0, Performance + ZOptimistic × StdDev)
```

### Пессимистичный сценарий
```
Пессимистичный = ФактФО × (Performance + ZPessimistic × StdDev)
```

Значения по умолчанию (seed): `ZPessimistic = 0.5`, `ZBase = 0`, `ZOptimistic = −0.5`.

---

## Правила расчёта

- Z берётся из `ProjectVersionPlanningParameters` для каждой дисциплины IIC; при отсутствии записи — значения по умолчанию (`PlanningZParameters.Default`).
- Если у направления нет нормы (`Performance = null` или `= 0`) — используются фактические трудозатраты (`ProjectLaborIntensities.PlannedHours`) как запасное значение; все три сценария совпадают.
- Если `StdDev = 0` или отсутствует — все три сценария равны `ФактФО × Performance` (или fallback на LI).
- Для нелистовых узлов часы каждого сценария = сумма часов дочерних листьев.
- `ФактФО` агрегируется снизу вверх по дереву направлений ИИС с учётом коэффициентов.
- Применяется на вкладках «Итого», «По объектам», «Детально» в web (`ForecastPanel`, `PlanningTab`).

---

## История изменений алгоритма

| Дата | Версия | Изменение |
|---|---|---|
| до 2026-05-06 | — | Лог-нормальное пространство: `base = ФО × exp(Estimate)`, сценарии через `exp(Estimate ± 0.5 × StdDev)` |
| 2026-05-06 | — | Линейный алгоритм: `base = ФО × Performance`, `opt/pes = ФО × (Performance ∓ 0.5 × StdDev)` |
| **2026-06-08** | **текущий** | **Параметрические нормы:** LI = N(V)·(1+σ·z)·V_eff; сценарии планирования; сохранённые LI на PPV |
| 2026-06-08 | — | Единая формула Z per discipline: все сценарии `ФО × (Performance + Z × StdDev)`, Z из `ProjectVersionPlanningParameters` |
