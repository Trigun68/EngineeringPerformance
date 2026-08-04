# Business Logic Inventory — Engineering Performance (WPF)

> **⚠️ УСТАРЕЛО (2026-07-14):** документ описывает **замороженный** WPF-стек. Актуальная бизнес-логика — [_doc/architecture/business-logic.md](../_doc/architecture/business-logic.md). WPF вне scope разработки.

**Дата:** 2026-06-08  
**Версия:** 1.0  
**Связанные документы:** [ui_inventory.md](./ui_inventory.md), [equipment-module.md](../plans/equipment-module.md)  
**Цель:** инвентаризация бизнес-логики для миграции WPF → ASP.NET Core 8 + React/TypeScript

---

## ⚠️ Источник истины о граничных условиях

Папка **`.src` в репозитории отсутствует**. В промпте указан `.src` как эталон; фактически граничные условия и контракты зафиксированы в:

| Заявленный путь `.src` | Фактический путь в репозитории |
|------------------------|--------------------------------|
| `.src/Tests/*.cs` | `EngineeringPerformance.Tests/*.cs` (23 файла) |
| `.src/Fixtures/*.xlsx` | Не версионируются; образец оборудования упомянут в `docs/plans/equipment-module.md` (`.src/Оборудование.xlsx`) |
| E2E-сценарии | `client/e2e/` (web-миграция) |

**Два кодовых базиса:** WPF использует `EngineeringPerformance.Infrastructure/Services/`; web-API — `backend/EngineeringPerformance.Infrastructure/`. Алгоритмы дублированы (Performance, Import); регрессионные тесты CI — на legacy-проекте `EngineeringPerformance.Tests`.

---

## Карта слоёв бизнес-логики

```
ViewModels (62) ──► Core.Interfaces (IDataImportService, IPerformanceService, …)
                         │
                         ▼
              EngineeringPerformance.Infrastructure/Services/
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
 PerformanceService  DataImportService  ProjectVersionGridDataService
 ProjectForecastService  AccessControlService  EquipmentImportCatalogMatcher
```

**Общий паттерн для 9 гридов `ProjectsPageView`:** один ViewModel (`ProjectsPageViewModel`) + два сервиса данных:
- `IPerformanceService.GetPerformanceDataAsync` — иерархия IIC, Z-score, агрегация объёмов/часов;
- `ProjectVersionGridDataService.LoadAsync` — сырые PPV/PLI/Equipment + снимки норм.

Отдельных «9 сервисов под 9 гридов» нет — логика формирования коллекций живёт в VM.

---

## 9 гридов ProjectsPageView — формирование данных

| # | x:Name | Binding | Метод загрузки | Источник данных |
|---|--------|---------|----------------|-----------------|
| 1 | `ForecastGrid` | `ForecastData` | `LoadForecastDataAsync` | `PerformanceService` + VM: `BuildSubtree`, часы = ФО × норма ± 0.5σ |
| 2 | `FacilityForecastGrid` | `FacilityForecastData` | `LoadForecastDataAsync` | Группировка assessment PPV + equipment по `(FacilityCode, FacilityName)` |
| 3 | `DetailForecastGrid` | `DetailForecastData` | `LoadForecastDataAsync` | 1 строка БД → 1 строка; PPV + ProjectEquipment |
| 4 | `FactPerformanceGrid` | `FactPerformanceData` | `LoadFactDataAsync` | Прямая проекция `PerformanceRowDto` → `PerformanceItem` |
| 5 | `FactFacilityGrid` | `FactFacilityData` | `LoadFactDataAsync` | `ComputeFactRows` → агрегация по `FacilityCode` |
| 6 | `FactDetailGrid` | `FactDetailData` | `LoadFactDataAsync` | `ComputeFactRows` → пропорциональное распределение LI по DocSet |
| 7 | `CompareGrid` | `CompareRows` | `LoadCompareDataAsync` | Два вызова `GetPerformanceDataAsync`; merge по `WorkDirectionId` |
| 8 | `CompareFacilityGrid` | `CompareFacilityData` | `LoadCompareDataAsync` | `BuildCompareFacilityRows(facilityA, facilityB)` |
| 9 | `CompareDetailGrid` | `CompareDetailData` | `LoadCompareDataAsync` | `BuildCompareDetailRows`; ключ `(FacilityCode, DocumentSet, PvCode, WorkId)` |

### Общие формулы прогноза (гриды 1–3)

```csharp
// Базовый / Оптимистичный / Пессимистичный (ProjectsPageViewModel.CalcHoursFromRow)
if (vol <= 0 || !row.NormProductivity.HasValue) return (null, null, null);
var p = row.NormProductivity.Value;
var std = row.NormStdDev ?? 0m;
baseH = Round(vol * p, 2);
optH  = std > 0 ? Round(vol * Max(0, p - 0.5m * std), 2) : baseH;
pesH  = std > 0 ? Round(vol * (p + 0.5m * std), 2) : baseH;
```

**Единица объёма:** `Quantity × Coefficient` (PPV) или `QuantityInIicGroupUnits()` (equipment).

### Фильтр «только различия» (гриды 7–9)

`ApplyCompareFilter` → `FilterCompareRows/FilterCompareFacility/FilterCompareDetail`: строка видима, если `ZScoreA ≠ ZScoreB` (округление 4 знака) или различается интерпретация; для TreeList поднимаются предки видимых листьев.

---

## GetPerformanceDataAsync

### Класс: `PerformanceService` (`EngineeringPerformance.Infrastructure/Services/PerformanceService.cs`)
### Назначение: агрегированная оценка факта по иерархии направлений IIC для версии проекта

### Входные параметры и выходные данные:
- **Вход:** `int projectId`, `int versionId`
- **Выход:** `IReadOnlyList<PerformanceRowDto>` + синтетическая строка `(пусто)` с `WorkDirectionId = -1`

### БИЗНЕС-ПРАВИЛА и РАЗВИЛКИ:

```csharp
// Параллельная загрузка: workDirections, versionExists, PPV, equipment, PLI, snapshots
var volumeByDirection = PPV
    .Where(pv => pv.PhysicalVolume.ClassifierId.HasValue && pv.PhysicalVolume.IsAssessment)
    .GroupBy(ClassifierId).Sum(QuantityInIicGroupUnits);
EquipmentVolumeAggregator.MergeInto(volumeByDirection, equipmentRows);

// ISS-006 / решение 2026-05-12: только PlannedHours, НЕ ActualHours
var hoursByDirection = laborIntensities
    .Where(li => li.WorkDirectionId.HasValue)
    .GroupBy(WorkDirectionId).Sum(PlannedHours);

// Нормы: ключ = WorkDirectionId из AnalysisVersionSnapshot, не из Norm
var normByDirection = snapshots.GroupBy(WorkDirectionId).First().Norm;

// Рекурсия BuildHierarchy (pre-order):
if (!visited.Add(direction.Id)) return (0, 0); // защита от циклов в БД
factProductivity = factVolume > 0 ? factHours / factVolume : null;
ownFactEstimate = factProductivity > 0 ? ln(factProductivity) : null;
// Родитель: средне-взвешенная FactEstimate по часам поддерева
aggregatedFactEstimate = subtreeEstimateHours > 0
    ? subtreeWeightedEstimate / subtreeEstimateHours
    : ownFactEstimate;
zScore = (aggregatedFactEstimate - normEstimate) / normStdDev  // если stdDev > 0
```

### ЗАВИСИМОСТИ:
- БД: `IicWorkDirections`, `ProjectPhysicalVolumes`, `ProjectEquipments`, `ProjectLaborIntensities`, `AnalysisVersionSnapshots`
- Хелперы: `EquipmentVolumeAggregator`, `PerformanceInterpretationHelper`, `IicWorkDirectionDisplayUnit`

### ГРАНИЧНЫЕ УСЛОВИЯ (из `EngineeringPerformance.Tests`):

| Условие | Поведение |
|---------|-----------|
| Пустая in-memory БД | Одна строка `(пусто)`, не пустая коллекция |
| Родитель без собственных часов | `FactEstimate` = взвешенное среднее детей по часам |
| Две нормы с одним `WorkDirectionId` в каталоге | Не падает; берётся норма из снимка версии |
| Цикл в иерархии IIC | Узел пропускается (`visited`), без StackOverflow |
| Equipment без ClassifierId / не assessment | Не входит в `volumeByDirection` |
| LI с `WorkDirectionId = null` | Не входит в `hoursByDirection` |

### ССЫЛКА НА ТЕСТЫ:
- Файл: `EngineeringPerformance.Tests/PerformanceServiceTests.cs`
- Методы: `GetPerformanceData_EmptyDatabase_ReturnsPlaceholderOnly`, `GetPerformanceData_ParentWithoutOwnFact_AggregatesWeightedEstimateFromChildren`, `GetPerformanceData_ParentZScore_ComputedFromAggregatedFactEstimate`, `GetPerformanceData_DuplicateNormWorkDirectionIdsInCatalog_DoesNotThrow`
- Файл: `EngineeringPerformance.Tests/EquipmentVolumeAggregatorTests.cs`

---

## ComputeFactRows (вкладки Факт: детально + по объектам)

### Класс: `ProjectsPageViewModel` (private static)
### Назначение: распределение трудоёмкости по строкам PPV и агрегация Z-score по объектам

### Входные параметры и выходные данные:
- **Вход:** `physicalVolumesAll`, `equipmentAll`, `laborIntensities` (DocSet + Hours), `normByDirection`, `snapshotNormIdByDirection`, `classifierMap`
- **Выход:** `(List<FactDetailRow>, List<FactFacilityRow>)`

### БИЗНЕС-ПРАВИЛА:

```csharp
// Связка LI ↔ PPV: хвост DocumentationCode после '|' == PV.DocumentSet
hoursByDocSetDir = LI.GroupBy((DocSet, WorkDirectionId)).Sum(Hours); // Hours = PlannedHours only

// Пропорция часов на PPV:
if (isAssessment && vol > 0 && DocumentSet not empty) {
    factHours = Round(setHours * (vol / setVol), 2);
    factProductivity = Round(setHours / setVol, 4);
    zScore = (ln(factProductivity) - normEstimate) / normStdDev;
}

// Equipment на факте: только объём в гриде; FactLaborHours = null
// (не vol × normProductivity — это формула прогноза, не факт. трудоёмкость)

// Строка "(пусто)": totalLiHours - totalMatchedHours > 0
```

### ГРАНИЧНЫЕ УСЛОВИЯ:
- LI без `|` в `DocumentationCode` → DocSet пустой → часы в `(пусто)`
- DocSet есть в LI, но нет assessment PPV с тем же DocSet → часы в `(пусто)`
- Equipment: без LI часы не заполняются; оценка/Z по оборудованию на факте не считаются

### ССЫЛКА НА ТЕСТЫ:
- Косвенно: `PerformanceServiceTests` (источник часов PlannedHours), `DataImportServiceTests.MergeLaborRowsByNaturalKey_*`

---

## ImportFromFileAsync / DataImportDialog

### Класс: `DataImportService` + `DataImportDialogViewModel`
### Назначение: импорт Excel (ФО / труд / оборудование) с полной заменой данных версии

### Входные параметры:
- **Вход:** `projectId`, `versionId`, `DataImportType`, `filePath`, `description`, `addMissingLaborToCatalog` (только Labor)
- **Выход:** `DataImportResult(Success, RecordsImported, ErrorMessage, IterationId, SuccessDetails)`

### РАЗВИЛКА по типу (VM + Service):

```csharp
// DataImportDialogViewModel.ImportAsync
if (SelectedFilePath empty) → ErrorMessage = "Выберите файл"; return;
if (DataType == LaborIntensity && preview.UnmatchedDocumentTypes.Any()) {
    MessageBox YesNoCancel → addMissingToCatalog / skip / cancel import
}
ImportResult = await _importService.ImportFromFileAsync(...);

// DataImportService.ImportFromFileAsync
if (!File.Exists(filePath)) → Success=false, RecordsImported=0
switch (dataType) {
    PhysicalVolume → ImportPhysicalVolumesAsync  // REPLACE mode
    LaborIntensity → ImportLaborIntensitiesAsync
    Equipment      → ImportEquipmentAsync
}
```

### ImportPhysicalVolumesAsync

```csharp
await ClearPhysicalVolumeDataForVersionAsync(); // полная замена, без merge
if (!TryResolvePhysicalVolumeImportLayout) → Success=true, 0 rows + пояснение
foreach row:
    candidates = EnumerateCandidateEpCodesFromRow(); // regex ЕП / КФО2
    if (candidates empty) → skip, log row number
    physicalVolume = TryResolvePhysicalVolume(lookup, candidate); // OrdinalIgnoreCase по Code
    if (null) → skip not in catalog
    // Каждая строка файла = отдельная запись БД (без сворачивания, ISS-007)
await BackfillPhysicalVolumeClassifiersFromAncestorsAsync();
```

**Сравнение кодов ФО:** `StringComparer.OrdinalIgnoreCase` в `BuildCodeLookup`. При дубликатах Code в каталоге — `PickBestPhysicalVolumeCandidate` (ClassifierId → длина Code → VolumeTypeId → Id).

### ImportEquipmentAsync

```csharp
await ClearEquipmentDataForVersionAsync();
if (!TryResolveEquipmentImportLayout) → 0 rows, "нет шапки «Наименование по каталогу»"
catalogLookup = EquipmentImportCatalogMatcher.BuildCatalogLookup(catalogRows);
foreach row:
    catalogName = trim(cell CatalogName); if empty → skip
    unitId = EquipmentUnitNameMatcher.ResolveUnitId(rawUnit, units); // NormalizeUnitToken: trim, trimEnd('.'), ToLowerInvariant
    resolve = EquipmentImportCatalogMatcher.ResolveOrCreate(catalogName, unitId, ...);
    if (SkippedDuplicate) → skip + warning
    if (Created) → pending catalog create (ClassifierId=null, IsAssessment=false, Coefficient=1)
```

**«Посимвольное сравнение»:** CheckBox в UI **отсутствует**. Для оборудования ключ `(Name, UnitId)` — **точное** сравнение имени (Ordinal, без нормализации регистра); единица — через `NormalizeUnitToken`. Документировано в `docs/plans/equipment-module.md` п.1.

### ImportLaborIntensitiesAsync (ключевые правила)

```csharp
// Preview: knownDocTypes — OrdinalIgnoreCase
// Merge: MergeLaborRowsByNaturalKey (DocumentationCode, WorkDirectionId) → sum Planned+Actual
// WorkDirectionId == null → unmatched list (не схлопываются)
// addMissingLaborToCatalog → создать LaborIntensity для неизвестных DocumentTypeName
```

### ГРАНИЧНЫЕ УСЛОВИЯ:

| Сценарий | Поведение (тест / код) |
|----------|------------------------|
| Файл не найден | `Success=false`, `ErrorMessage` not empty |
| Пустой лист / нет layout | `Success=true`, `RecordsImported=0`, текст в `SuccessDetails` |
| Дубликаты LI по ключу | Суммирование часов (ISS-006) |
| LI без WorkDirectionId | В `unmatched`, не в merge |
| Дубликат (Name+UnitId) в каталоге equipment | Строка импорта пропускается |
| То же имя, другая ед. изм. | Создаётся отдельная номенклатура |
| DeleteIteration на InMemory | `InvalidOperationException` (транзакции) |

### ССЫЛКА НА ТЕСТЫ:
- `EngineeringPerformance.Tests/DataImportServiceTests.cs` — `ImportFromFile_FileNotFound_*`, `MergeLaborRowsByNaturalKey_*`, `DeleteIteration_*`
- `EngineeringPerformance.Tests/EquipmentImportCatalogMatcherTests.cs` — все методы
- `EquipmentUnitNameMatcherTests` в том же файле

---

## EquipmentImportCatalogMatcher.ResolveOrCreate

### Класс: `EquipmentImportCatalogMatcher` (static)
### Назначение: сопоставление строки Excel со справочником оборудования

### БИЗНЕС-ПРАВИЛА:

```csharp
key = new EquipmentCatalogKey(catalogName, unitId); // имя как в Excel (trim), без ToLower

if (ambiguousKeys.Contains(key))  // >1 строка каталога с тем же ключом
    return SkippedDuplicate=true;

if (lookup.TryGetValue(key, out existing))
    return existing, Created=false;

// Автосоздание:
new Equipment { Name, UnitId, Coefficient=1, IsAssessment=false, ClassifierId=null, IsArchived=false }
```

### ССЫЛКА НА ТЕСТЫ:
- `ResolveOrCreate_ExistingNameAndUnit_ReturnsSameEquipment`
- `ResolveOrCreate_SameNameDifferentUnit_CreatesSeparateItems`
- `ResolveOrCreate_AmbiguousCatalogKey_SkipsDuplicate`
- `NormalizeUnitToken_MatchesWithAndWithoutDot`

---

## MergeLaborRowsByNaturalKey

### Класс: `DataImportService` (public static)
### Назначение: схлопывание строк трудоёмкости перед записью в БД

```csharp
key = (DocumentationCode, WorkDirectionId);
if (WorkDirectionId is int) → merge/sum PlannedHours + ActualHours
else → unmatched.Add(entity);
```

### ССЫЛКА НА ТЕСТЫ:
- `MergeLaborRowsByNaturalKey_DuplicateKey_SumsPlannedAndActualHours` → 24.75 / 5.0
- `MergeLaborRowsByNaturalKey_DistinctKeys_KeepsRowsSeparate` → 3 merged + 2 unmatched

---

## GenerateForecastAsync

### Класс: `ProjectForecastService`
### Назначение: отчёт прогноза по сценариям (Базовый 20% / Оптимистичный 15% / Пессимистичный 15%)

```csharp
// Сценарии: vol × performance, vol × (p ± 0.5×σ)
// version not found → InvalidOperationException
// пустые данные → не падает, пустые сценарии
```

### ССЫЛКА НА ТЕСТЫ:
- `ProjectForecastServiceTests.GenerateForecast_ScenarioHours_FollowNormEstimateWithHalfStdDeviation`
- `GenerateForecast_VersionNotFound_Throws`

---

## AccessControlService.HasAccess

### Класс: `AccessControlService`
### Назначение: RBAC по объектам UI (`Users`, `Projects`, `Assessment`, …)

```csharp
if (user.Role == Administrator) return true;
return GetEffectiveAccessLevel(user, objectName) >= requiredLevel;
// Read < Edit; кэш прав по RoleId
```

### ССЫЛКА НА ТЕСТЫ:
- `AccessControlServiceTests.Administrator_HasFullAccess_ToAllObjects`
- `Manager_CanEdit_WhenAccessRightExists`
- `Observer_CanOnlyRead_WhenAccessRightIsRead`

---

## ProjectVisibilityService.CanUserAccessProjectAsync

### Назначение: доступ к конкретному проекту (модель `ProjectPermission`)

```csharp
if (Administrator) → true
else → exists ProjectPermission for (userId, projectId)
// «Нет записи → нет доступа»
```

### ССЫЛКА НА ТЕСТЫ:
- `ProjectVisibilityServiceTests` (V2-C)

---

## ComparisonAccuracy.FromValuePercent

### Класс: `ComparisonAccuracy` (`EngineeringPerformance.Core`)
### Назначение: категория точности сравнения версий по % отклонения

| % | Категория |
|---|-----------|
| null | NotApplicable |
| &lt; 7 | VeryHigh |
| 7–8.99 | High |
| 9–10.99 | Standard |
| ≥ 11 | Reduced |
| отрицательное | VeryHigh (не валидируется) |

### ССЫЛКА НА ТЕСТЫ:
- `ComparisonAccuracyTests.FromValuePercent_BoundaryValues_ReturnsExpectedCategory`

---

## Конвертеры WPF → React (12 классов в 5 файлах)

| Класс | Файл | Назначение | Миграция на фронт |
|-------|------|------------|-------------------|
| `NullToCollapsedConverter` | NullConverters.cs | null → hidden | `value == null ? 'none' : 'block'` |
| `NullToBoolConverter` | NullConverters.cs | null → false | `!!value` |
| `BoolToVisibilityConverter` | NullConverters.cs | true → visible | `display: value ? 'block' : 'none'` |
| `AnalysisDirectionToStringConverter` | NullConverters.cs | Plan/Fact → «План»/«Факт» | i18n map enum |
| `BoolToFontStyleConverter` | ForecastConverters.cs | IsTotal → italic | `fontStyle: isTotal ? 'italic' : 'normal'` |
| `BoolToTotalBackgroundConverter` | ForecastConverters.cs | IsTotal → `#EBF5FB` | conditional row class |
| `BoolToFontWeightConverter` | AccessRightsConverters.cs | true → Bold | `fontWeight` |
| `IndentToMarginConverter` | AccessRightsConverters.cs | level × 16px margin | `paddingLeft: level * 16` |
| `InterpretationToBrushConverter` | InterpretationToBrushConverter.cs | текст интерпретации → цвет (#27AE60 / #F39C12 / #E74C3C) | `getInterpretationColor(text)` — substring match ru/en |
| `InverseBooleanToVisibilityConverter` | InverseBooleanToVisibilityConverter.cs | false → visible | invert bool visibility |

**Примечание:** в ui_inventory указано «9 конвертеров» — фактически **12 классов** в 5 файлах (часть файлов содержит по 2–4 класса).

---

## Индекс ключевых ViewModels (бизнес-ориентированный)

| ViewModel | Основная логика |
|-----------|-----------------|
| `ProjectsPageViewModel` | 9 гридов, импорт, сравнение, контекстные меню, 40+ команд |
| `DataImportDialogViewModel` | Валидация файла, preview labor, логирование `UserActionLog` |
| `IterationDeleteDialogViewModel` | Удаление итерации импорта по типу |
| `AssessmentFormViewModel` / `PlanningFormViewModel` | Обёртки над `PerformanceService` / прогноз |
| `ProjectForecastReportViewModel` | `ProjectForecastService.GenerateForecastAsync` |
| `ProjectVersionAnalysisViewModel` | Анализ версий, экспорт |
| `LoginViewModel` | `AuthenticationService`, хеш пароля |
| `MainViewModel` | Навигация + `AccessControlService` для видимости разделов |
| `ReferencesPageViewModel` | Справочники через `*DirectoryService` |
| `EquipmentsViewModel` / `PhysicalVolumesViewModel` | CRUD справочников, tree parent validation |

Полный перечень 62 VM — в [ui_inventory.md](./ui_inventory.md).

---

## Индекс сервисов Infrastructure (бизнес-логика)

| Сервис | Ответственность |
|--------|-----------------|
| `PerformanceService` | ИИС-иерархия, Z-score, ln(productivity) |
| `DataImportService` | Excel import PV/LI/Equipment |
| `ProjectVersionGridDataService` | Пакетная загрузка для гридов |
| `ProjectForecastService` | Сценарии прогноза |
| `ProjectForecastExportService` | Excel export прогноза |
| `ProjectVersionAnalysisService` | Сводные таблицы анализа версии |
| `ProjectAnalysisService` | Отчёты анализа проекта |
| `ProjectEquipmentService` / `ProjectPhysicalVolumeService` | CRUD проектных данных |
| `AccessControlService` | Глобальные права по ролям |
| `ProjectVisibilityService` | Права на проект |
| `AuthenticationService` | Логин, BCrypt |
| `EquipmentImportCatalogMatcher` | Ключи каталога оборудования |
| `EquipmentVolumeAggregator` | Объёмы equipment в PerformanceService |
| `ForecastPlannedVolumePersistenceService` | Сохранение плановых объёмов |
| `*DirectoryService` | CRUD справочников с кэшем |

---

## Полный реестр тестов `EngineeringPerformance.Tests` (замена `.src`)

| Файл | Покрываемая логика |
|------|-------------------|
| `PerformanceServiceTests` | Иерархия, Z-score, кэш IIC, дубликаты норм |
| `DataImportServiceTests` | File not found, merge LI, транзакции |
| `EquipmentImportCatalogMatcherTests` | Ключи оборудования, unit dot |
| `EquipmentVolumeAggregatorTests` | Assessment + ClassifierId |
| `ProjectForecastServiceTests` | Сценарии ±0.5σ |
| `ComparisonAccuracyTests` | Границы % точности |
| `AccessControlServiceTests` | Admin/Manager/Observer |
| `ProjectVisibilityServiceTests` | ProjectPermission |
| `ProjectVersionAnalysisServiceTests` | Тоталы анализа |
| `ProjectVersionAnalysisTotalsCalculatorTests` | Калькулятор итогов |
| `DetailForecastSummaryTableBuilderTests` | Сводная таблица прогноза |
| `AnalysisVersionSnapshotTests` | Снимки норм |
| `ProjectAnalysisServiceTests` | Анализ проекта |
| `IicWorkDirectionDisplayUnitTests` | Ед. изм. IIC |
| `ProjectEquipmentIicUnitsTests` | Ед. изм. equipment |
| `AuthenticationServiceTests` | Аутентификация |
| `DirectoryServiceTests` / `DirectoryCacheTests` | Справочники |
| `UserActionLogServiceTests` | Журнал действий |
| `ProjectCharacteristicSelectionServiceTests` | Характеристики проекта |
| `DatabaseSeederTests` / `AppSettingsTests` | Инфраструктура |
| `ConnectionStringMaskingTests` | Маскирование строк подключения |

---

## GAP для миграции (из бизнес-логики)

1. **Два PerformanceService / DataImportService** — parity-тесты backend vs legacy не в CI.
2. **`ProjectVersionService.CopyAsync`** — копия метаданных без PPV/PLI/Equipment (не покрыто тестами VM).
3. **Импорт web** — `ProjectExcelDataImportService` (backend) монолит; поведение должно совпасть с `DataImportService`.
4. **Ordinal equipment** — в web проверить `EquipmentImportCatalogMatcher` в `backend/.../Import/`.
5. **Group analytics** — placeholder в web API; WPF `GroupAnalysisForm` имеет полную логику в legacy.

---

*Документ сформирован по коду WPF (`EngineeringPerformance.App`, `EngineeringPerformance.Infrastructure`) с перекрёстной проверкой `EngineeringPerformance.Tests` вместо отсутствующей папки `.src`.*
