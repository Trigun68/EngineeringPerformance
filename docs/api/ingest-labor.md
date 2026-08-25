# Трудоёмкость: Excel и JSON-пакет

Дата обновления: 2026-08-24.  
Excel: `POST …/import/labor-intensities?addMissingLaborToCatalog=`.  
Preview: `POST …/import/labor-intensities/preview` (файл).  
JSON: `POST …/ingest/labor-intensities`, preview `POST …/ingest/labor-intensities/preview`.  
Права: import/ingest Edit; preview Read.  
Код: [`ProjectExcelDataImportService.LaborIntensity.cs`](../../backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.LaborIntensity.cs).

Итерации: `GET|DELETE …/import/labor-intensities/iterations`.

## Поведение Excel (не менять)

- **Replace** всех PLI версии.
- Связь только через справочник «Трудоёмкость»: Excel «тип документа TF» → `LaborIntensity.DocumentTypeName` (Trim, без учёта регистра). Колонка «направление» — **только** для детекта шапки, не FK.
- Направление строки = `LaborIntensity.IicWorkDirectionId` (может быть null).
- `addMissingLaborToCatalog=true` создаёт корневые карточки каталога без ИИС/норм.
- `documentationCode` = тип или `тип|марка`, max 450.
- Незаматченный тип всё равно сохраняется с null FK (счётчики в `details`).
- `MergeLaborRowsByNaturalKey` в коде **не вызывается**: строка файла = строка БД.
- Preview: `{ unmatchedDocumentTypes, totalDataRows }`.
- Копия версии **не** переносит LI, кроме Fact→Fact. Plan ждёт отдельный ingest.

Старый [`docs/features/data-import.md`](../features/data-import.md) описывает устаревшую раскладку колонок — ориентироваться на этот файл и код.

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
