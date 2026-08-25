# PDF-отчёты с графиками

**Дата обновления:** 2026-08-26  
**Код:** `VisualReportService.cs`, `QuestPdfReportComposer.cs`, `ScottPlotChartRenderer.cs`, `ReportVisualTheme.cs`, `PdfInsightBuilder.cs`, `client/src/api/visualReportsApi.ts`

## Дизайн-система «Executive Technical»

Единый визуальный стиль для всех 10 PDF-отчётов: глубокий графитово-синий (`Primary` `#1B2A38`) + инженерный
синий акцент (`AccentA` `#2E86C1`), нейтральные статусные цвета (`Positive`/`Negative`/`Neutral`), шрифт DejaVu Sans.
Все токены — в `ReportVisualTheme` (`Application/Export`).

Серверный pipeline: данные → **ScottPlot** (PNG, палитра темы, без заголовка в самом графике) →
**QuestPDF** (обложка + контентные страницы с хедером/футером).

### Титульная страница

Каждый PDF (все 10 отчётов) начинает с полноценной обложки `ComposeCoverPage`: градиентный тёмный фон,
бейдж типа отчёта (`PdfReportKind`), метаданные (проект/версия/дата формирования), 2–4 тизер-KPI.

### Хедер / футер контентных страниц

`ComposeHeader` — заголовок, бейдж типа отчёта, доп. метаданные, акцентная линия.
`ComposeFooter` — тонкая линия, "Confidential", номер страницы, дата формирования.

### KPI, шкала Z-score, callout-блоки

- `ComposeKpiStrip` / `ComposeProjectAssessmentKpis` — карточки с акцентной полосой и статус-точкой.
- `ComposeZScoreScaleLegend(container, currentZ?)` — шкала Excellent…Critical с маркером текущего значения.
- `ComposeCalloutBlock` / `ComposeCalloutList` — краткие авто-выводы (риск/внимание/сильная сторона/данные),
  максимум 3 на отчёт. Логика построения — `PdfInsightBuilder` (порт `reportCalc.ts::buildInsights` для
  отчёта «Оценка факта»; для прогноза/сравнения/трендов — единичные авто-фразы по числам). Расчётная логика
  (Z-score, перцентили) не меняется — только текстовая интерпретация уже посчитанных значений.

**Иерархия:** `ComposeHierarchicalTable` — отступ по `Level` визуальным маркером-линией в ячейке
(`ComposeIndentedLabel`), зебра-строки, чипы интерпретации вместо текста в последней колонке.

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
