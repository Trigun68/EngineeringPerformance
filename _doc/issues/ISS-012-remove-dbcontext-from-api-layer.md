# ISS-012: Вынос AppDbContext и бизнес-логики из Api-слоя

**Статус:** ✅ Closed (2026-08-31)
**Категория:** Architecture / Layering
**Приоритет:** P1 (High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §2 (A-01), §3 (M-04f)

## Контекст
Нарушен инвариант «контроллеры без бизнес-логики, AppDbContext не покидает Infrastructure».

## Сделано
1. `MfaController` — без `AppDbContext`; загрузка пользователя в `IMfaService`, Reset через `IUserManagementService`.
2. Combined PDF: проверка версий в `IVisualReportService.EnsureVersionsBelongToProjectAsync`.
3. `ITrustedProxyValidator` общий для `AuthController` и `SpnegoUserMiddleware`.
4. `ISpnegoIdentityResolver` — резолв пользователя без DbContext в middleware (SPNEGO в nginx **не** включали).
5. `SqlConnectionTimeoutClassifier` в `Application.Diagnostics`; Api не импортирует Persistence.

## Acceptance Criteria
- [x] В `EngineeringPerformance.Api` нет `using EngineeringPerformance.Infrastructure.Persistence` / `AppDbContext`.
- [x] Классификатор SQL timeout в Application.
- [ ] Полная оркестрация login в отдельном `IAuthService` — уже была до цикла; не расширяли.
- [x] `dotnet build` + тесты (см. отчёт цикла).

## Связанные
- `.cursorrules` §1/§3; ISS-013
