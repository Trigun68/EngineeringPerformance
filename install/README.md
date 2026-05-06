# Установка базы данных (SQL Server)

Инструкция по развёртыванию схемы и данных для **Engineering Performance**.

> **Источник истины для DDL — EF Core миграции**
> (см. `EngineeringPerformance.Infrastructure/Migrations/`).
> Скрипты в `migrations/schema/` оставлены для совместимости и развёртывания
> без .NET SDK на машине; их регенерация — `scripts/Regenerate-SqlSchema.ps1`.

## Требования

- Microsoft SQL Server (совместимый с используемым в проекте уровнем совместимости БД).
- Утилита [sqlcmd](https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility) (входит в состав SQL Server или доступна отдельно).
- Для разработки альтернативно: .NET SDK и `dotnet ef database update` (см. [документацию по миграциям](../docs/database/migration.md)).

## Важно: пути в `:r`

Скрипты `000_MasterMigration.sql`, `FullMigration_01_Structure.sql` и `FullMigration_02_DataImport.sql` подключают файлы из `schema/` и `data/` через `:r`. В **sqlcmd** относительные пути в `:r` разрешаются **относительно каталога файла**, в котором записана эта директива (см. [документацию sqlcmd](https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-commands)), а не обязательно относительно текущей папки в консоли.

Удобно перейти в каталог `migrations` и указать имя файла в `-i`:

```text
cd <корень_репозитория>\migrations
```

Либо вызвать `sqlcmd` с **полным путём** к `000_MasterMigration.sql` — вложенные `:r` по-прежнему должны находить `FullMigration_*.sql`, `schema\…`, `data\…` рядом с ними в дереве проекта.

## Состав цепочки скриптов

### Часть 1 — структура (`FullMigration_01_Structure.sql`)

Подключается по порядку:

| Файл | Назначение |
|------|------------|
| `schema/001_InitialSchema.sql` | Начальная схема БД (как в первой EF-миграции), идемпотентно по `__EFMigrationsHistory`. |
| `schema/006_Consolidated_20260415.sql` | Накопительный DDL (нормы ИИС, версии анализа, связи физ. объёмов, `DataImportIterations` и т.д.). |
| `schema/007_PhysicalVolumeCoefficient.sql` | Столбец `PhysicalVolumes.Coefficient` (перевод количества в единицы группы физ. объёмов ИИС), запись в `__EFMigrationsHistory`. |

### Часть 2 — данные (`FullMigration_02_DataImport.sql`)

| Файл | Назначение |
|------|------------|
| `data/002_SeedData.sql` | Роли, пользователи, единицы измерения, типы ФО, направления ИИС и нормы, **голые карточки проектов** (без версий анализа и без `ProjectUserAccess`), права доступа ролей. |
| `data/003_SeedDataFromExcel.sql` | Зарезервировано (пустой шаг; ранее — доп. единицы из Excel). |
| `data/004_LaborIntensity.sql` | Направления трудоёмкости LI-* в классификаторе ИИС. |
| `data/005_PhysicalVolumes.sql` | Номенклатура физических объёмов (генератор `migrations/tools/generate_fizobem_physical_volumes_seed.py`). |

Перегенерация `005` из Excel: `python migrations/tools/generate_fizobem_physical_volumes_seed.py` (из корня репозитория).

## Создание базы

Создайте пустую базу данных (имя на ваш выбор), затем укажите её в параметре `-d` команд `sqlcmd`.

Пример (локальный экземпляр, Windows-аутентификация):

```powershell
sqlcmd -S localhost -E -Q "CREATE DATABASE EngineeringPerformance;"
```

## Полная установка (структура + данные)

Из каталога `migrations`:

```powershell
sqlcmd -S <сервер> -d <база> -E -i 000_MasterMigration.sql
```

С учётной записью SQL Server:

```powershell
sqlcmd -S <сервер> -d <база> -U <логин> -P <пароль> -i 000_MasterMigration.sql
```

## Двухэтапная установка

1. Только структура (DDL):

   ```powershell
   sqlcmd -S <сервер> -d <база> -E -i FullMigration_01_Structure.sql
   ```

2. Затем импорт данных:

   ```powershell
   sqlcmd -S <сервер> -d <база> -E -i FullMigration_02_DataImport.sql
   ```

## Вспомогательный скрипт PowerShell

В этой папке лежит `Migrate.ps1`: он переходит в `..\migrations` и вызывает `sqlcmd` с выбранным файлом (корректный каталог для `:r`). Пример:

```powershell
cd <корень_репозитория>\install
.\Migrate.ps1 -Server localhost -Database EngineeringPerformance -Script 000_MasterMigration.sql
```

Параметр `-Script` может быть: `000_MasterMigration.sql`, `FullMigration_01_Structure.sql`, `FullMigration_02_DataImport.sql`.

При использовании SQL-логина добавьте `-SqlCmdUser` и `-SqlCmdPassword`.

## Документация

- Подробности по скриптам, EF Core и сущностям: [docs/database/migration.md](../docs/database/migration.md).
- Сущность номенклатуры и коэффициент: [docs/database/entities/PhysicalVolume.md](../docs/database/entities/PhysicalVolume.md).
