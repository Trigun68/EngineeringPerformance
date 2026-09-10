# Трудоёмкость: Excel и JSON-пакет

Дата обновления: 2026-09-06.  
Excel: `POST …/import/labor-intensities?addMissingLaborToCatalog=`.  
Preview: `POST …/import/labor-intensities/preview` (файл).  
JSON: `POST …/ingest/labor-intensities`, preview `POST …/ingest/labor-intensities/preview`.  
Права: import/ingest Edit; preview Read.  
Код: [`ProjectExcelDataImportService.LaborIntensity.cs`](../../backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.LaborIntensity.cs), [`LaborCatalogLookup.cs`](../../backend/EngineeringPerformance.Infrastructure/Import/LaborCatalogLookup.cs).

Итерации: `GET|DELETE …/import/labor-intensities/iterations`.

## Поведение Excel

- **Replace** всех PLI версии.
- Связь через справочник «Трудоёмкость»: Excel «тип документа TF» → `DocumentTypeName` **или** `Name` (Trim, без учёта регистра). Колонка «направление» — детект шапки; при auto-add может стать родителем новой карточки, если имя папки совпало.
- Направление строки = `LaborIntensity.IicWorkDirectionId` (может быть null).
- UI по умолчанию включает `addMissingLaborToCatalog`: новые карточки с `Name` = `DocumentTypeName` = текст TF, без IIC/норм (папка — если нашлось совпадение).
- `documentationCode` = тип или `тип|марка`, max 450. Марка = комплект документов объекта (`PPV.DocumentSet`).
- Незаматченный тип всё равно сохраняется с null FK (счётчики в `details`).
- `MergeLaborRowsByNaturalKey` в коде **не вызывается**: строка файла = строка БД.
- Preview: `{ unmatchedDocumentTypes, totalDataRows }` — тот же ключ Name/DocumentTypeName.
- Копия версии **не** переносит LI, кроме Fact→Fact. Plan ждёт отдельный ingest.

Оценка факта после импорта: см. [`hours-and-z-ssot.md`](../architecture/hours-and-z-ssot.md) (L1 IIC + объект/марка).

Старый [`_doc/features/data-import.md`](../features/data-import.md) описывает устаревшую раскладку колонок — ориентироваться на этот файл и код.

## JSON-пакет

```json
{
  "mode": "replace",
  "description": "from-erp",
  "allowEmptyReplace": false,
  "addMissingLaborToCatalog": false,
  "items": [
    {
      "documentTypeName": "РД-технология",
      "brand": "3U0000-ОП",
      "plannedHours": 120,
      "actualHours": 0,
      "notes": null
    }
  ]
}
```

Preview JSON: `{ "documentTypeNames": ["РД-технология", "…"] }` → тот же ответ, что Excel preview.

После успеха Excel/JSON трудоёмкости пересчёт ФО не запускается, сбрасывается кэш аналитики проекта.
