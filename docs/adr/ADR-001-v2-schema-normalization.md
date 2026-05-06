# ADR-001: V2 Schema Normalization

**Дата:** 2026-05-03
**Статус:** Принято
**Контекст:** Переход на CQRS + Domain Layer, подготовка к многолетней эксплуатации на тестовой БД до 10 пользователей. Breaking changes схемы разрешены пользователем.

---

## Обоснование

Текущая схема накопила доменные долги: строковые матчи вместо FK, даты как `nvarchar`, отсутствие уникальности норм во временном окне, разделение видимости проекта на `ProjectVisibility` enum + `ProjectUserAccess`, snapshot норм не фиксируется в `ProjectAnalysisVersion`. Всё это мешает корректности аналитики (Z-score, прогноз) и усложняет добавление фич.

## Состояние БД на момент ADR

- Сервер: `LAPTOP-ANTON\SQLEXPRESS` (SQL Server 2025 Express)
- БД: `EngineeringPerformance`
- Применено 12 EF миграций (до `20260419213543_AddPhysicalVolumeCoefficient` включительно)
- В коде 28 миграций — **БД отстаёт на 16 миграций**
- Значимые данные: `PhysicalVolumes=2497`, `IicWorkDirections=4`, `IicWorkDirectionNorms=3`, `Users=4`, `Projects=5`, `ProjectAnalysisVersions=5`, `AccessRights=30`, `ProjectUserAccess=2`
- `ProjectPhysicalVolumes=0`, `ProjectLaborIntensities=0` — таблицы транзакционных данных пусты, что сильно упрощает миграцию FK

## Принятые breaking changes

| № | Изменение | Обоснование |
|---|---|---|
| 1 | `Project.WorkStart: nvarchar → DateOnly`, добавить `WorkEnd DateOnly NULL`, `ContractDate DateOnly NULL` | Фильтрация/сортировка по датам через БД, типобезопасность |
| 2 | `IicWorkDirectionNorm`: уникальный индекс `(WorkDirectionId, EffectiveFrom)` + CHECK `EffectiveTo IS NULL OR EffectiveTo > EffectiveFrom` | Устраняет расхождение между `PerformanceService` и `ProjectForecastService` (недетерминированный выбор нормы при перекрытии окон) |
| 3 | `IicWorkDirection` и `PhysicalVolume`: валидация отсутствия циклов в Application-слое (unit-tested), без DB-триггеров | Сейчас guard только в runtime hierarchy builder |
| 4 | Новая таблица `AnalysisVersionSnapshot (VersionId, WorkDirectionId, NormId)` — снимок действующих норм на момент `CreatedAt` версии | Исторические отчёты не «плывут» при правке справочника норм |
| 5 | `User.WindowsSid nvarchar(64) NULL` рядом с `WindowsUsername` | Авто-логин по SID защищает от переименования учёток в AD |
| 6 | Новая таблица `ProjectPermission (Id, ProjectId, UserId, Level)` — замена `ProjectUserAccess` и `ProjectVisibility` enum | Единый ACL, «нет записи = нет доступа, кроме Administrator», миграция данных из старых таблиц |
| 7 | `ProjectPhysicalVolume`: прямой FK `PhysicalVolumeId NOT NULL` вместо строкового `CatalogId ↔ PhysicalVolume.Code` | Устраняет хрупкий бизнес-матч |
| 8 | `ProjectLaborIntensity`: прямой FK `PhysicalVolumeId NOT NULL` вместо `Brand ↔ PhysicalVolume.DocumentSet` | Аналогично |
| 9 | `DataImportIteration`: добавить `ImportedByUserId INT NULL`, `SourceFileName nvarchar(500) NULL`, `SourceFileHash char(64) NULL` | Аудит + защита от повторного импорта одного файла |

**НЕ меняем (по запросу пользователя):**
- `User.PasswordHash` остаётся (BCrypt-аутентификация сохранена)
- Login-flow, dialog services, WPF-навигация — не трогаются в этом PR
- DevExpress 25.2 остаётся

## План миграции

1. **Backup** тестовой БД → `backups/pre-v2-{timestamp}.bak`
2. **Snapshot** значимых данных в SQL → `backups/pre-v2-{timestamp}-data.sql` (для ручного восстановления при проблемах)
3. **Накат недостающих 16 миграций** через `dotnet ef database update` (приводим БД к коду)
4. **Генерация единой миграции V2** `V2_SchemaNormalization` (EF add-migration) поверх актуального состояния
5. **Применение V2** на тестовой БД
6. **Обновление `DatabaseSeeder`** для новых колонок и `AnalysisVersionSnapshot`
7. **Verify**: smoke-запуск приложения, проверка отчётов

## Rollback

- План A: `RESTORE DATABASE EngineeringPerformance FROM DISK='{backup}.bak' WITH REPLACE`
- План B: `dotnet ef database update 20260429235517_AddProjectCharacteristicSelections --project EngineeringPerformance.Infrastructure --startup-project EngineeringPerformance.App` (откат до предыдущей миграции)

## Риски

- **R1**: Конвертация `Project.WorkStart` из строки в дату. В БД 5 проектов, значения вида `"2024-01-15"` — парсятся `DATEFROMPARTS` или `CONVERT(date, value, 23)`. Обработать NULL/пустые строки → NULL.
- **R2**: Накат 16 отложенных миграций может упасть, если seed-данные расходятся с новой схемой (`AddPhysicalVolumeUnit` потребует `UnitId` на существующих 2497 записях). Мигрируем как nullable, дефолт NULL.
- **R3**: FK `PhysicalVolumeId NOT NULL` в `ProjectPhysicalVolume`/`ProjectLaborIntensity` — таблицы пусты, риск отсутствует.
- **R4**: Уникальный индекс `(WorkDirectionId, EffectiveFrom)` на 3 существующих нормах — нужно проверить отсутствие дубликатов перед применением.

## Ссылки
- Prior analysis: chat session 2026-05-03
- Migration progress: `tmp/active/orch-2026-05-03-v2-migration/progress.json`
