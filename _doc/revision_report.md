# Отчёт ревизии кодовой базы — Engineering Performance (Web)

**Дата:** 2026-06-09 (обновлено 2026-07-14 — аудит docs↔код; 2026-09-08 — цикл коррекции архитектуры)  
**Роль:** Principal Architect / Lead Fullstack  
**Стек:** ASP.NET Core 8 + React/TS + MS SQL + Docker/Nginx  
**Scope:** **только web** — WPF не рассматривается ([web-only-development-plan.md](../_doc/plans/web-only-development-plan.md))

---

## Сводка (актуально)

| Область | Оценка | Комментарий |
|---------|--------|-------------|
| Справочники API/UI | ✅ | CRUD, деревья, FK-валидация, soft-check delete |
| Расчёт производительности | ✅ / ⚠️ | Ядро единое; parent forecast FIX-01 закрыт |
| Excel-импорт | ⚠️ | Работает; ImportApiTests smoke; мало happy-path fixtures |
| Версионирование | ✅ | Deep copy: PPV/LI/Equipment + PlanningScenario + LI-поля |
| Права доступа | ✅ | dictionaries ACL; gate вкладок/навигации; единый `[HasPermission]` + `FallbackPolicy` + страж `ControllerAuthorizationCoverageTests` (ADR-006) |
| Групповой анализ | ✅ | Program/Customer + weighted Z; тренд → `/analytics/trends` |
| PDF-отчёты | ✅ | Стиль портала, иерархия, шкала Z (аудит 2026-07-14) |
| Тесты | ⚠️ | Api.Tests 50+; CI fail без SQL; Skip вне CI |
| Docker/Infra | ✅ | Multi-stage OK; единый `client/nginx.conf.template` (`X-Trusted-Proxy-Token`, HSTS, `include spnego*.inc`), overlay `docker-compose.spnego.yml`; журнал `dbo.SchemaMigrations` в db-init |

---

## Закрытые ранее «gaps» (не повторять как открытые)

| Было в ревизии | Факт |
|----------------|------|
| `/dictionaries/all` без ACL | `[HasPermission(References, Read)]` + тест 403 |
| Копия версии без данных | Deep copy полный + PlanningScenario/LI |
| Вкладки/nav без gate | `ProjectDetailPage` + `useAccessibleNavigation` |
| Group analytics placeholder | `GroupAnalyticsService` реальный |
| FluentValidation отсутствует | Validators + AutoValidation |
| Два алгоритма прогноза (листья) | `ForecastHoursAggregator` + FIX-01 родители |
| SPNEGO в docker-стеке не проходил `TrustedProxyValidator` | `client/nginx.conf.template` + envsubst `PROXY_TRUSTED_SECRET`; корневые `nginx.conf*` удалены |
| Монолит импорта (`ProjectExcelDataImportService`, ~3500 строк) | `PhysicalVolumeExcelImportService` / `EquipmentImportService` / `LaborIntensityImportService`; фасад только делегирует |
| Мутации версий/данных/пользователей без аудита | `IUserActionLogger` после `SaveChanges` во всех сервисах; `AuditLoggingApiTests` |
| Формулы вне движка (weighted Z, overrun, `Math.Exp`) | Только `IPerformanceCalculationEngine`; `WeightedZAggregator` — `internal` |

---

## Открытые риски

1. **Импорт** — нужны fixture happy-path тесты (S1-05); список проектов = 501.
2. **SPNEGO prod** — образ `nginx:alpine` без `ngx_http_auth_spnego_module`; включение — сборка образа с модулем + keytab (см. `DEPLOY.md`); конфиг уже готов.
3. **`IicNormId` на КФО** — поле не используется в расчётах (задокументировано).
4. **E2E/UAT** — расширять покрытие PDF/group-analysis.
5. ~~Монолит импорта~~ — закрыто 2026-09-08 (см. таблицу выше).

**Не в scope:** паритет и сверка с WPF.

**См. также:** `_doc/plans/architecture-audit-orchestration.md`, `_doc/architecture/business-logic.md`.
