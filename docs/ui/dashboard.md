# Главная (Dashboard)

**Статус**: Реализовано
**Дата**: 2026-05-05
**Связано с**: [Project Modal Redesign](../features/project-modal-redesign.md), [Project Analysis](../features/project-analysis.md)

## Назначение

Главная вкладка — стартовая страница приложения. Открывается автоматически при запуске (`MainViewModel.ShowDashboard()`), служит точкой входа в навигацию и местом размещения сводных KPI по портфелю проектов.

В первой итерации содержит одну крупную KPI-плитку «Активные проекты» — число проектов со статусом `«В работе»`, доступных текущему пользователю.

## Архитектура

```
┌────────────────────────────────────────────────────┐
│              UI Layer (WPF)                         │
│   ┌──────────────────────────────────────────────┐  │
│   │ ProjectDashboardView (UserControl)           │  │
│   │  └─ ProjectDashboardViewModel                │  │
│   │     (ILoadableViewModel)                     │  │
│   └──────────────────┬───────────────────────────┘  │
└──────────────────────┼──────────────────────────────┘
                       ▼
┌────────────────────────────────────────────────────┐
│              Services Layer                         │
│   ┌──────────────────────────────────────────────┐  │
│   │ IProjectVisibilityService                    │  │
│   │  └─ GetAccessibleProjectsAsync(currentUser)  │  │
│   └──────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────┘
```

## Компоненты

### `ProjectDashboardViewModel`

**Файл:** `EngineeringPerformance.App/ViewModels/ProjectDashboardViewModel.cs`

Реализует `ILoadableViewModel`, чтобы навигация через `NavigationService.LoadView` могла перезагрузить счётчик при возврате на главную.

**Свойства:**

| Свойство | Тип | Источник |
|----------|-----|----------|
| `ActiveProjectsCount` | `int` | Кол-во проектов с `WorkStatus == "В работе"` после фильтрации `IProjectVisibilityService` |
| `IsLoading` | `bool` | Флаг загрузки (для индикатора) |

**Метод `LoadAsync`:**

```csharp
var accessible = await _projectVisibilityService.GetAccessibleProjectsAsync();
ActiveProjectsCount = accessible.Count(p => p.WorkStatus == "В работе");
```

### `ProjectDashboardView.xaml`

**Файл:** `EngineeringPerformance.App/Views/ProjectDashboardView.xaml`

Один `UniformGrid Columns="3"` с одной заполненной плиткой:

- иконка проекта (Segoe Fluent Icons),
- надпись «Активные проекты»,
- крупное число (`FontSize="64"`, цвет акцента `#2563EB`),
- подпись «Проекты со статусом „В работе“».

Стиль карточки: фон `#FFFFFF`, скругление 8, тень — соответствует дашбордным паттернам из `.src/`.

## Навигация

`MainViewModel.ShowDashboard()` уже вызывался при старте приложения, поэтому отдельных правок навигации не потребовалось. Маршрут `Dashboard` зарегистрирован в `NavigationService` и открывается:

- автоматически при инициализации `MainWindow`,
- по нажатию пункта «Главная» в боковом меню.

## Учёт прав доступа

Подсчёт идёт через `IProjectVisibilityService.GetAccessibleProjectsAsync`, который применяет фильтрацию по `ProjectUserAccess` для текущего пользователя (`ICurrentUserService`). Системный администратор видит все проекты, остальные — только те, к которым у них есть доступ.

## Расширение

Следующие KPI-плитки можно добавить в тот же `UniformGrid` (3 колонки уже зарезервировано):

- «Версии анализа за период»
- «Среднее PEPM по портфелю»
- «Проекты с критическим Z-score»

Для каждой плитки — отдельное свойство в VM и соответствующий запрос в `IProjectVisibilityService` или `IPerformanceService`.
