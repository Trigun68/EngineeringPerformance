# PDF-отчёты с графиками

<<<<<<< HEAD
**Дата обновления:** 2026-08-26  
=======
**Дата обновления:** 2026-08-25  
>>>>>>> origin/master
**Код:** `VisualReportService.cs`, `QuestPdfReportComposer.cs`, `ScottPlotChartRenderer.cs`, `ReportVisualTheme.cs`, `PdfInsightBuilder.cs`, `client/src/api/visualReportsApi.ts`

## Дизайн-система «Executive Technical»

<<<<<<< HEAD
Единый визуальный стиль для всех 10 PDF-отчётов: глубокий графитово-синий (`Primary` `#1B2A38`) + инженерный
=======
Единый визуальный стиль для всех визуальных PDF-отчётов: глубокий графитово-синий (`Primary` `#1B2A38`) + инженерный
>>>>>>> origin/master
синий акцент (`AccentA` `#2E86C1`), нейтральные статусные цвета (`Positive`/`Negative`/`Neutral`), шрифт DejaVu Sans.
Все токены — в `ReportVisualTheme` (`Application/Export`).

Серверный pipeline: данные → **ScottPlot** (PNG, палитра темы, без заголовка в самом графике) →
**QuestPDF** (обложка + контентные страницы с хедером/футером).

### Титульная страница

<<<<<<< HEAD
Каждый PDF (все 10 отчётов) начинает с полноценной обложки `ComposeCoverPage`: градиентный тёмный фон,
бейдж типа отчёта (`PdfReportKind`), метаданные (проект/версия/дата формирования), 2–4 тизер-KPI.
=======
Каждый PDF начинает с полноценной обложки `ComposeCoverPage`: тёмный фон (сплошной `CoverGradientBottom` —
QuestPDF 2024.12 не поддерживает градиентные заливки), бейдж типа отчёта (`PdfReportKind`), метаданные
(проект/версия/дата формирования, время московское), 2–4 тизер-KPI.
>>>>>>> origin/master

### Хедер / футер контентных страниц

`ComposeHeader` — заголовок, бейдж типа отчёта, доп. метаданные, акцентная линия.
<<<<<<< HEAD
`ComposeFooter` — тонкая линия, "Confidential", номер страницы, дата формирования.
=======
`ComposeFooter` — тонкая линия, "Confidential", номер страницы, дата формирования (MSK).
>>>>>>> origin/master

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

Дизайн-превью без запуска PDF: dev-страница `/dev/pdf-mockups` (`client/src/pages/dev/PdfDesignMockups.tsx`) —
зеркало токенов `ReportVisualTheme` и раскладок обложки/хедера/KPI/шкалы/callout.

## Endpoints

> PDF для вкладок «Детально» (прогноз и факт) **отключён** (2026-08-26): полная детализация на десятки
> тысяч строк давала файл в 13k+ страниц / ~100 МБ, который ронял вкладку браузера. Детальные данные
> выгружаются только в Excel (`export/forecast-detail/excel`, `export/fact-detail/excel`).

| Endpoint | Вкладка | Графики |
|----------|---------|---------|
| `.../export/forecast-summary/pdf` | Прогноз Итого | Сценарии + top IIC + callout разброса |
| `.../export/forecast-facility/pdf` | Прогноз По объектам | Bar по объектам |
| `.../export/fact-summary/pdf` | Факт Итого | Z-bar + KPI + шкала + callouts; таблица: ФО, факт.часы, **План.Труд.Оптим./Баз./Песс.**, Z |
| `.../export/fact-facility/pdf` | Факт По объектам | Column hours; те же три план. колонки после факт. часов |
| `.../export/analysis.pdf` | Анализ версии | Делегирует fact-summary |
| `.../compare/export.pdf` | Сравнение | Waterfall Δ Z + KPI + callout; таблицы: **шесть** план. часов (опт./баз./песс. × A/B) |
| `/api/analytics/trends/export.pdf` | Тренды | Line + column + area + callout отклонения |
| `/api/projects/{id}/export/combined.pdf` | Отчёты | 4 key charts + шкала + callouts |
| `/api/projects/{id}/performance-report/export.pdf` | Отчёт производительности | KPI план/факт + история версий + выводы |

## Права

- Planning — прогноз PDF
- Assessment — факт PDF
- VersionComparison — compare PDF
- GroupTrend — trends PDF
- Projects — combined PDF
- Reports — performance-report PDF
