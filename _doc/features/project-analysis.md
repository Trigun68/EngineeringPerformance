# Project Analysis Feature

**Status**: Implemented
**Date**: 2026-04-15
**Last Updated**: 2026-05-05

## Overview

The Project Analysis feature provides comprehensive performance evaluation for engineering projects using statistical analysis. It calculates a PERM (Project Efficiency Rating Metric) indicator based on Z-scores of labor hours across disciplines, assesses planning accuracy, and generates actionable recommendations.

The feature is accessible from the sidebar as "Анализ проекта" (Project Analysis) and presents results in a four-section dashboard with color-coded visual indicators.

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      UI Layer (WPF)                         │
│  ┌──────────────────────┐    ┌────────────────────────────┐ │
│  │  AssessmentForm      │    │  ProjectSelectionDialog    │ │
│  │  (UserControl)       │◄───│  (Window)                  │ │
│  │  └─ AssessmentForm   │    │  └─ ProjectSelectionDialog │ │
│  │     ViewModel        │    │     ViewModel              │ │
│  └──────────┬───────────┘    └─────────────┬──────────────┘ │
└─────────────┼──────────────────────────────┼────────────────┘
              │                              │
              ▼                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Services Layer                            │
│  ┌──────────────────────────┐   ┌────────────────────────┐  │
│  │ IProjectAnalysisService  │   │ IProjectSelectionDialog│  │
│  │ └─ ProjectAnalysisService│   │ Service                │  │
│  └──────────┬───────────────┘   └────────────────────────┘  │
│             │                                               │
│             ▼                                               │
│  ┌──────────────────────────┐                               │
│  │ IPerformanceService      │                               │
│  │ (existing)               │                               │
│  └──────────────────────────┘                               │
└─────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Data Layer                                │
│  ┌──────────────────────────┐   ┌────────────────────────┐  │
│  │ AppDbContext             │   │ Performance data       │  │
│  │ └─ ProjectAnalysisVersions│   │ (via PerformanceService)│ │
│  └──────────────────────────┘   └────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Layers

| Layer | Components | Responsibility |
|-------|-----------|----------------|
| **UI** | `AssessmentForm`, `ProjectSelectionDialog` | Display analysis results, project selection |
| **ViewModels** | `AssessmentFormViewModel`, `ProjectSelectionDialogViewModel` | State management, commands, data binding |
| **Services** | `ProjectAnalysisService`, `ProjectSelectionDialogService` | Business logic, calculations, dialog orchestration |
| **DTOs** | `ProjectAnalysisReportDto`, `DisciplineAnalysisDto` | Data transfer between layers |
| **Data** | `AppDbContext`, `IPerformanceService` | Database access, performance data retrieval |

## Components

### Core Services

#### `IProjectAnalysisService` / `ProjectAnalysisService`

**Location**: `EngineeringPerformance.Core/Services/IProjectAnalysisService.cs`, `EngineeringPerformance.Infrastructure/Services/ProjectAnalysisService.cs`

Generates comprehensive project analysis reports. Depends on `IPerformanceService` for underlying performance data and `AppDbContext` for project/version metadata.

**Key method**:
```csharp
Task<ProjectAnalysisReportDto> GenerateReportAsync(int projectId, int versionId);
```

**Responsibilities**:
- Fetches performance data via `IPerformanceService.GetPerformanceDataAsync()`
- Filters to root disciplines (Level == 1)
- Calculates PERM indicator as labor-hours-weighted average of Z-scores
- Determines interpretation, percentile, and status from PERM value
- Computes accuracy score (percentage of disciplines within |Z| <= 1.0)
- Generates dynamic recommendations based on problem and excellent disciplines

#### `ProjectSelectionDialogService`

**Location**: `EngineeringPerformance.App/Services/ProjectSelectionDialogService.cs`

Orchestrates the project/version selection dialog using scoped DI lifetime.

**Interface**:
```csharp
void SetOwnerWindow(Window owner);
Task<(int ProjectId, int VersionId)?> ShowSelectionDialogAsync();
```

### DTOs

#### `ProjectAnalysisReportDto`

**Location**: `EngineeringPerformance.Core/DTOs/ProjectAnalysisReportDto.cs`

Immutable record containing the complete analysis report:

| Property | Type | Description |
|----------|------|-------------|
| `ProjectName` | `string` | Project display name |
| `ProjectCode` | `string` | Project code identifier |
| `VersionName` | `string` | Analysis version name |
| `PermIndicator` | `decimal?` | Weighted PERM value |
| `Interpretation` | `string` | Russian-language interpretation |
| `Percentile` | `string` | Percentile range (e.g., "16-31-й") |
| `Status` | `string` | Status label |
| `TotalLaborHours` | `decimal` | Sum of all root discipline labor hours |
| `Disciplines` | `IReadOnlyList<DisciplineAnalysisDto>` | Per-discipline breakdown |
| `AccuracyScore` | `decimal?` | Planning accuracy percentage |
| `AccuracyInterpretation` | `string` | Russian-language accuracy description |
| `AccuracyCharacteristics` | `IReadOnlyList<AccuracyCharacteristicDto>` | Перечень характеристик проекта (имя + выбранная опция + PD%), формирующих среднюю PD% |
| `Recommendations` | `IReadOnlyList<string>` | Actionable recommendations |
| `KeyDisciplines` | `IReadOnlyList<string>` | Disciplines with Z-score > 0.5 |

#### `DisciplineAnalysisDto`

**Location**: `EngineeringPerformance.Core/DTOs/DisciplineAnalysisDto.cs`

Per-discipline analysis result. Holds the full IIC tree (ParentId carries the hierarchy so the UI can render a `TreeListView`):

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Work direction id (used as `KeyFieldName`) |
| `ParentId` | `int?` | Parent work direction id (`ParentFieldName`); `null` for roots |
| `DisciplineName` | `string` | Work direction name |
| `LaborHours` | `decimal` | Fact labor hours |
| `PercentOfTotal` | `decimal` | Percentage of total labor hours |
| `ZScore` | `decimal?` | Statistical Z-score (already reflects the weighted-average `FactEstimate` computed by `PerformanceService`) |
| `Assessment` | `string` | Russian-language assessment |

### UI Components

#### `ProjectSelectionDialog`

**Location**: `EngineeringPerformance.App/Views/ProjectSelectionDialog.xaml`, `ViewModels/ProjectSelectionDialogViewModel.cs`

Modal dialog for selecting a project and its Fact version.

**Features**:
- Text search filtering by project name or code (case-insensitive)
- DevExpress `GridControl` for project list display
- `ComboBoxEdit` for version selection
- Only shows Fact versions (`AnalysisDirection.Fact`)
- Auto-selects the most recent Fact version
- Validation: "Select" button disabled until both project and version are chosen

**ViewModel state**:
- `Projects` / `FilteredProjects` - full and filtered project lists
- `SearchText` - triggers real-time filtering via `OnSearchTextChanged`
- `SelectedProject` - triggers `LoadFactVersionsAsync()` on change
- `AvailableVersions` - Fact versions for selected project
- `SelectedVersionId` - auto-set to first Fact version

#### `AssessmentForm`

**Location**: `EngineeringPerformance.App/Views/Forms/AssessmentForm.xaml`, `ViewModels/AssessmentFormViewModel.cs`

Main analysis dashboard with four sections.

**ViewModel commands**:
- `SelectProjectCommand` - opens project selection dialog and loads report
- `RefreshCommand` - re-runs analysis for current selection
- `ExportToExcelCommand` - placeholder for future export

**Loading behavior**:
- Uses `CancellationTokenSource` for cancellation on rapid re-selection
- Implements `ILoadableViewModel` interface
- Displays loading indicator and error messages

### Converters

#### `InterpretationToBrushConverter`

**Location**: `EngineeringPerformance.App/Converters/InterpretationToBrushConverter.cs`

Maps interpretation text to color brushes for visual indicators:

| Keyword Match | Color | Hex |
|--------------|-------|-----|
| "высок", "high", "above" | Green | `#27AE60` |
| "низк", "low", "below" | Red | `#E74C3C` |
| "средн", "medium", "moderate", "norm" | Orange | `#F39C12` |
| Default | Gray | `#95A5A6` |

## Data Flow

```
User clicks "Анализ проекта" in sidebar
  │
  ▼
AssessmentForm displayed (empty state)
  │
  ▼
User clicks "Выбрать проект"
  │
  ▼
ProjectSelectionDialog opens
  │
  ├─ Loads all active projects via IDirectoryService<Project>
  ├─ User filters/searches projects
  ├─ User selects project
  ├─ Loads Fact versions via IDirectoryService<ProjectAnalysisVersion>
  ├─ Auto-selects first Fact version
  └─ User clicks "Выбрать"
  │
  ▼
AssessmentFormViewModel.LoadReportAsync()
  │
  ├─ Calls IProjectAnalysisService.GenerateReportAsync(projectId, versionId)
  │   │
  │   ├─ IPerformanceService.GetPerformanceDataAsync(projectId, versionId)
  │   │   └─ Returns list of PerformanceRowDto with Z-scores
  │   │
  │   ├─ Filters to Level == 1 (root disciplines)
  │   ├─ Calculates total labor hours
  │   ├─ Creates DisciplineAnalysisDto for each root discipline
  │   ├─ Calculates PERM indicator (weighted average)
  │   ├─ Determines interpretation, percentile, status
  │   ├─ Calculates accuracy score
  │   ├─ Generates recommendations
  │   └─ Returns ProjectAnalysisReportDto
  │
  ├─ Populates Disciplines, Recommendations, KeyDiscines collections
  ├─ Generates PERM-based recommendation text
  └─ Updates UI via data binding
```

## Calculations

### PERM Indicator

The PEPM (Project Engineering Performance Metric, исторически назывался PERM) is a labor-hours-weighted average of Z-scores across all root disciplines:

```
PEPM = Σ(Z_i × Hours_i) / Σ(Hours_i)
```

Where:
- `Z_i` = Z-score of discipline i (с агрегированной `FactEstimate`, см. [Performance Analytics](performance-analytics.md))
- `Hours_i` = Fact labor hours of discipline i
- Only disciplines with Z-score and Hours > 0 are included

**Rounding**: 2 decimal places

> В UI индикатор обозначен как **PEPM**. В коде историческое имя `PermIndicator` сохранено для обратной совместимости с EF-снимками и тестами.

### Z-Score Interpretation

| Z-Score Range | Interpretation (RU) | Status | Color |
|--------------|---------------------|--------|-------|
| < -1.0 | Отличная производительность | В норме | Green `#4CAF50` |
| -1.0 to -0.5 | Хорошая производительность | В норме | Light green `#8BC34A` |
| -0.5 to 0.5 | Удовлетворительная производительность | Стабильный | Yellow `#FFC107` |
| 0.5 to 1.0 | Требует внимания | Требует внимания | Orange `#FF9800` |
| > 1.0 | Критически низкая производительность | Критический | Red `#F44336` |

### Percentile Mapping (since 2026-05-05)

PEPM ∈ [-1, 1] линейно отображается на перцентили [0, 100] с шагом 0.2:

| PEPM Range | Percentile |
|--------------|------------|
| < -1.0 | 0-й |
| от -1.0 до -0.8 | 0-10-й |
| от -0.8 до -0.6 | 10-20-й |
| от -0.6 до -0.4 | 20-30-й |
| от -0.4 до -0.2 | 30-40-й |
| от -0.2 до 0 | 40-50-й |
| от 0 до +0.2 | 50-60-й |
| от +0.2 до +0.4 | 60-70-й |
| от +0.4 до +0.6 | 70-80-й |
| от +0.6 до +0.8 | 80-90-й |
| от +0.8 до +1.0 | 90-100-й |
| > +1.0 | 100-й |

> Прежняя таблица на диапазоне [-2, 2] (с асимметричными бинами 1-й/2-5-й/6-15-й/…/98-100-й) заменена на линейную шкалу. Реализация: `ProjectAnalysisService.GetPercentile`. Регрессионные тесты — `ProjectAnalysisServiceTests.GenerateReport_PercentileMapping_FollowsLinearPepmRange`.

### Accuracy Score (Точность сравнения)

Точность сравнения вычисляется как **среднее значение PD%** по характеристикам проекта (`ProjectCharacteristicSelectionService.GetByProjectAsync`). Рассматриваются только строки с непустым `ValuePercent`.

```
AccuracyScore = average(ValuePercent) for rows where ValuePercent IS NOT NULL
```

Реализация: `ProjectAnalysisService.CalculateAccuracyFromCharacteristicsAsync(projectId)`.

**Округление**: 2 знака после запятой (`MidpointRounding.AwayFromZero`).

Если ни в одной характеристике нет `ValuePercent`, метрика возвращается как `null` и в UI отображается «—».

**Interpretation** (`ComparisonAccuracy.FromValuePercent`, единый источник для модулей характеристик и анализа):

| PD% Range | Точность сравнения |
|-----------|---------------------|
| < 7% | Очень высокая |
| 7% — < 9% | Высокая |
| 9% — < 11% | Стандартная |
| >= 11% | Пониженная |
| `null` | — (Н/Д) |

> Прежний алгоритм («доля строк с `\|Z\| <= 1.0`») был заменён на агрегат PD% характеристик, чтобы метрика «Точность сравнения» совпадала с одноимённым показателем в карточке проекта и не дублировала Z-распределение, уже отражённое в PEPM.

### Recommendations Generation

Recommendations are generated dynamically based on analysis results:

1. **Problem disciplines** (Z > 0.5): The highest Z-score discipline is flagged with its value
2. **Critical PERM** (PERM > 1.0): General recommendation for detailed analysis
3. **Excellent disciplines** (Z < -1.0): Best performer identified for practice sharing
4. **Default**: If no issues found, recommends continued monitoring

The ViewModel adds an additional PERM-based recommendation:

| PERM Range | Recommendation (RU) |
|-----------|---------------------|
| < -1.0 | Производительность отличная. Продолжайте в том же духе! |
| -1.0 to -0.5 | Производительность выше среднего. Поддерживайте текущий уровень. |
| -0.5 to 0.5 | Производительность около среднего. Есть потенциал для улучшения. |
| 0.5 to 1.0 | Производительность ниже среднего. Рекомендуется оптимизация. |
| > 1.0 | Производительность значительно ниже среднего. Требуются срочные меры. |

## UI Structure

### AssessmentForm Layout (since 2026-05-05)

```
┌─────────────────────────────────────────────────────────────┐
│  Top Toolbar                                                │
│  Проект: [name (Version)]            [Выбрать проект] [Обн.]│
├─────────────────────────────────────────────────────────────┤
│  Scrollable Content                                         │
│                                                             │
│  ┌─ Информация о проекте ───────────────────────────────┐   │
│  │ Project: [name (Version)]   Дисциплин: 12            │   │
│  │ ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐  │   │
│  │ │  PEPM       │ │ Перцентиль  │ │ Трудозатраты    │  │   │
│  │ │  [●] 0.45   │ │   51-68-й   │ │   12 340 ч      │  │   │
│  │ │  36 pt      │ │   36 pt     │ │   36 pt         │  │   │
│  │ └─────────────┘ └─────────────┘ └─────────────────┘  │   │
│  │ Статус: [●] Стабильный                               │   │
│  │ Интерпретация: Удовлетворительная производительность │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─ Точность оценки ────────────────────────────────────┐   │
│  │              ┌────────────────────┐                  │   │
│  │              │      8.42 %        │                  │   │
│  │              │      Высокая       │                  │   │
│  │              └────────────────────┘                  │   │
│  │  Описание: средневзвешенное PD% по характеристикам   │   │
│  │  проекта (ComparisonAccuracy.FromValuePercent).      │   │
│  │                                                      │   │
│  │  Характеристики проекта (GridControl):               │   │
│  │  ┌────────────────┬────────────────────┬─────────┐   │   │
│  │  │ Характеристика │ Выбранное значение │ PD, %   │   │   │
│  │  ├────────────────┼────────────────────┼─────────┤   │   │
│  │  │ Стадия         │ ПД                 │   6,00  │   │   │
│  │  │ Сложность      │ Средняя            │  10,00  │   │   │
│  │  ├────────────────┼────────────────────┼─────────┤   │   │
│  │  │                │            Среднее:│   8,00  │   │   │
│  │  └────────────────┴────────────────────┴─────────┘   │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─ Анализ по дисциплинам ──────────────────────────────┐   │
│  │  TreeListView (KeyFieldName=Id, ParentFieldName=     │   │
│  │  ParentId) — иерархия IIC                            │   │
│  │  Дисциплина │ Трудозатраты │ % │ z-score │ Оценка    │   │
│  │  [Color-coded rows by Z-score]                       │   │
│  │  Итого: 12 340 ч │ 100 %                             │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─ Рекомендации ───────────────────────────────────────┐   │
│  │  ┌─ Приоритетные действия ─┐  ┌─ Ключевые дисциплины┐│   │
│  │  │ [PEPM-based rec]        │  │ Discipline 1        ││   │
│  │  │ ⚠ Specific rec 1        │  │ Discipline 2        ││   │
│  │  └─────────────────────────┘  └─────────────────────┘│   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

> Реструктуризация 2026-05-05: дублирующиеся метрики удалены — «Трудозатраты», «PEPM», «Перцентиль», «Статус», «Кол-во дисциплин» отображаются только в блоке «Информация о проекте». Блок «Точность оценки» оставлен с одной KPI-плиткой и пояснительным текстом.

### Color-Coded Z-Score Rows

The discipline grid uses DevExpress `FormatCondition` for conditional formatting:

| Condition | Z-Score | Background | Foreground |
|-----------|---------|------------|------------|
| Less than -1.0 | Excellent | `#4CAF50` | White |
| Between -1.0 and -0.5 | Good | `#8BC34A` | White |
| Between -0.5 and 0.5 | Satisfactory | `#FFC107` | Black |
| Between 0.5 and 1.0 | Needs Attention | `#FF9800` | White |
| Greater than 1.0 | Critical | `#F44336` | White |

The Assessment column uses matching color rules based on the Russian interpretation text.

## How to Use

1. **Open the feature**: Click "Анализ проекта" in the sidebar navigation
2. **Select a project**: Click "Выбрать проект" to open the selection dialog
3. **Find your project**: Use the search field to filter by name or code
4. **Choose a version**: Select from available Fact versions (auto-selects the most recent)
5. **View the analysis**: The dashboard displays automatically after selection
6. **Refresh**: Click "Обновить" to re-run the analysis with current data

## Files Changed

### Core Layer
| File | Purpose |
|------|---------|
| `EngineeringPerformance.Core/Services/IProjectAnalysisService.cs` | Service interface |
| `EngineeringPerformance.Core/DTOs/ProjectAnalysisReportDto.cs` | Report DTO record |
| `EngineeringPerformance.Core/DTOs/DisciplineAnalysisDto.cs` | Discipline DTO record (Id/ParentId for TreeListView) |
| `EngineeringPerformance.Core/ComparisonAccuracy.cs` | Единый источник интерпретации PD% (используется и здесь, и в карточке проекта) |

### Infrastructure Layer
| File | Purpose |
|------|---------|
| `EngineeringPerformance.Infrastructure/Services/ProjectAnalysisService.cs` | Расчёт PEPM, Z-score, точности (через `ProjectCharacteristicSelectionService`) |
| `EngineeringPerformance.Infrastructure/Services/PerformanceService.cs` | Источник Z-score / FactEstimate (средне-взвешенный по поддереву с 2026-05-05) |

### App Layer
| File | Purpose |
|------|---------|
| `EngineeringPerformance.App/ViewModels/AssessmentFormViewModel.cs` | Main form ViewModel |
| `EngineeringPerformance.App/Views/Forms/AssessmentForm.xaml` | Main form UI (4 sections) |
| `EngineeringPerformance.App/Views/Forms/AssessmentForm.xaml.cs` | Code-behind |
| `EngineeringPerformance.App/ViewModels/ProjectSelectionDialogViewModel.cs` | Selection dialog ViewModel |
| `EngineeringPerformance.App/Views/ProjectSelectionDialog.xaml` | Selection dialog UI |
| `EngineeringPerformance.App/Views/ProjectSelectionDialog.xaml.cs` | Code-behind |
| `EngineeringPerformance.App/Services/ProjectSelectionDialogService.cs` | Dialog service |
| `EngineeringPerformance.App/Converters/InterpretationToBrushConverter.cs` | Text-to-color converter |
| `EngineeringPerformance.App/ViewModels/MainViewModel.cs` | Sidebar navigation entry added |
| `EngineeringPerformance.App/Services/NavigationService.cs` | "Assessment" route registered |
| `EngineeringPerformance.App/App.xaml.cs` | DI registration |

## Dependencies

- **DevExpress WPF**: `GridControl`, `TableView`, `TextEdit`, `ComboBoxEdit`, `FormatCondition`
- **CommunityToolkit.Mvvm**: `[ObservableProperty]`, `[RelayCommand]`
- **Entity Framework Core**: `IDbContextFactory<AppDbContext>`, `AsNoTracking()`, `Include()`
- **Microsoft.Extensions.DependencyInjection**: Scoped service resolution
