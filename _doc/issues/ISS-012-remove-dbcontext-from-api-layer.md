# ISS-012: Вынос AppDbContext и бизнес-логики из Api-слоя

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** Architecture / Layering
**Приоритет:** P1 (High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §2 (A-01), §3 (M-04f)

## Контекст
Нарушен инвариант «контроллеры без бизнес-логики, AppDbContext не покидает Infrastructure»:
- `AuthController` (502 строки): оркестрация логина, lockout, ветвление MFA, загрузка прав из `AccessRights`, `SaveChangesAsync` (LastLoginAt), маппинг `User` → DTO. Дубли: `MapUser` ≡ `MapLoginUser` (стр. 477-501).
- `MfaController`: `AppDbContext` в primary ctor, резолв текущего пользователя запросами к БД.
- `ProjectsController.ExportCombinedPdf`: валидация принадлежности версий проекту прямым запросом (стр. 186-198).
- `SpnegoUserMiddleware`: резолв пользователя из БД; `IsTrustedProxyRequest` продублирован с `AuthController` (438-449 ↔ 63-74).

## Решение (направление)
1. Application-сервисы: `IAuthService` (login/refresh/logout оркестрация), `IUserPermissionsLoader`; контроллеры — тонкие адаптеры.
2. Валидацию версий перенести в `IVisualReportService.ExportCombinedPdfAsync`.
3. Общий `ITrustedProxyValidator` (единая проверка `X-Trusted-Proxy-Token`) для middleware и контроллера.
4. Слить `MapUser`/`MapLoginUser`.

## Acceptance Criteria
- [ ] В `EngineeringPerformance.Api` нет `using EngineeringPerformance.Infrastructure.Persistence` / `AppDbContext`.
- [ ] Бизнес-логика auth покрыта unit-тестами на Application-уровне.
- [ ] `dotnet build` + `dotnet test` зелёные; smoke `./scripts/uat-smoke.sh` OK.

## Файлы
- `backend/EngineeringPerformance.Api/Controllers/AuthController.cs`, `MfaController.cs`, `ProjectsController.cs`
- `backend/EngineeringPerformance.Api/Middleware/SpnegoUserMiddleware.cs`
- новые сервисы в `backend/EngineeringPerformance.Application/Auth/`

## Связанные
- `.cursorrules` §1/§3; отчёт аудита
