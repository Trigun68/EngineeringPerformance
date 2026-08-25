# Ошибки и лимиты API

Дата обновления: 2026-08-24.

| HTTP | Когда |
|------|--------|
| 401 | Нет или просрочен JWT |
| 403 | Нет `PermissionType` или нет ACL на проект |
| 404 | Сущность не найдена |
| 400 | Валидация, неизвестный dataType/mode, пустой replace без флага, ошибки строк импорта |
| 422 | FluentValidation (справочники, проект) |
| 501 | `POST /api/projects/import/excel` — импорт каталога проектов файлом не реализован |
| 503 | SQL недоступен (`sql_unavailable`) |

Тело 400 импорта Excel и JSON-ingest:

```json
{
  "errorMessage": "…",
  "iterationId": 0,
  "rowErrors": [{ "rowNumber": 3, "message": "…" }]
}
```

Успех ingest/import: `importedRows`, `iterationId`, `details` (русский текст пропусков), `rowErrors` (частичные skip без отката всего пакета).

## Excel

- Расширение `.xlsx` (не `.xls`).
- Лимит **10 МБ** ([`ExcelUploadValidator.MaxFileSizeBytes`](../../backend/EngineeringPerformance.Api/Security/ExcelUploadValidator.cs)). Упоминания 50 МБ в старых правилах устарели.
- MIME OOXML или `application/octet-stream`, magic bytes ZIP `PK`.
- Построчный CRUD JSON лимита файла не имеет; разумный размер пакета ingest — тысячи строк, не десятки МБ в одном POST.

Гриды версии: `pageSize` максимум **1000**.
