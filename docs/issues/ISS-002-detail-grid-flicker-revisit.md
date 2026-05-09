# ISS-002: Доработать сохранение состояния детальной сетки (revisit)

**Создано:** 2026-05-09
**Обновлено:** 2026-05-09 (итерация 2 — частично закрыта)
**Приоритет:** 🟡 Medium
**Статус:** 🔄 In progress — закрыты пункты 3 и 5; пункты 1/2/4/6 ждут UI-сессии пользователя
**Связано:** [`docs/features/detail-grid-state-preservation.md`](../features/detail-grid-state-preservation.md)

## Контекст

После серии итераций (см. таблицу эволюции в feature-doc) пришли к комбинации:
- DevExpress нативно через `RestoreStateOnSourceChange="True"` + `RestoreStateKeyFieldName="ProjectPhysicalVolumeId"` — focus и группы.
- Ручной `SaveGridState`/`RestoreGridState` в `ProjectsPageView.xaml.cs` — только вертикальный скролл (по якорю-фокусу с `FocusedVisibleOffset`).

Это базово рабочий вариант, но пользователь отметил, что хочет вернуться к методу — есть остаточные «вздрагивания» и возможная потеря фокуса.

**Итерация 2 (2026-05-09) — что сделано без UI-сессии:**
- Унификация `LoadFactDataAsync` с Forecast-путём — убран дублирующий `factState`-restore (остаток отклонённого 2026-05-08 анти-паттерна, перетирал корректное восстановление из вызывающей команды).
- `RestoreGridState`: при отсутствии focused row в новом снимке — `Math.Clamp(state.TopRowIndex, 0, VisibleRowCount-1)` вместо безусловного присваивания. При `VisibleRowCount == 0` — выход с `TopRowIndex = 0`.
- `CompareGrid` подключён к `RestoreStateOnSourceChange="True"` + `RestoreStateKeyFieldName="Id"` (стабильный `WorkDirectionId`).

## Что проверить / улучшить в следующей сессии

1. ⏳ **Воспроизвести вздрагивание.** Снять видео сценария редактирования (Прогнозирование → Детально → правый клик → Редактировать физ. объём → ОК). Зафиксировать, в какой момент происходит мигание: до, после или во время `Dispatcher.BeginInvoke` для `RestoreGridState`. **Особенно — проверить, помог ли фикс итерации 2 на Fact-вкладке.**

2. ⏳ **Проверить альтернативы вместо смены `ItemsSource`:**
   - Обновлять существующую `ObservableCollection` через `Add/Remove/Replace` (не пересоздавать) — DevExpress тогда вообще не будет ресетить state, fix может оказаться one-line.
   - Или использовать `BeginDataUpdate()` / `EndDataUpdate()` вокруг замены, чтобы DevExpress не делал промежуточных рендеров.

3. ✅ **Удалённая строка.** ~~Если `FocusedRowId` отсутствует в новом снимке (редактирование номенклатуры могло перевести её под другую группу или удалить), fallback на абсолютный `TopRowIndex` может сместить экран. Подумать: вернуть focus на ближайшую соседнюю строку (по индексу).~~ Закрыто в итерации 2: `Math.Clamp` к `[0, VisibleRowCount-1]` гарантирует, что экран не «прыгает» в случайную позицию. Возврат focus на ближайшую соседнюю строку **не делается** — DevExpress сам восстанавливает focus через `RestoreStateOnSourceChange`, а если строка-якорь удалена, focus переходит на первую доступную (поведение DevExpress, обычно приемлемое).

4. ⏳ **Замерить количество layout-pass'ов** через DevExpress profiling или WPF perfTip, чтобы подтвердить/опровергнуть гипотезу о двойном рендере.

5. ✅ **Проверить вкладку Сравнение версий (`CompareGrid` / `CompareDetailData`)** — ~~она тоже под-вкладка «Детально», но не имеет `RestoreStateOnSourceChange`. Возможно, нужно унифицировать.~~ Закрыто в итерации 2 для `CompareGrid` (TreeListView, ключ — `Id`). `CompareFacilityGrid` и `CompareDetailGrid` намеренно не тронуты: они перезагружаются только при явной смене Версии А/Б пользователем — preservation там семантически не имеет смысла, ожидается «свежий» вид.

6. ⏳ **Тест-кейс редактирования IIC-нормы:** там `state.FocusedRowId` указывает на строку, у которой `NormId` мог измениться, но `ProjectPhysicalVolumeId` стабилен — должно работать. Проверить.

## Файлы для возврата

- `EngineeringPerformance.App/Views/ProjectsPageView.xaml` — строки 267–268 (Forecast), 554–555 (Fact), 689–692 (Compare). `RestoreStateOnSourceChange`, `RestoreStateKeyFieldName`.
- `EngineeringPerformance.App/Views/ProjectsPageView.xaml.cs` — `SaveGridState` (45–68), `RestoreGridState` (70–98 после итерации 2).
- `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs` — `LoadForecastDataAsync` (~460–510), `LoadFactDataAsync` (~712–760, упрощён в итерации 2).

## Не делать

- Не возвращаться к ручному `ExpandAllGroups` / `CollapseGroupRow` / `FocusedRowHandle =` — этот путь уже признан тупиковым (см. историю в feature-doc).
- Не убирать `RestoreStateOnSourceChange` без замены — потеряется работающее восстановление focus и групп.
- Не возвращать снятие state внутри `Load*Async` — это перетрёт корректный `savedState` из вызывающей команды (отклонено 2026-05-08, повторно подтверждено в итерации 2 на Fact-пути).

## Definition of Done

- Один редактируемый сценарий (правый клик → Edit → OK) на дев-БД проходит без визуального мигания и без перепрыгивания скролла. Запись на видео в `_work/`.
- Если потеря фокуса воспроизводится при удалении строки — добавлен fallback на ближайшую соседнюю строку.
- Обновлены: `feature-doc` (раздел «Что осталось решить» убран или переделан), CHANGELOG `[Unreleased]`.
