# ISS-003: Контекстное меню детальной сетки прогнозирования — Удалить / Копировать / Добавить

**Статус:** ✅ Closed (2026-05-10, UI-подтверждение пользователя 2026-05-10 + расширение уникального ключа)
**Создано:** 2026-05-09
**Закрыто:** 2026-05-10
**UI-подтверждение:** 2026-05-10 — пользователь проверил все 3 сценария меню, копирование строки с одной номенклатурой на разный объект работает корректно после применения миграции `ExpandProjectPhysicalVolumeUniqueIndex`
**Приоритет:** 🟡 Medium
**Связано:** [`_doc/features/detail-grid-state-preservation.md`](../features/detail-grid-state-preservation.md), `OnDetailForecastShowGridMenu` в `ProjectsPageView.xaml.cs`, [`ADR-003`](../adr/ADR-003-project-physical-volume-unique-key.md)

## Резюме закрытия

Все 3 пункта меню реализованы и работают:
1. **Удалить выбранную строку** — confirm-диалог → `IDataTableService<ProjectPhysicalVolume>.DeleteAsync` → reload с `savedState`. Логирование `PhysicalVolumeDelete`. Коммит `61f034f`.
2. **Добавить копированием** — модальный диалог `ProjectPhysicalVolumeCreateDialog` (`forCopy: true`) с предзаполненными полями + lookup на справочник `PhysicalVolume`. Коммит `61f034f`.
3. **Добавить строку** — тот же диалог (`forCopy: false`), поля пустые. Коммит `61f034f`.

Сопутствующие правки:
- Понятное русское сообщение при нарушении уникального ключа БД (`IsUniqueViolation`) — коммит `a7acc44`.
- Расширение уникального составного индекса `ProjectPhysicalVolume` с 3 до 6 колонок (см. `ADR-003`) — после того как пользователь обнаружил, что старый ключ блокирует валидный сценарий «одна номенклатура на разных объектах» при копировании. Коммит выполнен в той же итерации.

Focus и scroll сохраняются после всех операций — паттерн `state = SaveDetailForecastStateCallback?.Invoke(); ... LoadForecastDataAsync(pid, vid, state)`.

## Контекст

На странице **Проекты → Прогнозирование → Детально** при правом клике по строке таблицы открывается контекстное меню. Сейчас в нём 3 пункта (от ISS-002):
- Редактировать физ. объём
- Редактировать номенклатуру физ. объёма
- Редактировать норму IIC

Нужно добавить **3 новых пункта** для CRUD строк физ. объёмов проекта (`ProjectPhysicalVolume`).

## Что нужно

### Пункт 1: «Удалить выбранную строку»

- Открывает модальное окно с подтверждением: текст «Удалить выбранную строку?», кнопки **Удалить** и **Отмена**.
- При **Удалить**: `DELETE FROM ProjectPhysicalVolumes WHERE Id = {row.ProjectPhysicalVolumeId}` (через `IDbContextFactory<AppDbContext>` или существующий сервис), затем перезагрузить детальную сетку через `LoadForecastDataAsync(pid, vid, savedState)` с **сохранением focus/scroll** (см. ISS-002).
- При **Отмена**: окно закрывается, ничего не меняется.
- Логировать в `UserActionLog` с `ActionType=PhysicalVolumeDelete`, `EntityType="ProjectPhysicalVolume"`, `EntityId=row.ProjectPhysicalVolumeId`.

### Пункт 2: «Добавить строку копированием выбранной»

- Открывает модальное окно создания нового `ProjectPhysicalVolume` со значениями строки, на которой был сделан клик.
- В окне показываются:
  - **Поля для заполнения**: `FacilityCode`, `FacilityName`, `BindingType`, `DocumentSet`, `CatalogId`, `Tag`, `WorkId`, `WorkName`, `WorkComment`, `EstimationBasis`, `Quantity`.
  - **Выпадающий список** для выбора номенклатуры физ. объёма (справочник `PhysicalVolumes`) — текущий `PhysicalVolumeId` подставлен.
- **НЕ показывать** поля, которые не относятся напрямую к `ProjectPhysicalVolume` (это вычисляемые/связанные значения отображения в гриде): `IsAssessment`, `Coefficient`, `IicGroupName`, `IicUnitName`, `NormProductivity`, `NormStdDev`, `BaseHours`, `OptimisticHours`, `PessimisticHours`. Эти поля живут в связанных сущностях (`PhysicalVolume`, `IicWorkDirectionNorm`) и не должны редактироваться отсюда.
- 2 кнопки: **Сохранить** и **Отмена**.
- При **Сохранить**: `INSERT INTO ProjectPhysicalVolumes` с заданными полями, `ProjectId/VersionId` берутся из контекста VM. Затем перезагрузка сетки с сохранением состояния. Logging: `PhysicalVolumeCreate`.
- При **Отмена**: ничего не меняется.

### Пункт 3: «Добавить строку»

- То же, что пункт 2, но **поля пустые** (по умолчанию). Lookups те же.
- Тот же модальный окно с теми же кнопками и поведением.
- Logging: `PhysicalVolumeCreate`.

**Реализационная подсказка**: можно сделать **один** диалог `ProjectPhysicalVolumeCreateDialog` с двумя режимами `Prepare(forCopy: bool, source: DetailForecastItem?)` — экономит код и UI.

## Acceptance criteria

- [ ] 3 новых пункта в контекстном меню `DetailForecastGrid` (`OnDetailForecastShowGridMenu`).
- [ ] Удаление: confirm-dialog → delete in DB → reload с `savedState` (focus/scroll сохранены).
- [ ] Копирование: окно с предзаполненными полями + lookup на `PhysicalVolume`.
- [ ] Добавление: то же окно, поля пустые.
- [ ] **Без потери фокуса/скролла** на детальной сетке после операций — использовать `SaveDetailForecastStateCallback` / `LoadForecastDataAsync(..., state)` (паттерн из `EditProjectPhysicalVolumeAsync` ~1268).
- [ ] Логирование в `UserActionLog`: `PhysicalVolumeCreate`, `PhysicalVolumeDelete`.
- [ ] CHANGELOG `[Unreleased]` обновлён.
- [ ] `dotnet build` 0 ошибок 0 warnings.
- [ ] `dotnet test` 111+/111+ green (если есть юнит-тесты на VM-команды — добавить).

## Стартовые точки (для новой сессии)

**Прочитать первыми:**
1. `EngineeringPerformance.App/Views/ProjectsPageView.xaml.cs` — `OnDetailForecastShowGridMenu` (~100): сюда добавлять 3 `BarButtonItem`.
2. `EngineeringPerformance.App/Views/PhysicalVolumeQuickEditDialog.xaml` (+`.cs`) — паттерн модального диалога с lookup-ом на `PhysicalVolume`. **Это образец**.
3. `EngineeringPerformance.App/ViewModels/PhysicalVolumeQuickEditViewModel.cs` — паттерн VM с `Prepare(...)` + `LoadLookupsAsync()`.
4. `EngineeringPerformance.App/Views/IterationDeleteDialog.xaml` (+`.cs`) — образец confirm-dialog.
5. `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs:1267-1334` — `EditProjectPhysicalVolumeAsync` / `EditPhysicalVolumeNomenclatureAsync` / `EditIicNormAsync`: паттерн `state = SaveDetailForecastStateCallback?.Invoke(); ... await LoadForecastDataAsync(pid, vid, state);`.
6. `EngineeringPerformance.Core` — сущность `ProjectPhysicalVolume` (поля), `PhysicalVolume` (для lookup).
7. `EngineeringPerformance.Core.Enums.UserActionType` — есть ли уже `PhysicalVolumeCreate`/`PhysicalVolumeDelete`. Если нет — добавить.

**Создаваемые файлы (предположительно):**
- `EngineeringPerformance.App/Views/ProjectPhysicalVolumeCreateDialog.xaml(.cs)` — окно создания (с режимом «копия»/«пусто»).
- `EngineeringPerformance.App/ViewModels/ProjectPhysicalVolumeCreateViewModel.cs` — VM окна с lookup на `PhysicalVolumes`.
- `EngineeringPerformance.App/Services/IProjectPhysicalVolumeCreateDialogService.cs` (+ реализация) — фабрика, как для существующих диалогов.
- Возможно `EngineeringPerformance.App/Views/ConfirmDialog.xaml` (если нет переиспользуемого confirm-окна — посмотреть наличие в проекте сначала, может уже есть).

**DI**: регистрация в `App.xaml.cs` (Transient для View, Singleton для VM, Singleton для DialogService).

## Не делать

- ❌ Не редактировать `IsAssessment`, `Coefficient`, `IicGroupName`, `IicUnitName`, `NormProductivity`, `NormStdDev` в этом окне — они живут в `PhysicalVolume`/`IicWorkDirectionNorm`, для них есть отдельные диалоги.
- ❌ Не использовать `MessageBox.Show(...)` для confirm-дialog — стиль проекта = модальное окно с кастомным VM (паттерн `IterationDeleteDialog`).
- ❌ Не возвращать `ItemsSource = new ObservableCollection<...>` без `SaveDetailForecastStateCallback?.Invoke()` ДО вызова — иначе фокус слетит (см. ISS-002 «Не делать»).
- ❌ Не добавлять CRUD на `PhysicalVolume` (саму сущность каталога) из этого окна — это отдельный экран.

## Definition of Done

- 3 новых пункта меню работают по сценариям 1/2/3.
- Удаление + создание корректно отражаются в БД и в гриде после reload.
- Focus и scroll сохраняются на детальной сетке после reload (визуальная проверка пользователя — как для ISS-002).
- 0 build warnings, тесты зелёные.
- Запушено в `origin/master`.
