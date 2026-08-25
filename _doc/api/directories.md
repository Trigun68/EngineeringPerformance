# Справочники (directories)

Дата обновления: 2026-08-24.  
Код: [`DirectoriesController`](../../backend/EngineeringPerformance.Api/Controllers/DirectoriesController.cs), [`DirectoryResourceCatalog`](../../backend/EngineeringPerformance.Application/Directories/DirectoryResourceCatalog.cs).

База: `/api/directories/{slug}`. Право берётся из каталога slug → `PermissionType` (`[DirectoryPermission]`).

| Slug | Право | Иерархия |
|------|-------|----------|
| `physical-volumes-kfo` | PhysicalVolumes | да |
| `physical-volumes-ber` | PhysicalVolumes | да |
| `equipment` | Equipment | да |
| `labor-intensities` | LaborIntensities | да |
| `iic-work-directions` | IicWorkDirections | да |
| `units-of-measurement` | UnitsOfMeasurement | нет |
| `project-characteristics` | ProjectCharacteristics | нет |

| Метод | Путь |
|-------|------|
| GET | `{slug}` список (`includeArchived`, `page`, `pageSize`, `projectId`) |
| GET | `{slug}/tree` |
| GET | `{slug}/children?parentId=` |
| GET | `{slug}/{id}` |
| POST | `{slug}` |
| PUT | `{slug}/{id}` |
| PATCH | `{slug}/{id}/sort-order` |
| POST | `{slug}/{id}/archive` / `unarchive` |
| DELETE | `{slug}/{id}` |
| GET/POST | `{slug}/export.xlsx` / `export` |

Тело записи: `{ name, parentId, sortOrder, properties }`. Базовые поля не класть в `properties`. Ключи `properties` — camelCase полей сущности (см. ниже). Ответ элемента: те же ключи в `properties` плюс `id`, `name`, `parentId`, `sortOrder`, `isArchived`, даты.

Опции характеристик: `/api/characteristics/{characteristicId}/options` (CRUD).

## properties по slug

**physical-volumes-kfo:** `code`, `description`, `classifierId` (группа ИИС), `iicNormId` (зарезервировано; расчёт версии берёт снимок), `isAssessment`, `documentSet`, `coefficient`, `unitId`.

**physical-volumes-ber:** `code`, `description`, `classifierId`, `physicalVolumeKfoId`, `isAssessment`, `documentSet`, `coefficient`, `unitId`.

**equipment:** обязателен `projectId`. Также `description`, `classifierId`, `isAssessment`, `unitId`, `coefficient`. `catalogKeyHash` считается на сервере, не задавать вручную.

**labor-intensities:** `code`, `documentTypeName` (ключ связи с импортом трудоёмкости), `iicWorkDirectionId`, `techas`. Поля `normPpr` / `normOpr` / `normPd` / `normRd` в API не отдаются.

**iic-work-directions:** `code`, `description`, `unitId`. Отображаемое имя группы: `код — наименование (ед.)` (форматирует сервер/UI, не склеивать вручную).

**units-of-measurement:** `description`.

**project-characteristics:** опции — отдельный ресурс, не `properties`.
