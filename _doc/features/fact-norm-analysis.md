# Анализ факта и норм

**Дата:** 2026-08-04  
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

Точки факта: `IFactAssessmentPipeline` → `PerformanceRowDto` (x=объём, y=производительность). Z-расчёт по-прежнему только в `PerformanceCalculationEngine`.

## UI

`FactNormAnalysisPanel`: период (общий с аналитикой), бейдж точности, модалка выбора проектов (фильтр Contains, «Все проекты»), таблица предложений (ПКМ «Создать норму»), Highcharts факт vs норма, диалог создания нормы.

## Конфиг

Секция `Analytics` в `appsettings.json` (`FactNormAnalysis`, `Accuracy`, `NormCreation`).

## БД

Миграция `migrations/schema/112_NormSuggestionsAndAudit.sql` — таблицы `NormSuggestions`, `NormChangeAudits`.
