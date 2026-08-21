# Performance Analytics

**Status:** ✅ Implemented
**Date:** 2026-05-21

## Description

Z-score based performance analysis for construction/engineering work directions. Compares actual productivity against normative benchmarks using statistical deviation scoring.

## How It Works

### Data Sources

The `PerformanceService.GetPerformanceDataAsync()` aggregates data from five sources for a given project and analysis version:

| Source | Entity | Filter |
|--------|--------|--------|
| Analysis version | `ProjectAnalysisVersion` | `Id = versionId AND ProjectId = projectId` |
| Work directions | `IicWorkDirection` | `IsArchived = false` |
| Physical volumes | `ProjectPhysicalVolume` | `ProjectAnalysisVersionId = versionId` |
| Labor intensities | `ProjectLaborIntensity` | `ProjectAnalysisVersionId = versionId` |
| Norms | `IicWorkDirectionNorm` | `EffectiveFrom <= versionCreatedAt AND (EffectiveTo IS NULL OR EffectiveTo >= versionCreatedAt)` |

### Calculation Pipeline

For each work direction (recursive tree traversal):

```
Step 1: Aggregate fact data
  factVolume = SUM(ProjectPhysicalVolume.Quantity × PhysicalVolume.Coefficient) WHERE PhysicalVolume.ClassifierId = WorkDirectionId
  factHours  = SUM(ProjectLaborIntensity.PlannedHours) WHERE WorkDirectionId = WorkDirectionId

Step 2: Calculate fact productivity
  factProductivity = factHours / factVolume    (when factVolume > 0)

Step 3: Log-transform to normal distribution
  factEstimate = ln(factProductivity)          (when factProductivity > 0)

Step 4: Resolve applicable norm
  norm = IicWorkDirectionNorm WHERE WorkDirectionId matches AND effective date range covers versionCreatedAt

Step 5: Calculate Z-score
  zScore = (factEstimate - normEstimate) / normStdDev    (when normStdDev > 0)

Step 6: Determine interpretation
  Based on zScore thresholds (see table below)
```

### Weighted-Average FactEstimate (since 2026-05-05)

`FactEstimate` for every row is a labor-hours-weighted average across the subtree:

```
subtreeHours     = ownHours + Σ childSubtreeHours
subtreeWeighted  = ownEstimate × ownHours + Σ childSubtreeWeighted
FactEstimate_row = subtreeWeighted / subtreeHours     (when subtreeHours > 0)
```

`own*` values are only included when both the leaf estimate and its labor hours are positive. For leaf nodes this equals `ln(ownProductivity)`; for internal nodes it aggregates all descendants, matching the PEPM aggregation used on the assessment screen.

`Z-score` and `Interpretation` on every row are recomputed from the aggregated `FactEstimate` (norm values come directly from the version snapshot — no extra calculations).

`PerformanceRowDto.SubtreeHours` — часы поддерева, участвующие в агрегации `FactEstimate` (используются как вес при расчёте итоговой Z-оценки в отчёте «Анализ версии проекта»). Поле `FactLaborHours` — только прямые часы узла, без суммирования по детям.

### Плановые часы на «Оценка факта» (since 2026-08-14)

На вкладках **Итого**, **Итого по объектам** и **Детально** после «Факт. Трудоемкость» выводятся три сценария из сохранённых колонок `ProjectPhysicalVolumes`:

| Колонка UI | Поле ФО |
|---|---|
| План.Труд.Песс. | `LaborIntensityPessimistic` |
| План.Труд.Баз. | `LaborIntensityBase` |
| План.Труд.Оптим. | `LaborIntensityOptimistic` |

Значения уже посчитаны `LaborIntensityRecalculationService` с учётом `PlanningScenario` (поэлементно / накопительно). Гриды **суммируют** сохранённые поля, без повторного `N(V)`. Строки оборудования плановых часов не имеют.

На «Итого» синтетические строки «Итого: …» / «ИТОГО» суммируют плановые часы так же, как факт.трудоёмкость (свои + дети).

### Отчёты вкладки «Оценка факта» (since 2026-08-15)

Те же три колонки **План.Труд.Песс. / Баз. / Оптим.** (после факт. трудоёмкости, 2 знака) уходят в выгрузки и модалку. Источник — сохранённые `LaborIntensity*`, без повторного `N(V)`.

| Подвкладка | Кнопки | Плановые часы |
|---|---|---|
| **Итого** | Анализ версии, Экспорт PDF | PDF `fact-summary/pdf`; отдельной кнопки Excel нет |
| **Итого по объектам** | Excel, PDF | оба содержат три колонки |
| **Детально** | Excel всех / выделенных, PDF, Сводная таблица | Excel/PDF — три колонки; сводная — блок IIC: ФО / Песс. / Баз. / Опт. / Факт Тр. / Z |

Модалка **Анализ версии** (`FactAnalysisDialog`): в дереве направлений после «Факт ч» те же три колонки; Excel модалки — тоже; PDF модалки = `fact-summary/pdf`.

Workspace «Экспорт Excel» (лист `Fact`) тоже содержит три колонки. На «Итого» отдельной кнопки Excel нет.

### Отчёт «Анализ версии проекта» (since 2026-05-21)

Итоги рассчитываются в `ProjectVersionAnalysisTotalsCalculator` и сохраняются в `ProjectVersionAnalysisDto.Summary`:

| Поле | Источник |
|------|----------|
| `TotalFactLaborHours` | `SUM(ProjectLaborIntensity.PlannedHours)` по всем строкам файла |
| `UnallocatedLaborHours` | часы с `WorkDirectionId IS NULL` |
| `WeightedZScore` | `Σ(root.ZScore × root.SubtreeHours) / Σ(root.SubtreeHours)` |
| `PhysicalVolumeTotalsByUnit` | сумма `FactPhysicalVolume` по каждой единице измерения (без смешивания ед.) |
| `WeightedZScorePercentile` / `WeightedZScoreInterpretation` | по той же шкале, что на вкладке «Оценка факта → По объектам» |

**Ограничение UI:** на странице проекта модалка открывается с вкладки **Оценка факта → Итого**. Для версии **плана** — из карточки проекта (legacy WPF `ProjectEditWindow`).

В дереве направлений после «Факт ч» выводятся **План.Труд.Песс. / Баз. / Оптим.** (`AttachLivePlanHours`: live N(V)×Z по ФО и оборудованию, тот же путь что Forecast «ИТОГО»). Сохранённые `LaborIntensity*` не читаются. Excel модалки содержит те же колонки; PDF делегирует в `fact-summary/pdf`.

### Z-Score Interpretation

| Z-Score Range | Interpretation | Russian | Color |
|---------------|---------------|---------|-------|
| < -1.0 | Excellent | Отличная производительность | Green `#4CAF50` |
| -1.0 to -0.5 | Good | Хорошая производительность | Light Green `#8BC34A` |
| -0.5 to +0.5 | Satisfactory | Удовлетворительная производительность | Yellow `#FFC107` |
| +0.5 to +1.0 | NeedsAttention | Требует внимания | Orange `#FF9800` |
| > +1.0 | Critical | Критически низкая производительность | Red `#F44336` |

**Note:** Lower Z-scores indicate better performance because `factProductivity = hours/volume` — fewer hours per unit volume is more efficient. A negative Z-score means the actual log-productivity is below the norm estimate, which is desirable.

### Hierarchical Display

Work directions are displayed as a tree using DevExpress `TreeListView`:

- Root directions (`ParentId = NULL`) are rendered at level 0
- Children are recursively rendered at `level + 1`
- Each row: `FactPhysicalVolume` and `FactLaborHours` — прямые значения узла (без суммирования по детям в `FactLaborHours`)

### DTO Structure

```csharp
public sealed record PerformanceRowDto(
    int WorkDirectionId,
    string WorkDirectionName,
    string WorkDirectionCode,
    int? ParentId,
    int Level,
    decimal? FactPhysicalVolume,
    decimal? FactLaborHours,
    decimal? FactProductivity,
    decimal? FactEstimate,
    decimal? NormProductivity,
    decimal? NormEstimate,
    decimal? NormStdDev,
    decimal? ZScore,
    PerformanceInterpretation? Interpretation,
    decimal? SubtreeHours = null);
```

```csharp
public sealed record ProjectVersionAnalysisSummaryDto(
    decimal? TotalFactLaborHours,
    decimal? WeightedZScore,
    string WeightedZScorePercentile,
    PerformanceInterpretation? WeightedZScoreInterpretation,
    IReadOnlyList<PhysicalVolumeTotalByUnitDto> PhysicalVolumeTotalsByUnit,
    decimal? UnallocatedLaborHours);
```

### ViewModel Binding

The `PerformanceItem` class maps `PerformanceRowDto` for WPF binding:

| ViewModel Property | DTO Source | Notes |
|-------------------|------------|-------|
| `Id` | `WorkDirectionId` | TreeListView KeyFieldName |
| `ParentId` | `ParentId` | TreeListView ParentFieldName |
| `Name` | `WorkDirectionName` | — |
| `FactPhysicalVolume` | `FactPhysicalVolume` | DisplayFormat N2 |
| `FactLaborHours` | `FactLaborHours` | DisplayFormat N2 |
| `FactProductivity` | `FactProductivity` | DisplayFormat N4 |
| `FactEstimate` | `FactEstimate` | DisplayFormat N4 |
| `NormProductivity` | `NormProductivity` | DisplayFormat N4 |
| `NormEstimate` | `NormEstimate` | DisplayFormat N4 |
| `NormStdDev` | `NormStdDev` | DisplayFormat N4 |
| `ZScore` | `ZScore` | DisplayFormat N4 |
| `Interpretation` | `Interpretation.ToRussian()` | Color-coded badge |

### Trigger

Performance data loads automatically when `SelectedVersion` changes in the ViewModel. The `OnSelectedVersionChanged` partial method calls `LoadPerformanceDataAsync(projectId, versionId)`.

### Forecast Scenarios (since 2026-05-05)

`ProjectForecastService` строит три сценария по плановому объёму и нормам снимка версии. Все отклонения применяются **в лог-пространстве**, т.к. `NormEstimate = ln(NormProductivity)`:

```
baseHours         = plannedVolume × exp(NormEstimate)
optimisticHours   = plannedVolume × exp(NormEstimate − 0.5 × NormStdDev)
pessimisticHours  = plannedVolume × exp(NormEstimate + 0.5 × NormStdDev)
```

Z-метки сценариев: `Оптимистичный = −0.5`, `Базовый = 0`, `Пессимистичный = +0.5`.

Если у направления нет `NormStdDev`, оптимистичный/пессимистичный сценарии вырождаются в базовый (нечем модулировать). Если `plannedVolume = 0`, строка использует фактические часы из трудоёмкости как fallback.

Реализация: `ProjectForecastService.ComputeBaseHours` / `ComputeScenarioHours`. Регрессионный тест — `ProjectForecastServiceTests.GenerateForecast_ScenarioHours_FollowNormEstimateWithHalfStdDeviation`.

## Related Documentation

- [Project Modal Redesign](./project-modal-redesign.md)
- [Data Import](./data-import.md)
