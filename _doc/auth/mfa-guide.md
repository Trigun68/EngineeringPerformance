# MFA (TOTP) — краткая инструкция

Дата обновления: 2026-07-24.

## Пользователь

1. После входа (роли SuperAdmin/Admin) откроется диалог настройки.
2. Скопируйте секрет или `otpauth://` URI в Google Authenticator / Microsoft Authenticator / 1Password.
3. Введите 6-значный код → «Подтвердить и включить».
4. «Настроить позднее» — приложение остаётся доступным до включения MFA.

При уже включённой MFA вход: пароль → окно кода → cookies выдаются после `POST /api/auth/mfa/verify`.

## Администратор

- Сброс MFA: Пользователи → Reset MFA (`POST /api/users/{id}/reset-mfa`).
- После сброса пользователь снова проходит setup.

## Конфиг

- `Mfa:Enabled`, `Mfa:RequiredRoles` (по умолчанию SuperAdmin, Admin).
- Секреты шифруются DataProtection (`Mfa:SecretEncryptionPurpose`).
