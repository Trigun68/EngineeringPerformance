# API для внешних модулей

Дата обновления: 2026-08-24.

Полная карта: [`README.md`](README.md). Контракт: [`openapi.json`](openapi.json). Примеры: [`examples.http`](examples.http).

## 1. Базовый URL и окружение

| Среда | URL |
|-------|-----|
| Локальный Docker | `http://127.0.0.1:8081` (nginx → API) |
| API напрямую (dev) | `http://localhost:5080` (если поднят без frontend) |

- Все маршруты под префиксом `/api/`.
- JSON: camelCase.
- CORS: для браузерных клиентов `Cors__AllowedOrigins`; server-to-server CORS не ограничивает.
- TLS: в production HTTPS на reverse proxy.

Swagger UI: Development или `Swagger__Enabled=true` → `/swagger`. В Docker Compose флаг по умолчанию **false** (не включать без нужды).

Аутентификация и права: [auth.md](auth.md). Ошибки: [errors.md](errors.md).

## 2. Справочники и нормы

См. [directories.md](directories.md), [dictionaries.md](dictionaries.md), [iic-norms.md](iic-norms.md).

Кратко: `GET|POST|PUT /api/directories/{slug}`, bulk `GET /api/dictionaries/all`. Нормы: `/api/registers/iic-work-direction-norms`, алиас `/api/iic-norms`.

## 3. Проект end-to-end

Пошагово: [lifecycle.md](lifecycle.md). Карточка: [projects.md](projects.md). Версии: [versions.md](versions.md).

1. `POST /api/projects`
2. `PUT /api/projects/{id}/characteristics`
3. `POST /api/projects/{id}/versions` (`physicalVolumeTypeId` обязателен)
4. `GET|PUT …/planning-parameters`
5. Данные: построчно [row-crud.md](row-crud.md) **или** пакет [ingest-physical-volumes.md](ingest-physical-volumes.md) / [ingest-equipment.md](ingest-equipment.md) / [ingest-labor.md](ingest-labor.md). Excel `POST …/import/*` без изменений.
6. `POST …/recalculate`
7. `GET …/readiness`

## 4. Аналитика (read-only)

- `GET /api/projects/{id}/versions/{vid}/performance`
- `GET …/page-data` — «Итого» / «По объектам»; detail пустой
- `GET …/page-data/detail` — гриды «Детально»
- `GET …/compare?otherVersionId=` / `…/compare/detail`
- `GET …/analysis`
- `POST /api/analytics/trends/z-score`, `GET …/filters`

## 5. Проверка

```bash
docker run --rm --network host \
  -v "$(pwd)/backend:/src" -w /src \
  -e EP_TEST_CONNECTION="Server=127.0.0.1,1433;..." \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test EngineeringPerformance.Api.Tests/EngineeringPerformance.Api.Tests.csproj \
  --filter "FullyQualifiedName~ExternalApi"
```

```bash
./scripts/external-api-smoke.sh http://127.0.0.1:8081
```

Исходники: `backend/EngineeringPerformance.Api.Tests/ExternalApi*.cs`.
