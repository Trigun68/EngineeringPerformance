# Development Roadmap — Engineering Performance Web

**Дата:** 2026-06-09  
**Основа:** `docs/revision_report.md`  
**Цель:** паритет с WPF + >80% coverage на критических узлах

---

## Спринт 0 — Критические исправления (1–2 недели)

| ID | Задача | Файлы | Промпт для Cursor |
|----|--------|-------|-------------------|
| S0-01 | Закрыть `GET /dictionaries/all` правами | `DictionariesController.cs`, `Api.Tests` | «Добавь `[HasPermission(PermissionType.References, Read)]` на `DictionariesController.GetAll` и integration-тест 403 для observer» |
| S0-02 | Копия версии с данными | `ProjectVersionService.cs`, `IProjectVersionService`, `ProjectVersionsController`, `ProjectDetailPage.tsx` | «Реализуй `CopyAsync`: клонировать PPV, ProjectLaborIntensity, ProjectEquipment и import iterations как в WPF `ProjectAnalysisVersionCopyService`» |
| S0-03 | Порт тестов PerformanceService в backend CI | `EngineeringPerformance.Infrastructure.Tests` (новый), `PerformanceService.cs` (backend) | «Создай xUnit-проект, перенеси `PerformanceServiceTests` с legacy на backend Infrastructure, подключи в `api-tests.yml`» |
| S0-04 | API-тест 403 permissions | `Api.Tests/PermissionsApiTests.cs` (новый) | «Добавь тест: observer без `Users` получает 403 на `GET /api/users`» |
| S0-05 | UI: gate nav Projects/References/Help | `AppLayout.tsx` | «Скрывай пункты меню по `usePermission('Projects'|'References'|'HelpPage')` как в WPF MainViewModel» |
| S0-06 | UI: gate ProjectDetail tabs | `ProjectDetailPage.tsx` | «Фильтруй вкладки по permissions: ProjectPhysicalVolumes, ProjectLaborIntensities, ProjectEquipments, ProjectDataUpload, Assessment, Planning, VersionComparison, GroupAnalysis» |
| S0-07 | Убрать stub ImportExcel проектов | `ProjectExcelDataImportService.Api.cs`, `ProjectsController` | «Либо реализуй импорт списка проектов, либо верни 501 и убери кнопку импорта до готовности» |

---

## Спринт 1 — Функциональный паритет (2–3 недели)

| ID | Задача | Файлы | Промпт для Cursor |
|----|--------|-------|-------------------|
| S1-01 | Групповой анализ (реальный API) | `GroupAnalyticsService.cs`, `GroupAnalysisTab.tsx` | «Замени placeholder в `GroupAnalyticsService` на агрегацию по нескольким проектам по аналогии с WPF GroupAnalysisForm» |
| S1-02 | Групповой тренд | `GroupTrendTab.tsx`, controller | «Реализуй endpoint тренда и UI графика (recharts уже в зависимостях или MUI)» |
| S1-03 | Версия: edit + archive UI | `ProjectDetailPage.tsx`, `projectVersionsApi.ts` | «Добавь редактирование версии (description, direction, physicalVolumeTypeId) и archive/unarchive как в WPF» |
| S1-04 | Import wizard permissions | `ImportWizard.tsx`, `ProjectDetailPage.tsx` | «Оберни вкладку Импорт в `usePermission('ProjectDataUpload')`» |
| S1-05 | Excel import tests | `Api.Tests/ImportApiTests.cs`, `fixtures/*.xlsx` | «Добавь тесты импорта: валидный файл 200, неверная структура 400, несовпадение кода каталога → preview unmatched» |
| S1-06 | FluentValidation POC | `Application/Validators/`, `Program.cs` | «Добавь FluentValidation для `DirectoryWriteRequest` и `ProjectWriteRequest`, подключи auto-validation» |
| S1-07 | Контекстное меню проектных гридов | `ProjectDetailPage.tsx`, hooks | «Вынеси `useProjectDataContextMenu` с Edit/Delete/Copy по образцу WPF Fact detail grid (ISS-005)» |

---

## Спринт 2 — Тестирование >80% coverage (2 недели)

| ID | Задача | Файлы | Промпт для Cursor |
|----|--------|-------|-------------------|
| S2-01 | Testcontainers MS SQL | `Api.Tests`, `.github/workflows/api-tests.yml` | «Настрой Testcontainers.MsSql для стабильного CI без skip» |
| S2-02 | Import unit tests | `Infrastructure.Tests/ProjectExcelDataImportServiceTests.cs` | «Тесты нормализации EP-кода, labor type matching, equipment (Name, UnitId)» |
| S2-03 | IIC calculation edge cases | `Infrastructure.Tests/PerformanceServiceTests.cs` | «Граничные случаи: σ=0, пустые нормы, PlannedHours=0, иерархия PPV» |
| S2-04 | Vitest setup | `client/vitest.config.ts`, `usePermission.test.ts` | «Добавь Vitest, тесты `usePermission` и `useDictionaryStore`» |
| S2-05 | Playwright: import flow | `e2e/import-wizard.spec.ts` | «E2E: login → project → import tab → upload fixture xlsx → grid row visible» |
| S2-06 | Playwright: observer denied | `e2e/permissions.spec.ts` | «E2E: login observer → /settings/users → redirect или 403 message» |

---

## Спринт 3 — DX и Infra (1–2 недели)

| ID | Задача | Файлы | Промпт для Cursor |
|----|--------|-------|-------------------|
| S3-01 | Nginx SPNEGO prod profile | `nginx.conf.spnego.template`, `docker-compose.prod.yml` | «Добавь compose profile `spnego` с auth_gss, keytab mount, документация в `_install/DEPLOY.md`» |
| S3-02 | Разбить ProjectExcelDataImportService | `Infrastructure/Import/*` partial classes | «Декомпозируй на PhysicalVolumeImporter, LaborImporter, EquipmentImporter» |
| S3-03 | Унифицировать authorization | все Controllers | «Приведи Directories и Dictionaries к `[HasPermission]`; убери дубли `EnsureAccessAsync` где возможно» |
| S3-04 | AG Grid context menu hook | `hooks/useGridContextMenu.ts` | «Рефакторинг DataTable consumers: projects, directories, audit» |
| S3-05 | OpenAPI / Swagger dev-only | `Program.cs` | «Включи Swagger только в Development для DX» |

---

## Блок 4 — Каркас тестов (запланирован, не реализован)

### Backend (`EngineeringPerformance.Api.Tests` + новый `Infrastructure.Tests`)

```
Api.Tests/
  PermissionsApiTests.cs      # 403 без прав
  ImportApiTests.cs             # Excel upload + preview
Infrastructure.Tests/         # NEW project
  ProjectExcelDataImportServiceTests.cs
  PerformanceServiceTests.cs    # port from legacy
  EquipmentImportCatalogMatcherTests.cs
```

**Зависимости:** xUnit, Moq, FluentAssertions, Testcontainers.MsSql, ClosedXML (fixtures).

### Frontend

```
client/
  vitest.config.ts
  src/hooks/usePermission.test.ts
  src/store/useDictionaryStore.test.ts
  e2e/import-wizard.spec.ts
  e2e/permissions.spec.ts
```

**Зависимости:** vitest, @testing-library/react, Playwright (уже есть).

---

## Блок 5 — Infra checklist (статус)

| Item | Статус | Действие |
|------|--------|----------|
| backend/Dockerfile multi-stage + non-root | ✅ | — |
| frontend/Dockerfile Node → nginx | ✅ | — |
| nginx security headers | ✅ | — |
| auth_gss SPNEGO | ❌ | S3-01 |
| docker-compose secrets | ✅ | — |
| .env в .gitignore | ✅ | — |
| tunnel DNS `web` collision | ✅ fixed | Документировать `ep-web` в Cloudflare |

---

## Метрики готовности к cutover

- [ ] Все S0 задачи закрыты
- [ ] Playwright green на CI
- [ ] Backend tests без silent skip
- [ ] UAT checklist `_doc/plans/uat-phase4-checklist.md` подписан
- [ ] WPF остаётся fallback до sign-off
