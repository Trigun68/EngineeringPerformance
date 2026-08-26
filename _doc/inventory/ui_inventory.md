# UI Inventory — WPF Engineering Performance

**Дата:** 2026-06-09  
**Источник:** `EngineeringPerformance.App/Views/**/*.xaml`, `ViewModels/`, `Converters/`  
**Цель:** полная инвентаризация UI для миграции на React + MUI + AG Grid

---

## Статус папки `.src`

| Путь | Результат |
|------|-----------|
| `EngineeringPerformance/.src` | **Отсутствует** в репозитории |
| `client/src` | React web-клиент (целевой стек), не WPF |
| Альтернативные источники граничных условий | `EngineeringPerformance.Tests/` (25 файлов), `_doc/plans/equipment-module.md`, `client/e2e/migration-smoke.spec.ts` |

**Важно:** CheckBox «Посимвольное сравнение» в UI **не найден**. Посимвольное сравнение (`StringComparison.Ordinal`) задокументировано для **оборудования** в `_doc/plans/equipment-module.md` и реализовано в `EquipmentImportCatalogMatcher` (backend), не как отдельный UI-переключатель.

---

## Сводная статистика UI

| Метрика | Значение |
|---------|----------|
| Файлов `.xaml` | 69 (включая `App.xaml`) |
| Окон / UserControl / форм | 68 |
| ViewModels | 62 |
| IValueConverter | 9 классов (11 конвертеров) |
| Форм с DevExpress GridControl | 35+ |
| Форм с TreeListView (иерархия) | 6 справочников |
| DXTabControl (вкладки) | 6 контейнеров |
| Кастомных контекстных меню (ShowGridMenu) | 8+ точек |

### Каталог форм (краткий)

| # | XAML | Тип | DevExpress | Назначение |
|---|------|-----|------------|------------|
| 1 | `MainWindow` | Window | dxmvvm | Shell: sidebar + ContentHost |
| 2 | `LoginWindow` | Window | dxe, dxmvvm | Авторизация |
| 3 | `SplashWindow` | Window | dx | Заставка |
| 4 | `LoadingWindow` | Window | — | Прогресс загрузки справочников |
| 5 | `DbSettingsWindow` | Window | dxe | Подключение к БД |
| 6 | `ProjectDashboardView` | UserControl | — | Главная (дашборд) |
| 7 | `ProjectsPageView` | UserControl | dx, dxe, dxg | Проекты: прогноз/факт/сравнение |
| 8 | `ReferencesPageView` | UserControl | DXTabControl | Контейнер справочников |
| 9 | `DataPageView` | UserControl | DXTabControl | Данные проекта (legacy вкладки) |
| 10 | `SettingsPageView` | UserControl | DXTabControl | Настройки |
| 11 | `UserActionLogView` | UserControl | dxg | Журнал действий |
| 12 | `InterpretationSettingsView` | UserControl | — | Настройки интерпретации σ |
| 13 | `DirectoryWindowBase` | UserControl | dxg, dxe | Базовый шаблон справочника |
| 14–21 | `PhysicalVolumeTypesWindow` … `ProjectCharacteristicsWindow` | UserControl | dxg/dxe | 8 вкладок справочников |
| 22–27 | `*EditWindow`, `*QuickEditDialog` | Window/Dialog | dxe | Редактирование сущностей |
| 28–35 | `Project*Window`, `Project*Dialog` | mixed | dxg | Данные проекта |
| 36–41 | `Forms/*Form.xaml` | UserControl/Window | dxg | Аналитика (оценка, план, группы) |
| 42–45 | `ProjectVersionAnalysisWindow`, `ProjectForecastReportWindow` | Window | dxg | Отчёты |
| 46–68 | Прочие диалоги | Window | mixed | Confirm, Picker, Import, Selection |

---

## Глобальные IValueConverter

| Конвертер | Файл | Назначение | Использование (Visibility/стиль) |
|-----------|------|------------|----------------------------------|
| `NullToCollapsedConverter` | `NullConverters.cs` | null → Collapsed | ProjectsPage: AccuracyScore, Deviation |
| `NullToBoolConverter` | `NullConverters.cs` | null → false | Кнопки импорта (PlanVersionId) |
| `BoolToVisibilityConverter` | `NullConverters.cs` | bool → Visible/Collapsed | HasProject, панели |
| `InverseBooleanToVisibilityConverter` | `InverseBooleanToVisibilityConverter.cs` | инверсия | LoginWindow: PasswordBox vs TextEdit |
| `AnalysisDirectionToStringConverter` | `NullConverters.cs` | enum → строка | Версии анализа |
| `InterpretationToBrushConverter` | `InterpretationToBrushConverter.cs` | PerformanceInterpretation → Brush | Гриды оценки (цвет строк) |
| `BoolToFontStyleConverter` | `ForecastConverters.cs` | итоговые строки | PlanningForm |
| `BoolToTotalBackgroundConverter` | `ForecastConverters.cs` | фон итогов | PlanningForm |
| `BoolToFontWeightConverter` | `AccessRightsConverters.cs` | жирный шрифт | AccessRightsWindow |
| `IndentToMarginConverter` | `AccessRightsConverters.cs` | отступ дерева прав | AccessRightsWindow |

**DevExpress-конвертеры (в XAML):** `dxmvvm:BooleanToVisibilityConverter`, `BooleanNegationConverter` — LoginWindow.

---

## DevExpress — используемые компоненты

| Компонент | Namespace | Где |
|-----------|-----------|-----|
| `GridControl` + `TableView` | dxg | Справочники, пользователи, журнал |
| `GridControl` + `TreeListView` | dxg | PV, Equipment, Labor, IIC dirs, IIC norms |
| `ComboBoxEdit` | dxe | Фильтры, lookup-поля в формах |
| `TextEdit` / `PasswordBoxEdit` | dxe | Login, Import, редактирование |
| `DXTabControl` / `DXTabItem` | dx | ProjectsPage, References, Settings, Data |
| `DispatcherService` | dxmvvm | MainWindow, LoginWindow |
| `BarButtonItem` | dxb | Контекстные меню гридов |
| `RussianGridControlLocalizer` | App.xaml.cs | Русификация встроенного меню грида |

**Поведение DevExpress (критично для миграции):**
- `RestoreStateOnSourceChange="True"` + `RestoreStateKeyFieldName="Id"` — сохранение раскладки колонок
- `MergeItemsPreservingReferences` в VM — не сбрасывать раскрытие TreeList при refresh
- `AllowDragDrop="True"` на TreeListView — изменение parent/sortOrder
- `ShowSearchPanelMode="Always"` — встроенный поиск в дереве PV

---

## MainWindow.xaml

### Назначение:
Главная оболочка: sidebar-навигация (280px), заголовок страницы, `ContentControl` для текущего View.

### Элементы управления:
| x:Name | Тип | DevExpress? | Binding | Валидация | Обязательное? | Примечания |
|--------|-----|-------------|---------|-----------|---------------|------------|
| NavigationList | ListBox | Нет | ItemsSource=NavigationGroups[0].Items | — | — | SelectionChanged → code-behind |
| ContentHost | ContentControl | Нет | Content=CurrentView | — | — | |
| — | TextBlock | Нет | CurrentViewTitle | — | — | Заголовок |
| — | TextBlock | Нет | CurrentUser.DisplayName, RoleName | — | — | Профиль |

### Контекстные меню и горячие клавиши:
| Имя / Клавиша | Действие | Условия (РАЗВИЛКА) |
|---------------|----------|---------------------|
| NavigationList click | Navigate(viewName) | `MainViewModel.AddIfAccessible` — пункт только если `CanRead(user, objectName)` |
| — | Ctrl+S | **Не найдено** в MainWindow |

### Модальные окна:
Не открывает напрямую — через `INavigationService`.

### Обработчики (РАЗВИЛКИ):
| Событие | Метод | Логика |
|---------|-------|--------|
| SelectionChanged | NavigationList_SelectionChanged | NavigateTo выбранный ViewName |
| ctor | ShowDashboard | if (startup) → ProjectDashboardView + LoadAsync |

---

## LoginWindow.xaml

### Назначение:
Форма входа (логин/пароль), отображение Windows-учётки, переход к настройкам БД.

### Элементы управления:
| x:Name | Тип | DevExpress? | Binding | Валидация | Обязательное? | Примечания |
|--------|-----|-------------|---------|-----------|---------------|------------|
| — | TextEdit | **ДА** | Username | — | Да | |
| — | PasswordBoxEdit / TextEdit | **ДА** | Password | — | Да | Toggle visibility |
| — | Button | Нет | LoginCommand | — | — | IsEnabled=!IsBusy |
| — | Button | Нет | OpenDbSettingsCommand | — | — | Шестерёнка |
| — | TextBlock | Нет | WindowsUsername | — | — | Visible if HasWindowsUsername |

### Контекстные меню:
Нет.

### Модальные окна:
- `DbSettingsWindow` — из OpenDbSettingsCommand

### Обработчики (РАЗВИЛКИ):
| Событие | Метод | Логика |
|---------|-------|--------|
| LoginCommand | LoginViewModel | if (invalid credentials) → ErrorMessage; else → MainWindow |
| TogglePasswordVisibility | — | PasswordVisible = !PasswordVisible |

**Источник тестов:** `AuthenticationServiceTests.cs` — граничные случаи логина.

---

## ReferencesPageView.xaml + вкладки справочников

### Назначение:
Контейнер `DXTabControl`; вкладки строятся в `ReferencesPageViewModel.BuildTabs()` по правам.

### Вкладки (РАЗВИЛКА по правам):
| Вкладка | View | Permission object |
|---------|------|-----------------|
| Виды справочников ФО | PhysicalVolumeTypesWindow | PhysicalVolumeTypes |
| Номенклатура ФО | PhysicalVolumesWindow | PhysicalVolumes |
| Оборудование | EquipmentsWindow | Equipment |
| Трудоёмкость | LaborIntensitiesWindow | LaborIntensities |
| Группы IIC | IicWorkDirectionsWindow | IicWorkDirections |
| Нормы ИИС | IicWorkDirectionNormsWindow | IicWorkDirectionNorms |
| Единицы измерения | UnitOfMeasurementsWindow | UnitsOfMeasurement |
| Характеристики проекта | ProjectCharacteristicsWindow | ProjectCharacteristics |

---

## PhysicalVolumesWindow.xaml (эталон иерархического справочника)

### Назначение:
Дерево номенклатуры физических объёмов: CRUD, архив, фильтры, Excel export, drag-and-drop иерархии.

### Элементы управления:
| x:Name | Тип | DevExpress? | Binding | Валидация | Обязательное? | Примечания |
|--------|-----|-------------|---------|-----------|---------------|------------|
| — | Button×6 | Нет | CreateAndEdit, Edit, Delete, Archive, Unarchive, Refresh | CanEdit в VM | — | Toolbar иконки |
| — | CheckBox | Нет | ShowArchived | — | — | Persist в UserSettings |
| — | ComboBoxEdit | **ДА** | SelectedVolumeTypeIdFilter | — | Нет | NullText="Все" |
| — | ComboBoxEdit | **ДА** | SelectedAssessmentFilterOption | — | Нет | Да/Нет/Все |
| TreeListView | TreeListView | **ДА** | Items, SelectedItem | — | — | Key=Id, Parent=ParentId |
| — | GridColumn×9 | **ДА** | Code, Name, Description, … | — | — | |

### Контекстные меню:
| Имя меню | Действие | Условия |
|----------|----------|---------|
| DevExpress row menu + custom | Развернуть всё / Свернуть всё | MenuType = RowCell/Column/GroupPanel |
| DoubleClick | EditItemCommand | if SelectedItem != null |

### Модальные окна:
- `PhysicalVolumeEditWindow` — CreateAndEdit / Edit
- `PhysicalVolumeQuickEditDialog` — быстрое редактирование из проекта

### Обработчики (РАЗВИЛКИ):
| Событие | Метод | Логика |
|---------|-------|--------|
| MouseDoubleClick | GridControl_MouseDoubleClick | if (SelectedItem) → EditItemCommand |
| DropRecord | TreeListView_DropRecord | capture draggedId, targetParentId, DropPosition |
| CompleteRecordDragDrop | TreeListView_CompleteRecordDragDrop | CalculateSortOrderIndex → HandleDragDropAsync |
| ShowGridMenu | TreeListView_ShowGridMenu | if menuType in (RowCell, Column, GroupPanel) → expand/collapse items |

**Аналогичная структура:** `EquipmentsWindow`, `LaborIntensitiesWindow`, `IicWorkDirectionsWindow`, `IicWorkDirectionNormsWindow`.

---

## DirectoryWindowBase.xaml (базовый плоский справочник)

### Назначение:
Упрощённый шаблон: Users, Projects (standalone), PhysicalVolumeTypes, Units.

### Элементы:
| Элемент | DevExpress? | Binding |
|---------|-------------|---------|
| Toolbar buttons | Нет | CreateItem, Save, Delete, Archive, Unarchive, Refresh |
| CheckBox ShowArchived | Нет | ShowArchived |
| TextEdit Search | **ДА** | SearchText |
| GridControl TableView | **ДА** | Items, SelectedItem — колонки Name, IsArchived, CreatedAt |

**РАЗВИЛКА в VM:** `DirectoryViewModelBase.LoadAsync` — if (SettingsHelper) restore ShowArchived from DB before fetch.

---

## ProjectsPageView.xaml

### Назначение:
Центральный экран проекта: выбор проекта, вкладки Прогнозирование / Оценка факта / Сравнение / Отчёты, 9 GridControl, импорт Excel.

### Ключевые элементы:
| Область | Тип | DevExpress? | Binding / Command |
|---------|-----|-------------|-------------------|
| Шапка проекта | TextBlock, Border | Нет | SelectedProjectCode/Name, AccuracyScore |
| Кнопки | Button | Нет | SelectProject, OpenProjectData, OpenCharacteristics, OpenParticipants |
| Вкладки | DXTabControl | **ДА** | 4+ вкладки аналитики |
| ForecastGrid … CompareDetailGrid | GridControl×9 | **ДА** | VM collections |
| Импорт | Button | Нет | ImportPlanPhysicalVolumes, ImportFact*, ImportEquipment* |

### Контекстные меню (кастомные — критично):
| Грид | Меню | Условия (РАЗВИЛКА) |
|------|------|---------------------|
| DetailForecastGrid | Редактировать ФО / номенклатуру / норму EAC / удалить / создать копию | if row.IsTotal → **меню скрыто**; if !IsActionableDetailRow → Handled=true |
| FactDetailGrid | Аналогично для факта | if !IsActionableFactDetailRow → Handled=true |
| DetailForecast | if IsEquipmentRow | → EditProjectEquipment, EditEquipmentNomenclature |
| DetailForecast | else | → EditProjectPhysicalVolume, EditPhysicalVolumeNomenclature |

**Источник:** `ProjectsPageView.xaml.cs` строки 63–120; команды в `ProjectsPageViewModel` (40+ RelayCommand).

### Модальные окна (из ProjectsPage):
- `ProjectPickerDialog`, `ProjectVersionPickerDialog`
- `DataImportDialog` — импорт ФО/труд/оборудование
- `ProjectDataDialog`, `ProjectParticipantsDialog`, `ProjectCharacteristicsDialog`
- `ProjectPhysicalVolumeCreateDialog`, `ProjectEquipmentCreateDialog`
- QuickEdit диалоги (ФО, оборудование, нормы)
- `ProjectForecastReportWindow`, `ProjectVersionAnalysisWindow`

### Горячие клавиши:
Не найдены глобальные KeyBinding в XAML.

---

## DataImportDialog.xaml

### Назначение:
Модальный импорт Excel для версии проекта (ФО / труд / оборудование).

### Элементы управления:
| x:Name | Тип | DevExpress? | Binding | Валидация | Обязательное? | Примечания |
|--------|-----|-------------|---------|-----------|---------------|------------|
| — | TextEdit (readonly) | **ДА** | SelectedFilePath | — | Да | |
| — | TextEdit | **ДА** | Description | — | Нет | Auto-fill из имени файла |
| — | TextBlock | Нет | ErrorMessage | — | — | Красный текст |
| — | Button | Нет | BrowseFileCommand | Filter .xlsx/.xls | — | OpenFileDialog |
| — | Button | Нет | ImportCommand | — | — | IsDefault |
| — | Button | Нет | CancelCommand | — | — | IsCancel |

### CheckBox «Посимвольное сравнение»:
**Отсутствует в XAML.** Сопоставление выполняется в `IDataImportService` / `ProjectExcelDataImportService` по типу данных.

### Обработчики (РАЗВИЛКИ):
| Событие | Метод | Логика |
|---------|-------|--------|
| ImportAsync | DataImportDialogViewModel | if (SelectedFilePath empty) → ErrorMessage="Выберите файл"; else → switch(DataType) import |
| BrowseFile | — | Filter Excel; if OK → set path + Description=filename |
| After import | — | if labor → may show unmatched types dialog (см. VM далее) |

**Источник тестов:** `DataImportServiceTests.cs` (legacy), `EquipmentImportCatalogMatcherTests.cs` — Ordinal match для equipment.

---

## DataPageView.xaml (legacy контейнер данных проекта)

### Назначение:
`DXTabControl` с вкладками данных проекта (используется из `ProjectEditWindow` и навигации).

### Вкладки (РАЗВИЛКА — `DataPageViewModel.BuildTabs`):
| Вкладка | View | Permission |
|---------|------|------------|
| Версии анализа | ProjectAnalysisVersionsWindow | ProjectAnalysisVersions |
| Физ. объёмы по проекту | ProjectPhysicalVolumesWindow | ProjectPhysicalVolumes |
| Трудоёмкость по проекту | ProjectLaborIntensitiesWindow | ProjectLaborIntensities |

**Примечание:** основной UX перенесён в `ProjectsPageView`; DataPage — legacy/вложенный режим.

---

## Forms — аналитические формы

| Форма | Назначение | DevExpress | Ключевые элементы |
|-------|------------|------------|-------------------|
| `AssessmentForm.xaml` | Оценка факта (Z-score, интерпретация) | dxg GridControl×N | Performance rows, цвета через InterpretationToBrushConverter |
| `PlanningForm.xaml` | Прогнозирование / плановые объёмы | dxg, dxe | Planned volumes, save command CanExecute |
| `VersionComparisonForm.xaml` | Сравнение версий A/B | dxg | Merge rows |
| `GroupAnalysisForm.xaml` | Групповой анализ | dxg | Multi-project (WPF) |
| `GroupTrendForm.xaml` | Групповой тренд | — | Charts/tables |
| `ProjectDataUploadForm.xaml` | Загрузка данных проекта | dx | Upload UI |

**РАЗВИЛКА PlanningForm:** `CanSavePlannedVolumes` — RelayCommand CanExecute в `PlanningFormViewModel`.

---

## Модальные окна — полный реестр

| Диалог | Открывается из | Поля / действия |
|--------|----------------|-----------------|
| `ProjectEditWindow` | Projects | Проект, вкладки версий/участников, DXGrid |
| `ProjectQuickEditDialog` | Projects | Быстрое редактирование |
| `ProjectPickerDialog` | ProjectsPage | Выбор проекта; context: Edit, Archive |
| `ProjectVersionPickerDialog` | ProjectsPage | Выбор версии; Copy, Edit, Delete |
| `ProjectAnalysisVersionCopyDialog` | Version picker | Copy with data |
| `ProjectAnalysisVersionEditWindow` | Versions | name, description, direction |
| `ConfirmDialog` | various | OK/Cancel |
| `IterationDeleteDialog` | Import history | Rollback import iteration |
| `ProjectSelectionDialog` | Group analysis | Multi-select projects |
| `ProjectParticipantsDialog` | Projects | Grid участников |
| `ProjectAdditionalFieldsWindow` | Project data | Доп. поля |
| `AccessRightsWindow` | Settings | Матрица прав (дерево) |
| `UserEditWindow` | Users | CRUD пользователя |
| `*EditWindow` (10+) | Справочники | Сущностно-специфичные поля |

---

## Паттерны доступности UI (общие РАЗВИЛКИ)

```text
// MainViewModel.BuildNavigation
foreach (navItem)
  if (accessControl.CanRead(CurrentUser, objectName))
    add to NavigationGroups
  else
    skip item entirely

// ReferencesPageViewModel / DataPageViewModel
foreach (tabConfig)
  if (!accessControl.CanRead(user, accessObjectName)) continue
  else Tabs.Add(tab)

// DirectoryViewModelBase commands
[RelayCommand] DeleteItem — typically requires CanEdit on object (override in derived VM)

// ProjectsPageViewModel import buttons
IsEnabled = PlanVersionId != null (via NullToBool converter)
```

**Роли в seed (не «Редактор/Пользователь» в коде):** Administrator, Manager (Менеджер проекта), Observer (Наблюдатель) — см. `migrations/data/002_SeedData.sql`.

---

## Миграция UI → React (уже сопоставлено)

| WPF | Web (client/) | Статус |
|-----|---------------|--------|
| MainWindow sidebar | `AppLayout.tsx` | Частично (не все nav gated) |
| ReferencesPage tabs | `ReferencesLayout` + `DirectoryPage` | Реализовано |
| PhysicalVolumesWindow tree | `DirectoryPage` + AG Grid treeData | Без drag-drop |
| ProjectsPageView | `ProjectsPage` + `ProjectDetailPage` | Частично |
| DataImportDialog | `ImportWizard.tsx` | Без description field parity |
| LoginWindow | `App.tsx` LoginForm | Реализовано |
| AccessRightsWindow | `AccessRightsPage.tsx` | Реализовано |
| UserActionLogView | `UserActionLogPage.tsx` | Реализовано |

---

## Файл подготовлен

Детально разобраны: shell, login, справочники (эталон PV), ProjectsPage (контекстные меню), import dialog, converters, DevExpress patterns, каталог 68 форм.

**Не детализированы построчно (низкий риск / дублируют паттерн):** отдельные `*EditWindow` для каждой сущности — структура: dxe-поля + OK/Cancel + validation в VM.
