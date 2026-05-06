# Session summary: V2-A Additive Schema Fields

**Дата:** 2026-05-03
**Сессия:** `orch-2026-05-03-v2-migration`, фаза **V2-A**
**Статус:** ✅ завершено

## Цель

Первая фаза рефакторинга схемы БД (ADR-001, 9 breaking changes). V2-A покрывает только **аддитивные** изменения: никаких удалений колонок/таблиц, только добавление новых полей и ограничений. Rollback тривиален (`Down()` миграции сбрасывает всё).

## Что сделано

### Аналитический блок (перед работой с кодом)

1. Обнаружено, что MCP `mssql`-tool подключается к `LAPTOP-ANTON\SQLEXPRESS`, а реальная рабочая БД приложения — `(localdb)\MSSQLLocalDB` (из `%APPDATA%\EngineeringPerformance\settings.json`). Первичный backup был сделан не с той БД.
2. Сделан корректный backup LocalDB (15.4 МБ) в `backups/LOCALDB_EngineeringPerformance_pre-v2_20260503.bak`.
3. Валидированы риски V2-миграции против реальных данных:
   - Дубликатов `(WorkDirectionId, EffectiveFrom)` нет → уникальный индекс безопасен.
   - `Project.WorkStart` — 2 значения, оба пустые строки → `DateOnly?` конверсия тривиальна.
   - `ProjectPhysicalVolumes` 1412/1412 матчится по `CatalogId ↔ PhysicalVolume.Code` → FK `NOT NULL` в V2-D будет безопасен.
   - `ProjectLaborIntensities` 0/3 матчится (у `PhysicalVolume.DocumentSet` везде NULL) → FK в V2-D делать NULLABLE.

### Промежуточная SyncBeforeV2 миграция

БД отставала от кода: EF detected pending model changes (`LaborIntensities.DocumentTypeName`, `ProjectLaborIntensities.LaborIntensityId` FK). Сгенерирована и применена `20260503185712_SyncBeforeV2` — довела БД до текущего состояния кода перед V2-A.

### V2-A изменения

**Сущности:**
- `User`: + `WindowsSid string?`
- `Project`: + `WorkEnd DateOnly?`, + `ContractDate DateOnly?`
- `DataImportIteration`: + `ImportedByUserId int?` (FK → User, SetNull), + `SourceFileName string?`, + `SourceFileHash string?` (char(64))

**Конфигурации:**
- `UserConfiguration`: `WindowsSid` max 64, уникальный индекс с фильтром `IS NOT NULL`
- `ProjectConfiguration`: `WorkEnd`/`ContractDate` тип `date`
- `DataImportIterationConfiguration`: индексы на `ImportedByUserId` и `SourceFileHash` (с фильтром); FK на User
- `IicWorkDirectionNormConfiguration`: уникальный индекс `UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom` + CHECK `CK_IicWorkDirectionNorms_EffectiveRange` (`EffectiveTo IS NULL OR EffectiveTo > EffectiveFrom`)

**Миграция:** `20260503204336_V2A_AdditiveFields`
- Применена к LocalDB
- Изменена сторона: drop старого non-unique индекса на `WorkDirectionId` (поглощён новым композитным уникальным индексом)

### Верификация

- `dotnet build` — Success, 0 ошибок, 2 warnings (DevExpress evaluation license)
- `dotnet test` — **71/71 PASSED**, 5 сек
- Проверена фактическая схема БД через `INFORMATION_SCHEMA.COLUMNS`, `sys.check_constraints`, `sys.indexes` — все изменения на месте

## Изменённые файлы

```
EngineeringPerformance.Infrastructure/
  Data/Entities/
    User.cs                                    (+WindowsSid)
    Project.cs                                 (+WorkEnd, +ContractDate)
    DataImportIteration.cs                     (+3 audit fields, +ImportedByUser nav)
  Data/Configurations/
    UserConfiguration.cs                       (+WindowsSid unique index)
    ProjectConfiguration.cs                    (+date columns)
    DataImportIterationConfiguration.cs        (+FK, +indexes)
    IicWorkDirectionNormConfiguration.cs       (+unique, +CHECK)
  Migrations/
    20260503185712_SyncBeforeV2.cs             (new, sync pending model)
    20260503204336_V2A_AdditiveFields.cs       (new, V2-A)
    + Designer.cs + AppDbContextModelSnapshot.cs

docs/adr/
  ADR-001-v2-schema-normalization.md           (new)

backups/
  LOCALDB_EngineeringPerformance_pre-v2_20260503.bak  (new, 15.4 MB)
  snapshot-manifest-pre-v2_20260503_220300.md         (new)
  README-pre-v2_20260503_213657.md                    (legacy, wrong DB — оставлен для истории)

tmp/active/orch-2026-05-03-v2-migration/
  progress.json                                (updated)
```

## Результаты тестов

```
Пройден!   : не пройдено 0, пройдено 71, пропущено 0, всего 71, длительность 5 s
```

## Риски и что НЕ сделано

**V2-A сознательно не включает:**
1. `WorkStart string → DateOnly?` — выносится в V2-A.1 (требует изменения `ProjectEditWindow.xaml` (TextEdit → DateEdit), `ProjectEditViewModel`, `ProjectsViewModel`, `ProjectDto`, `DatabaseSeeder`, `ProjectVisibilityService`). Существующие 2 строки пустые → при конверсии уйдут в NULL.
2. `AnalysisVersionSnapshot` (V2-B) — требует сервисного кода для наполнения при создании версии.
3. `ProjectPermission` (V2-C) — удаление `ProjectUserAccess` и `Project.Visibility`, переписывание `ProjectVisibilityService`.
4. FK `PhysicalVolumeId` в `ProjectPhysicalVolume` (V2-D) — самое инвазивное, 1412 строк данных, требует UPDATE-JOIN biz-matching в Up().

## Откат

```sql
-- Быстрый откат V2-A (не теряет данных):
dotnet ef database update 20260503185712_SyncBeforeV2 --project EngineeringPerformance.Infrastructure --startup-project EngineeringPerformance.App

-- Полный откат к pre-V2:
USE [master];
ALTER DATABASE [EngineeringPerformance] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [EngineeringPerformance]
FROM DISK = N'C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\backups\LOCALDB_EngineeringPerformance_pre-v2_20260503.bak'
WITH REPLACE, NOUNLOAD, STATS = 10;
ALTER DATABASE [EngineeringPerformance] SET MULTI_USER;
```

## Следующая фаза

**V2-A.1** — конверсия `WorkStart string → DateOnly?`. Меньше, чем V2-B/C/D, но затрагивает UI. Предлагается делать отдельным PR.

---

_Работа вёлась без Git: проект не является git-репо (`Is a git repository: false`). Отслеживание изменений — вручную через этот отчёт._
