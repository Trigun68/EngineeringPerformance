# Визуализация сравнения версий

**Дата обновления:** 2026-08-25  
**Код:** `client/src/features/projects/compare/`, `backend/.../ProjectCompareService.cs`

## Возможности MVP

- Dual picker версий А/Б с сохранением в `UserPreferences`
- Подвкладки Итого (tree IIC) / По объектам / Детально
- Фильтр «Показывать различия» (паритет WPF: presence `A+B`, productivityDelta)
- Всегда таблица; выгрузка Excel/PDF справа у подвкладок
- Чип «Различия версий»: плановые часы выбранного сценария, B − A, итог текущей подвкладки
- PDF: `GET /api/projects/{id}/compare/export.pdf` (ScottPlot + QuestPDF)

## Связанные документы

- [Тренд оценки факта (Z)](./trend-analysis.md)
- [Тренд производительности нормы](./productivity-trend.md)
