# ISS-005: Контекстное меню Detail-сетки Fact + раздельные команды Fact/Plan

**Статус:** ✅ Closed (2026-05-11)
**Категория:** UX / ProjectsPage / DetailGrid
**Приоритет:** Medium
**Подход:** Вариант A — раздельные Fact-команды.

## Контекст
На странице «Проекты» две Detail-сетки:
- `DetailForecastGrid` — Прогнозирование → Детально (Plan-версия).
- `FactDetailGrid` — Оценка факта → Детально (Fact-версия).

В `ProjectsPageView.xaml.cs:28` обработчик `OnDetailForecastShowGridMenu` подписан только на `DetailForecastView.ShowGridMenu`. У `FactDetailGrid` контекстного меню нет — пользователь не может редактировать/удалять/копировать строки `ProjectPhysicalVolume`, открытые в версии факта.

Команды VM (`EditProjectPhysicalVolumeAsync`, `EditPhysicalVolumeNomenclatureAsync`, `EditIicNormAsync`, `DeleteProjectPhysicalVolumeAsync`, `CreateProjectPhysicalVolumeFromCopyAsync`, `CreateProjectPhysicalVolumeAsync`) после успеха вызывают **`LoadForecastDataAsync(pid, vid, state)`** и берут `PlanVersionId`, а не `FactVersionId`. Поэтому даже если просто прокинуть подписку на `FactDetailGrid`, после правки строка пропадёт/останется устаревшей: перезагрузится Plan-вкладка, а Fact-таблица не обновится.

## Проблема
1. Невозможно вызвать CRUD-меню на `FactDetailGrid` (нет подписки).
2. Существующие команды жёстко завязаны на Plan (берут `PlanVersionId`, вызывают `LoadForecastDataAsync`).
3. Save/Restore состояния детальной сетки тоже у обеих веток разный (`SaveDetailForecastStateCallback` vs `SaveFactDetailStateCallback`).

## Цель
Унифицировать поведение контекстного меню для Plan и Fact Detail-сеток, сохранив раздельные источники данных и колбэков состояния.

## Acceptance Criteria
- [x] На `FactDetailGrid` работает то же контекстное меню (6 пунктов: 3 редактирования, 3 CRUD), что и на `DetailForecastGrid`.
- [x] При запуске команды через Fact-меню — после успеха перезагружается `LoadFactDataAsync` (берётся `FactVersionId`), фокус и скролл `FactDetailGrid` сохраняются через `SaveFactDetailStateCallback`/`RestoreFactDetailStateCallback`.
- [x] При запуске команды через Plan-меню — поведение прежнее (`LoadForecastDataAsync`, `SaveDetailForecastStateCallback`).
- [x] Логирование в `UserActionLog` остаётся корректным (entityId, projectId, versionId соответствуют реальной версии, где произошло действие).
- [x] Кнопка «Редактировать норму IIC» дисейблится, если у строки нет `NormId` (как сейчас).
- [x] Сценарий «Добавить строку копированием выбранной» в Fact-версии корректно подставляет `FactVersionId` в новый `ProjectPhysicalVolume`.

## Возможные подходы
**A. Раздельные команды (`EditFactProjectPhysicalVolumeCommand` и т. п.).**
Плюсы: явное разделение, нулевой риск регрессии Plan. Минусы: дублирование тел методов.

**B. Параметризация (передавать `direction: AnalysisDirection` или `Action<int,int,DetailGridState?>` загрузчик в команду).**
Плюсы: один код. Минусы: усложнение сигнатур команд, конфликт с `[RelayCommand]` (тип параметра должен соответствовать привязке).

**C. Гибрид: один набор команд, внутри метода — `var versionId = sourceIsPlan ? PlanVersionId : FactVersionId; var reload = sourceIsPlan ? LoadForecastDataAsync : LoadFactDataAsync;`**, признак `sourceIsPlan` хранится в свойстве VM, выставляется при `ShowGridMenu` для соответствующей сетки.
Плюсы: компромисс. Минусы: скрытое состояние.

Рекомендация по обсуждению с пользователем: **A** — явное разделение проще читать и сопровождать.

## Стартовые точки
- `EngineeringPerformance.App/Views/ProjectsPageView.xaml.cs:28,51-95` — подписка и обработчик меню Plan.
- `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs` — команды `EditProjectPhysicalVolumeAsync` / `EditPhysicalVolumeNomenclatureAsync` / `EditIicNormAsync` / `DeleteProjectPhysicalVolumeAsync` / `CreateProjectPhysicalVolumeFromCopyAsync` / `CreateProjectPhysicalVolumeAsync` — каждая берёт `PlanVersionId` и вызывает `LoadForecastDataAsync`.
- `ProjectsPageView.xaml.cs:36-39` — колбэки состояния (`SaveDetailForecastStateCallback` для Plan, `SaveFactDetailStateCallback` для Fact) — уже разделены, дублировать не нужно.
- `EngineeringPerformance.App/Views/ProjectPhysicalVolumeCreateDialog.*` — диалог создания принимает `projectId`, `versionId`, поэтому достаточно прокинуть `FactVersionId`.

## Не делать
- Не объединять Save/Restore колбэки — они уже корректно разделены, переиспользование сломает фокус.
- Не показывать Fact-меню на Plan-гриде и наоборот.
- Не пытаться править Plan-команды задним числом — Plan уже работает, перепроверено в ISS-003.

## Связанные
- ISS-003 (закрыта) — контекстное меню Detail-сетки Plan, реализация которого вдохновляет этот тикет.
- Шаг 3 редизайна «Оценка факта» (2026-05-11) — после которого станет ясна модель `FactDetailRow`, контекстное меню должно работать с ней.
