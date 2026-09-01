# HTTP API Engineering Performance

Дата обновления: 2026-09-01.

Один REST-контракт для SPA и внешних модулей. Отдельного public API, gateway и роли «integration» нет: `Authorization: Bearer` после `POST /api/auth/login`, права `PermissionType` + ACL проекта.

## Платформа контракта

| Слой | Где | Назначение |
|------|-----|------------|
| OpenAPI 3 | `/swagger/v1/swagger.json` (Swashbuckle 6.9) | Машинный SoT маршрутов и схем |
| Swagger UI | `/swagger` | Try it. Включается в Development или `Swagger__Enabled=true`. В Docker Compose **по умолчанию выключен** — дефолт не менять |
| Снимок | [`openapi.json`](openapi.json) | Закоммиченный контракт; при расхождении верить живому swagger.json |
| Примеры | [`examples.http`](examples.http) | REST Client / JetBrains HTTP |
| Smoke | `scripts/external-api-smoke.sh` | Login Bearer → ключевые GET |
| Тесты | `backend/EngineeringPerformance.Api.Tests/ExternalApi*.cs` | Интеграция без cookie SPA |

Базовый URL локального Docker: `http://127.0.0.1:8081` (nginx → API). JSON — camelCase.

## Карта документов

| Файл | Тема |
|------|------|
| [auth.md](auth.md) | Login, Bearer, cookie SPA, MFA, права |
| [errors.md](errors.md) | HTTP-коды, rowErrors, лимиты Excel |
| [projects.md](projects.md) | Карточка проекта |
| [versions.md](versions.md) | Версии анализа, снимок норм, copy, readiness |
| [directories.md](directories.md) | Справочники по slug и `properties` |
| [dictionaries.md](dictionaries.md) | `GET /dictionaries/all` |
| [iic-norms.md](iic-norms.md) | Нормы ИИС |
| [row-crud.md](row-crud.md) | Построчный CRUD ФО / оборудование / трудоёмкость |
| [ingest-physical-volumes.md](ingest-physical-volumes.md) | Excel и JSON-пакет физ. объёмов |
| [ingest-equipment.md](ingest-equipment.md) | Excel и JSON-пакет оборудования |
| [ingest-labor.md](ingest-labor.md) | Excel и JSON-пакет трудоёмкости |
| [lifecycle.md](lifecycle.md) | Сценарий внешнего модуля от проекта до readiness |
| [monitoring.md](monitoring.md) | Админский мониторинг, `GET /resource-metrics` |
| [external-integration.md](external-integration.md) | Краткий обзор (оглавление) |

Excel-загрузка (`POST …/import/*`) **не заменяется**: JSON-пакет живёт на `POST …/ingest/*`.
