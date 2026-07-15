# Визуализация сравнения версий

**Дата обновления:** 2026-07-13  
**Код:** `client/src/features/projects/compare/`, `backend/.../ProjectCompareService.cs`

## Возможности MVP

- Dual picker версий А/Б с сохранением в `UserPreferences`
- Подвкладки Итого (tree IIC) / По объектам / Детально
- Фильтр «Показывать различия» (паритет WPF: presence `A+B`, productivityDelta)
- Режимы отображения: Таблица / График / Split
- KPI strip: Δ трудоёмкость по объектам, средний Δ Z
- API: `GET /api/projects/{id}/compare/chart`
- PDF: `GET /api/projects/{id}/compare/export.pdf` (ScottPlot + QuestPDF)

## Связанные документы

- [Трендовый анализ](./trend-analysis.md)
