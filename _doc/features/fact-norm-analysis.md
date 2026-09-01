# Анализ факта и норм

**Дата:** 2026-08-31 (подпись UI: «Логнормальное распределение» с 2026-09-01)  
**Код:** `FactNormAnalysisEngine`, `FactNormAnalysisService`, вкладка `/analytics` → «Сравнение факта и норм»

## Назначение

Сверка фактической производительности (Fact-версии за период) с действующими нормами ИИС, подгонка референтных формул, вердикты и предложения по изменению нормы, создание новой нормы из предложения.

## Права

| Действие | Permission |
|----------|------------|
| Просмотр / запуск анализа / экспорт | `GroupTrend` Read |
| «Создать норму» | `IicWorkDirectionNorms` Edit |

## API

- `POST /api/analytics/fact-norm-analysis` — `{ periodStart, periodEnd, projectIds?, workDirectionIds? }`
- `GET /api/analytics/reference-formulas`
- `GET /api/analytics/fact-norm-analysis/export?…&format=csv|xlsx`
- `POST /api/analytics/norms/from-suggestion`

## Математика

Чистый движок `IFactNormAnalysisEngine` (Application): CONSTANT, LINEAR, POWER, LOGARITHMIC, EXPONENTIAL, POLYNOMIAL2, HYPERBOLIC; выбор по BIC при adj R² ≥ порога; вердикты ChangeFormulaType / AdjustParameters / NormIsActual.

**Логнормальное распределение не входит в BIC-каталог** (`ReferenceFormulaCatalog.All`, 7 типов). Код `KIMCURVE` доступен через `TryGet` только чтобы посчитать R² текущей нормы. Для текущей логнормальной нормы σ факта — `std(ln(Y))`, не линейное СКО производительности. Предложение «создать норму» никогда не пишет этот тип.

Точки факта: **только IsPrimary Fact**-версии за период (по одной на проект), `IFactAssessmentPipeline` → `PerformanceRowDto` (x=объём, y=производительность). В ответе: `NormMeanPerformance`, `FactMeanPerformance`, метаданные проекта/версии на точках. Z-расчёт по-прежнему только в `PerformanceCalculationEngine`.

## UI

`FactNormAnalysisPanel`: период (общий с аналитикой), бейдж точности, модалка выбора проектов (фильтр Contains, «Все проекты»), таблица предложений (ПКМ «Создать норму»), Highcharts факт vs норма, диалог создания нормы.

## Конфиг

Секция `Analytics` в `appsettings.json` (`FactNormAnalysis`, `Accuracy`, `NormCreation`).

## БД

Миграция `migrations/schema/112_NormSuggestionsAndAudit.sql` — таблицы `NormSuggestions`, `NormChangeAudits`.
