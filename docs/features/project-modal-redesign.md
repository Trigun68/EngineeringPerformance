# Project Modal Redesign

**Status:** ✅ Implemented
**Date:** 2026-04-15
**Report:** [orch-2026-04-15-project-modal-redesign](../../.cursor/workspace/active/orch-2026-04-15-project-modal-redesign/)

## Description

Complete redesign of the `ProjectEditWindow` from a simple form into a comprehensive project management workspace with three-column layout, auto-save, project manager CRUD, performance analytics with Z-score analysis, and Excel import/export for physical volumes and labor intensities.

## Architecture

### Component Overview

```
ProjectEditWindow
├── Header Bar (WindowTitle binding)
├── Top Section (Row 0, MinHeight 350px)
│   ├── Left Column (500px) — Project Data Form
│   ├── Center Column (250px) — Analysis Versions Grid
│   └── Right Column (250px) — Project Managers Grid
└── Bottom Section (Row 2, MinHeight 250px)
    ├── Performance TreeListView (10 columns)
    └── Action Buttons (Import/Delete for PV & LI)
```

### Window Dimensions

| Property | Value |
|----------|-------|
| Default Size | 1200x800 |
| MinHeight (top) | 350px |
| MinHeight (bottom) | 250px |
| Column Gap | 12px |
| Row Gap | 12px |

### Left Column — Project Data

13 editable fields in a 2-column label/editor grid (label width: 130px):

| Field | Binding | Type |
|-------|---------|------|
| Код | `Code` | TextEdit |
| Название | `Name` | TextEdit |
| Номер работы | `WorkNumber` | TextEdit |
| Номер договора | `ContractNumber` | TextEdit |
| Номер календ. плана | `CalendarPlanNumber` | TextEdit |
| Тип | `Type` | TextEdit |
| Заказчик | `Customer` | TextEdit |
| Подрядчик | `Contractor` | TextEdit |
| ГИП | `Gip` | TextEdit |
| Статус работы | `WorkStatus` | TextEdit |
| Начало работы | `WorkStart` | TextEdit |
| Программа | `Program` | TextEdit |
| Видимость | `SelectedVisibility` | ComboBoxEdit (AllUsers, OnlyMe, SpecificUsers) |

All bindings use `UpdateSourceTrigger=PropertyChanged` for immediate change detection.

### Center Column — Analysis Versions

DevExpress `GridControl` with `TableView` displaying:
- **Версия** — `VersionName` (star width)
- **Создана** — `CreatedAt` (80px)

Action buttons: Add, Edit, Delete (32x32 icon buttons).

### Right Column — Project Managers

DevExpress `GridControl` with `TableView` displaying:
- **Имя** — `UserName` (star width)
- **Роль** — `Role` (80px)

Action buttons: Add, Edit, Delete (32x32 icon buttons).

### Bottom Section — Performance

Full-width `TreeListView` with hierarchical display (`KeyFieldName="Id"`, `ParentFieldName="ParentId"`):

| Column | Field | Format | Width |
|--------|-------|--------|-------|
| Наименование | `Name` | — | 200 |
| Факт ФО | `FactPhysicalVolume` | N2 | 90 |
| Факт Трудоемк | `FactLaborHours` | N2 | 100 |
| Факт производит. | `FactProductivity` | N4 | 110 |
| Факт оценка | `FactEstimate` | N4 | 90 |
| Норм производит. | `NormProductivity` | N4 | 110 |
| Норм оценка | `NormEstimate` | N4 | 90 |
| Норм станд. откл. | `NormStdDev` | N4 | 110 |
| Z-score | `ZScore` | N4 | 80 |
| Интерпретация | `Interpretation` | Color-coded badge | 200 |

> Раскрытое значение `FactEstimate` для всех узлов — средне-взвешенное по часам поддерева (см. [Performance Analytics: Weighted-Average FactEstimate](./performance-analytics.md#weighted-average-factestimate-since-2026-05-05)). `NormEstimate` берётся из снимка норм без перерасчёта.

### Кнопка «Анализ версии проекта» (since 2026-05-05)

Под блоком Performance расположена кнопка `📊 Анализ версии проекта`, привязанная к `OpenVersionAnalysisCommand` (`ProjectEditViewModel`). Команда активируется только когда проект сохранён и выбрана версия анализа; открывает модальное окно `ProjectVersionAnalysisWindow` с расширенным отчётом по IIC, физ. объёмам и видам работ. См. [Project Version Analysis](./project-version-analysis.md).

## Auto-Save Mechanism

**Pattern:** Debounce timer with change tracking.

| Constant | Value |
|----------|-------|
| `AutoSaveDelayMs` | 500ms |
| Timer Type | `DispatcherTimer` (UI thread) |

**Flow:**
1. Each property has a `partial void On[Field]Changed()` handler
2. Handler sets `_hasChanges = true`, restarts the debounce timer
3. After 500ms of inactivity, `AutoSaveTimerElapsed` fires
4. `SaveProjectAsync()` validates `Code` and `Name` are non-empty, then performs upsert
5. New projects auto-transition to edit mode after first save (EntityId, IsEditMode, WindowTitle all update)

**Safeguards:**
- `_isSaving` flag prevents concurrent saves
- `_hasChanges` flag prevents unnecessary DB calls
- Timer stops on window close

## ViewModel Dependencies

`ProjectEditViewModel` requires 8 injected services:

| Service | Interface | Purpose |
|---------|-----------|---------|
| Project service | `IDirectoryService<Project>` | CRUD for projects |
| Version service | `IDirectoryService<ProjectAnalysisVersion>` | Analysis version management |
| User service | `IDirectoryService<User>` | User lookup for managers |
| Version dialog | `IProjectAnalysisVersionEditDialogService` | Version edit dialog |
| Manager dialog | `IProjectManagerEditDialogService` | Manager assignment dialog |
| Import dialog | `IDataImportDialogService` | Excel import file picker |
| Delete dialog | `IIterationDeleteDialogService` | Iteration deletion confirmation |
| Performance service | `IPerformanceService` | Z-score calculation |
| Context factory | `IDbContextFactory<AppDbContext>` | Direct DbContext access |

## Related Documentation

- [Performance Analytics](./performance-analytics.md)
- [Project Version Analysis](./project-version-analysis.md)
- [Data Import](./data-import.md)
- [DataImportIteration Entity](../database/entities/DataImportIteration.md)
