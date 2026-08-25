# Физ. объёмы: Excel и JSON-пакет

Дата обновления: 2026-08-24.  
Excel: `POST /api/projects/{id}/versions/{vid}/import/physical-volumes` (multipart `file`).  
JSON: `POST /api/projects/{id}/versions/{vid}/ingest/physical-volumes`.  
Права: `ProjectDataUpload` Edit + ACL проекта Edit.  
Код Excel: [`ProjectExcelDataImportService.PhysicalVolume.cs`](../../backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.PhysicalVolume.cs).

Итерации: `GET|DELETE …/import/physical-volumes/iterations?importedAt=`.

## Поведение Excel (не менять)

- **Replace:** удаляются все строки ФО версии и прошлые итерации этого типа, затем вставка.
- Шифр ЕП: колонки «ID/Код/Шифр по каталогу», шаблон «Список работ»; нормализация тире и нулей (`01-01-001-01` / КФО `KQ.02.160`).
- Нет шифра или нет в каталоге версии (КФО/БЭР) — строка **skip**, импорт `Success=true`, текст в `details`.
- Поля строки: quantity, facility (часто `код - имя`), bindingType, documentSet, catalogId, tag, workId, workName, workComment, estimationBasis, catalogCode/Name с номенклатуры.
- Не заполняет: `parentId`, машинные часы, `status` (это пересчёт / ручная правка).
- Rollback по `createdAt == importedAt` имеет смысл только для **текущего** replace-батча.

## JSON-пакет

Тело:

```json
{
  "mode": "replace",
  "description": "from-erp",
  "allowEmptyReplace": false,
  "items": [
    {
      "epCode": "01-01-001-01",
      "quantity": 12.5,
      "facilityCode": "11-0515-02-01",
      "facilityName": "Здание линии",
      "workId": "W-1",
      "workName": "Монтаж",
      "bindingType": null,
      "documentSet": "РД",
      "catalogId": null,
      "tag": null,
      "workComment": null,
      "estimationBasis": null,
      "physicalVolumeId": null
    }
  ]
}
```

- `epCode` — натуральный ключ номенклатуры (та же нормализация, что Excel). Можно вместо него `physicalVolumeId`.
- `mode`: `replace` (как Excel) или `upsert` (по ключу ADR-003, без очистки остальных строк).
- Пустой `items` при `replace` без `allowEmptyReplace: true` — **400** (защита от случайной очистки). Excel-файл с нулём строк по-прежнему чистит версию.

Ответ как у Excel: `importedRows`, `iterationId`, `details`, `rowErrors`. После успеха — пересчёт версии.
