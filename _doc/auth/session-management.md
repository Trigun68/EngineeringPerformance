# Управление сессией: ротация refresh-токена и вкладки

Дата обновления: 2026-08-17. Связано: `_doc/auth/architecture.md`,
`_doc/issues/ISS-017-session-refresh-cross-tab.md`.

## Жизненный цикл

| Шаг | Что происходит |
|-----|----------------|
| Логин | Выдаются `ep_auth_token` (JWT ~15 мин) и `ep_refresh_token` (opaque, 7 дней) |
| 401 на любом запросе | `apiClient` один раз вызывает `POST /auth/refresh` и повторяет исходный запрос |
| Heartbeat | Периодический `POST /auth/refresh` продлевает сессию при активной вкладке |
| Logout / мёртвый refresh | Cookie удаляются, срабатывает `onUnauthorized` |

## Ротация с grace-окном (backend)

Каждый успешный refresh отзывает предъявленный токен (`RevocationReason = "Rotated"`,
`ReplacedByToken` → новый) и выдаёт замену. Отзыв и создание замены сохраняются **одним**
`SaveChangesAsync` через navigation property.

Повторный приход **того же** токена обрабатывается так:

```
токен отозван как "Rotated"  И  отзыв был ≤ 30 секунд назад  И  замена ещё активна
    → выдать новый токен (это вторая вкладка, а не кража)
иначе
    → Unauthorized, cookie удаляются
```

Grace-окно `RefreshTokenService.RotationGracePeriod` = **30 секунд**. Оно закрывает гонку вкладок,
но не легализует повторное использование давно отозванного токена.

## Координация вкладок (frontend)

`client/src/services/apiClient.ts`:

- Результат refresh — `RefreshOutcome`:
  - `ok` — исходный запрос повторяется;
  - `transient` — сеть/5xx: сессия считается живой, ошибка отдаётся вызывающему коду;
  - `dead` — 401/403: вызывается `onUnauthorized` (единственный путь к разлогину).
- Лок в `localStorage` (`engineering-performance-auth-refresh`, TTL 10 c): первая вкладка
  становится лидером и делает запрос, остальные ждут её результат (опрос каждые 150 мс) и не
  инициируют собственную ротацию.
- Изменения статуса рассылаются через BroadcastChannel; при его отсутствии — событие `storage`.

## Heartbeat

`AppLayout` продлевает сессию раз в 0.8 × `sessionExpirationMinutes`. Значение доступно только
администраторам (`GET /admin/system-settings` требует роль), поэтому остальные роли используют
интервал по умолчанию **10 минут** — иначе UI показывал ложное «Недостаточно прав».

## Диагностика «выбрасывает из системы»

| Симптом | Проверить |
|---------|-----------|
| Разлогин при работе в нескольких вкладках | Лок в `localStorage`, grace-окно в `RefreshTokenService` |
| Разлогин при передеплое API | Refresh должен вернуть `transient`, а не `dead` |
| «Недостаточно прав» вскоре после входа | Heartbeat не должен звать `/admin/system-settings` без роли |
| Разлогин через ~15 минут простоя | Heartbeat не запущен: проверить `user` в `AppLayout` |

## Тесты

- Backend: `EngineeringPerformance.Infrastructure.Tests/Auth/RefreshTokenRotationTests.cs`.
- Frontend: `client/src/services/__tests__/apiClient.test.ts` (7 сценариев: retry, лидер/ведомый,
  транзиентные ошибки, dead-сессия).
