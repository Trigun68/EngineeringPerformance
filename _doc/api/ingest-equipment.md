# Оборудование: Excel и JSON-пакет

Дата обновления: 2026-08-24.  
Excel: `POST …/import/equipment` (есть алиас без `{dataType}`).  
JSON: `POST …/ingest/equipment`.  
Права: `ProjectDataUpload` Edit + ACL Edit.  
Код: [`ProjectExcelDataImportService.Equipment.cs`](../../backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.Equipment.cs), [`EquipmentImportCatalogMatcher`](../../backend/EngineeringPerformance.Infrastructure/Import/EquipmentImportCatalogMatcher.cs).

Итерации: `GET|DELETE …/import/equipment/iterations`.

## Поведение Excel (не менять)

- **Replace:** полная очистка оборудования версии, затем вставка.
- Обязательная шапка «Наименование по каталогу».
- Сопоставление каталога проекта: `(Name, UnitId)`; единица из колонки или по имени (`EquipmentUnitNameMatcher`). Неоднозначный ключ — skip.
- Нет позиции — stub в справочнике проекта (`coefficient=1`) или копия признаков с другого проекта.
- Поля строки версии: quantity, quantityInWork, positionNumber, facility, documentSet, diagramNumber, shortName, catalogName, typeMark, catalogId, tag, fullEquipmentId, supplyStandard, olReference, estimationBasis.
- Натуральный ключ строки версии: EquipmentId + FacilityCode + DocumentSet + FullEquipmentId + Tag.
- Плановые часы сценариев на строке Excel **не** заполняет.

## JSON-пакет

```json
{
  "mode": "replace",
  "description": "from-erp",
  "allowEmptyReplace": false,
  "items": [
    {
      "catalogName": "Насос",
      "unitName": "шт",
      "quantity": 2,
      "quantityInWork": 1,
      "facilityCode": "11-0515",
      "facilityName": "Корпус",
      "documentSet": "РД",
      "tag": "P-101",
      "fullEquipmentId": "P-101",
      "equipmentId": null,
      "unitId": null
    }
  ]
}
```

`catalogName` обязателен (если нет `equipmentId`). `unitName` или `unitId`. `mode` `upsert` обновляет по натуральному ключу строки, не удаляя прочие.

Карточки справочника с `ProjectId` проекта удаляются вместе с проектом (`DELETE /api/projects/{id}`).
