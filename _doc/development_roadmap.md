# Development Roadmap — Engineering Performance Web

**Дата:** 2026-07-14  
**Основа:** `_doc/architecture/business-logic.md`, `_doc/plans/web-only-development-plan.md`  
**Цель:** зрелый web-продукт; **WPF вне scope** (не паритет, не эталон)

**Политика:** разработка только `backend/` + `client/`. См. [web-only-development-plan.md](../_doc/plans/web-only-development-plan.md).

---

## Спринт 0 — Критические исправления (1–2 недели)

| ID | Задача | Файлы | Промпт для Cursor |
|----|--------|-------|-------------------|
| S0-01 | Закрыть `GET /dictionaries/all` правами | `DictionariesController.cs`, `Api.Tests` | «Добавь `[HasPermission(PermissionType.References, Read)]` на `DictionariesController.GetAll` и integration-тест 403 для observer» |
| S0-02 | Копия версии с данными планирования | `ProjectVersionDeepCopyService.cs`, `ProjectVersionsController` | «Клонируй PPV (с LI-полями), PEQ, PlanningScenario; PLI — только Fact; snapshot норм на дату копии» |
| S0-03 | Унификация прогноза (A-01) | `ProjectForecastService.cs`, `LaborIntensityCalculator` | «Переведи `ProjectForecastService` на `LaborIntensityCalculator` — те же часы, что `ProjectsPageGridBuilder`» |
| S0-04 | API-тест 403 permissions | `Api.Tests/PermissionsApiTests.cs` | «Добавь тест: observer без `Users` получает 403 на `GET /api/users`» |
| S0-05 | UI: gate nav Projects/References/Help | `AppLayout.tsx` | «Скрывай пункты меню по `usePermission('Projects'|'References'|'HelpPage')`» |
| S0-06 | UI: gate ProjectDetail tabs | `ProjectDetailPage.tsx` | «Фильтруй вкладки по permissions: ProjectPhysicalVolumes, ProjectLaborIntensities, ProjectEquipments, ProjectDataUpload, Assessment, Planning, VersionComparison, GroupAnalysis» |
| S0-07 | Убрать stub ImportExcel проектов | `ProjectsController`, UI | «Либо реализуй импорт списка проектов, либо верни 501 и убери кнопку до готовности» |

---

## Спринт 1 — Бизнес-логика и функциональность (2–3 недели)

| ID | Задача | Файлы | Промпт для Cursor |
|----|--------|-------|-------------------|
| S1-01 | Групповой анализ (реальный API) | `GroupAnalyticsService.cs`, `GroupAnalysisTab.tsx` | «Замени placeholder: агрегация по нескольким проектам по спецификации business-logic.md» |
| S1-02 | Групповой тренд | `TrendAnalyticsService` / новый сервис, UI | «Реализуй групповой тренд или удали заглушку `group-trend`; переименуй permission при необходимости» |
| S1-03 | Версия: edit + archive UI | `ProjectDetailPage.tsx`, `projectVersionsApi.ts` | «Редактирование версии (description, direction, physicalVolumeTypeId) и archive/unarchive» |
| S1-04 | Import wizard permissions | `ImportWizard.tsx` | «Оберни вкладку Импорт в `usePermission('ProjectDataUpload')`» |
| S1-05 | Excel import tests | `Api.Tests/ImportApiTests.cs`, fixtures | «Тесты импорта: валидный файл 200, неверная структура 400, preview unmatched» |
| S1-06 | FluentValidation POC | `Application/Validators/`, `Program.cs` | «FluentValidation для `DirectoryWriteRequest` и `ProjectWriteRequest`» |
| S1-07 | Валидация FK справочников (A-04) | `DirectoryFacade.cs` | «Проверка ClassifierId, UnitId, ParentId при create/update» |
| S1-08 | UI: предупреждение fallback прогноза (A-03) | `ForecastPanel.tsx`, `PlanningTab.tsx` | «Показывай warning, если норма отсутствует на Plan без PLI» |

---

## Спринт 2 — Тестирование >80% coverage (2 недели)

| ID | Задача | Файлы | Промпт для Cursor |
|----|--------|-------|-------------------|
| S2-01 | Testcontainers MS SQL | `Api.Tests`, `.github/workflows/api-tests.yml` | «Testcontainers.MsSql для стабильного CI без skip» |
| S2-02 | Import unit tests | `Infrastructure.Tests/` | «Нормализация EP-кода, labor type matching, equipment (Name, UnitId)» |
| S2-03 | IIC calculation edge cases | `PerformanceCalculationEngineTests` | «σ=0, пустые нормы, PlannedHours=0, иерархия PPV» |
| S2-04 | Vitest setup | `client/vitest.config.ts` | «Vitest: `usePermission`, `useDictionaryStore`» |
| S2-05 | Playwright: import flow | `e2e/import-wizard.spec.ts` | «E2E: login → project → import → grid row visible» |
| S2-06 | Playwright: observer denied | `e2e/permissions.spec.ts` | «E2E: observer → /settings/users → 403» |

---

## Спринт 3 — DX и Infra (1–2 недели)

| ID | Задача | Файлы |
|----|--------|-------|
| S3-01 | Nginx SPNEGO prod profile | SoT: `client/nginx.conf.template` + `client/nginx/spnego.inc.example`; overlay `docker-compose.spnego.yml` (тот же образ frontend, без корневых `nginx.conf*`); `_doc/deployment/DEPLOY.md` |
| S3-02 | Разбить ProjectExcelDataImportService | `Infrastructure/Import/*` |
| S3-03 | Унифицировать authorization | Controllers → `[HasPermission]` |
| S3-04 | TanStack Query (тяжёлые экраны) | `projects`, `dictionaries/all` |
| S3-05 | OpenAPI / Swagger dev-only | `Program.cs` |

---

## Метрики готовности

- [x] S0-01 dictionaries ACL
- [x] S0-02 deep copy планирования
- [x] S0-03 унификация прогноза (+ FIX-01 parents)
- [x] S0-06 gate ProjectDetail tabs
- [x] S0-07 ImportExcel проектов → 501 + клиентский stub убран
- [x] S1-01 групповой анализ
- [x] S1-07 FK справочников
- [x] S1-08 UI warning fallback
- [ ] Playwright green полный suite на CI
- [x] Backend tests без silent skip в CI (fail); вне CI — SkipException
- [ ] UAT `_doc/plans/uat-phase4-checklist.md` — **только web-клиент**
- [ ] ~~WPF fallback~~ — **не применяется** (web-only)

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-07-14 | Аудит docs↔код: закрыты S0/S1 ключевые; PDF portal-like |
| 2026-07-14 | Web-only: убран паритет WPF; приоритет A-01 унификация прогноза |
| 2026-06-09 | Первоначальная версия roadmap |
