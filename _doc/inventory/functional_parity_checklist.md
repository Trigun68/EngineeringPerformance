# Functional Parity Checklist — WPF → Web Migration

**Дата:** 2026-06-08  
**Версия:** 1.0  
**Связанные документы:** [ui_inventory.md](./ui_inventory.md), [business_logic_inventory.md](./business_logic_inventory.md), [data_access_inventory.md](./data_access_inventory.md), [permissions_inventory.md](./permissions_inventory.md)  
**Роли:** `Administrator`, `ProjectManager`, `Observer`  
**ACL:** глобальные `AccessRights` + построчные `ProjectPermissions`  
**Принцип:** паритет с WPF **+ исправление legacy GAP** (не слепое копирование)

---

## Легенда статусов (для исполнения спринтов)

| Маркер | Значение |
|--------|----------|
| `[ ]` | Не реализовано / не проверено |
| `[~]` | Частично (см. комментарий) |
| `[x]` | Готово + тест green |

**Типы тестов:** `xUnit` (backend), `Vitest` (frontend), `Playwright` (e2e).

---

## Модуль: Аутентификация и сессия

### 1. Аутентификация и права (ACL)

- [ ] **JWT + HttpOnly cookie:** Логин возвращает cookie `ep_auth_token`; logout очищает сессию.
  - Backend: `AuthController.Login` / `Logout`
  - Frontend: `useAuthStore.login`, axios `withCredentials: true`
  - Тест (xUnit): `AuthApiTests.Login_ValidCredentials_SetsCookieAndReturnsUser`
  - Тест (Playwright): `auth.spec.ts` → login admin → виден `AppLayout`

- [ ] **SPNEGO (опционально prod):** `X-Remote-User` + `X-Trusted-Proxy-Token` при `Spnego:Enabled`.
  - Backend: `AuthController.TryWindowsLogin`
  - Тест (xUnit): `AuthApiTests.Spngo_WithoutTrustedToken_Returns401`

- [ ] **Роли в JWT/профиле:** `roleName` ∈ {`Administrator`, `ProjectManager`, `Observer`}.
  - Тест (xUnit): `DatabaseSeederTests.SeedsThreeRoles_WithDemoUsers`

### 2. Глобальная матрица прав

- [ ] **Administrator bypass:** Admin обходит `AccessRights` на всех endpoint'ах.
  - Backend: `PermissionService.HasAccessAsync` + `PermissionAuthorizationHandler`
  - Тест (xUnit): `PermissionsApiTests.Administrator_GetUsers_Returns200`

- [ ] **`[HasPermission]` на защищённых контроллерах:** Все мутации и чувствительные GET используют атрибут.
  - Backend: `HasPermissionAttribute`, `PermissionRequirement`
  - Тест (xUnit): `PermissionsApiTests.Observer_GetUsers_Returns403`

- [ ] **Матрица прав UI (только Admin):** Редактирование `AccessRights` доступно только Administrator.
  - Backend: `AccessRightsController` → `Forbid` для non-admin
  - Frontend: `AccessRightsPage` + скрытие route для non-admin
  - Тест (Playwright): `permissions.spec.ts` → observer → `/settings/access-rights` → 403 или redirect

- [ ] **Frontend `usePermission`:** Зеркало WPF `AccessControlConstants` (уровни 0/1/2).
  - Frontend: `hooks/usePermission.ts`
  - Тест (Vitest): `usePermission.test.ts` → Admin always level 2; missing key → 0

### 3. Построчный доступ (ProjectPermissions)

- [ ] **`RequireProjectAccess(Read)`** на чтение данных версии.
  - Backend: `RequireProjectAccessAttribute`
  - Тест (xUnit): `ProjectAccessTests.UserWithoutPermission_GetVersions_Returns403`

- [ ] **`RequireProjectAccess(Edit)`** на импорт, копию, archive, CRUD проектных данных.
  - Тест (xUnit): `ProjectAccessTests.Observer_WithReadOnlyPermission_Import_Returns403`

- [ ] **GAP-fix: учёт `ProjectPermission.Level`:** Read ≠ Edit (WPF проверял только наличие записи).
  - Backend: `ProjectVisibilityService.CanAccessProjectAsync` — `permission.Level >= requiredLevel`
  - Legacy WPF: обновить `ProjectVisibilityService.CanUserAccessProjectAsync` (или deprecate)
  - Тест (xUnit): `ProjectVisibilityServiceTests.ReadPermission_CannotPassEditGate`

- [ ] **Administrator видит все проекты; остальные — только с `ProjectPermission`.**
  - Backend: `GetProjectsQuery` + `GetAccessibleProjectIdsAsync`
  - Тест (xUnit): `ProjectVisibilityServiceTests.GetAccessibleProjects_ReturnsOnlyPermitted_ForNonAdmin`

---

## Модуль: Навигация и оболочка UI

### 1. Списки и отображение (исправление WPF GAP)

- [ ] **Скрытие пунктов меню по ACL** (не только Dashboard/Audit/Settings).
  - Frontend: `AppLayout.tsx` — `usePermission('Projects'|'References'|'HelpPage'|'Dashboard')`
  - WPF эталон: `MainViewModel.AddIfAccessible`
  - Тест (Playwright): `permissions.spec.ts` → observer без `References` → пункт «Справочники» скрыт

- [ ] **Блокировка прямого URL без прав:** Route guard или redirect на 403-страницу.
  - Frontend: wrapper `RequirePermission` на routes
  - Тест (Playwright): `permissions.spec.ts` → deep link `/references/physical-volumes` без прав

- [ ] **GAP-fix: список проектов с `ProjectVisibility`.** WPF показывал все коды через `ProjectDirectoryService.GetAllAsync`.
  - Backend: `GET /api/projects` фильтрует по `GetAccessibleProjectIdsAsync`
  - Frontend: `ProjectsPage.tsx` (AG Grid) — только API-список
  - Тест (xUnit): `ProjectsApiTests.Observer_SeesOnlyVisibleProjects`
  - Тест (Playwright): `projects.spec.ts` → observer видит 1 проект из seed

### 2. Конвертеры WPF → React (12 классов)

- [ ] **`usePermission` / conditional render** вместо `AddIfAccessible` + `BoolToVisibilityConverter`.
- [ ] **`formatAnalysisDirection(direction)`** вместо `AnalysisDirectionToStringConverter` (`Plan` → «План»).
- [ ] **`getInterpretationColor(text)`** вместо `InterpretationToBrushConverter`.
- [ ] **`rowClassName` для итогов** вместо `BoolToTotalBackgroundConverter` / `BoolToFontStyleConverter`.
- [ ] **`paddingLeft: level * 16`** вместо `IndentToMarginConverter`.
- [ ] **`disabled={!selectedVersionId}`** вместо `NullToBoolConverter` на кнопках импорта.
  - Тест (Vitest): утилиты форматирования + `usePermission`
  - Тест (Playwright): import без версии → кнопка disabled

---

## Модуль: Управление проектами и версиями

### 1. CRUD проекта

- [ ] **Создание/редактирование проекта** (код, заказчик, ГИП, …).
  - Backend: `ProjectsController`
  - Frontend: `ProjectsPage.tsx` / диалог редактирования
  - Тест (xUnit): `ProjectsApiTests.Manager_CreateProject_Returns201`

- [ ] **Участники проекта (`ProjectPermissions`).**
  - Backend: `ProjectPermissionsController`
  - Frontend: вкладка участников на `ProjectDetailPage`
  - Тест (xUnit): `ProjectPermissionsApiTests.AddParticipant_Idempotent`

- [ ] **Архивация проекта** скрывает из списка non-admin.
  - Тест (xUnit): `ProjectsApiTests.ArchivedProject_NotInList`

### 2. Версии анализа

- [ ] **CRUD версий:** create, update metadata, archive (soft).
  - Backend: `ProjectVersionsController`
  - Frontend: `ProjectDetailPage` — UI edit/archive (GAP vs WPF picker)
  - Тест (xUnit): `ProjectVersionsApiTests.Create_PopulatesAnalysisVersionSnapshot`

- [ ] **Снимок норм при создании версии** (`AnalysisVersionSnapshot` на `CreatedAt`).
  - Backend: `AnalysisVersionSnapshotService.PopulateSnapshotAsync`
  - Тест (xUnit): `AnalysisVersionSnapshotTests.OneNormPerWorkDirectionPerVersion`

- [ ] **GAP-fix: копирование версии — проверка прав.** WPF `CopyVersionAsync` без `CanEdit`.
  - Backend: `POST .../copy` — `[HasPermission(ProjectAnalysisVersions, Edit)]` + `RequireProjectAccess(Edit)` ✅ уже есть
  - Frontend: `ProjectDetailPage` — `canEditVersions` + `requiredLevel: 2` на контекстном меню
  - Тест (xUnit): `ProjectVersionsApiTests.Observer_Copy_Returns403`
  - Тест (Playwright): observer не видит «Копировать» в grid версий

### 3. Deep Copy версии (исправление WPF GAP)

- [ ] **Копируются `ProjectPhysicalVolumes`** (все поля Facility/DocSet/WorkId).
  - Backend: `ProjectVersionCopyService` (новый или расширение `CopyAsync`)
  - Эталон: WPF `ProjectAnalysisVersionCopyService` шаг 2
  - Тест (xUnit): `CopyVersionTests.Copy_IncludesPpv_CountMatchesSource`

- [ ] **Копируются `ProjectLaborIntensities`** (только для `Direction == Fact`).
  - Тест (xUnit): `CopyVersionTests.Copy_FactVersion_IncludesLaborIntensities`

- [ ] **GAP-fix: копируются `ProjectEquipments`** (WPF legacy пропускал).
  - Тест (xUnit): `CopyVersionTests.Copy_IncludesEquipment_AssertCountsMatch`

- [ ] **Копируются `DataImportIterations`** (аудит 1-в-1: ImportedAt, SourceFileName, RecordCount).
  - Тест (xUnit): `CopyVersionTests.Copy_PreservesImportIterations`

- [ ] **Новый `AnalysisVersionSnapshot`** генерируется по нормам на `copy.CreatedAt` (не клон старого снимка).
  - Тест (xUnit): `CopyVersionTests.Copy_RegeneratesSnapshot_FromActiveNorms`

- [ ] **Транзакция:** copy атомарен — rollback при ошибке.
  - Тест (xUnit): `CopyVersionTests.Copy_Failure_RollsBack_NoOrphanVersion`

---

## Модуль: Девять гридов ProjectsPage (Прогноз / Факт / Сравнение)

### 1. Загрузка данных (производительность)

- [ ] **Один Scoped `DbContext` на HTTP-запрос** (не 5 параллельных как в legacy `PerformanceService`).
  - Backend: рефакторинг загрузки dashboard
  - Тест (xUnit): интеграционный тест без `IDbContextFactory` multiply на один endpoint

- [ ] **Единый batch-endpoint для вкладки** (опционально `GET .../dashboard?versionId=`).
  - Заменяет двойной вызов Performance + GridData
  - Тест (xUnit): `DashboardApiTests.GetForecast_ReturnsThreeCollections`

- [ ] **`.AsNoTracking()` на всех read-query** для гридов.
  - Тест (code review / analyzer): нет tracked entities в read handlers

### 2. Бизнес-логика и расчёты (`PerformanceCalculationEngine`)

- [ ] **Создан `IPerformanceCalculationEngine` / `PerformanceCalculationEngine`.** Вся математика вне контроллеров и React.
  - Перенос из: `PerformanceService.BuildHierarchy`, `ProjectsPageViewModel.ComputeFactRows`, формулы ±0.5σ
  - Запрет: дублирование Z-score / LI distribution в UI

- [ ] **Иерархия IIC, pre-order, строка `(пусто)`** при пустой БД.
  - Тест (xUnit): `PerformanceCalculationTests.EmptyDatabase_ReturnsPlaceholderRow` ← `PerformanceServiceTests`

- [ ] **Агрегация родителя: взвешенный `FactEstimate` по `PlannedHours` поддерева.**
  - Тест (xUnit): `PerformanceCalculationTests.Parent_AggregatesWeightedEstimateFromChildren`

- [ ] **Только `PlannedHours` в агрегации часов** (не `ActualHours`, ISS-006).
  - Тест (xUnit): `PerformanceCalculationTests.ActualHours_IgnoredInAggregation`

- [ ] **Объёмы: PPV assessment + `EquipmentVolumeAggregator.MergeInto`.**
  - Тест (xUnit): `PerformanceCalculationTests.Equipment_MergesIntoDirectionVolume`

- [ ] **Z-score: `NormEstimate`/`NormStdDev` из snapshot; parent Z из агрегированного FactEstimate.**
  - Тест (xUnit): `PerformanceCalculationTests.ParentZScore_FromAggregatedFactEstimate`

- [ ] **Распределение LI по DocSet** (хвост `DocumentationCode` после `|`).
  - Тест (xUnit): `PerformanceCalculationTests.Labor_DistributesProportionallyByDocSet`

- [ ] **Строка `(пусто)` для нераспределённых часов LI.**
  - Тест (xUnit): `PerformanceCalculationTests.UnmatchedLaborHours_GoesToEmptyRow`

- [ ] **Прогноз: Base / Optimistic / Pessimistic** = vol × (p ± 0.5σ).
  - Тест (xUnit): `PerformanceCalculationTests.ForecastScenarios_HalfStdDeviation` ← `ProjectForecastServiceTests`

### 3. UI — 9 гридов (AG Grid)

| # | WPF Grid | Web target | Чеклист |
|---|----------|------------|---------|
| 1 | `ForecastGrid` | Planning/Forecast tab tree | [ ] Колонки + итоги italic |
| 2 | `FacilityForecastGrid` | By facility aggregate | [ ] Group by FacilityCode |
| 3 | `DetailForecastGrid` | Detail forecast grid | [ ] Context menu Edit PV/Equipment |
| 4 | `FactPerformanceGrid` | Assessment tab hierarchy | [ ] Z-score, percentile, interpretation color |
| 5 | `FactFacilityGrid` | Fact by facility | [ ] Weighted Z per facility |
| 6 | `FactDetailGrid` | Fact detail | [ ] LI proportion columns |
| 7 | `CompareGrid` | Compare tab tree | [ ] ZDelta, filter «только различия» |
| 8 | `CompareFacilityGrid` | Compare facilities | [ ] HoursDelta |
| 9 | `CompareDetailGrid` | Compare detail | [ ] Key (FacilityCode, DocumentSet, PvCode, WorkId) |

- [ ] **Фильтр «только различия»** сохраняет иерархию Tree (поднимает предков).
  - Frontend: `CompareTab.tsx`
  - Тест (Playwright): `compare.spec.ts` → toggle filter → row count changes

- [ ] **Контекстные меню Detail** (WPF: Edit FO, nomenclature, norm EAC, delete, copy row).
  - Frontend: `useProjectDataContextMenu` hook
  - Тест (Playwright): `project-detail.spec.ts` → right-click → menu items visible when `canEdit`

- [ ] **Сохранение layout грида** (аналог `GridLayoutAutoSave`).
  - Frontend: `userSettingsApi` + AG Grid state
  - Тест (Playwright): resize column → reload → width persisted

---

## Модуль: Импорт Excel

### 1. Права и UI (GAP-fix)

- [ ] **`CanEdit(ProjectDataUpload)` перед открытием wizard.**
  - Frontend: `ImportWizard.tsx`, `ProjectDetailPage` — `usePermission('ProjectDataUpload').canEdit`
  - WPF GAP: кнопки только `NullToBool` на versionId
  - Тест (Playwright): `import.spec.ts` → observer → кнопка импорта disabled/hidden

- [ ] **Выбор версии обязателен** (`!selectedVersionId` → disabled).
  - Тест (Playwright): `import.spec.ts` → no version → cannot submit

- [ ] **API: Import защищён** `[HasPermission(ProjectDataUpload, Edit)]` + `RequireProjectAccess(Edit)`.
  - Тест (xUnit): `ImportApiTests.Observer_PostImport_Returns403`

### 2. Валидация файла

- [ ] **`ExcelUploadValidator`:** extension, MIME, PK zip magic, max 50 MB.
  - Тест (xUnit): `ImportApiTests.InvalidMime_Returns400`

### 3. Три типа импорта

- [ ] **Physical Volumes:** replace strategy, EP-code normalization, skip без кода → details.
  - Тест (xUnit): `ImportApiTests.Pv_FileNotFound_ReturnsFailure`
  - Тест (xUnit): `ImportApiTests.Pv_ValidFile_RecordsImported` (fixture xlsx)

- [ ] **Labor:** preview unmatched doc types; optional `addMissingLaborToCatalog`.
  - Тест (xUnit): `ImportApiTests.Labor_Preview_ReturnsUnmatchedTypes`
  - Тест (xUnit): `MergeLaborRowsByNaturalKey` contract в `Infrastructure.Tests` (даже если не в runtime merge)

- [ ] **Equipment:** `EquipmentImportCatalogMatcher` — Ordinal name + normalized unit; skip ambiguous catalog key.
  - Тест (xUnit): `EquipmentImportTests.AmbiguousCatalogKey_SkipsRow_StillCommits` ← replace semantics
  - Тест (xUnit): `EquipmentImportTests.NormalizeUnitToken_DotInsensitive`

### 4. Стратегия Replace и транзакции

- [ ] **Replace:** clear version data + insert new в одной транзакции.
  - Тест (xUnit): `ImportApiTests.SecondImport_ReplacesFirst_IterationCount`

- [ ] **Skip duplicate (equipment) НЕ откатывает транзакцию** — partial commit с summary (500 skip + 500 OK).
  - Тест (xUnit): `ImportTests.Import_WithDuplicates_ReplacesDataAndCommits`

- [ ] **Exception на SaveChanges → rollback** (данные версии восстановлены).
  - Тест (xUnit): `ImportTests.Import_SaveFailure_RollsBack` (mock/SQLite)

- [ ] **Delete iteration** по `CreatedAt == importedAt` в транзакции.
  - Тест (xUnit): `ImportApiTests.DeleteIteration_RemovesRecords` (real DB / Testcontainers)

### 5. Сервисный контракт `MergeLaborRowsByNaturalKey`

- [ ] **Вынесен в `LaborImportMergeService`** (готов к вызову, документирован).
  - Тест (xUnit): `MergeLaborRowsByNaturalKey_DuplicateKey_SumsHours` ← `DataImportServiceTests`
  - Примечание: WPF runtime импорт построчный; контракт для backend parity и будущего merge

---

## Модуль: Справочники (References)

- [ ] **8+ вкладок справочников** с tree CRUD (PV, Equipment, Labor, IIC, UoM, …).
  - Backend: `DirectoriesController` + `DirectoryFacade`
  - Frontend: `DirectoryPage.tsx`, `referenceConfig.ts`
  - Тест (xUnit): `DirectoriesApiTests.List_RequiresReadPermission`

- [ ] **Нормы IIC** — регистр с `EffectiveFrom`/`EffectiveTo`, фильтр на дату.
  - Frontend: `IicNormsPage.tsx`
  - Тест (xUnit): `IicNormsApiTests.GetEffectiveNorms_ForDate`

- [ ] **GAP-fix: `GET /api/dictionaries/all` с ACL** (сейчас только `[Authorize]`).
  - Backend: `[HasPermission(References, Read)]` или аналог
  - Тест (xUnit): `DictionariesApiTests.Observer_WithoutReferences_Returns403`

- [ ] **Excel export справочника.**
  - Тест (xUnit): `DirectoriesApiTests.Export_ReturnsXlsx`

- [ ] **Observer: Read-only** — нет кнопок Create/Edit/Archive.
  - Тест (Playwright): `references.spec.ts` → observer → toolbar hidden

---

## Модуль: Аналитика и отчёты

- [ ] **Оценка факта (Assessment tab)** — parity с WPF `AssessmentForm` / Fact grids.
  - Тест (xUnit): `VersionAnalysisApiTests.GetPerformance_ReturnsRows`

- [ ] **Планирование (Planning tab)** — редактирование плановых объёмов, dirty save.
  - Frontend: `PlanningTab.tsx`
  - Тест (Playwright): `planning.spec.ts` → edit cell → save

- [ ] **Сравнение версий** — dual version picker + 3 compare grids.
  - Тест (Playwright): `compare.spec.ts`

- [ ] **Прогноз PDF/Excel export.**
  - Backend: QuestPDF / ClosedXML `using` scope
  - Тест (xUnit): `ForecastExportApiTests.Export_ReturnsPdf`

- [ ] **Групповой анализ / тренд** — WPF `GroupAnalysisForm`, `GroupTrendForm` (сейчас placeholder в API).
  - Backend: реальный `GroupAnalyticsService`
  - Тест (xUnit): `GroupAnalyticsApiTests.MultiProject_AggregatesZScore`

---

## Модуль: Настройки, пользователи, аудит

- [ ] **CRUD пользователей** (Admin).
  - Тест (xUnit): `UsersApiTests.Manager_GetUsers_Returns403`

- [ ] **Журнал действий** (`UserActionLog`) — импорт, login, project mutations.
  - Frontend: `UserActionLogPage.tsx` + nav guard `UserActionLog`
  - Тест (xUnit): `AuditApiTests.Import_CreatesLogEntry`

- [ ] **Настройки пользователя** (layout гридов, interpretation thresholds).
  - Backend: `UserSettingsController`

---

## Модуль: Инфраструктура и качество

- [ ] **Порт `EngineeringPerformance.Tests` алгоритмов в backend CI.**
  - Проект: `EngineeringPerformance.Infrastructure.Tests` или расширение `Api.Tests`
  - CI: `.github/workflows/api-tests.yml` без skip при доступной БД

- [ ] **Testcontainers MS SQL** для стабильных integration-тестов.
  - Тест: `DeleteIteration`, `CopyVersion`, `Import` с транзакциями

- [ ] **Playwright smoke расширен:** import, permissions, compare filter.
  - `client/e2e/import-wizard.spec.ts`, `permissions.spec.ts`

- [ ] **Vitest:** `usePermission`, dictionary store, format utils.

- [ ] **Docker:** `ep-api`, `ep-web`, `ep-sqlserver`; nginx security headers.

---

## Спринт 0 — минимальный cut (критические исправления)

Приоритетные пункты чек-листа для первой итерации кода:

| ID | Пункты чек-листа выше |
|----|------------------------|
| S0-A | Deep Copy + Equipment + тест `CopyVersionTests.*` |
| S0-B | `ProjectVisibility` Level + список проектов + тесты 403 |
| S0-C | `dictionaries/all` ACL |
| S0-D | Import UI gate `ProjectDataUpload` + Playwright |
| S0-E | `PerformanceCalculationEngine` scaffold + порт 3 regression tests |
| S0-F | Один DbContext на dashboard read path |

---

## 📊 Итоговая сводка миграции

| Категория | Найдено в WPF | Исправлено/улучшено в новом стеке | Покрыто тестами |
|-----------|---------------|-----------------------------------|-----------------|
| Роли/ACL | 2 уровня (`AccessRights` + `ProjectPermissions`); GAP: WPF без Level, список всех проектов, импорт без Edit | 2 уровня + `RequireProjectAccess(Level)` + nav/tab guards + Admin-only matrix | `[ ]` → цель: xUnit + Playwright S0 |
| Расчёты | Размазаны по `PerformanceService` + `ProjectsPageViewModel` (~2500 строк VM) | Единый `PerformanceCalculationEngine`; UI только отображает DTO | `[ ]` → порт `PerformanceServiceTests` + новые edge cases |
| Импорт | Replace OK; UI без ACL; `MergeLabor` только в тестах | Права на UI+API; Replace сохранён; skip ≠ rollback; validator 50MB | `[ ]` → `ImportApiTests` + e2e |
| Копирование | PPV+LI+iterations; **без Equipment**; web — только шапка | **Full Deep Copy** включая Equipment; транзакция; права Edit | `[ ]` → `CopyVersionTests` |
| Производительность БД | 5 `DbContext` на `GetPerformanceDataAsync`; дубль PPV queries | 1 Scoped `DbContext`; batch read; `.AsNoTracking()` | `[ ]` → perf integration + code review |
| 9 гридов UI | DevExpress GridControl×9, контекстные меню, layout save | AG Grid×9, hooks, `DataTable`, user settings | `[~]` частично (Compare filter ✅) |
| Справочники | 6 TreeList + norms window | `DirectoryPage` + `IicNormsPage` | `[~]` API ✅; dictionaries ACL `[ ]` |
| Групповая аналитика | `GroupAnalysisForm`, `GroupTrendForm` | Placeholder → full API | `[ ]` Спринт 1 |
| Конвертеры (12) | WPF `IValueConverter` | React utils + `usePermission` | `[ ]` Vitest |
| Безопасность | UI-only checks на части экранов | API-first + UI mirror | `[~]` API в основном ✅; gaps в dictionaries/import UI |

**Оценка готовности web (на 2026-06-08):** ~65% функционального паритета; ~40% чек-листа закрыто частично; **0%** формально отмечено `[x]` в этом документе — документ является **планом приёмки**, не отчётом о завершении.

---

## Трассировка на инвентаризацию (5 файлов)

| Файл | Использование в чек-листе |
|------|---------------------------|
| `ui_inventory.md` | 9 гридов, 68 форм, конвертеры, контекстные меню |
| `business_logic_inventory.md` | Формулы, импорт, equipment ordinal, ComputeFactRows |
| `data_access_inventory.md` | Replace import, транзакции, 1 DbContext, Copy GAP |
| `permissions_inventory.md` | Роли, GAP-ы, copy/import ACL, dictionaries |
| `functional_parity_checklist.md` | **Этот файл** — атомарная приёмка |

---

*Чек-лист предназначен для Спринта 0+ из `_doc/development_roadmap.md`. Отмечайте `[x]` по мере реализации и green-тестов.*
