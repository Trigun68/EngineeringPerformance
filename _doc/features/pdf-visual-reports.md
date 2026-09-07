# PDF-отчёты с графиками

**Дата обновления:** 2026-09-07  
**Код:** `VisualReportService.cs`, `QuestPdfReportComposer.cs`, `ScottPlotChartRenderer.cs`, `ReportVisualTheme.cs`, `PdfNumberFormat.cs`, `PdfScenarioLabels.cs`, `PdfTableColumn.cs`, `PdfInsightBuilder.cs`, `client/src/api/visualReportsApi.ts`  
**Правило для агентов:** [`.cursor/rules/pdf-visual-design.mdc`](../../.cursor/rules/pdf-visual-design.mdc)

## Дизайн-система «Executive Technical»

Единый визуальный стиль для всех визуальных PDF-отчётов: глубокий графитово-синий (`Primary` `#1B2A38`) + инженерный
синий акцент (`AccentA` `#2E86C1`), нейтральные статусные цвета (`Positive`/`Negative`/`Neutral`), шрифт DejaVu Sans.
Все токены — в `ReportVisualTheme` (`Application/Export`).

Серверный pipeline: данные → **ScottPlot** (PNG, палитра темы, без заголовка в самом графике) →
**QuestPDF** (обложка + контентные страницы с хедером/футером).

### Сценарии трудоёмкости

Порядок **всегда** Оптимистичный → Базовый → Пессимистичный (KPI, графики, таблицы). Полные названия,
без `Опт.` / `База` / `Пессим.`. Сравнение версий: `Оптимистичный (версия А)`.

Заливка столбцов — как в гриде страницы (`#e3f2fd` / `#e8f5e9` / `#ffebee`). Столбики графиков —
насыщенные пары `#2E86C1` / `#27AE60` / `#E74C3C`.

### Шрифт

Контентные страницы: тёмный текст (`Primary` / `ContentSecondary` `#2C3E50`). Бледно-серый `Muted` на
контенте не используется. Белый текст — только титульная страница (инверсия на тёмном фоне).

### Числа

`PdfNumberFormat`: ru-RU (пробел групп, запятая), физобъём и трудоёмкость — 1 знак, производительность и Z — 2 знака,
стоимость на титуле — целые рубли (`CurrencyRub`). Числовые колонки выровнены вправо (`PdfTableColumn.Numeric`).

### Названия IIC

Только SoT `код — наименование (ед.)`. Отдельную колонку «Код» у направления не выводить. Аббревиатура **IIC**.

### Титульная страница

Каждый PDF начинает с полноценной обложки `ComposeCoverPage`: тёмный фон (сплошной `CoverGradientBottom` —
QuestPDF 2024.12 не поддерживает градиентные заливки), бейдж типа отчёта (`PdfReportKind`), метаданные
(проект/версия/дата формирования, время московское), 2–4 тизер-KPI. Прогноз «По объектам» получает те же три
KPI сценариев, что «Итого». Под часами сценария на **титуле** — стоимость ₽ (ставка справочника
на дату версии `CreatedAt` × трудоёмкость направления). Контентные KPI — только часы.

### Хедер / футер контентных страниц

`ComposeHeader` — заголовок, бейдж типа отчёта, доп. метаданные, акцентная линия.
`ComposeFooter` — тонкая линия, "Confidential", номер страницы, дата формирования (MSK).

### KPI, шкала Z-score, callout-блоки

- `ComposeKpiStrip` / `ComposeProjectAssessmentKpis` — карточки с акцентной полосой и статус-точкой.
- `ComposeZScoreScaleLegend(container, currentZ?)` — шкала слева `+3`/Критично → справа `−3`/Отлично, маркер текущего значения.
- `ComposeCalloutBlock` / `ComposeCalloutList` — краткие авто-выводы (риск/внимание/сильная сторона/данные),
  максимум 3 на отчёт. Логика построения — `PdfInsightBuilder`.

**Иерархия:** `ComposeHierarchicalTable` — отступ по `Level`, зебра-строки, заливка сценариев, чипы интерпретации.

Modal `export/forecast.pdf` делегирует в visual `forecast-summary/pdf`.

Дизайн-превью без запуска PDF: dev-страница `/dev/pdf-mockups` (`client/src/pages/dev/PdfDesignMockups.tsx`).

## Endpoints

> PDF для вкладок «Детально» (прогноз, факт, сравнение) **отключён**: детализация на десятки тысяч строк
> непригодна для PDF. Детальные данные выгружаются только в Excel.

| Endpoint | Вкладка | Графики |
|----------|---------|---------|
| `.../export/forecast-summary/pdf` | Прогноз Итого | Сценарии Opt→Base→Pes + top IIC |
| `.../export/forecast-facility/pdf` | Прогноз По объектам | KPI трёх сценариев + bar по объектам |
| `.../export/fact-summary/pdf` | Факт Итого | Z-bar + KPI; таблица: ФО, факт.часы, три сценария, Z; без колонки кода IIC |
| `.../export/fact-facility/pdf` | Факт По объектам | «Фактические часы по объектам»; те же три план. колонки |
| `.../export/analysis.pdf` | Анализ версии | Делегирует fact-summary |
| `.../compare/export.pdf` | Сравнение (Итого / По объектам) | Waterfall Δ Z; полные имена сценариев × A/B |
| `/api/analytics/trends/export.pdf` | Тренды | Line + column + area |
| `/api/projects/{id}/export/combined.pdf` | Сводный | 4 key charts |
| `/api/projects/{id}/performance-report/export.pdf` | Сводный вер 2 | KPI план/факт + история + направления; объём на L1 = поддерево |
| `.../export/report/pdf` | Сводный вер 1 | План/факт, топ-5 и таблица групп IIC **первого уровня** (часы + объём поддерева) |

## Права

- Planning — прогноз PDF
- Assessment — факт PDF
- VersionComparison — compare PDF
- GroupTrend — trends PDF
- Projects — combined PDF
- Reports — performance-report PDF / report PDF
