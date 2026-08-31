# Версии анализа

Дата обновления: 2026-08-24.  
Код: [`ProjectVersionsController`](../../backend/EngineeringPerformance.Api/Controllers/ProjectVersionsController.cs).

Права: `ProjectAnalysisVersions`. Данные версии — `[RequireProjectAccess]`.

Направление (`AnalysisDirection`): `Plan = 1`, `Fact = 2`, `Forecast = 3`. Версия привязана к типу номенклатуры ФО (`physicalVolumeTypeId`: КФО или БЕР из `dictionaries/all.physicalVolumeTypes`).

При создании (и при `asOfDate` на update) пишется **снимок норм** `AnalysisVersionSnapshot`. Расчёты версии используют снимок, не живой справочник.

| Метод | Путь | Назначение |
|-------|------|------------|
| GET | `/api/projects/{id}/versions` | Список |
| GET | `…/versions/{vid}` | Одна |
| POST | `…/versions` | Создать |
| PUT | `…/versions/{vid}` | Изменить (в т.ч. `asOfDate` → переснимок) |
| DELETE | `…/versions/{vid}` | Архив |
| POST | `…/versions/{vid}/restore` | Разархив |
| POST | `…/versions/{vid}/set-primary` | Основная для Direction |
| POST | `…/versions/{vid}/copy` | Глубокая копия |
| GET\|PUT | `…/planning-parameters` | Z по направлениям ИИС |
| POST | `…/recalculate` | Пересчёт плановых часов |
| GET | `…/aggregated-labor-intensities` | Агрегация |
| GET | `…/versions/{vid}/readiness` | Полнота для интегратора |

## Create/update

[`ProjectVersionWriteRequest`](../../backend/EngineeringPerformance.Application/Projects/ProjectVersionDtos.cs): `versionName`, `description`, `direction`, `physicalVolumeTypeId`, опционально `name`, `planningScenario`, `zParameterLevel`, `asOfDate`, `isPrimary` (только create).

## Copy

[`CopyVersionRequest`](../../backend/EngineeringPerformance.Application/Projects/CopyVersionRequest.cs): `newVersionName`, `description`, `targetDirection`. Копируются ФО, оборудование, итерации импорта (то же направление), снимок норм, параметры планирования. **Трудоёмкость — только Fact→Fact.** Plan→Fact: ФО и оборудование без PLI.

## Planning parameters

PUT `{ items: [{ iicWorkDirectionId, zPessimistic, zBase, zOptimistic }] }`.

## Readiness

Счётчики блоков и `issues[]`:

- `missing_physical_volume_type`
- `missing_planning_parameters`
- `missing_physical_volumes`
- `missing_equipment`
- `missing_labor_intensities`
- `missing_norm_snapshots`

Severity: `warning` / `info`.
