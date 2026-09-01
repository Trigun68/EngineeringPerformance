# Резервное копирование и восстановление

Дата обновления: 2026-08-31.

Том Docker **`engineeringperformance_mssql_data`** — единственное постоянное хранилище БД.  
`docker compose down -v` уничтожает том. Не делать при переезде исходников.

## Бэкап (SQL Server в контейнере `ep-sqlserver`)

Имя контейнера и пароль — из `.env` (`MSSQL_SA_PASSWORD`). Бэкап кладётся в `/var/opt/mssql/backup` внутри тома.

```bash
docker exec ep-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q \
  "BACKUP DATABASE [EngineeringPerformance] TO DISK = N'/var/opt/mssql/backup/EngineeringPerformance.bak' WITH INIT, COMPRESSION;"
```

Скопировать файл на хост:

```bash
docker cp ep-sqlserver:/var/opt/mssql/backup/EngineeringPerformance.bak ./EngineeringPerformance.bak
```

## Восстановление

1. Остановить API (`docker compose stop backend`), чтобы не было открытых соединений.
2. Восстановить с `REPLACE`:

```bash
docker exec ep-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q \
  "RESTORE DATABASE [EngineeringPerformance] FROM DISK = N'/var/opt/mssql/backup/EngineeringPerformance.bak' WITH REPLACE;"
```

3. Запустить backend: `docker compose start backend`.

Миграции после restore не гонять, если бэкап уже содержит нужную схему. Новые sqlcmd-скрипты (`migrations/schema/11x_*.sql`) применяет `ep-db-init` только при создании БД или при повторном init на существующей БД (идемпотентно).

## Что не бэкапить этим runbook

- Redis (кэш page-data, можно потерять).
- Образы `ep-backend` / `ep-frontend` — пересобираются из git.
