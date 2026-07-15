# PDF-отчёты с графиками

**Дата обновления:** 2026-07-14  
**Код:** `VisualReportService.cs`, `QuestPdfReportComposer.cs`, `ScottPlotChartRenderer.cs`, `client/src/api/visualReportsApi.ts`

## Архитектура

Серверный pipeline: данные → **ScottPlot** (PNG) → **QuestPDF** (PDF, DejaVu Sans).

**Стиль портала:** `ReportVisualTheme` = `wpColors` (`#2C3E50`, accent `#3498DB`), KPI-карточки, тёмный header.

**Оценка проекта:** KPI (часы, Z, перцентиль, интерпретация) + **шкала Z** Excellent…Critical.

**Иерархия:** `ComposeHierarchicalTable` — отступ по `Level` (`└`).

Modal `export/forecast.pdf` делегирует в visual `forecast-summary/pdf`.

## Endpoints

| Endpoint | Вкладка | Графики |
|----------|---------|---------|
| `.../export/forecast-summary/pdf` | Прогноз Итого | Сценарии + top IIC |
| `.../export/forecast-facility/pdf` | Прогноз По объектам | Bar по объектам |
| `.../export/forecast-detail/pdf` | Прогноз Детально | Top-20 PV |
| `.../export/fact-summary/pdf` | Факт Итого | Z-bar + KPI + шкала |
| `.../export/fact-facility/pdf` | Факт По объектам | Column hours |
| `.../export/fact-detail/pdf` | Факт Детально | Top \|Z\| |
| `.../export/analysis.pdf` | Анализ версии | Делегирует fact-summary |
| `.../compare/export.pdf` | Сравнение | Waterfall Δ Z + KPI |
| `/api/analytics/trends/export.pdf` | Тренды | Line + column + area |
| `/api/projects/{id}/export/combined.pdf` | Отчёты | 4 key charts + шкала |

## Права

- Planning — прогноз PDF
- Assessment — факт PDF
- VersionComparison — compare PDF
- GroupTrend — trends PDF
- Projects — combined PDF
