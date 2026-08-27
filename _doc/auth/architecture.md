# Архитектура аутентификации (Corporate Auth)

Дата обновления: 2026-07-24.

## Режимы

| Режим | LDAP | SPNEGO | Пользователи |
|-------|------|--------|--------------|
| **Standalone** (по умолчанию) | `Ldap:Enabled=false` | `Spnego:Enabled=false` | Локально через `/api/users` |
| **Corporate** (опционально) | `Ldap:Enabled=true` | `Spnego:Enabled=true` | Sync AD + SSO |

## Токены

| Cookie | TTL | Назначение |
|--------|-----|------------|
| `ep_auth_token` | ~15 мин (access) | JWT access, HttpOnly |
| `ep_refresh_token` | 7 дней | Opaque refresh (в БД только SHA-256), ротация при `/auth/refresh` |

`POST /api/auth/refresh` — **AllowAnonymous**, читает refresh-cookie (не требует живой access JWT).
Ротация, grace-окно 30 c и координация вкладок описаны в `_doc/auth/session-management.md`.

## Роли

| Id | Имя | Права |
|----|-----|-------|
| 1 | SuperAdmin | Bypass матрицы + все проекты |
| 2 | Admin | Edit по seed (бывш. ProjectManager) |
| 3 | Engineer | Read по seed (бывш. Observer) |
| 4 | Gip | Edit Projects/Planning/Assessment/Import; Read справочники |

## MFA

- TOTP (Otp.NET), секрет в БД через DataProtection.
- Обязателен для ролей из `Mfa:RequiredRoles` (по умолчанию SuperAdmin, Admin) — soft-prompt в UI (можно отложить).
- Challenge: `RequiresMfa` + `mfaSessionToken` → `POST /auth/mfa/verify`.

## Защита

- Account lockout: 5 неудач → 15 мин (`RateLimitOptions`).
- Auth audit: таблица `AuthAuditLogs`.
- Rate limit на login (ASP.NET RateLimiter + lockout).

Ключевые файлы: `AuthController`, `JwtTokenService`, `RefreshTokenService`, `MfaService`, `useAuthStore`, `apiClient`.
