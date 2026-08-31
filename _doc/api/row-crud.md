# Построчный CRUD данных версии

Дата обновления: 2026-08-24.  
Код: [`ProjectDataController`](../../backend/EngineeringPerformance.Api/Controllers/ProjectDataController.cs).  
База: `/api/projects/{projectId}/versions/{versionId}/`.

Это точечные правки UI и простые интеграции. **Не дублирует Excel:** нет нормализации шифра, нет итерации импорта, нет replace пакета. Для массовой загрузки — [ingest-physical-volumes.md](ingest-physical-volumes.md) / оборудование / трудоёмкость.

GET — пагинация `page`, `pageSize` (1…1000), `sort`. После create/update/delete ФО и оборудования вызывается тот же пересчёт, что после Excel (`RecalculateVersionAsync`). Трудоёмкость — invalidate кэша аналитики.

| Ресурс | Право | POST/PUT тело (обязательное) |
|--------|-------|------------------------------|
| `physical-volumes` | ProjectPhysicalVolumes | `physicalVolumeId`, `quantity`; опционально объект, работа, часы/ед., поля Excel (bindingType, documentSet, tag, workId, …) |
| `labor-intensities` | ProjectLaborIntensities | `plannedHours`, `actualHours`; опционально `laborIntensityId`, `documentationCode`, `brand`, `notes` |
| `equipment` | ProjectEquipments | `equipmentId`, `quantity`; опционально `quantityInWork` и поля Excel (facility, tag, …) |

`physicalVolumeId` — Id номенклатуры **того** каталога, который выбран у версии (КФО или БЕР). Иначе 400.

Уникальность строки ФО: номенклатура × `facilityCode` × `facilityName` × `workId` ([ADR-003](../adr/ADR-003-project-physical-volume-unique-key.md)).
