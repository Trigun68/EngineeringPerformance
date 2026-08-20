# API для внешних модулей

Дата обновления: 2026-08-04.  
Контракт: [`openapi.json`](openapi.json). Примеры запросов: [`examples.http`](examples.http).

## 1. Базовый URL и окружение

| Среда | URL |
|-------|-----|
| Локальный Docker | `http://127.0.0.1:8081` (nginx → API) |
| API напрямую (dev) | `http://localhost:5080` (если поднят без frontend) |

- Все маршруты под префиксом `/api/`.
- JSON: camelCase.
- CORS: для браузерных клиентов задаётся `Cors__AllowedOrigins`; машинные клиенты (server-to-server) CORS не ограничивают.
- TLS: в production обязателен HTTPS на reverse proxy.

Swagger UI: Development или при `Swagger__Enabled=true` → `/swagger`.

## 2. Аутентификация

### Login

`POST /api/auth/login`

```json
{ "username": "admin", "password": "admin" }
```

Ответ (фрагмент):

```json
{
  "expiresAt": "...",
  "accessToken": "<jwt>",
  "tokenType": "Bearer",
  "user": { "id": 1, "username": "admin", "permissions": { } }
}
```

Для внешних модулей передавайте заголовок:

```http
Authorization: Bearer <accessToken>
```

HttpOnly cookie `ep_auth_token` по-прежнему выставляется для SPA — её можно игнорировать.

При включённой MFA: `requiresMfa: true`, `mfaSessionToken` — затем `POST /api/mfa/verify`; в финальном ответе снова будет `accessToken`.

### Refresh

`POST /api/auth/refresh` сейчас опирается на refresh-cookie. Машинным клиентам проще повторить login. (Refresh token в body — вне этой поставки.)

### Права

- Глобальная матрица: `PermissionType` (Projects, PhysicalVolumes, Planning, …) + уровни 0/1/2.
- ACL проекта: доступ к данным версии проверяется через `[RequireProjectAccess]`.
- Отдельной роли «integration» нет — используйте Admin/Manager с нужными правами и ACL.

## 3. Справочники

| Slug | Permission |
|------|------------|
| `physical-volumes-kfo` | PhysicalVolumes |
| `physical-volumes-ber` | PhysicalVolumes |
| `equipment` | Equipment |
| `labor-intensities` | LaborIntensities |
| `iic-work-directions` | IicWorkDirections |
| `units-of-measurement` | UnitsOfMeasurement |
| `project-characteristics` | ProjectCharacteristics |

Базовые операции:

- `GET /api/directories/{slug}` — список
- `GET /api/directories/{slug}/{id}` — элемент
- `POST /api/directories/{slug}` — создать
- `PUT /api/directories/{slug}/{id}` — изменить
- `POST /api/directories/{slug}/{id}/archive` — архив

Bulk lookups: `GET /api/dictionaries/all`.

### Нормы ИИС

- SoT: `/api/registers/iic-work-direction-norms`
- Алиас: `/api/iic-norms` (GET/POST/PUT/DELETE)

## 4. Проект end-to-end

1. `POST /api/projects` — создать проект  
2. `PUT /api/projects/{id}/characteristics` — характеристики  
3. `POST /api/projects/{id}/versions` — версия (`physicalVolumeTypeId` обязателен: КФО/БЭР из `dictionaries/all.physicalVolumeTypes`)  
4. `GET|PUT …/versions/{vid}/planning-parameters` — Z по направлениям  
5. `POST …/physical-volumes` / `equipment` / `labor-intensities` — данные версии  
6. `POST …/recalculate` — пересчёт трудоёмкостей  
7. `GET …/readiness` — проверка полноты  

### Readiness

`GET /api/projects/{projectId}/versions/{versionId}/readiness`

Возвращает счётчики блоков и `issues[]` с кодами:

- `missing_physical_volume_type`
- `missing_planning_parameters`
- `missing_physical_volumes`
- `missing_equipment`
- `missing_labor_intensities`
- `missing_norm_snapshots`

Severity: `warning` / `info`.

## 5. Аналитика (read-only)

- `GET /api/projects/{id}/versions/{vid}/performance`
- `GET /api/projects/{id}/versions/{vid}/page-data` — «Итого» / «По объектам»; detail пустой
- `GET /api/projects/{id}/versions/{vid}/page-data/detail` — гриды «Детально» (encoded)
- `GET /api/projects/{id}/versions/{vid}/compare?otherVersionId=` — сравнение без detail
- `GET /api/projects/{id}/versions/{vid}/compare/detail?otherVersionId=` — detail сравнения
- `GET /api/projects/{id}/versions/{vid}/analysis`
- `POST /api/analytics/trends/z-score` — тренд Z (план/факт, isPrimary, сегменты по нормам)
- `GET /api/analytics/trends/z-score/filters` — проекты / направления / weightModes

## 6. Ошибки и лимиты

| HTTP | Когда |
|------|--------|
| 401 | Нет/просрочен JWT |
| 403 | Нет права / нет ACL на проект |
| 404 | Сущность не найдена |
| 400 / 422 | Валидация (FluentValidation / InvalidOperation) |

Excel-импорт: `.xlsx`, max 50 MB, проверка MIME/magic bytes.

## 7. Проверка работоспособности

Автотесты (Bearer, без cookie UI):

```bash
docker run --rm --network host \
  -v "$(pwd)/backend:/src" -w /src \
  -e EP_TEST_CONNECTION="Server=127.0.0.1,1433;..." \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test EngineeringPerformance.Api.Tests/EngineeringPerformance.Api.Tests.csproj \
  --filter "FullyQualifiedName~ExternalApi"
```

Smoke после деплоя:

```bash
./scripts/external-api-smoke.sh http://127.0.0.1:8081
```

Исходники тестов: `backend/EngineeringPerformance.Api.Tests/ExternalApi*.cs`.
