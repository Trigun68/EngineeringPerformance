# Трендовый анализ

**Дата обновления:** 2026-07-13  
**Код:** `client/src/pages/analytics/TrendsAnalyticsPage.tsx`, `backend/.../TrendAnalyticsService.cs`

## Архитектура

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

## UI

- Маршрут `/analytics/trends` (право `GroupTrend`)
- Фильтры: проект, формула, диапазон дат
- 3 графика Highcharts: spline, column Δ, area S-curve
