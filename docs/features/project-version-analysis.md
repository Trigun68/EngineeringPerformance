# Анализ версии проекта (отчёт)

**Статус**: Реализовано
**Дата**: 2026-05-05
**Связано с**: [Project Analysis Feature](project-analysis.md), [Performance Analytics](performance-analytics.md)

## Назначение

Отчёт «Анализ версии проекта» открывается по кнопке под блоком «Производительность» на странице редактирования проекта и собирает в одном окне:

- полностью раскрытую иерархию справочника направлений IIC (с единицами измерения);
- ненулевые физ. объёмы проекта с привязкой к направлениям (номенклатура, ед. изм., количество);
- отдельный блок видов работ из трудоёмкости проекта для сопоставления;
- показатели производительности (факт/норм) и Z‑оценку с интерпретацией по каждому направлению.

Назначение — предоставить ГИПу/инженеру‑аналитику единый сводный вид по версии без переключения между карточкой проекта, физ. объёмами и трудоёмкостью.

## Архитектура

```
┌────────────────────────────────────────────────────────────────┐
│                       UI Layer (WPF)                           │
│  ┌──────────────────────────┐   ┌──────────────────────────┐   │
│  │ ProjectEditWindow        │   │ ProjectVersionAnalysis   │   │
│  │  └─ ProjectEdit          │──►│ Window (modal)           │   │
│  │     ViewModel            │   │  └─ ProjectVersion       │   │
│  │     (OpenVersionAnalysis)│   │     AnalysisViewModel    │   │
│  └──────────────────────────┘   └───────────┬──────────────┘   │
└────────────────────────────────────────────┼──────────────────┘
                                             │
                                             ▼
┌────────────────────────────────────────────────────────────────┐
│                       Services Layer                           │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ IProjectVersionAnalysisService                           │  │
│  │  └─ ProjectVersionAnalysisService                        │  │
│  │     ├── IPerformanceService (иерархия + метрики)         │  │
│  │     ├── AppDbContext: ProjectPhysicalVolumes (физ.объёмы)│  │
│  │     └── AppDbContext: ProjectLaborIntensities (виды раб.)│  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

### Источники данных

| Данные | Источник | Примечание |
|---|---|---|
| Иерархия + производительность | `IPerformanceService.GetPerformanceDataAsync` | Тот же источник, что у блока «Производительность» на странице проекта, расчёт `FactEstimate` — средне‑взвешенный по часам поддерева |
| Ненулевые физ. объёмы | `ProjectPhysicalVolumes` + `Include(PhysicalVolume.Unit)` | Фильтр: `Quantity > 0` и `ClassifierId != null`; группировка по `WorkDirectionId` |
| Виды работ | `ProjectLaborIntensities` + `Include(WorkDirection)` | Сортировка по `DocumentationCode` |

## DTOs

`EngineeringPerformance.Core/DTOs/ProjectVersionAnalysisDto.cs`:

- `ProjectVersionAnalysisDto` — верхний уровень отчёта (ProjectName, VersionName, Directions, LaborIntensities).
- `ProjectVersionDirectionDto` — узел иерархии IIC с метриками и вложенным списком физ. объёмов.
- `ProjectVersionPhysicalVolumeDto` — ненулевая позиция физ. объёма проекта (Code, Name, UnitName, Quantity).
- `ProjectVersionLaborIntensityDto` — запись видов работ (DocumentationCode, Brand, WorkDirectionName, PlannedHours, ActualHours).

## UI

### Точка входа

Кнопка «Анализ версии проекта» (`@EngineeringPerformance.App/Views/ProjectEditWindow.xaml`) добавлена под блоком «Производительность» (Row 3 основной сетки). Команда — `OpenVersionAnalysisCommand` в `ProjectEditViewModel`. Требует, чтобы проект был сохранён и была выбрана версия анализа.

### Окно отчёта

`@EngineeringPerformance.App/Views/ProjectVersionAnalysisWindow.xaml` — модальное окно 1400×800 с заголовком проекта/версии, кнопками «Обновить»/«Закрыть» и прокручиваемой областью:

1. **Заголовок секции «Справочник направлений IIC (полная иерархия)»** — `ItemsControl` с шаблоном‑карточкой по каждому направлению:
   - шифр (моноширинный badge) + название + ед. изм.
   - отступ слева по уровню иерархии (`IndentMargin = Level × 24`);
   - строка KPI (8 метрик): Факт ФО · Факт труд. · Факт произв. · Факт оценка · Норм произв. · Норм оценка · Норм СО · Z‑оценка (с цветовым индикатором);
   - строка «Интерпретация» с текстом из `PerformanceInterpretation.ToRussian()`;
   - вложенный `GridControl` с ненулевыми физ. объёмами (если есть) — код, номенклатура, ед. изм., количество.

2. **Секция «Виды работ (трудоёмкость проекта)»** — единый `GridControl` с колонками: шифр документации, марка, направление IIC, план/факт, ч; нижний итог `Sum` по часам.

### Дизайн‑референс

Макет построен по карточно‑сеточному паттерну дашбордов из `.src/` (файлы `56e47961-*.jpg`, `f51904ad-*.webp`): крупные метрики, нейтральный фон `#F0F2F5`, карточки `#FFFFFF` с тенью и скруглением 8, цветовая палитра акцентов — синий `#2563EB` (шифр), индикатор интерпретации — через `InterpretationToBrushConverter`.

## DI

Регистрация в `App.xaml.cs`:

```csharp
services.AddSingleton<IProjectVersionAnalysisService, ProjectVersionAnalysisService>();
services.AddTransient<ProjectVersionAnalysisWindow>();
services.AddTransient<ProjectVersionAnalysisViewModel>();
```

VM зарегистрирован как `Transient`, чтобы каждое открытие окна получало чистое состояние.

## Взаимодействие с другими модулями

- **PerformanceService**: после правки в рамках задачи #16 `FactEstimate` для родительских узлов — средне‑взвешенный по часам поддерева, `Z‑score` и `Interpretation` пересчитываются. Отчёт автоматически получает эти значения.
- **ProjectAnalysisService (AssessmentForm)**: использует тот же `IPerformanceService`, поэтому метрики дисциплин в обоих представлениях совпадают.
- **ProjectEdit**: команда `OpenVersionAnalysis` не закрывает окно редактирования; отчёт открывается как модальный диалог с `Owner = GetOwnerWindow()`.

## Ограничения

- Отчёт не поддерживает экспорт в Excel (планируется отдельной задачей).
- Изменение фильтров/сортировки внутри вложенного `GridControl` физ. объёмов не сохраняется между сессиями.
- При большом объёме IIC (сотни узлов) рендер карточек может замедлять скролл; при необходимости стоит перевести верхнюю секцию на `VirtualizingStackPanel`/`TreeListView` с `DataControlDetailDescriptor`.
