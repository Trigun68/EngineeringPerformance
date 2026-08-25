# ISS-002: Доработать сохранение состояния детальной сетки (revisit)

**Создано:** 2026-05-09
**Обновлено:** 2026-05-09 (итерация 2 завершена, UI-проверка пользователя — ОК)
**Приоритет:** 🟡 Medium
**Статус:** ✅ Closed — UI-проверка подтвердила отсутствие вздрагиваний после `adb3e4e`. Пп. 2 и 4 (микро-оптимизации) не требуются — проблема не воспроизводится.
**Связано:** [`_doc/features/detail-grid-state-preservation.md`](../features/detail-grid-state-preservation.md)

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

1. ✅ **Воспроизвести вздрагивание.** UI-проверка пользователя 2026-05-09 после `adb3e4e`: сценарий редактирования (Прогнозирование/Оценка факта → Детально → ПКМ → Edit → OK) проходит без визуального мигания и потери фокуса.

2. 🚫 **Проверить альтернативы вместо смены `ItemsSource`** — не требуется. Микро-оптимизация имела смысл только при подтверждённом мигании; раз проблема не воспроизводится, in-place `Add/Remove` или `BeginDataUpdate`/`EndDataUpdate` остаются как backlog-вариант на случай, если проблема вернётся в будущих сценариях.

3. ✅ **Удалённая строка.** Закрыто в итерации 2: `Math.Clamp` к `[0, VisibleRowCount-1]` гарантирует, что экран не «прыгает» в случайную позицию. DevExpress сам восстанавливает focus на первую доступную строку через `RestoreStateOnSourceChange`.

4. 🚫 **Замерить количество layout-pass'ов** — не требуется. Двойной рендер был обусловлен дублирующим restore (DG-001), снят в итерации 2.

5. ✅ **Проверить вкладку Сравнение версий (`CompareGrid`)** — закрыто в итерации 2 (TreeListView, ключ — `Id`). `CompareFacilityGrid` и `CompareDetailGrid` намеренно не тронуты: они перезагружаются только при явной смене Версии А/Б — preservation там семантически не имеет смысла.

6. ✅ **Тест-кейс редактирования IIC-нормы** — UI-проверка пользователя 2026-05-09: проходит как часть общего сценария «правый клик → Edit → OK». `ProjectPhysicalVolumeId` стабилен при изменении `NormId`, focus восстанавливается корректно.

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
