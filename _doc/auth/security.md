# Безопасность auth (кратко)

Дата обновления: 2026-07-24.

| Контроль | Реализация |
|----------|------------|
| Пароли | BCrypt (work factor библиотеки по умолчанию) |
| Access JWT | HMAC-SHA256, короткий TTL (~15 мин) |
| Refresh | SHA-256 hash в БД, ротация, revoke / logout-all |
| MFA secrets | DataProtection at rest |
| Lockout | 5 fails → 15 мин |
| Audit | `AuthAuditLogs` (без паролей/токенов/secret) |
| SPNEGO | Только при `Spnego:Enabled` + TrustedProxySecret |
| LDAP | Только при `Ldap:Enabled` |
| Cookies | HttpOnly; Secure в prod/HTTPS |

Инциденты: `POST /auth/logout-all`, сброс MFA админом, смена пароля, ротация `Jwt:Secret` (инвалидирует все access JWT).
