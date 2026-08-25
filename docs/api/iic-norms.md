# Нормы ИИС

Дата обновления: 2026-08-24.  
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

Имена групп в ответах — формат `код — наименование (ед.)`.

Изменение норм **не пересчитывает** уже созданные версии (снимок). Пересчёт затронутых версий делает оркестратор после CRUD, где это предусмотрено кодом сервиса норм.
