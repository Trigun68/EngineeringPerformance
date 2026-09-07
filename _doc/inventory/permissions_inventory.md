# Permissions Inventory — Engineering Performance

**Дата:** 2026-09-07  
**Версия:** 1.1  
**Связанные документы:** [data_access_inventory.md](./data_access_inventory.md), [business_logic_inventory.md](./business_logic_inventory.md)  
**Источник тестов:** `EngineeringPerformance.Tests/AccessControlServiceTests.cs`, `ProjectVisibilityServiceTests.cs`, `EngineeringPerformance.Api.Tests/ControllerAuthorizationCoverageTests.cs`

---

## Важное уточнение по ролям

В промпте указаны «Администратор / Редактор / Пользователь». В коде и seed **фактические имена ролей:**

| Роль в промпте | `UserRole` / seed | Пользователь seed |
|----------------|-------------------|-------------------|
| Администратор | `Administrator` | `admin` / `admin` |
| Редактор | `ProjectManager` | `manager` |
| Пользователь (read-only) | `Observer` | `observer` |

Двухуровневая модель доступа:

1. **Глобальная матрица** — `AccessRights` (роль × `ObjectName` × `AccessLevel`: None/Read/Edit).
2. **Доступ к проекту** — `ProjectPermissions` (пользователь × проект × `AccessLevel`).

Administrator **обходит обе** проверки на уровне сервисов (без записей в `AccessRights`).

---

## Матрица прав доступа (ACL)

### 1. Роли и базовые возможности

**Администратор (`Administrator`)**
- Полный доступ ко всем объектам UI (`AccessControlConstants.AllObjectNames`) без записей в `AccessRights`.
- Видит все неархивные проекты; `ProjectPermissions` не требуется.
- Полный доступ к матрице прав; web API проверяет объект `Administration`, не роль в контроллере (`AccessRightsController`, вкладка «Права пользователей»).

**Менеджер проектов (`ProjectManager`)** — аналог «Редактора»
- Seed: **Edit (2)** на все объекты из `AccessControlConstants` (`DatabaseSeeder.SeedAccessRightsAsync`).
- Импорт Excel, CRUD справочников, редактирование проектов — при наличии глобального Edit.
- Доступ к конкретному проекту — только при записи в `ProjectPermissions` (на backend также проверяется `Level >= Edit` для мутаций).

**Наблюдатель (`Observer`)** — аналог «Пользователя»
- Seed: **Read (1)** на все объекты матрицы.
- Не может редактировать справочники, импортировать данные (API требует Edit на `ProjectDataUpload`).
- Просмотр назначенных проектов через `ProjectPermissions`.

### 2. Детальная матрица по модулям

| Модуль / Сущность | Действие | Admin | ProjectManager | Observer | Где проверено |
|-------------------|----------|-------|----------------|----------|---------------|
| Навигация (Главная, Проекты, …) | Видимость пункта меню | ✅ | ✅* | ✅* | WPF: `MainViewModel.AddIfAccessible` + `NavigationService.CanRead`; Web: `AppLayout` + `usePermission` |
| Проекты | Список всех проектов | ✅ все | ⚠️ все в WPF | ⚠️ все в WPF | WPF: `ProjectDirectoryService.GetAllAsync` **без фильтра**; Web: `GetProjectsQuery` + `ProjectVisibilityService` |
| Проекты | Доступ к проекту по ID | ✅ | ✅ если `ProjectPermission` | ✅ если `ProjectPermission` | `ProjectVisibilityServiceTests`; API: `RequireProjectAccess` |
| Проекты | Создание/редактирование | ✅ | ✅ Edit `Projects` | ❌ | Web: `ProjectsPage` `canEdit`; WPF: через навигацию |
| Версии анализа | Просмотр | ✅ | ✅ Read `ProjectAnalysisVersions` | ✅ Read | API: `[HasPermission(..., Read)]` |
| Версии | Создание / Update / Archive | ✅ | ✅ Edit | ❌ | `ProjectVersionsController` + `RequireProjectAccess(Edit)` |
| Версии | **Копирование (Copy)** | ✅ | ✅ Edit | ❌ API / ⚠️ WPF | API: `POST .../copy` + Edit; WPF: `CopyVersionAsync` **без проверки CanEdit** |
| 9 гридов / Performance | Просмотр данных | ✅ | ✅ `Projects` Read + проект | ✅ Read | Навигация на `Projects`; **нет ACL в `PerformanceService`** |
| Импорт Excel | Upload + Replace | ✅ | ✅ Edit `ProjectDataUpload` + проект Edit | ❌ | API: `ImportController`; WPF: кнопки **без ACL**, только `PlanVersionId != null` |
| Импорт | Удаление итерации | ✅ | ✅ Edit | ❌ | API: `RollbackIteration`; WPF: `IterationDeleteDialog` |
| Справочники | Просмотр | ✅ | ✅ | ✅ | `ReferencesPageViewModel`, `DirectoriesController.EnsureAccessAsync` |
| Справочники | CRUD | ✅ | ✅ Edit на объект | ❌ | `DirectoryPage` `canEdit` |
| Нормы IIC | Редактирование | ✅ | ✅ Edit `IicWorkDirectionNorms` | ❌ | `IicNormsPage` |
| Настройки → Пользователи | Просмотр/редактирование | ✅ | ⚠️ Read Users (seed) | ❌ | `SettingsPageViewModel` → object `Users` |
| Настройки → Матрица прав | Редактирование | ✅ | ⚠️ по объекту `Administration` (Read — просмотр, Edit — правка) | ❌ | API: `AccessRightsController` / `PermissionMatrixController` — `[HasPermission(Administration, Read)]` на GET, `Edit` на PUT (больше не `Forbid` / `IsAdministrator()`); `PUT /api/system-settings` — `Administration:Edit`; WPF: вкладка при `CanRead("Settings")` |
| Журнал действий | Просмотр | ✅ | ✅* | ✅* | Web: `AppLayout.canAccessAudit`; WPF: `UserActionLog` в навигации |
| Админка (логи, health, RED, **Ресурсы**, аудит настроек, `/metrics`) | Просмотр | ✅ (UI + API) | ❌ UI (скрыто) / ⚠️ API при Read `Administration` | ❌ UI | Web: `AppLayout` супер-админ; API: `[HasPermission(Administration, Read)]` на `/api/monitoring/*` и `GET /metrics` |
| `GET /api/dictionaries/all` | Bulk-справочники | ✅ любой auth | ✅ любой auth | ✅ любой auth | ⚠️ **GAP: нет проверки прав** |
| Участники проекта | Назначение | ✅ | ✅ Edit проекта (UI) | ❌ | `ProjectParticipantsDialogViewModel` |

\* При наличии соответствующей записи в `AccessRights` (seed даёт всем трём ролям базовый доступ к объектам навигации).

### 3. РАЗВИЛКИ в правах (Row-Level Security и UI)

#### Глобальная матрица (`AccessControlService`)

```csharp
if (user.RoleName == Administrator) return true; // любой уровень
var level = AccessRights.FirstOrDefault(r => r.ObjectName == objectName)?.AccessLevel ?? None;
return level >= requiredLevel;
```

- Проверка **не в SQL**: фильтрация списков проектов в WPF **не использует** `AccessRights`.
- `GetPerformanceDataAsync`, `DataImportService`, `ProjectVersionGridDataService` — **не проверяют** пользователя.

#### Доступ к проекту (`ProjectVisibilityService`)

**Legacy WPF:**
```csharp
if (Administrator) return true;
return ProjectPermissions.Any(pp => pp.ProjectId == projectId && pp.UserId == user.Id);
// Level НЕ проверяется — достаточно наличия записи
```

**Backend (web):**
```csharp
if (Administrator) return true;
return permission is not null && permission.Level >= requiredLevel;
```

| Сценарий | WPF | Backend |
|----------|-----|---------|
| Observer с `ProjectPermission.Level = Read` | Может импортировать, если дошёл до кнопки | Import → **403** (`RequireProjectAccess(Edit)`) |
| Manager без `ProjectPermission` | Видит проект в диалоге выбора | API → **403** |
| Admin | Всё | Всё |

#### Фильтрация списка проектов

- **НЕ** `CreatedById` — такого поля в модели нет.
- Web: `GetAccessibleProjectsAsync` / `GetAccessibleProjectIdsAsync` — join `ProjectPermissions`.
- WPF: `ProjectSelectionDialogViewModel.LoadProjectsAsync` → `_projectService.GetAllAsync()` — **все проекты**.

#### Скрытие UI (WPF)

| Паттерн | Пример | Конвертер |
|---------|--------|-----------|
| Пункт меню не создаётся | `AddIfAccessible(..., "Projects")` | — |
| Вкладка настроек пропускается | `CanRead(user, "Users")` | — |
| Кнопка импорта | `IsEnabled="{Binding PlanVersionId, Converter=NullToBool}"` | `NullToBoolConverter` — **не права**, а выбор версии |
| Навигация заблокирована | `NavigationService.NavigateTo` → silent return | — |

#### «Закрытая версия»

Отдельного флага `IsClosed` / блокировки редактирования версии для non-Admin **не найдено**. Используется только `IsArchived` на версии (скрытие из picker: `includeArchived: false`).

---

## 4. Интеграция с 12 конвертерами и UI

| Конвертер | Связь с правами | Поле / условие |
|-----------|-----------------|----------------|
| `NullToBoolConverter` | **Enablement**, не ACL | `PlanVersionId` / `FactVersionId` — кнопки импорта активны при выбранной версии |
| `BoolToVisibilityConverter` | Может скрывать панели по VM-флагу | Не привязан к `CanEdit` в `ProjectsPageView` |
| `InverseBooleanToVisibilityConverter` | Инверсия видимости | Не используется для ACL в проанализированных View |
| `AnalysisDirectionToStringConverter` | Форматирование | `ProjectAnalysisVersion.Direction` — не security |
| `InterpretationToBrushConverter` | Форматирование Z-score | Вычисляемое поле, доступно всем, кто видит грид |
| Остальные (`Indent`, `BoolToFontStyle`, …) | Оформление гридов | Не security |

**Web-эквивалент:** `usePermission(objectName)` → `canRead` / `canEdit`; `DataTable.contextMenuItems` с `requiredLevel: 1 | 2`.

---

## 5. Тесты «Unauthorized» / «Forbidden»

В `EngineeringPerformance.Tests` **нет** тестов с именами Unauthorized/Forbidden. Блокировки зафиксированы косвенно:

| Тест | Что блокирует |
|------|---------------|
| `AccessControlServiceTests.User_HasNoAccess_WhenNoRightExists` | Manager без записи в `AccessRights` → `CanRead("NonExistent") == false` |
| `AccessControlServiceTests.Observer_CanOnlyRead_WhenAccessRightIsRead` | Observer: Read ✅, Edit ❌ на `Projects` |
| `AccessControlServiceTests.Manager_CanEdit_WhenAccessRightExists` | Manager с Edit на объект |
| `ProjectVisibilityServiceTests.NonAdmin_CannotAccess_ProjectWithoutPermission` | Manager без `ProjectPermission` → `CanUserAccessProjectAsync == false` |
| `ProjectVisibilityServiceTests.GetAccessibleProjects_ReturnsOnlyPermitted_ForNonAdmin` | Список проектов = только с permission |
| `ProjectVisibilityServiceTests.Administrator_CanAccess_AnyProject` | Admin bypass |

`AccessDeniedException` объявлен в Core, но **нигде не выбрасывается** — мёртвый тип.

---

## 6. Защита копирования версии (`CopyProjectVersion`)

| Слой | Проверка |
|------|----------|
| **API** `POST /api/projects/{id}/versions/{vid}/copy` | `[Authorize]` + `HasPermission(ProjectAnalysisVersions, Edit)` + `RequireProjectAccess(Edit)` |
| **WPF** `ProjectVersionPickerDialogViewModel.CopyVersionAsync` | Только `CanSelect` (выбрана версия); **нет** `CanEdit` / `ProjectDataUpload` |
| **WPF** `ProjectAnalysisVersionCopyService.CopyVersionAsync` | **Нет** проверки caller identity |
| **Web UI** `ProjectDetailPage` | `canEditVersions` + context menu `requiredLevel: 2` |

**Вывод:** Observer с Read на `Projects` и `ProjectAnalysisVersions` в WPF теоретически может инициировать копию через picker, если имеет `ProjectPermission` (даже Read). API это запретит.

---

## 7. Пробелы (GAPs) в безопасности

| # | GAP | Риск | Рекомендация (Спринт 0+) |
|---|-----|------|---------------------------|
| 1 | WPF `DataImportService` / `PerformanceService` без ACL | Прямой вызов сервиса обходит UI | Проверка в Application layer + API уже есть |
| 2 | WPF список проектов без `ProjectVisibility` | Observer видит коды всех проектов | `GetAccessibleProjectsAsync` в picker |
| 3 | WPF `CanUserAccessProjectAsync` игнорирует `ProjectPermission.Level` | Read = Edit на уровне проекта | Унифицировать с backend |
| 4 | Кнопки импорта на `ProjectsPageView` без `CanEdit` | Observer + выбранная версия → импорт | `IsEnabled` от `CanEdit(ProjectDataUpload)` |
| 5 | `GET /api/dictionaries/all` только `[Authorize]` | Утечка справочников | Gate по роли или минимум Read на `References` |
| 6 | `AccessDeniedException` не используется | Нет единого контракта ошибок | Throw из сервисов / middleware |
| 7 | Copy version в WPF без Edit-check | Observer может копировать локально | `CanExecute` на `CopyVersionCommand` |
| 8 | Матрица прав WPF: вкладка «Права» при `CanRead("Settings")` | Не-админ может открыть UI матрицы | Web API уже на `Administration` Read/Edit; WPF выровнять |
| 9 | Seed SQL `002_SeedData.sql` устарел (нет Equipment, UserActionLog, …) | Расхождение dev/prod прав | Синхронизировать с `DatabaseSeeder` |

---

## 8. Сводка: кто что может с 9 гридами и импортом

```
                    ┌─────────────────────────────────────────┐
                    │         Administrator                    │
                    │  Все проекты + все объекты + импорт     │
                    └─────────────────────────────────────────┘
                    ┌─────────────────────────────────────────┐
                    │  ProjectManager + ProjectPermission      │
                    │  Глобальный Edit; API: проект Edit       │
                    │  WPF: ⚠️ импорт без проверки Edit       │
                    └─────────────────────────────────────────┘
                    ┌─────────────────────────────────────────┐
                    │  Observer + ProjectPermission (Read)     │
                    │  Просмотр гридов (если дошёл до UI)      │
                    │  API: импорт/копия/мутации → 403         │
                    └─────────────────────────────────────────┘
```

---

## 9. Объекты `AccessControlConstants` (полный список для матрицы)

`Dashboard`, `Users`, `Projects`, `PhysicalVolumes`, `PhysicalVolumeTypes`, `Equipment`, `IicWorkDirections`, `ProjectAnalysisVersions`, `IicWorkDirectionNorms`, `EngineeringWorkCosts`, `UnitsOfMeasurement`, `ProjectPhysicalVolumes`, `ProjectEquipments`, `ProjectLaborIntensities`, `ProjectDataUpload`, `Assessment`, `Planning`, `VersionComparison`, `GroupAnalysis`, `GroupTrend`, `References`, `DataPage`, `SettingsPage`, `HelpPage`, `LaborIntensities`, `ProjectCharacteristics`, `UserActionLog`, **`Administration`**.

Уровни: `None=0`, `Read=1`, `Edit=2` (`AccessLevel` enum).

## 10. Единый паттерн авторизации API (2026-09-07)

- `FallbackPolicy = RequireAuthenticatedUser`: любой endpoint без атрибутов требует вход; аноним только с `[AllowAnonymous]`.
- На действиях — `[HasPermission]` (или allowlist самообслуживания); `[Authorize(Roles=…)]` запрещён. Guard: `backend/EngineeringPerformance.Api.Tests/ControllerAuthorizationCoverageTests.cs`.
- `GET /metrics` — аутентификация и `Administration:Read` (не публичный scrape).

---

*Документ сформирован по WPF (`AccessControlService`, `ProjectVisibilityService`, ViewModels) и backend (`PermissionService`, `RequireProjectAccess`, controllers) с перекрёстной проверкой `EngineeringPerformance.Tests` и `ControllerAuthorizationCoverageTests`.*
