---
name: engineering-performance-code
description: |
  Project coding requirements for Engineering Performance web stack.
  Overrides generic community skills that assume Tailwind/Postgres/Next.js.

  Use when writing or reviewing C#, TypeScript, React, EF Core, Docker, or
  Playwright code in this repo. Triggers: implement feature, fix bug, refactor,
  API endpoint, AG Grid, MUI, Zustand, import Excel, PerformanceCalculationEngine,
  db-init, e2e test.
---

# Engineering Performance — code requirements

## Stack (strict)

| Layer | Use | Do not use |
|-------|-----|------------|
| Backend | ASP.NET Core 8, C# 12, EF Core, MS SQL Server | Python/FastAPI, PostgreSQL, Npgsql |
| Frontend | React 18+, TypeScript, MUI, AG Grid, Zustand, Vite, axios | Next.js App Router, Tailwind, shadcn/Radix as defaults |
| PDF/Excel | QuestPDF, ClosedXML (`using` declarations) | — |
| Tests | xUnit + FluentAssertions (API), Vitest (client), Playwright E2E via Docker `:8081` | Vite `:5173` for E2E |
| Infra | Docker Compose (`backend`/`frontend`), Nginx + SPNEGO | — |

When a community skill suggests Tailwind / TanStack Router / Postgres / Next.js — **adapt to this table**, do not introduce those stacks.

## Backend

1. Layers: `Api → Application → Domain → Infrastructure`. No business math in controllers or import services.
2. Calculations only in `PerformanceCalculationEngine` (`IPerformanceCalculationEngine`) — see `.cursor/rules/performance-calculation-engine-ssot.mdc`.
3. IIC group labels always `код — наименование (ед.)` via `FormatCodeNameWithResolvedUnit` / `formatIisNorm` — see `.cursor/rules/iic-group-naming.mdc`.
4. Export filenames: Russian `{модуль}_{вкладка}_-_{код}_{название}_({версия}).ext` via `ExportFileNameBuilder` / `IExportFileNameService`; client reads `Content-Disposition` — see `.cursor/rules/export-file-naming.mdc`.
5. DTOs are `record` types in Application; never return EF entities from API.
6. Primary constructors, `Async` suffix, `CancellationToken` last.
7. Permissions: `[HasPermission(PermissionType.X, AccessLevel.Read|Edit)]`.
8. EF: parameterized queries only; `.AsNoTracking()` for UI grid reads; one `DbContext` per request.
9. Version copy must deep-clone PPV, LI, Equipment, iterations.

## Frontend

1. No `any` — use `unknown` + narrowing.
2. Dictionaries: `GET /api/dictionaries/all` → `useDictionaryStore`; invalidate after mutations.
3. AG Grid only via `DataTable`; `columnDefs` in `useMemo`; layout via `userSettingsApi`.
4. Permissions: `usePermission(objectName)` — names match WPF `AccessControlConstants`.
5. API clients normalize PascalCase from backend; `withCredentials: true`.
6. UI language: Russian for user-facing strings; code identifiers English.
7. Export/import captions: only `ACTION_LABELS` + `ExportActionButton` / `ToolbarActionButton` (see `.cursor/rules/ui-action-labels.mdc`).
7. Export/import captions: only `ACTION_LABELS` + `ExportActionButton` / `ToolbarActionButton` (see `.cursor/rules/ui-action-labels.mdc`).

## Language policy

- Code, commits, API routes/names: **English**
- Comments in code and docs under `docs/` / `_doc/`: **Russian**

## Verification after changes

| Change | Run |
|--------|-----|
| `client/` | `cd client && npm run build` (+ Vitest; Playwright on `:8081` after UI) |
| `backend/` | `dotnet test` (or Docker SDK image if no host `dotnet`) |
| Compose / migrations | `docker compose up -d --build` |

## Related skills

Prefer these installed skills for depth, then apply **this** skill as the project filter:

- Backend API: `dotnet-webapi`, `dotnet-authorization`, `optimizing-ef-core-queries`
- Tests: `dotnet-testing`, `run-tests`, `code-testing-agent`, `react-testing`, `playwright-cli`
- React quality: `production-react`, `vercel-react-best-practices` (perf patterns only; keep MUI/Zustand)
- SPA structure: `react-spa` (Vite/axios OK; replace Tailwind/Radix with MUI)
- Infra: `docker`, `ci-cd`, `authoring-github-workflows`
- Quality baseline: `code-quality-standards`, `code-writing`, `security-guidelines`
