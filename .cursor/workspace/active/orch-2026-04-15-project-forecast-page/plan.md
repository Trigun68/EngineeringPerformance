# Plan: Project Forecast Page (Прогноз по проекту)

**Created:** 2026-04-15
**Orchestration:** orch-2026-04-15-project-forecast-page
**Goal:** Create a comprehensive Project Forecast page based on the attached design, with project/version selection (Plan direction only) and scenario-based labor hour forecasting.
**Total Tasks:** 7
**Priority:** High

## Tasks Overview

1. **SVC-001:** Create ProjectForecastService for scenario-based forecasting
   - Priority: High
   - Dependencies: None
   - Files: `IProjectForecastService.cs`, `ProjectForecastService.cs`, `ProjectForecastReportDto.cs`, `ForecastScenarioDto.cs`, `PlannedVolumeDto.cs`

2. **UI-001:** Extend ProjectSelectionDialog to support Plan direction
   - Priority: High
   - Dependencies: None
   - Files: `ProjectSelectionDialogViewModel.cs` (modify), `ProjectSelectionDialogService.cs` (modify)

3. **UI-002:** Create PlanningForm XAML layout
   - Priority: High
   - Dependencies: SVC-001, UI-001
   - Files: `PlanningForm.xaml`

4. **VM-001:** Create PlanningFormViewModel
   - Priority: High
   - Dependencies: UI-002, SVC-001, UI-001
   - Files: `PlanningFormViewModel.cs`

5. **UI-003:** Implement planned volumes grid and scenario calculations
   - Priority: High
   - Dependencies: UI-002, VM-001
   - Files: `PlanningForm.xaml` (volumes + scenarios sections)

6. **UI-004:** Implement interpretation and recommendations sections
   - Priority: Medium
   - Dependencies: UI-002, VM-001
   - Files: `PlanningForm.xaml` (interpretation section)

7. **TEST-001:** Build, verify, and integration test
   - Priority: Medium
   - Dependencies: All previous tasks
   - Files: All

## Dependencies Graph

```
SVC-001 ──→ UI-002 ──→ UI-003 ──┐
         ──→ UI-001 ──→ VM-001 ──┼──→ UI-004 ──→ TEST-001
                                  └──→ UI-003 ────────────┘
```

## Architecture Decisions

### Report Structure (based on attached image)
1. **Project Characteristics** — Project name, type, size, contract type, priority
2. **Planned Volumes Input** — Grid with Discipline, Subdiscipline, Element, Unit, Planned Volume
3. **Labor Hours by Scenario** — Table with Optimistic (z=-0.5), Base (z=0), Pessimistic (z=+0.5) scenarios
4. **Interpretation & Recommendations** — Scenario comparison table
5. **Top-3 Disciplines** — Key disciplines by labor hours (base scenario)

### Project Selection
- Reuse ProjectSelectionDialog with Plan direction filter
- Only versions with Direction = Plan available

### Scenario Calculations
- **Optimistic (z = -0.5):** `PlannedHours × exp(-0.5 × NormStdDev)`
- **Base (z = 0):** `PlannedHours` (direct from planned data)
- **Pessimistic (z = +0.5):** `PlannedHours × exp(+0.5 × NormStdDev)`

### Styling
- Match existing app theme: `#2C3E50` headers, `#F0F2F5` background, `#FFFFFF` cards
- Scenario colors:
  - Optimistic: Green (#4CAF50)
  - Base: Blue (#3498DB)
  - Pessimistic: Orange (#FF9800)

## Progress (updated by orchestrator)
- ⏳ SVC-001: Create ProjectForecastService for scenario-based forecasting (Pending)
- ⏳ UI-001: Extend ProjectSelectionDialog to support Plan direction (Pending)
- ⏳ UI-002: Create PlanningForm XAML layout (Pending)
- ⏳ VM-001: Create PlanningFormViewModel (Pending)
- ⏳ UI-003: Implement planned volumes grid and scenario calculations (Pending)
- ⏳ UI-004: Implement interpretation and recommendations sections (Pending)
- ⏳ TEST-001: Build, verify, and integration test (Pending)
