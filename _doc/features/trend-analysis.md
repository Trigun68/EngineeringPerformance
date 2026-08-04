# Трендовый анализ

**Дата обновления:** 2026-07-27  
**Код:** `client/src/pages/analytics/AnalyticsPage.tsx`, `backend/.../TrendAnalyticsService.cs`

## UI (текущее состояние)

- Маршрут `/analytics` (право `GroupTrend`); `/analytics/trends` → redirect.
- Вкладки: «Трендовый анализ», «Сравнение факта и норм».
- Общие фильтры периода (пресеты + произвольный диапазон).
- **Графики временно не отображаются** — каркас до следующего этапа наполнения.
- Вкладка **«Сравнение факта и норм»** реализована: см. [`fact-norm-analysis.md`](fact-norm-analysis.md).

## Архитектура (API, без изменений)

- `IFactAssessmentPipeline` — абстракция формулы оценки (`v1-docset-proportional` → `PerformanceCalculationEngine`)
- `granularity=version` — точки по `ProjectAnalysisVersion.CreatedAt`
- События импорта из `DataImportIteration.ImportedAt`
- Кэш Redis/IDistributedCache, TTL 10 мин
- PDF: `GET /api/analytics/trends/export.pdf`

## API

| Endpoint | Назначение |
|----------|------------|
| `GET /api/analytics/trends/formulas` | Список формул |
| `GET /api/analytics/trends` | Временные ряды (факт, прогноз, Δ, S-curve) |
| `GET /api/analytics/trends/detail` | Детальные строки по версии |
