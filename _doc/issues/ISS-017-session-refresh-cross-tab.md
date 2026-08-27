# ISS-017: Ложные разлогины — гонка refresh-токена между вкладками

**Статус:** ✅ Выполнено (2026-08-17)
**Категория:** Auth / Session
**Приоритет:** P0 (High)
**Источник:** жалоба пользователя «часто на клиенте выбрасывает» + разбор в аудите

## Контекст
Refresh-токен ротируется: каждый успешный `POST /auth/refresh` отзывает предъявленный токен
(`RevocationReason = "Rotated"`) и выдаёт новый. Отсюда три причины ложных разлогинов:

1. **Гонка вкладок.** Две вкладки почти одновременно шлют refresh с одним и тем же cookie.
   Первая ротирует токен, вторая приходит с уже отозванным → `Unauthorized` → cookie удаляются →
   разлогинены **обе** вкладки, хотя сессия жива.
2. **Транзиентные ошибки.** Клиент трактовал любой неуспешный refresh (таймаут, 502 при
   передеплое) как «сессия мертва» и вызывал `onUnauthorized`.
3. **Heartbeat у неадминистраторов.** `AppLayout` запрашивал `/admin/system-settings`, чтобы
   узнать интервал; роль без прав получала 403 и ложное «Недостаточно прав».

## Реализация
**Backend** (`RefreshTokenService`):
- Ротация выполняется одним `SaveChangesAsync` через navigation property `ReplacedByToken`
  (раньше — два сохранения, между которыми возможна гонка).
- Grace-окно **30 секунд**: если предъявлен токен, отозванный как `Rotated`, и его замена ещё
  активна, выдаётся новый токен вместо `Unauthorized`. Повторное использование давно отозванного
  или скомпрометированного токена по-прежнему отклоняется.

**Frontend** (`apiClient.ts`):
- Тип результата `RefreshOutcome = 'ok' | 'dead' | 'transient'`; `onUnauthorized` вызывается
  только для `dead`, транзиентные ошибки возвращаются вызывающему коду.
- Межвкладочный лок в `localStorage` (`engineering-performance-auth-refresh`, TTL 10 c):
  ведомые вкладки ждут результат лидера, а не шлют собственный refresh.
- Починен fallback BroadcastChannel: слушается событие `storage` окна.
- `refreshSessionHeartbeat()` — единая точка heartbeat.

**AppLayout:** интервал сессии запрашивается только у администраторов; остальные роли используют
безопасный интервал по умолчанию (10 минут).

## Acceptance Criteria
- [x] Параллельный refresh из двух вкладок не разлогинивает пользователя.
- [x] Сетевая ошибка/5xx на refresh не сбрасывает сессию.
- [x] Неадминистратор не получает 403 от heartbeat.
- [x] Тесты: `RefreshTokenRotationTests` (backend), `apiClient.test.ts` — 7 сценариев (frontend).

## Файлы
- `backend/EngineeringPerformance.Infrastructure/Auth/RefreshTokenService.cs`
- `backend/EngineeringPerformance.Infrastructure.Tests/Auth/RefreshTokenRotationTests.cs`
- `client/src/services/apiClient.ts`, `client/src/services/__tests__/apiClient.test.ts`
- `client/src/layout/AppLayout.tsx`

## Связанные
- `_doc/auth/session-management.md`
