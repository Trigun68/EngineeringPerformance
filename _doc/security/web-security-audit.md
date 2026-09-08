# Security audit — web stack (Phase 4)

**Дата:** 2026-06-08  
**Область:** `backend/EngineeringPerformance.Api`, `client/`, Docker

---

## Резюме

| Область | Статус | Реализация |
|---------|--------|------------|
| Аутентификация JWT | ✅ | `JwtTokenService`, min 32 char secret, expiry configurable |
| SPNEGO / Kerberos | ✅ (опционально) | `SpnegoUserMiddleware`, trusted proxy secret |
| Авторизация | ✅ | `[Authorize]`, `[RequireProjectAccess]`, `[HasPermission]` |
| CORS | ⚠️ dev | Dev: AllowAnyOrigin; **prod: задать `Cors__AllowedOrigins`** |
| Rate limiting | ✅ | Login: 10 req/min fixed window |
| Excel upload | ✅ | Max 50 MB, .xlsx only, magic bytes, `[RequestSizeLimit]` |
| Secrets in repo | ✅ | `.env` gitignored, compose требует env vars |
| HTTPS | ⚠️ | Terminate на NGINX в prod (не в dev compose) |
| Swagger | ⚠️ | Только Development — отключён в prod build |

---

## JWT

- Secret валидируется при старте (`Program.cs`, ≥32 символов).
- Expiration: `Jwt__ExpirationMinutes` (default 60).
- Клиент: interceptor 401 → logout (`useAuthStore`).

**Рекомендация prod:** refresh token или shorter TTL + silent renew (техдолг).

---

## CORS

Production **не должен** использовать `AllowAnyOrigin`. Задать в конфигурации:

`Cors:AllowedOrigins: ["https://ep.company.local"]`

---

## File upload

`ExcelUploadValidator`: размер, расширение, Content-Type, PK signature ZIP/XLSX.  
Контроллеры import/projects: `[RequestSizeLimit(50MB)]`.

---

## SPNEGO

При `Spnego__Enabled=true` обязателен `Spnego__TrustedProxySecret`.  
NGINX передаёт `X-Remote-User` только после `auth_gss`.

**PoC:** проверить на целевой инфраструктуре до cutover (риск из migration_map §9.1).

---

## Admin endpoints

- `GET/PUT /api/access-rights` — `[HasPermission(Administration, Read)]` на GET, `Edit` на PUT (не `Forbid` / `IsAdministrator`). То же для `GET/PUT /api/permissions/matrix` (`PermissionMatrixController`).
- `GET /api/audit/user-actions` — `PermissionType.UserActionLog` Read.

---

## Frontend

- Кнопки «Журнал» / «Права» скрыты без прав (`App.tsx`).
- `usePermission()` на страницах и вкладках.

---

## Действия перед prod cutover

1. [ ] Задать `Cors__AllowedOrigins`
2. [ ] TLS на reverse proxy
3. [ ] Ротация `JWT_SECRET` и `SQL_SA_PASSWORD`
4. [ ] Отключить Swagger в non-Development
5. [ ] SPNEGO PoC на prod NGINX
