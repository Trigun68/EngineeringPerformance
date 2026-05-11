# Сохранение состояния детальной сетки (Forecast / Fact)

**Статус:** ✅ Реализовано (ISS-002 закрыта 2026-05-09, итерация 2 + UI-подтверждение пользователя)

## Назначение

При редактировании строки детальной сетки (физ. объём / номенклатура / норма IIC) сетка перезагружает данные через смену `ItemsSource`. Без специальной обработки:
- DevExpress теряет фокус (первая видимая строка становится focused).
- Скролл прыгает в начало.
- Раскрытие групп схлопывается.
- Двойной рендер вызывает мигание.

Текущая реализация комбинирует **нативный механизм DevExpress** + **минимальную ручную фиксацию скролла**.

## Где применяется

`EngineeringPerformance.App/Views/ProjectsPageView.xaml`:
- `DetailForecastGrid` (вкладка «Прогнозирование», под-вкладка «Детально»).
- `FactDetailGrid` (вкладка «Оценка факта», под-вкладка «Детально»).

## Метод

### XAML — нативный DevExpress

```xml
<dxg:GridControl ...
                 RestoreStateOnSourceChange="True"
                 RestoreStateKeyFieldName="ProjectPhysicalVolumeId">
```

Эти два свойства поручают DevExpress самому сохранять/восстанавливать **focused row** и **раскрытие групп** при смене `ItemsSource`. Ключ — `ProjectPhysicalVolumeId`, стабильный между перезагрузками (PK строки). До 2026-05-09 это делалось вручную (ExpandAll → CollapseGroupRow → присваивание `FocusedRowHandle`) — приводило к мигающему перерендеру и race-conditions.

### Code-behind — фиксация скролла

`ProjectsPageView.xaml.cs` устанавливает делегаты `Save*StateCallback` / `Restore*StateCallback` в `ProjectsPageViewModel`, делегируя реализацию в `DetailGridStateHelper`:

- `DetailGridStateHelper.Save(grid)` — сохраняет:
  - `TopRowIndex` (абсолютный),
  - `FocusedRowId` (ProjectPhysicalVolumeId сфокусированной строки),
  - `FocusedVisibleOffset` = визуальное расстояние от focused-строки до topmost-строки (в visible-индексах).
- `DetailGridStateHelper.Restore(grid, state)` — после смены `ItemsSource`:
  1. Если `FocusedRowId >= 0` — ищет визуальный индекс этой строки в новом снимке и ставит `TopRowIndex = visibleIndex - FocusedVisibleOffset`. Это «привязка к якорю»: focused-строка остаётся на той же высоте экрана.
  2. Если фокусной строки нет (была удалена) — fallback на абсолютный `TopRowIndex`.

DevExpress делает focus и группы сам (через `RestoreStateOnSourceChange`); code-behind отвечает только за вертикальный скролл, который DevExpress не сохраняет.

### VM — оркестрация

`ProjectsPageViewModel.LoadForecastDataAsync` / `LoadFactDataAsync`:
1. Перед заменой коллекции — `var state = SaveDetailForecastStateCallback?.Invoke();` (вызывается из команд редактирования, **не внутри** Load — иначе state снимается уже после смены ItemsSource и оказывается пустым).
2. Заменяет `DetailForecastData` / `FactDetailData` (DevExpress срабатывает `RestoreStateOnSourceChange`).
3. Через `Dispatcher.BeginInvoke` (после привязки) вызывает `RestoreDetailForecastStateCallback?.Invoke(state)` для восстановления скролла.

`DetailGridState` (POCO):
```csharp
public sealed class DetailGridState
{
    public int TopRowIndex { get; set; } = 0;
    public int FocusedRowId { get; set; } = -1;
    public int FocusedVisibleOffset { get; set; } = 0;
}
```

## История эволюции (для понимания, что НЕ работало)

| Дата | Подход | Почему отклонён |
|---|---|---|
| 2026-05-08 (e5f6725) | Сохранять `ExpandedGroupTexts` + `TopRowIndex`; CollapseAll → раскрыть нужные | После CollapseAll `VisibleRowCount` падал, часть групп не находилась |
| 2026-05-08 (01db0b7) | Перевернули: ExpandAll → схлопнуть лишние; Save вызывали внутри Load | Save внутри Load снимал состояние ПОСЛЕ смены ItemsSource — всегда пустое; перетирал корректный state |
| 2026-05-08 (6548b65) | UI-thread save | Помогло частично — состояние снималось правильно, но мигание оставалось |
| 2026-05-08 (3fe55e2) | Save expanded (а не collapsed) groups | Логика инвертирована, но проблема двойного рендера сохранялась |
| 2026-05-08 (f96000b) | RowHandle-якорь вместо TopRowIndex | Убрало смещение на 1 строку, но мигание от ручного управления группами осталось |
| 2026-05-09 (5502dd5) | FocusedRowId + offset (текущий fallback) | Стабильно для случая «нет групп»; сохраняет позицию относительно фокуса |
| 2026-05-09 (74eea05) | `RestoreStateOnSourceChange="True"` + `RestoreStateKeyFieldName` | DevExpress делает focus/groups, code-behind — только скролл |
| 2026-05-09 (ISS-002 iter. 2) | Унификация Fact-пути с Forecast (убран дублирующий `factState`-restore); `Math.Clamp` для `TopRowIndex` при уменьшении `VisibleRowCount`; `RestoreStateOnSourceChange` подключён к `CompareGrid` | **Текущая версия.** Снят один источник «вздрагивания» на Fact-вкладке; экран не прыгает при удалении строки |

## Итог (ISS-002 закрыта 2026-05-09)

См. [`docs/issues/ISS-002-detail-grid-flicker-revisit.md`](../issues/ISS-002-detail-grid-flicker-revisit.md). После итерации 2 + UI-подтверждения пользователя:
- ✅ Дублирующий restore-pass на Fact-пути устранён (был источником мигания).
- ✅ Fallback при отсутствии focused row — `Math.Clamp` к актуальному `VisibleRowCount`.
- ✅ `CompareGrid` подключён к нативному механизму DevExpress.
- ✅ UI-сценарии редактирования (Forecast/Fact, IIC-норма, удаление строки) проходят без визуального мигания и потери фокуса.

Микро-оптимизации (in-place `Add/Remove` или `BeginDataUpdate`/`EndDataUpdate`) и профайлинг layout-pass'ов оставлены как backlog-варианты на случай, если проблема вернётся в новых сценариях. Сейчас не требуются.

## Файлы

- View: `EngineeringPerformance.App/Views/ProjectsPageView.xaml` (строки 267–268 для Forecast, 554–555 для Fact)
- Code-behind: `EngineeringPerformance.App/Views/ProjectsPageView.xaml.cs` (wiring `Save/Restore*StateCallback` → `DetailGridStateHelper`)
- Helper: `EngineeringPerformance.App/Views/DetailGridStateHelper.cs` (`Save`, `Restore` — статические методы)
- VM: `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs` (`LoadForecastDataAsync`, `LoadFactDataAsync`, делегаты `Save*StateCallback` / `Restore*StateCallback`)
