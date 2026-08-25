# Журнал действий пользователей

**Статус:** ✅ Реализовано
**Дата:** 2026-05-09
**ADR:** [`ADR-002-user-action-journal.md`](../adr/ADR-002-user-action-journal.md)
**Связанные задачи:** `orch-2026-05-09-12-15-user-action-journal`

## Назначение

Аудит ключевых действий пользователей: вход/выход, CRUD проектов и версий анализа, импорт и удаление физических объёмов / трудоёмкости, редактирование строк детальной сетки. Хранит **только метаданные** (кто/что/когда/над какой сущностью); старые/новые значения не сохраняются.

Просмотр и управление журналом — на странице **Настройки → Журнал действий** (доступно только Admin).

## Схема БД

Таблица `dbo.UserActionLogs` (миграция `20260509112303_AddUserActionLogs`, SQL-дубль `migrations/schema/015_AddUserActionLogs.sql`):

| Поле | Тип | Назначение |
|---|---|---|
| `Id` | `int IDENTITY PK` | первичный ключ |
| `UserId` | `int FK Users.Id (Restrict)` | автор действия |
| `Timestamp` | `datetime2(3)` UTC | время события |
| `ActionType` | `nvarchar(64)` | имя enum-значения `UserActionType` |
| `EntityType` | `nvarchar(100) NULL` | имя сущности (`Project`, `ProjectAnalysisVersion`, …) |
| `EntityId` | `int NULL` | ID затронутой записи |
| `Description` | `nvarchar(500) NULL` | человекочитаемое описание |
| `IpOrHost` | `nvarchar(64) NULL` | `Environment.MachineName` |

**Индексы:** `(UserId, Timestamp DESC)`, `(ActionType)`, `(EntityType, EntityId)`.

## Категории действий (enum `UserActionType`)

| Группа | Значения |
|---|---|
| Аутентификация | `Login`, `Logout`, `LoginFailed` |
| Проекты | `ProjectCreate`, `ProjectEdit`, `ProjectDelete` |
| Версии анализа | `VersionCreate`, `VersionEdit`, `VersionDelete` |
| Физические объёмы | `PhysicalVolumeImport`, `PhysicalVolumeDelete`, `PhysicalVolumeEdit` |
| Трудоёмкость | `LaborIntensityImport`, `LaborIntensityDelete`, `LaborIntensityEdit` |
| Норма IIC | `IicNormEdit` |

## Точки внедрения логирования

| Действие | Где пишется | Файл |
|---|---|---|
| `Login` (Manual / Auto) | `AuthenticationService.LoginAsync` / `TryAutoLoginAsync` | `EngineeringPerformance.Infrastructure/Services/AuthenticationService.cs` |
| `LoginFailed` | `AuthenticationService.LoginAsync` (для известного UserName, неверный пароль) | `AuthenticationService.cs` |
| `ProjectCreate` / `ProjectEdit` / `ProjectDelete` | `ProjectPickerDialogViewModel.AddProjectAsync` / `EditProjectAsync` / `DeleteProjectAsync` | `EngineeringPerformance.App/ViewModels/ProjectPickerDialogViewModel.cs` |
| `ProjectCreate` / `ProjectEdit` (через окно редактирования) | `ProjectEditViewModel.SaveAsync` | `ProjectEditViewModel.cs` |
| `VersionCreate` / `VersionEdit` | `ProjectAnalysisVersionEditViewModel.SaveAsync` | `ProjectAnalysisVersionEditViewModel.cs` |
| `VersionDelete` | `ProjectEditViewModel.DeleteVersionAsync` | `ProjectEditViewModel.cs` |
| `PhysicalVolumeImport` / `LaborIntensityImport` | `DataImportDialogViewModel.ImportAsync` | `DataImportDialogViewModel.cs` |
| `PhysicalVolumeDelete` / `LaborIntensityDelete` | `IterationDeleteDialogViewModel.DeleteAsync` | `IterationDeleteDialogViewModel.cs` |
| `PhysicalVolumeEdit` / `LaborIntensityEdit` / `IicNormEdit` | `ProjectsPageViewModel.EditProjectPhysicalVolumeAsync` / `EditPhysicalVolumeNomenclatureAsync` / `EditIicNormAsync` | `ProjectsPageViewModel.cs` |

`Logout` — на текущем этапе не пишется, в приложении нет явной команды выхода (сессия завершается только закрытием приложения).

## Контракт сервиса

`EngineeringPerformance.Core/Services/IUserActionLogService.cs`:

- `LogAsync(userId, actionType, entityType?, entityId?, description?, ct)` — best-effort. Ошибки записи в журнал **не пробрасываются наружу**, пишутся только в Serilog (чтобы не ломать основную бизнес-операцию).
- `GetAsync(filter, skip, take, ct)` — выборка с фильтром (`UserActionLogFilter`: период UTC, UserId, ActionType, EntityType, EntityId) и пагинацией. Сортировка — `Timestamp DESC, Id DESC`.
- `ExportToFileAndPurgeAsync(filePath, ct)` — пишет CSV (UTF-8 BOM, разделитель `;`, RFC 4180-экранирование), затем `ExecuteDeleteAsync` по таблице. Файл пишется **до** удаления — при ошибке IO БД остаётся нетронутой. Возвращает `UserActionLogExportResult(FilePath, ExportedCount, DeletedCount)`.
- `ImportFromFileAsync(filePath, ct)` — читает CSV, выполняет дедупликацию по `(UserId, Timestamp, ActionType, EntityType, EntityId)`. Записи с `UserId`, отсутствующим в БД, попадают в orphan-bucket и пропускаются. Возвращает `UserActionLogImportResult(TotalRows, ImportedCount, SkippedDuplicates, SkippedOrphans, SkippedInvalid, Errors)`.

Реализация: `EngineeringPerformance.Infrastructure/Services/UserActionLogService.cs`.

## Формат CSV

```
Id;UserId;UserName;Timestamp;ActionType;EntityType;EntityId;Description;IpOrHost
```

- Кодировка — UTF-8 BOM, разделитель — `;` (Excel-friendly RU).
- `Timestamp` — ISO-8601 UTC с миллисекундами: `2026-05-09T11:23:03.123Z`.
- Экранирование значений с `;`, `"`, `\r`, `\n` — двойными кавычками; внутренние кавычки удваиваются (`"…""…"`).
- `UserName` пишется в файл информационно. При импорте используется только `UserId`; если он отсутствует в БД — строка пропускается с записью в `Errors`.

## UI

**Расположение:** Настройки → вкладка «Журнал действий».

`EngineeringPerformance.App/Views/UserActionLogView.xaml` + `UserActionLogViewModel`:

- Фильтры: период (`DateEdit`, по умолчанию −30 дней до сегодня), пользователь (`ComboBoxEdit`), тип действия (`ComboBoxEdit`).
- Тулбар: «Обновить», «Сохранить в файл и удалить» (с подтверждением + проверкой роли Administrator), «Восстановить из файла», статус-сообщение.
- Грид (`DevExpress GridControl`): колонки Time UTC, User, Action, Entity, Id, Description, Host. Группировка через `ShowGroupPanel="True"`.
- Пагинация — простая, лимит 1000 записей за выборку.

## Доступ

| Роль | Просмотр | Экспорт+Удалить | Импорт |
|---|---|---|---|
| Administrator | ✅ | ✅ | ✅ |
| Manager / Observer | ❌ (вкладка скрыта) | ❌ | ❌ |

Контроль реализован через `AccessControlService.CanRead(user, "UserActionLog")` в `SettingsPageViewModel.BuildTabs` (вкладка не показывается без права чтения) и через runtime-проверку роли `Administrator` перед `ExportAndPurgeAsync`. Объект `UserActionLog` зарегистрирован в `AccessControlConstants` с родителем `SettingsPage`.

## Что НЕ реализовано (намеренно)

- **Авто-архивация по расписанию / порогу** — backlog: [`_doc/issues/ISS-001-user-action-journal-auto-archival.md`](../issues/ISS-001-user-action-journal-auto-archival.md).
- **Old/New value capture** — отказались ради компактности БД.
- **Логирование чтений / просмотров** — журналируются только writes/state changes.
- **Logout** — нет явной команды выхода в приложении.

## История изменений

См. `_doc/changelog/CHANGELOG.md`, секции `[Unreleased]` от 2026-05-09:
- `user action log schema`
- `user action log core contracts`
- `user action log service`
- `log Login / LoginFailed events`
- `log PhysicalVolume / LaborIntensity imports`
- `log Project CRUD and import-iteration delete`
- `log Version CRUD and Project edit/create from project edit dialog`
- `log detail-grid quick edits`
- `user action log UI tab`
