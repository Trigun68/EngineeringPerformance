# EngineeringPerformance

Веб-приложение для анализа инженерной производительности (миграция с WPF).

## Быстрый старт (Docker)

```bash
cp .env.example .env
# задать SQL_SA_PASSWORD и JWT_SECRET (≥32 символов)
docker compose build api web
docker compose up -d
```

Открыть http://localhost:${WEB_PORT:-80} — логин по умолчанию `admin` / `admin`.

## Структура

| Путь | Назначение |
|------|------------|
| `backend/` | ASP.NET Core 8 API |
| `client/` | React + Vite + MUI + AG Grid |
| `EngineeringPerformance.App/` | WPF reference (archived) |
| `_install/DEPLOY.md` | Развёртывание и rollback |
| `_doc/migration_map.md` | Карта миграции WPF → Web |

## Проверки

```bash
./scripts/uat-smoke.sh http://localhost:8081
./scripts/perf-smoke.sh http://localhost:8081
cd client && npm run test:e2e
```

API tests (Docker SDK):

```bash
docker run --rm --network host -v $(pwd)/backend:/src -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test EngineeringPerformance.Api.Tests/EngineeringPerformance.Api.Tests.csproj
```

## Фазы миграции

- Phase 1–2: API, справочники, данные проекта ✅
- Phase 3: Аналитика и экспорт ✅
- Phase 4: Cutover — см. `_doc/plans/cutover-runbook.md`
