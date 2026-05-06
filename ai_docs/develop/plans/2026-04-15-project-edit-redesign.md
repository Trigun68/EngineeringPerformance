# Plan: Project Edit Modal Redesign

**Created:** 2026-04-15
**Orchestration:** orch-2026-04-15-20-00-project-edit-redesign
**Status:** Ready to execute

## Goal

Redesign the `ProjectEditWindow` to match the new interface mockup: three-column top layout (project data, analysis versions, project managers), and a full-width "Производительность" (Performance) tab with calculated metrics grid and Excel import/export controls.

## Architecture Context

- WPF + DevExpress controls, MVVM with CommunityToolkit.Mvvm
- EF Core 10.0.5 with SQL Server
- Clean Architecture: Core (interfaces/enums), Infrastructure (entities/services), App (views/viewmodels)
- Existing entities: `Project`, `ProjectUserAccess`, `ProjectAnalysisVersion`, `IicWorkDirection`, `IicWorkDirectionNorm`, `ProjectPhysicalVolume`, `ProjectLaborIntensity`
- No Excel import library exists yet — need to add one (ClosedXML recommended for .NET 10)

## Tasks

### UI-001: Redesign ProjectEditWindow Layout (Complex)
**Description:** Restructure `ProjectEditWindow.xaml` from the current two-column + tabs layout to a three-column top row ("Основные данные проекта" left/wide, "Версии анализа" center, "Менеджеры проекта" right) with a single full-width "Производительность" tab below. Remove the footer Save/Cancel buttons and the old placeholder tabs. Window size should be adjusted to accommodate the wider layout (e.g., 1200x900).

**Acceptance criteria:**
- Three bordered sections in the top row with proper spacing
- Single "Производительность" tab in the bottom section
- No Save/Cancel footer buttons (project fields auto-save)
- Window dimensions updated appropriately
- All existing project field bindings preserved

**Dependencies:** None
**Estimated complexity:** Moderate
**Files affected:** `EngineeringPerformance.App/Views/ProjectEditWindow.xaml`

---

### UI-002: Add Action Buttons to Analysis Versions Block (Simple)
**Description:** Replace the current text-based buttons ("Добавить", "Изменить", "Удалить") in the analysis versions section with icon-based buttons (add, edit, delete) using DevExpress `SimpleButton` with glyph images or `BarButtonItem` style. Keep existing command bindings intact.

**Acceptance criteria:**
- Icon buttons replacing text buttons
- Same commands bound: `AddVersionCommand`, `EditVersionCommand`, `DeleteVersionCommand`
- Tooltips in Russian for each button

**Dependencies:** UI-001
**Estimated complexity:** Simple
**Files affected:** `EngineeringPerformance.App/Views/ProjectEditWindow.xaml`

---

### UI-003: Project Managers Block — ViewModel (Moderate)
**Description:** Add `ProjectManagers` collection and CRUD commands to `ProjectEditViewModel`. Create a new `ProjectManagerEditViewModel` and `ProjectManagerEditWindow` for the modal dialog that:
- Displays a filterable list of users with `ProjectManager` role (RoleId = 2)
- Allows adding/removing users from the project's `ProjectUserAccess` collection
- Text filter searches users by `DisplayName`
- Only shows users not already assigned to the project

**Acceptance criteria:**
- `ObservableCollection<User>` property for project managers
- `LoadProjectManagersCommand` that loads from `ProjectUserAccess`
- `AddManagerCommand`, `RemoveManagerCommand`
- Modal dialog with text filter and user list
- Only `ProjectManager` role users available for selection
- Changes persisted to database immediately

**Dependencies:** UI-001
**Estimated complexity:** Moderate
**Files affected:**
- `EngineeringPerformance.App/ViewModels/ProjectEditViewModel.cs`
- `EngineeringPerformance.App/ViewModels/ProjectManagerEditViewModel.cs` (new)
- `EngineeringPerformance.App/Views/ProjectManagerEditWindow.xaml` (new)
- `EngineeringPerformance.App/Views/ProjectManagerEditWindow.xaml.cs` (new)
- `EngineeringPerformance.App/App.xaml.cs` (DI registration)

---

### UI-004: Project Managers Block — XAML (Simple)
**Description:** Add the "Менеджеры проекта" bordered section to the right column of the top row in `ProjectEditWindow.xaml`. Contains a `GridControl` bound to `ProjectManagers` showing user display names, with icon buttons for add/remove operations.

**Acceptance criteria:**
- DevExpress `GridControl` displaying manager names
- Add/remove icon buttons with proper command bindings
- Section header "Менеджеры проекта"
- Consistent styling with other top-row sections

**Dependencies:** UI-001, UI-003
**Estimated complexity:** Simple
**Files affected:** `EngineeringPerformance.App/Views/ProjectEditWindow.xaml`

---

### UI-005: Auto-Save for Project Fields (Moderate)
**Description:** Implement auto-save behavior for project fields: trigger save on `LostFocus` or `TextChanged` (with debounce delay) for each field. Remove the explicit Save/Cancel commands from the footer. The `SaveCommand` should be converted to an internal auto-save method that saves the project entity without closing the dialog.

**Acceptance criteria:**
- Each field triggers auto-save on lost focus
- Debounce mechanism to avoid excessive saves during rapid typing
- No visible Save/Cancel buttons
- Error handling for save failures (display in existing `ErrorMessage` binding)
- Project entity updates without closing the window

**Dependencies:** UI-001
**Estimated complexity:** Moderate
**Files affected:**
- `EngineeringPerformance.App/ViewModels/ProjectEditViewModel.cs`
- `EngineeringPerformance.App/Views/ProjectEditWindow.xaml`

---

### UI-006: Performance Tab — Grid and Calculated Columns (Complex)
**Description:** Implement the "Производительность" tab with a DevExpress `GridControl` that loads when an analysis version is selected. Auto-select the first version if none is selected. The grid displays hierarchical work directions (`IicWorkDirection`) with calculated columns:

| Column | Source |
|---|---|
| Наименование | `IicWorkDirection.Description` (hierarchical tree) |
| Факт ФО | `SUM(ProjectPhysicalVolume.Quantity)` grouped by work direction |
| Факт Трудоемк | `SUM(ProjectLaborIntensity.ActualHours)` grouped by work direction |
| Факт производит. | `ActualHours / Quantity` |
| Факт оценка | `ln(Факт производит.)` |
| Норм производит | `IicWorkDirectionNorm.NormValue` for version date |
| Норм оценка | `IicWorkDirectionNorm.Estimate` for version date |
| Норм станд. откл. | `IicWorkDirectionNorm.StandardDeviation` for version date |
| Z-score | `(Факт оценка - Норм оценка) / Норм станд. откл.` |
| Интерпретация | Based on Z-score thresholds (see interpretation table) |

**Acceptance criteria:**
- Grid loads data when version is selected
- Hierarchical tree view for work directions (parent/child grouping)
- All calculated columns display correct values
- Z-score interpretation based on thresholds:
  - Z > 2: "Значительно выше нормы"
  - 1 < Z <= 2: "Выше нормы"
  - -1 <= Z <= 1: "В пределах нормы"
  - -2 <= Z < -1: "Ниже нормы"
  - Z < -2: "Значительно ниже нормы"
- Handles division by zero and null values gracefully
- Norm values resolved by `EffectiveFrom`/`EffectiveTo` matching the version's `CreatedAt` date

**Dependencies:** UI-001
**Estimated complexity:** Complex
**Files affected:**
- `EngineeringPerformance.App/ViewModels/ProjectEditViewModel.cs` (new properties/commands)
- `EngineeringPerformance.App/ViewModels/PerformanceViewModel.cs` (new, or inline in ProjectEditViewModel)
- `EngineeringPerformance.App/Views/ProjectEditWindow.xaml` (tab content)
- `EngineeringPerformance.Infrastructure/Services/PerformanceCalculationService.cs` (new)

---

### UI-007: Performance Tab — Data Loading Service (Complex)
**Description:** Create a `PerformanceCalculationService` in Infrastructure that aggregates data from `ProjectPhysicalVolume`, `ProjectLaborIntensity`, and `IicWorkDirectionNorm` for a given analysis version. The service should:
- Query physical volumes and labor intensities for the selected version
- Group by `WorkDirectionId` (including parent direction aggregation)
- Resolve applicable norms by date range
- Calculate all derived metrics (productivity, log-evaluation, Z-score)
- Return a flat list of `PerformanceRowDto` for grid binding

**Acceptance criteria:**
- Single method: `GetPerformanceDataAsync(int analysisVersionId)`
- Correct aggregation of quantities and hours by work direction
- Proper norm resolution using `EffectiveFrom`/`EffectiveTo` date ranges
- All calculations match the specification
- Handles missing data (no volumes, no norms) without crashing

**Dependencies:** None (can be done in parallel with UI-006)
**Estimated complexity:** Complex
**Files affected:**
- `EngineeringPerformance.Core/DTOs/PerformanceRowDto.cs` (new)
- `EngineeringPerformance.Core/Services/IPerformanceCalculationService.cs` (new)
- `EngineeringPerformance.Infrastructure/Services/PerformanceCalculationService.cs` (new)
- `EngineeringPerformance.App/App.xaml.cs` (DI registration)

---

### DB-001: Excel Import — Infrastructure and Iteration Tracking (Moderate)
**Description:** Add ClosedXML NuGet package to the project. Create import iteration tracking: add `DataImportIteration` entity with fields `Id`, `ProjectAnalysisVersionId`, `DataType` (PhysicalVolume/LaborIntensity), `ImportedAt`, `Description`, `RecordCount`. Create `IDataImportService` interface and implementation with methods for importing physical volumes and labor intensity from Excel files. Design the service interface to support future API-based import (abstract the data source).

**Acceptance criteria:**
- ClosedXML package added to `.csproj`
- `DataImportIteration` entity created with EF Core migration
- `IDataImportService` interface with methods:
  - `ImportPhysicalVolumesAsync(int versionId, Stream excelStream, string description)`
  - `ImportLaborIntensityAsync(int versionId, Stream excelStream, string description)`
  - `GetImportIterationsAsync(int versionId, string dataType)`
  - `DeleteImportIterationAsync(int iterationId)`
- Service maps Excel rows to `ProjectPhysicalVolume` / `ProjectLaborIntensity` entities
- Each import creates an iteration record with timestamp

**Dependencies:** None
**Estimated complexity:** Moderate
**Files affected:**
- `EngineeringPerformance.Infrastructure/EngineeringPerformance.Infrastructure.csproj`
- `EngineeringPerformance.Infrastructure/Data/Entities/DataImportIteration.cs` (new)
- `EngineeringPerformance.Infrastructure/Data/Configurations/DataImportIterationConfiguration.cs` (new)
- `EngineeringPerformance.Core/Services/IDataImportService.cs` (new)
- `EngineeringPerformance.Infrastructure/Services/DataImportService.cs` (new)
- `EngineeringPerformance.Infrastructure/Data/AppDbContext.cs`
- EF Core migration

---

### UI-008: Import/Export Controls and Iteration Management (Moderate)
**Description:** Add import/export buttons to the right of the Performance tab header: "Импорт физ. объёмов", "Удалить физ. объёмы", "Импорт трудоёмкости", "Удалить трудоёмкость". Import buttons open a file picker dialog. Delete buttons show a modal with a list of unique import iteration timestamps for selection. Wire up the commands to `IDataImportService`.

**Acceptance criteria:**
- Four action buttons positioned to the right of the Performance tab
- File picker opens on import (filter for `.xlsx` files)
- Delete modal shows list of iterations with timestamps and record counts
- User selects iteration(s) to delete, confirms, and data is removed
- Commands bound to ViewModel with proper error handling
- Loading indicator during import operations

**Dependencies:** DB-001, UI-006
**Estimated complexity:** Moderate
**Files affected:**
- `EngineeringPerformance.App/Views/ProjectEditWindow.xaml`
- `EngineeringPerformance.App/ViewModels/ProjectEditViewModel.cs`
- `EngineeringPerformance.App/ViewModels/DataImportIterationSelectViewModel.cs` (new)
- `EngineeringPerformance.App/Views/DataImportIterationSelectWindow.xaml` (new)

---

### UI-009: Wire Up DI and Integration Testing (Simple)
**Description:** Register all new services and viewmodels in `App.xaml.cs`. Ensure the `ProjectEditWindow` opens correctly with the new layout and all sections load data properly. Verify auto-save, manager CRUD, performance grid, and import/export flows work end-to-end.

**Acceptance criteria:**
- All new services registered in DI container
- `ProjectEditWindow` opens without errors
- All three top sections display and load data
- Performance tab loads when version is selected
- Auto-save works for project fields
- Manager add/remove works
- Import file picker opens and processes files
- No unhandled exceptions in any flow

**Dependencies:** UI-001 through UI-008 (all other tasks)
**Estimated complexity:** Simple
**Files affected:** `EngineeringPerformance.App/App.xaml.cs`

---

## Dependencies Graph

```
                    ┌─────────┐
                    │  DB-001 │ (Excel import infrastructure)
                    └────┬────┘
                         │
┌─────────┐         ┌────▼────┐    ┌─────────┐
│  UI-001 │────────►│  UI-006 │◄───│  UI-007 │ (Performance grid + service)
└────┬────┘         └────┬────┘    └─────────┘
     │                   │
     ├────────►┌─────────▼─────────┐
     │         │     UI-008        │ (Import/export controls)
     │         └─────────┬─────────┘
     │                   │
     ├────────►┌─────────▼─────────┐
     │         │     UI-009        │ (DI wiring + integration)
     │         └───────────────────┘
     │
     ├────────►┌─────────┐    ┌─────────┐
     │         │  UI-002 │    │  UI-003 │ (Managers VM)
     │         └─────────┘    └────┬────┘
     │                             │
     │         ┌─────────┐    ┌────▼────┐
     │         │  UI-005 │    │  UI-004 │ (Managers XAML)
     │         └─────────┘    └─────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Execution Order (parallel where noted)  │
├─────────────────────────────────────────┤
│ 1. DB-001 + UI-007 (parallel)           │
│ 2. UI-001                               │
│ 3. UI-002 + UI-003 + UI-005 (parallel)  │
│ 4. UI-004                               │
│ 5. UI-006                               │
│ 6. UI-008                               │
│ 7. UI-009                               │
└─────────────────────────────────────────┘
```

## Architecture Decisions

- **Auto-save**: Use `DispatcherTimer` with 500ms debounce for text fields; `LostFocus` event for date/combobox fields
- **Excel library**: ClosedXML (modern API, .NET 10 compatible, no COM dependencies)
- **Performance calculations**: Dedicated service in Infrastructure layer, returns DTOs to App layer
- **Manager selection modal**: Reuse existing `User` entity, filter by `RoleId = 2` (ProjectManager)
- **Import iteration tracking**: Separate entity to track import batches, enabling selective deletion
- **Z-score interpretation**: Hardcoded threshold table in ViewModel (not configurable, per requirements)

## Implementation Notes

- The `ProjectPhysicalVolume` entity does not have a direct `WorkDirectionId` — the mapping between physical volumes and work directions needs to be determined from the existing data model. If `PhysicalVolume` has a relationship to `IicWorkDirection`, use that. Otherwise, the aggregation may need to be by `PhysicalVolumeId` only.
- The `ProjectLaborIntensity` has `WorkDirectionId` directly, so aggregation is straightforward.
- Norm resolution uses `EffectiveFrom <= version.CreatedAt AND (EffectiveTo IS NULL OR EffectiveTo >= version.CreatedAt)`.
- The hierarchical tree in the performance grid should use DevExpress `TreeListView` or grouped `TableView` with parent/child relationships from `IicWorkDirection.Parent`.
- Consider adding a loading indicator (`LoadingViewModel` pattern already exists in the project) for the performance grid data load.

## Progress (updated by orchestrator)
- ⏳ UI-001: Redesign ProjectEditWindow Layout (Pending)
- ⏳ UI-002: Add Action Buttons to Analysis Versions Block (Pending)
- ⏳ UI-003: Project Managers Block — ViewModel (Pending)
- ⏳ UI-004: Project Managers Block — XAML (Pending)
- ⏳ UI-005: Auto-Save for Project Fields (Pending)
- ⏳ UI-006: Performance Tab — Grid and Calculated Columns (Pending)
- ⏳ UI-007: Performance Tab — Data Loading Service (Pending)
- ⏳ DB-001: Excel Import — Infrastructure and Iteration Tracking (Pending)
- ⏳ UI-008: Import/Export Controls and Iteration Management (Pending)
- ⏳ UI-009: Wire Up DI and Integration Testing (Pending)
