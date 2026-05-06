# Plan: Project Analysis Page (Анализ проекта)

**Created:** 2026-04-15
**Orchestration:** orch-2026-04-15-project-analysis-page
**Goal:** Create a comprehensive Project Analysis page based on the attached report design, with project/version selection and productivity analysis.
**Total Tasks:** 8
**Priority:** High

## Tasks Overview

1. **SVC-001:** Create ProjectAnalysisService for aggregated report data
   - Priority: High
   - Dependencies: None
   - Files: `IProjectAnalysisService.cs`, `ProjectAnalysisService.cs`, `ProjectAnalysisReportDto.cs`

2. **UI-001:** Create ProjectSelectionDialog with filtering
   - Priority: High
   - Dependencies: None
   - Files: `ProjectSelectionDialog.xaml`, `ProjectSelectionDialogViewModel.cs`, `ProjectSelectionDialogService.cs`

3. **UI-002:** Create AssessmentForm XAML layout
   - Priority: High
   - Dependencies: UI-001, SVC-001
   - Files: `AssessmentForm.xaml`

4. **VM-001:** Create AssessmentFormViewModel
   - Priority: High
   - Dependencies: UI-002, SVC-001, UI-001
   - Files: `AssessmentFormViewModel.cs`

5. **UI-003:** Implement Discipline Analysis grid with conditional formatting
   - Priority: High
   - Dependencies: UI-002, VM-001
   - Files: `AssessmentForm.xaml` (discipline section)

6. **UI-004:** Implement Project Info and Accuracy sections
   - Priority: Medium
   - Dependencies: UI-002, VM-001
   - Files: `AssessmentForm.xaml` (info sections)

7. **UI-005:** Implement Recommendations section
   - Priority: Medium
   - Dependencies: UI-002, VM-001
   - Files: `AssessmentForm.xaml` (recommendations section)

8. **TEST-001:** Build, verify, and integration test
   - Priority: Medium
   - Dependencies: All previous tasks
   - Files: All

## Dependencies Graph

```
SVC-001 ──→ UI-002 ──→ UI-003 ──┐
         ──→ UI-001 ──→ VM-001 ──┼──→ UI-004 ──→ UI-005 ──→ TEST-001
                                  └──→ UI-003 ────────────┘
```

## Architecture Decisions

### Report Structure (based on attached image)
1. **Project Info Section** — Project name, PERM indicator, interpretation, percentile, status, total labor hours
2. **Discipline Analysis** — Grid with columns: Discipline, Labor Hours, % of Total, Z-Score, Assessment (color-coded)
3. **Comparison Accuracy** — Project characteristics table with accuracy interpretation
4. **Recommendations** — Priority actions and key disciplines for improvement

### Project Selection
- Modal dialog with TextEdit for filtering projects by name/code
- GridControl showing filtered projects
- Only non-archived projects

### Version Selection
- ComboBoxEdit bound to versions with Direction = Fact
- Auto-selects first Fact version when project changes

### Styling
- Match existing app theme: `#2C3E50` headers, `#F0F2F5` background, `#FFFFFF` cards
- Color-coded Z-score cells matching the interpretation table:
  - < -1.0: Green (Excellent)
  - -1.0 to -0.5: Light green (Good)
  - -0.5 to +0.5: Yellow (Satisfactory)
  - +0.5 to +1.0: Orange (Needs Attention)
  - > +1.0: Red (Critical)

## Progress (updated by orchestrator)
- ⏳ SVC-001: Create ProjectAnalysisService for aggregated report data (Pending)
- ⏳ UI-001: Create ProjectSelectionDialog with filtering (Pending)
- ⏳ UI-002: Create AssessmentForm XAML layout (Pending)
- ⏳ VM-001: Create AssessmentFormViewModel (Pending)
- ⏳ UI-003: Implement Discipline Analysis grid with conditional formatting (Pending)
- ⏳ UI-004: Implement Project Info and Accuracy sections (Pending)
- ⏳ UI-005: Implement Recommendations section (Pending)
- ⏳ TEST-001: Build, verify, and integration test (Pending)
