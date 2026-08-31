# Нормы ИИС

Дата обновления: 2026-08-31.  
SoT: [`RegistersController`](../../backend/EngineeringPerformance.Api/Controllers/RegistersController.cs) `/api/registers/iic-work-direction-norms`.  
Алиас: [`IicNormsController`](../../backend/EngineeringPerformance.Api/Controllers/IicNormsController.cs) `/api/iic-norms` (тот же `IIicNormService`).

Право: `IicWorkDirectionNorms`.

| Метод | Назначение |
|-------|------------|
| GET список | `asOf` — срез на дату; `workDirectionId` — история направления |
| GET `{id}` | Одна норма |
| POST / PUT / DELETE | CRUD |
| export | Excel |

Тело записи [`IicNormWriteRequest`](../../backend/EngineeringPerformance.Application/Registers/IicNormDtos.cs): `workDirectionId`, `performance`, `estimate`, `standardDeviation`, `unitId`, `effectiveFrom`, `effectiveTo`, `normFormula`, `deviationFormula`, `history`.

`FormulaType.KimCurve = 12` («Кривая Кима»): `N = P`, `Estimate = ln(P)`, `StandardDeviation = σ` в лог-пространстве, `DeviationModelType.Lognormal = 5`. Валидация P > 0, σ > 0. Линейные нормы по-прежнему пишут `StandardDeviation = σ_rel · P`.

Имена групп в ответах — формат `код — наименование (ед.)`.

CRUD нормы вызывает `AfterNormChangedAsync` только для версий, чья дата попадает в период **этой** нормы. Массовый `RecalculateAllVersions` при добавлении типа формулы **не** выполняется.
