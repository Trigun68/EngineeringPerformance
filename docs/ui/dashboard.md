# Главная (Dashboard)

**Статус**: Реализовано  
**Дата обновления**: 2026-05-14  
**Связано с**: [Project Modal Redesign](../features/project-modal-redesign.md), [Project Analysis](../features/project-analysis.md)

## Назначение

Главная вкладка — стартовая страница приложения. Открывается автоматически при запуске (`MainViewModel.ShowDashboard()`), служит точкой входа в навигацию и отображает методологию оценки проектов.

На главной в первую очередь показываются **диаграмма «Интерпретация стандартного отклонения (σ)»** (`Assets/standard-deviation-interpretation.png`, масштаб ~80% от прежней ширины) и **подпись к рисунку**, затем блок **«Методология оценки проектов»** (Z-score, норма из снимка версии, пороги как в `PerformanceInterpretationHelper`). KPI «Активные проекты» не используется.

## Архитектура

```
┌────────────────────────────────────────────────────┐
│              UI Layer (WPF)                         │
│   ┌──────────────────────────────────────────────┐  │
│   │ ProjectDashboardView (UserControl)           │  │
│   │  └─ ProjectDashboardViewModel                  │  │
│   │     (ILoadableViewModel, LoadAsync — no-op)   │  │
│   └──────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────┘
```

## Компоненты

### `ProjectDashboardViewModel`

**Файл:** `EngineeringPerformance.App/ViewModels/ProjectDashboardViewModel.cs`

Реализует `ILoadableViewModel` для единообразия навигации; `LoadAsync` — пустая (`Task.CompletedTask`), данные на главной не подгружаются.

### `ProjectDashboardView.xaml`

**Файл:** `EngineeringPerformance.App/Views/ProjectDashboardView.xaml`

Корневой `ScrollViewer` → `StackPanel`: заголовок к диаграмме, `Image` (`MaxWidth` 704, контейнер `MaxWidth` 736 ≈ −20% к прежним 880/920), подпись под рисунком, заголовок «Методология оценки проектов», `Border` с тремя абзацами текста (`MaxWidth` 736).

## Навигация

`MainViewModel.ShowDashboard()` уже вызывался при старте приложения, поэтому отдельных правок навигации не потребовалось. Маршрут `Dashboard` зарегистрирован в `NavigationService` и открывается:

- автоматически при инициализации `MainWindow`,
- по нажатию пункта «Главная» в боковом меню.

## Расширение

При необходимости KPI (счётчики по портфелю) можно снова добавить в `ProjectDashboardView` и расширить `ProjectDashboardViewModel` с вызовом `IProjectVisibilityService` / `IPerformanceService`.
