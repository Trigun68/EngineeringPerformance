# Сравнение архитектуры и roadmap совершенствования

**Дата:** 2026-06-29  
**Основание:** целевой стек React + ASP.NET 8 (enterprise)  
**Ограничение:** документ фиксирует план и приоритеты; кодовые изменения не включены.

---

## 1) Сравнение текущей архитектуры с целевым стеком

### Frontend (React)

| Направление | Целевой стек | По факту в проекте | Статус |
|---|---|---|---|
| Базовый UI стек | React 18 + TS + Vite + Router v6 | Есть: React 18, TS strict, Vite, Router v6 | Соответствует |
| Server-state | React Query | Нет, используется `useEffect` + API layer + Zustand (частично кэш) | Gap |
| Runtime-валидация | Zod | Нет | Gap |
| Формы | RHF (опционально) | Нет, ручные MUI-формы | Допустимо, но есть техдолг |
| Error boundary | ErrorBoundary (опционально) | Нет централизованного boundary | Gap (не критичный) |
| UI state | Zustand (опционально) | Есть и используется | Соответствует |
| API client | Axios (опционально) | Есть и используется | Соответствует |
| Component docs | Storybook (опционально) | Нет | Отложить |
| API mocking | MSW (опционально) | Нет | Отложить |

Подтверждающие файлы: [client/package.json](../../client/package.json), [client/src/App.tsx](../../client/src/App.tsx), [client/src/services/apiClient.ts](../../client/src/services/apiClient.ts), [client/src/store/useDictionaryStore.ts](../../client/src/store/useDictionaryStore.ts).

### Backend (ASP.NET 8)

| Направление | Целевой стек | По факту в проекте | Статус |
|---|---|---|---|
| Web framework | ASP.NET Core 8 | Есть | Соответствует |
| CQRS | MediatR | Нет MediatR, есть собственный light CQRS частично | Частично |
| Валидация | FluentValidation | Подключен, но покрытие неполное | Частично |
| Логирование | Serilog | Есть | Соответствует |
| Health checks | Health Checks | Есть, но не весь набор стандартных probes | Частично |
| ORM | EF Core 8 | Есть | Соответствует |
| Быстрые read-запросы | Dapper | Нет | Отложить до подтвержденных bottleneck |
| Миграции в CI/CD | Обязательно | Частично, есть риски рассинхрона | Gap |
| Кэш | Redis + IMemory + OutputCache | Redis есть, IMemory/OutputCache по факту нет | Частично |
| Async/queues | RabbitMQ + Hangfire + Outbox | Нет | Отложить |
| Auth enterprise | OIDC/Keycloak + JWT + RBAC | JWT + RBAC + SPNEGO есть, OIDC/Keycloak нет | Частично |
| Observability | OTel + Prometheus + central logs | Serilog + внутренний мониторинг, OTel/Prometheus нет | Частично |

Подтверждающие файлы: [backend/Directory.Build.props](../../backend/Directory.Build.props), [backend/EngineeringPerformance.Api/Program.cs](../../backend/EngineeringPerformance.Api/Program.cs), [backend/EngineeringPerformance.Infrastructure/DependencyInjection.cs](../../backend/EngineeringPerformance.Infrastructure/DependencyInjection.cs), [backend/EngineeringPerformance.Api/EngineeringPerformance.Api.csproj](../../backend/EngineeringPerformance.Api/EngineeringPerformance.Api.csproj), [backend/EngineeringPerformance.Infrastructure/EngineeringPerformance.Infrastructure.csproj](../../backend/EngineeringPerformance.Infrastructure/EngineeringPerformance.Infrastructure.csproj).

### DevOps / Тесты / БД

| Направление | Целевой стек | По факту в проекте | Статус |
|---|---|---|---|
| Контейнеризация | Docker | Есть, зрелый compose-профиль | Соответствует |
| Оркестрация | Kubernetes | Нет | Отложить |
| CI/CD | Полный pipeline + progressive deploy | Есть частичный CI (API tests, E2E), нет полноценного CD | Gap |
| Тесты backend | xUnit + Testcontainers | Есть xUnit; Testcontainers есть, но не весь контур в CI | Частично |
| Тесты frontend | RTL + Playwright | Есть | Соответствует |
| БД | SQL Server/PostgreSQL + масштабирование | SQL Server есть; реплики/партиционирование нет | Достаточно на текущем этапе |

Подтверждающие файлы: [docker-compose.yml](../../docker-compose.yml), [.github/workflows/api-tests.yml](../../.github/workflows/api-tests.yml), [.github/workflows/e2e-tests.yml](../../.github/workflows/e2e-tests.yml), [client/playwright.config.ts](../../client/playwright.config.ts), [scripts/docker-db-init.sh](../../scripts/docker-db-init.sh).

---

## 2) Что считаем ненужным сейчас (и почему)

1. **Kubernetes + blue-green/canary**  
   Не приоритет при текущем single-host Docker Compose и отсутствии требований по multi-cluster.
2. **Keycloak/OIDC как обязательный шаг «сейчас»**  
   Текущая модель JWT + RBAC + SPNEGO покрывает on-prem сценарий.
3. **RabbitMQ/Hangfire/Outbox «на старте»**  
   В кодовой базе нет зрелых асинхронных bounded context, требующих брокера.
4. **Dapper как стандарт по умолчанию**  
   Вводить точечно после профилирования конкретных узких read-path.
5. **Storybook/MSW как блокер релизов**  
   Полезно, но ниже по приоритету, чем CI reliability, валидация и server-state стратегия.

---

## 3) Предложение по совершенствованию архитектуры (приоритеты)

## Этап 1 — надежность платформы и релизного контура (обязательный)

1. Стабилизировать процесс миграций и избежать drift между схемой и runtime.
2. Усилить CI: исключить «ложно-зеленые» прогоны при пропуске тестов/данных.
3. Закрепить минимальные quality gates: build + tests + smoke на критических потоках.

### Этап 2 — server-state архитектура frontend (обязательный)

1. Инкрементально внедрить TanStack Query в самые проблемные модули (проекты, тяжелые справочники).
2. Оставить Zustand для client-state (auth/UI), не дублировать server-cache.
3. Унифицировать паттерны query key, invalidation, retry, cancellation.

### Этап 3 — валидация и контрактность (обязательный)

1. Довести FluentValidation до полного покрытия write DTO/commands.
2. Добавить Zod для критичных frontend-форм и входных payload.
3. Утвердить единый формат ошибок и контракты ProblemDetails end-to-end.

### Этап 4 — observability без переусложнения (после 1–3)

1. Сначала централизация логов/метрик в текущей модели.
2. OpenTelemetry/Prometheus вводить только при подтвержденной необходимости.

### Этап 5 — security hardening (после 1–3)

1. Refresh token flow для JWT.
2. Финализация production-профиля SPNEGO + fallback сценариев.
3. Регулярная сверка permission-matrix backend/frontend.

---

## 4) Гейты внедрения отложенных технологий

### OIDC / Keycloak

Переходим к внедрению, если выполняется хотя бы **2** условия:
- Нужно SSO для **2+ внешних приложений**.
- Появились внешние B2B/B2E федерации (не только AD/SPNEGO).
- Появились требования комплаенса к централизованному IdP и lifecycle сессий.

### RabbitMQ / Hangfire / Outbox

Переходим к внедрению, если выполняется хотя бы **2** условия:
- Есть долгие операции (**> 10 сек**) в синхронных HTTP потоках, влияющие на UX/SLA.
- Нужны гарантированные ретраи/идемпотентная доставка между bounded context.
- Появились регулярные фоновые задачи с расписанием и SLA.

### OpenTelemetry / Prometheus / Grafana

Переходим к внедрению, если выполняется хотя бы **2** условия:
- Более **1 инстанса** backend в production.
- Нужна трассировка межсервисных цепочек (distributed tracing).
- Текущие средства мониторинга не дают RCA по p95/p99 инцидентам.

### Kubernetes + progressive deploy

Переходим к внедрению, если выполняется хотя бы **2** условия:
- Требуется zero-downtime deployment с жестким SLA.
- Горизонтальное масштабирование сервисов становится регулярной задачей.
- Появляется необходимость мультисред/мультитенантности с управляемым rollout.

---

## 5) Результат выполнения текущего плана

- Приоритет этапов 1–3 зафиксирован как обязательный.
- Список «ненужных сейчас» технологий формализован.
- Гейты перехода к OIDC/очередям/OTel/K8s согласованы в виде проверяемых условий.

