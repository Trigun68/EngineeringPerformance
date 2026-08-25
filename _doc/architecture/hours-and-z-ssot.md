# Единый SoT часов и Z-оценки

**Дата:** 2026-08-25  
**Код:** `IPerformanceCalculationEngine`, `LaborIntensityCalculator`, `LaborIntensityRecalculationService`, `ProjectsPageGridBuilder`, `FactAssessmentPipeline`, `PerformanceReportService`

Связанные: [business-logic.md](./business-logic.md), [forecast-algorithm.md](../../docs/features/forecast-algorithm.md), [performance-report.md](../../docs/features/performance-report.md), [version-comparison.md](../../docs/features/version-comparison.md).

Два независимых алгоритма. Параметры планирования `ZBase` / `ZOptimistic` / `ZPessimistic` — **входы в часы плана**, не колонка «оценка».

---

## 1. Плановая трудоёмкость

Единственная реализация формул: `IPerformanceCalculationEngine.RecalculateLaborIntensities` / `ComputePpvForecastHours` / `ComputeEquipmentForecastHours` (`LaborIntensityCalculator`, `ForecastHoursAggregator`).

Web-путь: **движок → persist в БД → invalidate кэша → GET читает колонки**. Live `N(V)` на GET **запрещён**. Холодный GET сам заполняет Redis (singleflight); persist не греет кэш на том же запросе.

### Входы (только на persist)

| Источник | Правило |
|----------|---------|
| Сценарий версии | `ProjectAnalysisVersion.PlanningScenario` |
| Три Z | `ProjectVersionPlanningParameter` по группе IIC |
| Строки ФО | только `IsAssessment = true` (`оценка «Да»`) |
| Нормы | снимок `AnalysisVersionSnapshot` на дату версии |
| Объём строки | `V_i = Quantity × Coefficient` |

Для нелинейной `N(V)` группировка обязательна: `N(ΣV) ≠ Σ N(V_i)`.

### Две фазы

1. Получить норму `N` (зависит от сценария).
2. Часы варианта: `LI(z) = N · (1 + σ · z) · V`.

| Сценарий | Как берётся N |
|----------|----------------|
| `ByProjectIicGroup` | `V_g = Σ V_i` по группе IIC **на весь проект** → `N_g = N(V_g)` → каждая строка оценки использует `N_g` |
| `ByObjectIicGroup` | то же **внутри объекта** (`FacilityCode`); объекты не смешивают объёмы |
| `ByEachValue` | без группировки: `N_i = N(V_i)` на строке |

Эквивалент: групповые часы `N_g·(1+σz)·V_g`, доля строки `V_i/V_g`.

Оборудование **не** группируется по сценарию: `N(V_eq)×z` на позицию. Колонки: `ProjectEquipment.LaborIntensityBase/Pessimistic/Optimistic`.

Строка с оценкой «Нет» не входит в `V_g` и не получает LI (колонки `null`).

Три **колонки** (песс./баз./опт.) — не три версии `ProjectAnalysisVersion`. Для Z плана на экране берутся **уже сохранённые** часы активного варианта (сравнение — селектор сценария; отчёт и аналитика — **Base**).

### Persist и кэш

`ILaborIntensityRecalculationService.RecalculateAsync` пишет ФО и оборудование (пул **EP-Analytics**). Оркестратор (`RecalculateVersionAsync`) затем только `InvalidateProjectAsync`. Первый `GET page-data` / `page-data/detail` собирает encoded-blob **один раз** на ключ версии (`PageDataBuildCoalescer`) и кладёт в Redis до bump generation (TTL 12 ч).

Две полосы SQL (один Server/DB, разный `Application Name`):

| Полоса | Пул | Кто |
|--------|-----|-----|
| Interactive | `EP-Interactive`, Min Pool 2, Max 32, Connect 5 с | auth, refresh, preferences, dictionaries, health |
| Analytics | `EP-Analytics`, Max 8, Connect 15 с, Command 60 с | page-data, persist часов, SQL-метрики, тяжёлые гриды |

Триггеры: кнопка / `POST …/recalculate`; смена сценария или Z; импорт и CRUD ФО/оборудования; нормы ИИС — **только затронутые** версии (`AfterNormChangedAsync`, без `InvalidateAll`).

Агрегаты по направлениям не хранятся: GET суммирует сохранённые строки ФО + оборудования (`StoredPlanHours`).

Backfill после миграции колонок оборудования: `scripts/recalculate-all-labor.sh` (неархивные версии).

Итоги плана: `SumAllForecastScenarioHours` (все assessed PPV + оборудование) и свёртка own+потомки — одни и те же цифры на прогнозе, факте и сравнении.

---

## 2. Часы факта

**Не считаются.** Источник — `ProjectLaborIntensity.PlannedHours`.

`ActualHours` хранится в БД и на форме ввода, **не** участвует в Z, KPI отчёта, трендах и сравнении.

Неотнесённые строки (`WorkDirectionId` пуст) не входят в выделенные часы факта; KPI неотнесённых — отдельно (`UnallocatedPlannedHours`).

---

## 3. Z-оценка (план и факт — одна формула)

Не класс трудоёмкости. Лист:

```
Z = (ln(hours / volume) − Estimate) / σ
```

Свёртка **всегда** по часам: `Σ(Z × hours) / Σ(hours)` через `ComputeWeightedZ` / `ComputeWeightedZFromPerformanceRows` / `WeightedZAggregator`.

Запрещено:

- штамповать `ZBase` как «оценку» плана;
- среднее арифметическое Z;
- режим тренда `uniform`.

План: `BuildPlanPerformance(bundle, engine, PlanHourKind)` → те же `CalculatePerformance`, что факт с PLI, от **сохранённых** часов (без группировки сценария на чтении).  
Факт: `BuildPerformance` / `GetPerformanceDataAsync(..., LaborHoursKind.Planned)`.

Одинаковые часы, объём и норма → одинаковый Z на прогнозе, факте, сравнении, отчёте и аналитике.

---

## 4. Матрица вкладок

| Экран | Часы плана | Часы факта | Z плана | Z факта |
|-------|------------|------------|---------|---------|
| Прогноз «Итого» / объекты / детально | `LaborIntensity*` ФО + оборудование | — | `CalculatePerformance(stored)` | — |
| Модалка «Прогноз» (карточки групп) | stored own узла среди `!isTotal` (не live N(V), не свёртка потомков) | — | — | — |
| Модалка «Анализ версии» | stored own узла (не свёртка потомков) | PLI own узла | — | Z узла (`CalculatePerformance`) |
| Оценка факта | overlay из тех же колонок (три варианта) | `PlannedHours` | — | `CalculatePerformance(PLI)` |
| Сравнение «Итого» | stored × 3 варианта; ИТОГО = `SumAllForecastScenarioHours` | PLI (own+потомки на итогах) | Z песс/баз/опт от stored | факт Z |
| Сравнение «По объектам» | stored с `BuildFactGrids` + pinned ИТОГО | PLI + pinned ИТОГО | — | факт Z объекта |
| Отчёт | stored Base + GrandTotals | allocated `PlannedHours` | `BuildPlanPerformance` | PLI Planned |
| Аналитика тренд Z | pipeline: Plan/Forecast = stored | Fact = PLI | часы-вес | часы-вес |
| Аналитика тренд производительности | `ComputeForecastHours` при V* (медиана/P10/P90 факта) | — | — | — |
| Групповой анализ | — | pipeline факт | — | `ComputeWeightedZ` по проектам |
| Веса диалога планирования | суммы stored Base | — | — | — |

`FactAssessmentPipeline`: `Direction` Plan/Forecast → stored plan hours; Fact → PLI.

`ComputePpvForecastHours` / `RecalculateLaborIntensities` — только persist-путь, не GET.

---

## 5. Что не входит

- Удаление колонки `ActualHours`.
- Группировка оборудования по сценарию.
- WPF / `EngineeringPerformance.Core`.
- Стилизация Excel факта/сравнения.
