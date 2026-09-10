# ADR-006: Авторизация API — только `[HasPermission]` + глобальная `FallbackPolicy`

**Дата:** 2026-09-08  
**Статус:** Accepted  
**Авторы:** команда Engineering Performance

---

## Контекст

До цикла коррекции архитектуры (сентябрь 2026) в `backend/EngineeringPerformance.Api` сосуществовали три способа закрыть endpoint:

1. `[HasPermission(PermissionType.X, AccessLevel.Read|Edit)]` — матрица прав `AccessRights` (роль × объект × уровень) через `PermissionService.HasAccessAsync`; для директорий — `[DirectoryPermission]` / `EnsureAccessAsync`.
2. `[Authorize(Roles = "SuperAdmin,Administrator")]` на классах (`SystemSettingsController`) — матчит только `RoleId = 1`, роль `Admin` (`RoleId = 2`) не покрывалась.
3. Ручные проверки `IsAdministrator()` + `Forbid()` внутри действий (`AccessRightsController`, `PermissionMatrixController`).

Следствия: разная семантика «администратор» в разных контроллерах, endpoint без атрибутов оставался доступным любому аутентифицированному пользователю, ошибку легко было пропустить на ревью.

Ограничения: `PermissionType.Administration` не выдаётся никому, кроме `SuperAdmin` (bypass в `HasAccessAsync`), поэтому `[HasPermission(Administration, …)]` эквивалентен «только SuperAdmin». Heartbeat `GET /api/system-settings` вызывается из `AppLayout` любым пользователем.

## Варианты решения

### Вариант A: один атрибут `[HasPermission]` для всего, `FallbackPolicy = RequireAuthenticatedUser`, тест-страж

Все действия помечаются `[HasPermission]` / `[DirectoryPermission]` / `[AllowAnonymous]`; self-эндпоинты (профиль, настройки пользователя, MFA, heartbeat) — `[Authorize]` из явного allowlist. Любой endpoint без атрибутов закрывается глобальной политикой. Рефлексивный тест `ControllerAuthorizationCoverageTests` падает на `[Authorize(Roles=…)]` и на `[AllowAnonymous]`/`[Authorize]` вне allowlist.

**Плюсы:** одна семантика доступа; невозможен «забытый» endpoint; прав администратора нет в коде — они в матрице.  
**Минусы:** объект `Administration` нужен в клиентской матрице; self-allowlist надо поддерживать.

### Вариант B: оставить `[Authorize(Roles=…)]` для админских экранов, добавить только `FallbackPolicy`

**Плюсы:** минимальный diff.  
**Минусы:** сохраняется вторая семантика «кто администратор»; `Admin` (`RoleId = 2`) по-прежнему не имеет доступа; страж не может формально проверить корректность ролей.

### Вариант C: ActionFilter/middleware с картой «route → PermissionType»

**Плюсы:** атрибуты не нужны.  
**Минусы:** карта живёт отдельно от контроллера, расходится при переименовании маршрутов; хуже читаемость и ревью.

## Решение

**Выбран вариант A.**

- `AccessRightsController`, `PermissionMatrixController`: `GET` → `Administration:Read`, `PUT` → `Administration:Edit`; ручные `IsAdministrator()` удалены.
- `SystemSettingsController`: классовый `[Authorize(Roles=…)]` снят; `PUT` → `Administration:Edit`; `GET` — `[Authorize]` (heartbeat, в allowlist).
- `NormsController.GetFormulaTemplates/Preview` → `IicWorkDirectionNorms:Read`.
- `Program.cs`: `options.FallbackPolicy = RequireAuthenticatedUser`; `/metrics` — `RequireAuthorization("Permission:Administration:Read")`.
- Клиент: объект `Administration` («Администрирование») в `accessControlConstants.ts` и `permissionObjectHierarchy.ts`.
- Страж: `backend/EngineeringPerformance.Api.Tests/ControllerAuthorizationCoverageTests.cs` (allowlist anonymous: `AuthController.Login/WindowsIdentity/Negotiate/Logout/Refresh`, `MfaController.Verify`; allowlist self: `UsersController.GetCurrentUser/ChangeOwnPassword`, `UserPreferencesController.*`, `UserSettingsController.*`, `AuthController.LogoutAll/GetSessions/Me`, `MfaController.Setup/VerifySetup/Disable/Status`, `SystemSettingsController.Get`) + тест «аноним на endpoint без атрибутов → 401».

## Последствия

### Положительные
- Единая точка правды по доступу — матрица `AccessRights`; UI и API читают один и тот же объект.
- Новый контроллер без атрибутов ломает тесты, а не открывает данные.

### Отрицательные / Риски
- Health-эндпоинты и `[AllowAnonymous]` действия должны оставаться в allowlist стража — иначе красный тест при добавлении.
- `Administration` в UI-матрице виден только SuperAdmin; выдача другим ролям не поддерживается сервисом (bypass), пункт матрицы информационный.

### Нейтральные
- `DirectoriesController` продолжает использовать `EnsureAccessAsync` (тот же `PermissionService`, другой вход) — фиксируется §11.3 `business-logic.md`.

## Связанные документы

- `_doc/architecture/business-logic.md` §11 «Права доступа»
- `_doc/inventory/permissions_inventory.md`
- `.cursorrules` §5 Security
- CHANGELOG 2026-09-07 «единая авторизация»
