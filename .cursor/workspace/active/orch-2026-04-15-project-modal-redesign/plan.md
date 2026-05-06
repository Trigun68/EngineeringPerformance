# Plan: Project Modal Redesign with Performance Analytics

**Created:** 2026-04-15
**Orchestration:** orch-2026-04-15-project-modal-redesign
**Goal:** Redesign ProjectEditWindow with three-column layout, project managers CRUD, performance tab with Z-score analysis, and Excel import/export.
**Total Tasks:** 10
**Priority:** High

## Tasks Overview

1. **DB-001:** Create Data Import Iteration Entities & Migrations
   - Priority: High
   - Dependencies: None
   - Files: `DataImportIteration.cs`, `DataImportIterationConfiguration.cs`, migration

2. **SVC-001:** Implement Excel Import Service with API-ready abstraction
   - Priority: High
   - Dependencies: DB-001
   - Files: `IExcelImportService.cs`, `ExcelImportService.cs`, `IDataImportService.cs`

3. **SVC-002:** Implement Performance Data Aggregation Service
   - Priority: High
   - Dependencies: None
   - Files: `IPerformanceService.cs`, `PerformanceService.cs`, `PerformanceRowDto.cs`

4. **UI-001:** Redesign ProjectEditWindow XAML Layout
   - Priority: High
   - Dependencies: None
   - Files: `ProjectEditWindow.xaml`

5. **UI-002:** Implement Project Managers CRUD Block
   - Priority: High
   - Dependencies: UI-001
   - Files: `ProjectManagerEditWindow.xaml`, `ProjectManagerEditViewModel.cs`, `ProjectManagerEditDialogService.cs`

6. **UI-003:** Implement Import/Export UI & Iteration Delete Dialogs
   - Priority: Medium
   - Dependencies: SVC-001, UI-001
   - Files: `DataImportDialog.xaml`, `DataImportDialogViewModel.cs`, `IterationDeleteDialog.xaml`

7. **UI-004:** Implement Performance Tab Grid
   - Priority: High
   - Dependencies: SVC-002, UI-001
   - Files: `ProjectEditWindow.xaml` (Performance tab section)

8. **VM-001:** Refactor ProjectEditViewModel
   - Priority: High
   - Dependencies: UI-002, SVC-001, SVC-002, UI-003, UI-004
   - Files: `ProjectEditViewModel.cs`

9. **DI-001:** Register new services in DI container
   - Priority: Medium
   - Dependencies: SVC-001, SVC-002
   - Files: `App.xaml.cs`

10. **TEST-001:** Build, verify, and integration test
    - Priority: Medium
    - Dependencies: All previous tasks
    - Files: All

## Dependencies Graph

```
DB-001 ──→ SVC-001 ──→ UI-003 ──┐
                                │
UI-001 ──→ UI-002 ──────────────┤
     ├──→ UI-003 ───────────────┤
     ├──→ UI-004 ←── SVC-002 ───┼──→ VM-001 ──→ DI-001 ──→ TEST-001
     └──→ VM-001 ←──────────────┘
```

## Architecture Decisions

### Auto-Save Strategy
- Use DevExpress `TextEdit.EditValueChanged` with a debounce timer (500ms)
- On lost focus, immediately save if value changed
- Track dirty state per field to avoid unnecessary DB calls

### Excel Import Abstraction
- `IDataImportService<T>` interface with `ImportFromFileAsync` and `ImportFromApiAsync` methods
- `ExcelImportService<T>` implements file-based import using ClosedXML
- Future API import can implement same interface

### Performance Calculation
- Z-score = (ln(FactProductivity) - NormEstimate) / NormStdDev
- Interpretation based on Z-score thresholds:
  - < -1.0: Отличная производительность
  - -1.0 to -0.5: Хорошая производительность
  - -0.5 to +0.5: Удовлетворительная производительность
  - +0.5 to +1.0: Требует внимания
  - > +1.0: Критически низкая производительность

### Iteration Tracking
- Each import creates a `DataImportIteration` record with timestamp and description
- Delete dialog shows unique timestamps for the current project+version
- Selecting a timestamp deletes all records from that iteration

## Progress (updated by orchestrator)
- ⏳ DB-001: Create Data Import Iteration Entities & Migrations (Pending)
- ⏳ SVC-001: Implement Excel Import Service with API-ready abstraction (Pending)
- ⏳ SVC-002: Implement Performance Data Aggregation Service (Pending)
- ⏳ UI-001: Redesign ProjectEditWindow XAML Layout (Pending)
- ⏳ UI-002: Implement Project Managers CRUD Block (Pending)
- ⏳ UI-003: Implement Import/Export UI & Iteration Delete Dialogs (Pending)
- ⏳ UI-004: Implement Performance Tab Grid (Pending)
- ⏳ VM-001: Refactor ProjectEditViewModel (Pending)
- ⏳ DI-001: Register new services in DI container (Pending)
- ⏳ TEST-001: Build, verify, and integration test (Pending)
