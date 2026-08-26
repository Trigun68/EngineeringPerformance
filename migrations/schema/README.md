# SQL Schema Migrations

## Источник истины

Источник истины для схемы БД — **EF Core миграции** в
`EngineeringPerformance.Infrastructure/Migrations/`.

Файлы в этой папке (`00X_*.sql`) — **legacy**, оставлены для совместимости
с утилитами развёртывания (`install/Migrate.ps1`, `migrations/000_MasterMigration.sql`).
Не редактируйте их вручную — после изменения EF-модели регенерируйте
консолидированный SQL:

```powershell
pwsh -File scripts/Regenerate-SqlSchema.ps1
```

Скрипт вызывает `dotnet ef migrations script --idempotent` и сохраняет
итог в `_generated.sql`. Этот файл — каноническая текущая версия схемы.

## Применение

Для новой инсталляции используется контейнер `ep-db-init` (см. `_doc/deployment/DEPLOY.md`),
который применяет SQL-миграции из `migrations/`. Legacy-утилита `EngineeringPerformance.DbInitializer`
перенесена в `archive/` (2026-08-26) и более не поддерживается.

Если нужен «голый» SQL (CI, удалённое развёртывание без .NET SDK на машине) —
запустите `_generated.sql` через `sqlcmd`/SSMS на пустой БД.
