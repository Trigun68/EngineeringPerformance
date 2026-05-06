# Session summary: V2-C ProjectPermission

**Дата:** 2026-05-04
**Сессия:** `orch-2026-05-03-v2-migration`, фаза **V2-C**
**Статус:** ✅ завершено (код был сделан в прошлой сессии, сегодня — верификация и закрытие)

## Цель

Единая модель доступа к проектам: таблица `ProjectPermissions (ProjectId, UserId, Level)` заменяет связку `Project.Visibility` + `ProjectUserAccess`. Семантика «нет записи → нет доступа» (кроме Administrator).

## Что сделано (к моменту этой сессии — целиком)

### Схема и сущности

- **`Infrastructure/Data/Entities/ProjectPermission.cs`** — новая сущность с полем `Level : AccessLevel` (`None=0 / Read=1 / Edit=2`).
- **`Core/Enums/AccessLevel.cs`** — enum + `ToRussian()` extension.
- **`Infrastructure/Data/Configurations/ProjectPermissionConfiguration.cs`** — уникальный индекс `UX_ProjectPermissions_Project_User` (одна запись на пару), FK на `Project` и `User` с Cascade, дефолт `Level=Read`.
- **`Infrastructure/Data/AppDbContext.cs`** — `DbSet<ProjectPermission> ProjectPermissions` + `ApplyConfiguration`.
- **`Infrastructure/Data/Entities/Project.cs`** — удалено поле `Visibility`, добавлена навигация `ICollection<ProjectPermission> Permissions`.
- **Удалена** сущность `ProjectUserAccess` и её конфигурация (остаются только в исторических `.Designer.cs` миграций — это норма EF).

### Миграция

**`20260503220818_V2C_ProjectPermissions`** (уже применена в LocalDB):

1. `CreateTable ProjectPermissions` + 2 индекса (`IX_UserId`, уникальный `UX_Project_User`).
2. Два `Sql()` для миграции данных:
   - `Visibility=1` (SpecificUsers) → переносим явные записи `ProjectUserAccess` как `Level=Read`.
   - `Visibility=0` (AllUsers) → выдаём `Read` всем не-админам (сохранение совместимости с прежним неявным доступом).
3. `DropTable ProjectUserAccess`.
4. `DropColumn Visibility` из `Projects`.

`Down()` полностью обратим: создаёт `ProjectUserAccess` и возвращает `Visibility`.

### Сервис

**`Infrastructure/Services/ProjectVisibilityService.cs`** переписан:

- `CanUserAccessProjectAsync` — admin видит всё, иначе проверка `ProjectPermissions.Any(pp.ProjectId == projectId && pp.UserId == user.Id)` + активность проекта.
- `GetAccessibleProjectsAsync` — admin получает все не-архивные, иначе `Projects.Where(p => context.ProjectPermissions.Any(...))`.
- `AddUserToProjectAsync` / `RemoveUserFromProjectAsync` — идемпотентные CRUD поверх `ProjectPermissions` с `Level=Read` по умолчанию.

### Seeder

**`Infrastructure/Services/DatabaseSeeder.cs`** — метод `SeedProjectPermissionsAsync` сидит: `manager → PRJ-002 : Edit`, `observer → PRJ-002 : Read`. Вызывается только если таблица пуста.

### Тесты

**`Tests/ProjectVisibilityServiceTests.cs`** — **9 тестов**:

| Тест | Что проверяет |
|---|---|
| `Administrator_CanAccess_AnyProject` | Admin всегда true независимо от permissions |
| `NonAdmin_CannotAccess_ProjectWithoutPermission` | Нет записи → false |
| `NonAdmin_CanAccess_ProjectWithPermission` | Есть запись → true |
| `NonAdmin_CannotAccess_ArchivedProject_EvenWithPermission` | `IsArchived=true` блокирует даже при наличии права |
| `GetAccessibleProjects_ReturnsAll_ForAdministrator` | Admin видит оба проекта |
| `GetAccessibleProjects_ReturnsOnlyPermitted_ForNonAdmin` | Не-админ видит только свои |
| `AddUserToProject_CreatesPermission` | CRUD-add работает |
| `RemoveUserFromProject_RemovesPermission` | CRUD-remove работает |
| `AddUserToProject_IsIdempotent` | Повторный add не создаёт дубликат (уникальный индекс) |

### Верификация

- `dotnet build EngineeringPerformance.slnx` — Success, 0 warnings.
- `dotnet test` — **75/75 PASSED**.
- `__EFMigrationsHistory` в LocalDB содержит `20260503220818_V2C_ProjectPermissions`.
- Легаси `.Visibility` и `ProjectUserAccess` в runtime-коде не встречаются (только в комментариях-ссылках и в `.Designer.cs`-истории EF).

## Известное состояние данных в LocalDB

`ProjectPermissions` в рабочей LocalDB пустая (0 строк) при наличии 2 не-архивных проектов (`PRJ-25-310`, `PRJ-25-970`) и 2 не-админских пользователей (`manager`, `observer`).

**Причина:** на момент применения миграции `ProjectUserAccess` была пуста, а `Projects.Visibility` не содержал ни одного ряда со значением `0` (миграционный SQL не имел что вставлять).

**Решение (согласовано с пользователем):** оставить как есть — вариант 1 из трёх рассмотренных. Это соответствует дизайну V2C «нет записи → нет доступа»; admin должен явно выдать права `manager`/`observer` через UI или SQL:

```sql
-- пример: выдать observer Read на оба проекта
INSERT INTO ProjectPermissions (ProjectId, UserId, Level)
SELECT p.Id, u.Id, 1
FROM Projects p CROSS JOIN Users u
WHERE u.Username = 'observer' AND NOT p.IsArchived;
```

## Изменённые файлы (за всё время фазы V2-C, включая прошлые сессии)

```
EngineeringPerformance.Core/
  Enums/AccessLevel.cs                            (+None/Read/Edit)

EngineeringPerformance.Infrastructure/
  Data/Entities/
    ProjectPermission.cs                          (new)
    Project.cs                                    (-Visibility, +Permissions nav)
    ProjectUserAccess.cs                          (deleted)
  Data/Configurations/
    ProjectPermissionConfiguration.cs             (new)
    ProjectUserAccessConfiguration.cs             (deleted)
  Data/AppDbContext.cs                            (+DbSet, +ApplyConfiguration)
  Services/
    ProjectVisibilityService.cs                   (fully rewritten)
    DatabaseSeeder.cs                             (+SeedProjectPermissionsAsync)
  Migrations/
    20260503220818_V2C_ProjectPermissions.cs      (new)
    + Designer.cs + AppDbContextModelSnapshot.cs

EngineeringPerformance.Tests/
  ProjectVisibilityServiceTests.cs                (9 тестов на новую модель)

tmp/active/orch-2026-05-03-v2-migration/progress.json  (status: v2c-completed)
```

## Откат

```bash
dotnet ef database update 20260503213828_V2B_AnalysisVersionSnapshot \
  --project EngineeringPerformance.Infrastructure \
  --startup-project EngineeringPerformance.App
```

`Down()` возвращает `Visibility`-колонку и `ProjectUserAccess`-таблицу. **Данные `ProjectPermissions` теряются** — откат подразумевает ручное восстановление через `Visibility` + `ProjectUserAccess` или повторное применение миграции.

## Что дальше

Из плана `orch-2026-05-03-v2-migration` остался pending:

- **V2D** — FK `PhysicalVolumeId` (PPV NOT NULL, PLI NULL).

Опциональные (не в плане, по итогам обсуждения):
- **V2-B.2** — закрытие residual drift для in-place edit норм.
- **UI для управления ProjectPermissions** — сейчас его нет, admin управляет только через SQL; при желании можно вынести в отдельную `Medium`-фазу.
