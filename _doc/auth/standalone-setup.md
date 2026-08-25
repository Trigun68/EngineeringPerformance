# Standalone: быстрый запуск auth

Дата обновления: 2026-07-24.

## Условия

- `Ldap:Enabled=false`, `Spnego:Enabled=false` (дефолт compose).
- MSSQL + Redis (Redis опционален — fallback in-memory).
- Миграция `108_CorporateAuthRolesAndTokens.sql` через `docker-db-init`.

## Первый вход

1. Пользователь seed `admin` (роль SuperAdmin).
2. Логин/пароль — как в seed / `.env` (не коммитить секреты).
3. При `Mfa:Enabled=true` появится диалог настройки MFA — можно **«Настроить позднее»** (`data-testid="mfa-setup-skip"`).

## Управление пользователями

- UI: Настройки → Пользователи.
- API: `POST/PUT /api/users`, unlock, reset-mfa, reset-password (временный пароль + `MustChangePassword`).

## Переменные (compose)

```bash
Jwt__AccessTokenLifetimeMinutes=15
Jwt__RefreshTokenLifetimeDays=7
Mfa__Enabled=true          # для e2e можно false
Ldap__Enabled=false
Spnego__Enabled=false
```

См. также [architecture.md](architecture.md), [mfa-guide.md](mfa-guide.md).
