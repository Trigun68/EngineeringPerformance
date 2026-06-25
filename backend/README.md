# Engineering Performance — Web API (ASP.NET Core 8)

Three-tier backend for the WPF → web migration. Coexists with the legacy desktop solution; uses the **same SQL Server database**.

## Structure

| Project | Layer |
|---------|--------|
| `EngineeringPerformance.Domain` | Entities, enums |
| `EngineeringPerformance.Application` | DTOs, CQRS handlers, interfaces |
| `EngineeringPerformance.Infrastructure` | EF Core, repositories, export/import |
| `EngineeringPerformance.Api` | Controllers, auth, middleware |

## Run locally

```bash
cd backend
dotnet restore EngineeringPerformance.Web.slnx
dotnet run --project EngineeringPerformance.Api
```

Swagger: `http://localhost:5000/swagger` (Development).

## Docker

From repository root:

```bash
cp .env.example .env
docker compose up --build
```

## Auth

1. **SPNEGO** — Nginx passes `X-Remote-User`; `SpnegoUserMiddleware` maps to `Users.WindowsUsername`.
2. **JWT** — `POST /api/auth/login` when SPNEGO is unavailable.

## Pilot module

`ProjectsController` — CRUD, list export (PDF/Excel), import stub.
