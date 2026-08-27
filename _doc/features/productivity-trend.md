# Тренд производительности нормы

**Дата обновления:** 2026-08-24  
**Код:** `ProductivityTrendAnalyticsService`, `POST /api/analytics/trends/productivity`, `client/src/features/analytics/productivity-trend/`

Вкладка **«Анализ тренда производительности»** на `/analytics` (`productivity-trend`). Право `GroupTrend`. Общий период с остальными вкладками аналитики.

Отвечает на вопрос: **как сдвинулась сама норма** (в том числе Linear → Logarithmic), а не «как мы работали против тогдашней нормы» (это [тренд Z](./trend-analysis.md)).

## Методика

Сравнивать коэффициенты `a`/`b` разных `FormulaType` нельзя: это разные функции N(V).

Сопоставимая величина — **часы при опорном объёме V\***:

1. V\* — медиана (или P10 / P90) фактического объёма оценки по основным Fact-версиям периода, по группе IIC (ФО + оборудование, только `IsAssessment`).
2. Часы = `IPerformanceCalculationEngine.ComputeForecastHours(norm, deviation, V*, V*, PlanningZParameters.Default)`. При ZBase = 0 базовые часы ≈ N(V\*)·V\*.
3. N(V\*) = Base / V\* — арифметика на выходе движка, не новый метод.
4. Индекс и декомпозиция (эффект нормы vs эффект объёма) — отношения/разности этих часов в сервисе аналитики.

Ужесточение = меньше часов N(V\*). Пересечение знака Δ на P10 и P90 = «неоднозначно». Для логарифма V\* &lt; 1 — флаг `log_volume_lt_one` (N = 0 по формуле).

Снимки версий хранят **указатель NormId**, не замороженные коэффициенты. Индекс читает регистр `IicWorkDirectionNorm`.

Запрещено: `NormFormulaEvaluator` / `INormCalculator` / `ForecastCalculation` в обход движка; новые методы на `IPerformanceCalculationEngine`; трактовать Z как ужесточение нормы.

Имена групп IIC: `IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit`.

## API

`POST /api/analytics/trends/productivity`

Тело: `{ periodStart, periodEnd, volumeMode: median|p10|p90, projectIds?, workDirectionIds? }`.

Ответ: индекс Σ часов, серии N(V*) по группам (цвет = FormulaType), таблица переходов, предостережения, контрфактические часы на фактических объёмах проектов.

Кэш Redis `prod-trend:{user}:{period}:{volumeMode}:…`, пул **EP-Analytics**.

Имя выгрузки (если появится файл): `аналитика_тренд_производительности.xlsx` (`ExportFileNameBuilder.Views.ProductivityTrend`).
