# Тренд оценки факта (Z)

**Дата обновления:** 2026-08-24  
**Код:** `client/src/pages/analytics/AnalyticsPage.tsx`, `TrendAnalysisPanel.tsx`, `ZTrendAnalyticsService.cs`

Вкладка **«Анализ тренда оценки факта»** на `/analytics` (значение prefs `trends`, `data-testid="analytics-tab-trends"`). Право `GroupTrend`.

Отвечает на вопрос: как фактическая (и плановая) оценка **Z** двигалась относительно **тогда действующей** нормы.

Связанные: [productivity-trend.md](./productivity-trend.md) (движение самой нормы), [fact-norm-analysis.md](./fact-norm-analysis.md), [hours-and-z-ssot.md](../architecture/hours-and-z-ssot.md).

## UI

- Маршрут `/analytics` (право `GroupTrend`); `/analytics/trends` → redirect.
- Общие фильтры периода (пресеты + произвольный диапазон) на все вкладки аналитики.
- Панель: общий график Z, серии по группам IIC, легенда периодов норм, вес `basisHours`.
- Соседние вкладки: «Анализ тренда производительности», «Сравнение факта и норм», «Анализ норм».

## API

| Endpoint | Назначение |
|----------|------------|
| `POST /api/analytics/trends/z-score` | Ряды Z плана/факта, сегментация по нормам |
| `GET /api/analytics/trends/z-score/filters` | Проекты и корневые группы IIC |
| `GET /api/analytics/trends/export.pdf` | PDF динамики (legacy visual) |

Кэш Redis `ztrends:{user}:{period}:{projects}:{groups}`, TTL `Analytics:Trends:CacheTtlMinutes`.
