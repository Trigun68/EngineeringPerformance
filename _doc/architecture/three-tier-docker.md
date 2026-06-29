# Трёхзвенная архитектура (Docker)

**Обновлено:** 2026-06-29  
**Связанные документы:** [docs/migration_map.md](../../docs/migration_map.md), [docs/development_roadmap.md](../../docs/development_roadmap.md), [_doc/plans/architecture-comparison-and-improvement-roadmap.md](../plans/architecture-comparison-and-improvement-roadmap.md)

## 1) Обзор архитектуры (текущее состояние)

```
Браузер
  └── Nginx (SPNEGO proxy headers, статика React, /api reverse proxy)
        ├── client/ (React 18 + TypeScript + MUI + AG Grid + Zustand + TanStack Query)
        └── backend/EngineeringPerformance.Api (ASP.NET Core 8)
              ├── Application (DTO, validators, contracts, interfaces)
              ├── Domain (entities, enums)
              └── Infrastructure (EF Core, import/export, auth, monitoring)
                    ├── MS SQL Server
                    └── Redis (кэш/health-индикация)
```

Текущий production-контур остается трёхзвенным: Web (Nginx + SPA) -> API -> SQL Server.
Redis подключен как дополнительная инфраструктурная зависимость, не как отдельный бизнес-слой.

## 2) Backend слои и кросс-срезы

### Слои

- `Api`: контроллеры, middleware, авторизация.
- `Application`: контракты, DTO, FluentValidation-валидаторы.
- `Domain`: сущности и бизнес-типы.
- `Infrastructure`: EF Core, импорт/экспорт, auth-сервисы, мониторинг.

### Ключевые middleware и cross-cutting

- `CorrelationIdMiddleware` добавляет `X-Correlation-Id` в ответ и лог-контекст.
- `RequestMetricsMiddleware` собирает RED-метрики и p95 latency.
- `ExceptionHandlingMiddleware` выдает RFC 7807 `ProblemDetails` с `correlationId`.
- `SpnegoUserMiddleware` обрабатывает trusted proxy заголовки для Windows auth.

## 3) Frontend архитектурные принципы

- **Server-state:** TanStack Query (`QueryClientProvider`, `queryClient`) для запросов/кэша серверных данных.
- **Client-state:** Zustand для UI/auth state и словарей.
- **UI:** MUI + AG Grid (treeData, grouped tables, табличные сценарии WPF parity).
- **API слой:** axios-клиент с `withCredentials`, обработкой 401/403 и single-flight `refresh`.
- **Контрактность:** runtime-проверка критичных API-ответов через Zod в API-адаптерах.

## 4) Аутентификация и авторизация

| Направление | Реализация |
|---|---|
| JWT cookie auth | `POST /api/auth/login`, HttpOnly cookie `ep_auth_token`, endpoint `POST /api/auth/refresh` |
| SPNEGO fallback | trusted proxy (`X-Trusted-Proxy-Token` + `X-Remote-User`), endpoint диагностики `GET /api/auth/spnego-profile` |
| RBAC API | `[HasPermission]` + `PermissionAuthorizationHandler` |
| RBAC UI | `usePermission()` и скрытие/блокировка действий |
| Drift контроль прав | `PermissionObjectCatalog` + `GET /api/monitoring/permission-drift` |

## 5) Наблюдаемость и эксплуатация

- Monitoring API:
  - `GET /api/monitoring/system-metrics`
  - `GET /api/monitoring/health`
  - `GET /api/monitoring/logs`
  - `GET /api/monitoring/audit-config`
  - `GET /api/monitoring/permission-drift`
- Monitoring UI показывает RED метрики, p95, статус зависимостей и статус permission drift.
- Health endpoint для инфраструктуры: `GET /api/health` (anonymous).

## 6) Валидация и контракт API

- Backend: FluentValidation подключен через auto-validation.
- Ошибки валидации нормализуются в единый `ValidationProblemDetails` (camelCase keys, `errorCode`, `correlationId`).
- Frontend: адаптеры API используют контрактный парсинг (Zod) для раннего обнаружения рассинхрона DTO.

## 7) CI/CD и quality gates

- `api-tests.yml`: поднимает SQL + Redis + db-init, выполняет API/infrastructure tests.
- `e2e-tests.yml`: запускает client unit tests, client build, затем Playwright E2E в Docker.
- Локальный единый прогон: `scripts/quality-gates.sh` (backend tests + client test/build + smoke).

## 8) Статус зрелости и отложенные компоненты

Состояние после выполнения этапов 1-5 roadmap:

- Этап 1 (platform reliability): реализован.
- Этап 2 (frontend server-state): реализован инкрементально.
- Этап 3 (validation + contract): реализован.
- Этап 4 (observability): реализован базовый слой (correlation + p95 + dashboard).
- Этап 5 (security hardening): реализован (refresh/SPNEGO profile/permission drift).

Отложено до подтвержденной необходимости: OIDC/Keycloak, очередь задач (RabbitMQ/Hangfire/Outbox), OTel/Prometheus/Grafana, Kubernetes/progressive deploy.

## 9) Локальный запуск

```bash
cp .env.example .env
docker compose up --build
```

Web UI: `http://localhost:${WEB_PORT}` (по умолчанию `8081`).
