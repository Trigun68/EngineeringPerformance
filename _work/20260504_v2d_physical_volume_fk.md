# Session summary: V2-D PhysicalVolume FK явный + SET NULL

**Дата:** 2026-05-04
**Сессия:** `orch-2026-05-03-v2-migration`, фаза **V2-D**
**Статус:** ✅ завершено (последняя фаза оркестрации)

## Цель

Явно сконфигурировать FK `ProjectLaborIntensities.PhysicalVolumeId` → `PhysicalVolumes.Id`:

- `PLI.PhysicalVolumeId` — nullable (уже был NULL в схеме, но как shadow-свойство EF).
- Поведение при удалении родителя — **SET NULL** на уровне БД (было `NO_ACTION` по умолчанию). История трудозатрат выживает, привязка обнуляется.
- На `ProjectPhysicalVolumes.PhysicalVolumeId` — NOT NULL + Restrict (уже было корректно, изменений не потребовалось).

## Что сделано

### Entity + конфигурация

**`Infrastructure/Data/Entities/ProjectLaborIntensity.cs`** — добавлено явное scalar-свойство:

```csharp
public int? PhysicalVolumeId { get; set; }
```

Раньше поле существовало в БД как shadow-property через навигацию `PhysicalVolume? PhysicalVolume`. Сейчас сервисы и тесты могут работать с id напрямую, без `Include(li => li.PhysicalVolume)`.

**`Infrastructure/Data/Configurations/ProjectLaborIntensityConfiguration.cs`** — явный FK с `SetNull`:

```csharp
builder.HasOne(li => li.PhysicalVolume)
    .WithMany(pv => pv.LaborIntensities)
    .HasForeignKey(li => li.PhysicalVolumeId)
    .OnDelete(DeleteBehavior.SetNull);

builder.HasIndex(li => li.PhysicalVolumeId);
```

Без этого EF по-прежнему создаёт FK, но с `DeleteBehavior.ClientSetNull` → в БД это `NO_ACTION`. При попытке удалить `PhysicalVolume`, на который ссылается PLI, транзакция падала бы с FK-conflict вместо обнуления FK.

### Миграция

**`20260504081758_V2D_PhysicalVolumeFkExplicit`** — минимальная (только alter FK):

```csharp
migrationBuilder.DropForeignKey("FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId", "ProjectLaborIntensities");
migrationBuilder.AddForeignKey(
    "FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId",
    "ProjectLaborIntensities",
    column: "PhysicalVolumeId",
    principalTable: "PhysicalVolumes",
    principalColumn: "Id",
    onDelete: ReferentialAction.SetNull);
```

- Колонку не трогаем — уже была `int NULL`.
- Индекс `IX_ProjectLaborIntensities_PhysicalVolumeId` уже существовал через конвенцию; миграция его не пересоздаёт.
- `Down()` возвращает FK к дефолтному `NO_ACTION`.

### Верификация

- `dotnet build EngineeringPerformance.slnx` — Success, 0 errors.
- `dotnet test` — **75/75 PASSED**.
- В LocalDB `sys.foreign_keys`:
  - `FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId` → **SET_NULL** ✅
  - `FK_ProjectPhysicalVolumes_PhysicalVolumes_PhysicalVolumeId` → **NO_ACTION** (Restrict — не трогали, так и задумано) ✅
- `__EFMigrationsHistory` содержит `20260504081758_V2D_PhysicalVolumeFkExplicit`.

### Почему нет unit-теста на SET_NULL

InMemory-провайдер EF не эмулирует FK и cascade/set-null поведение — проверка семантики `ON DELETE SET NULL` требует интеграционного теста против реального SQL Server. Это отдельная история (не в скоупе V2-миграции). Текущие тесты (75/75) гарантируют: компиляция, модель валидна, миграция чистая (нет `PendingModelChangesWarning`).

## Изменённые файлы

```
EngineeringPerformance.Infrastructure/
  Data/Entities/
    ProjectLaborIntensity.cs                       (+PhysicalVolumeId scalar)
  Data/Configurations/
    ProjectLaborIntensityConfiguration.cs          (+explicit HasOne/SetNull, +HasIndex)
  Migrations/
    20260504081758_V2D_PhysicalVolumeFkExplicit.cs (new, alter FK only)
    + Designer.cs + AppDbContextModelSnapshot.cs

tmp/active/orch-2026-05-03-v2-migration/progress.json  (status: completed)
```

## Откат

```bash
dotnet ef database update 20260503220818_V2C_ProjectPermissions \
  --project EngineeringPerformance.Infrastructure \
  --startup-project EngineeringPerformance.App
```

`Down()` возвращает FK к NO_ACTION. Данные не теряются, scalar-свойство в entity будет по-прежнему читаться (уже мапилось и до явной конфигурации).

## Оркестрация `orch-2026-05-03-v2-migration` — итог

Все 6 фаз закрыты:

| Фаза | Статус | Миграция |
|---|---|---|
| A — backup LocalDB | ✅ | — |
| SYNC — подтянуть зависшие EF-миграции | ✅ | `20260503185712_SyncBeforeV2` |
| V2A — additive fields (WindowsSid, WorkEnd/ContractDate, Norm unique+CHECK, DataImport audit) | ✅ | `20260503204336_V2A_AdditiveFields` |
| V2A1 — `WorkStart` nvarchar → date | ✅ | `20260503213307_V2A1_WorkStartToDate` |
| V2B — `AnalysisVersionSnapshots` + populate | ✅ | `20260503213828_V2B_AnalysisVersionSnapshot` |
| V2B.1 — сервисы на снимке + автонаполнение в CreateAsync | ✅ | — |
| V2C — `ProjectPermissions` (заменил `Visibility`+`ProjectUserAccess`) | ✅ | `20260503220818_V2C_ProjectPermissions` |
| V2D — явный FK PLI → PhysicalVolume, SET_NULL | ✅ | `20260504081758_V2D_PhysicalVolumeFkExplicit` |

Всего применённых миграций: **27**. Тесты: **75/75**. Один backup перед V2: `backups/LOCALDB_EngineeringPerformance_pre-v2_20260503.bak`.

## Остаточные вопросы (не блокируют оркестрацию)

1. **`ProjectPermissions` в LocalDB пустая** — не-админы (`manager`, `observer`) не видят ни одного проекта. Согласовано оставить как есть (семантика V2C «нет записи → нет доступа»). Admin при необходимости добавляет права вручную.
2. **Residual drift в `AnalysisVersionSnapshots`** — in-place edit `IicWorkDirectionNormService.UpdateAsync` всё ещё может менять исторические Z-score-отчёты через FK. Варианты закрытия описаны в `_work/20260504_v2b1_services_on_snapshot.md`. Отдельная фаза, если понадобится.
3. **UI для управления `ProjectPermissions`** — не входил в V2C; сейчас только SQL. Отдельная Medium-фаза при необходимости.

## Следующие шаги

Оркестрация закрыта. Состояние в `tmp/active/orch-2026-05-03-v2-migration/progress.json`. По CLAUDE.md «Автоочистка завершённых сессий через 7 дней» — можно оставить до самоочистки либо удалить вручную после подтверждения пользователя.
