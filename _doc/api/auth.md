# Аутентификация и права

Дата обновления: 2026-08-24.  
Код: [`AuthController`](../../backend/EngineeringPerformance.Api/Controllers/AuthController.cs).

## Login для внешнего модуля

`POST /api/auth/login`

```json
{ "username": "admin", "password": "admin" }
```

Успех (фрагмент): `expiresAt`, `accessToken`, `tokenType: "Bearer"`, `user` (id, username, permissions).

Дальше на каждый запрос:

```http
Authorization: Bearer <accessToken>
```

HttpOnly cookie `ep_auth_token` выставляется для SPA — машинный клиент её игнорирует.

Неверные учётные данные / блокировка: **401**. Rate limit на login.

## MFA

Если в ответе `requiresMfa: true` и `mfaSessionToken` — затем `POST /api/mfa/verify`. В финальном ответе снова будет `accessToken`.

## Refresh

`POST /api/auth/refresh` опирается на refresh-cookie. Машинным клиентам проще повторить login. Refresh token в body не поставляется.

Прочие: `GET /api/auth/me`, `POST /api/auth/logout`, `POST /api/auth/logout-all`, `GET /api/auth/sessions`. Windows/SPNEGO (`negotiate`, `windows-identity`) — для браузера за nginx, не для типичного server-to-server.

## Права

- Глобальная матрица: `PermissionType` + уровни 0 / 1 (Read) / 2 (Edit).
- Данные версии: `[RequireProjectAccess]` — ACL проекта (`GET|PUT /api/projects/{id}/permissions`).
- Загрузка Excel и JSON-ingest: `ProjectDataUpload` + Edit на проект.
- Отдельной роли «integration» нет: Admin или Manager с нужными правами и ACL.

Список объектов прав — [`_doc/inventory/permissions_inventory.md`](../inventory/permissions_inventory.md).
