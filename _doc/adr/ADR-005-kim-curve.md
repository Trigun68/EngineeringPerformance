# ADR-005: FormulaType «Кривая Кима» (логнормаль)

**Дата:** 2026-08-31  
**Статус:** Accepted

## Контекст

Нужен тип нормы, в котором производительность в сценариях планирования живёт в лог-пространстве:  
`P(z) = e^(μ + z·σ)`, `μ = ln(P)`, `σ` — СКО **логарифма**, не линейная доля `σ_rel · P`.

Существующие 11 типов `N(V)` и линейная мультипликативная модель `LI = N·(1+σ·z)·V` остаются. Добавление типа **не** пересчитывает уже сохранённые нормы и снимки версий (`RecalculateAllVersions` не вызывается).

Z-оценка уже логнормальна: `Z = (ln(hours/volume) − Estimate) / StandardDeviation`. Если для Кима записать `Estimate = μ` и `StandardDeviation = σ` (лог), ветка в `PerformanceCalculationEngine` не нужна.

## Варианты

### A. Новый FormulaType + DeviationModelType.Lognormal

`KimCurve = 12`, `N(V) = a` (как Constant). Часы: `LI(z) = e^(μ + z·σ) · V`. Хранение: `Performance = P`, `Estimate = ln(P)`, `StandardDeviation = σ` без умножения на P.

### B. Только DeviationModelType без нового FormulaType

Нельзя отличить Ким от Constant в справочнике и FactNorm. Отклонено.

### C. σ = ln(линейного СКО)

Не совпадает с выбранной математикой. Отклонено.

## Решение

**Вариант A.**

- Входы: P > 0, σ > 0. μ производная, только чтение.
- При z = 0 часы совпадают с Constant: `P · V`.
- FactNorm BIC-каталог (`ReferenceFormulaCatalog.All`) **без** KIMCURVE. Текущую норму Кима анализируют (`TryGet`), σ факта — `std(ln(Y))`.
- UI: три подписи — Производительность, μ, σ. `meanPerformance` не подписывается как μ.

## Последствия

- Существующие Linear/Constant не меняют формулы часов и Z.
- Новая норма Кима на новой группе IIC попадает в as-of снимок только версий с `CreatedAt` в периоде нормы (`AfterNormChangedAsync`).
- Миграция схемы не нужна: значения enum — int без CHECK.

## Связанные документы

- [ADR-004](ADR-004-formula-type-constant-hyperbolic-exponential.md)
- [_doc/features/forecast-algorithm.md](../features/forecast-algorithm.md)
- [_doc/architecture/hours-and-z-ssot.md](../architecture/hours-and-z-ssot.md)
- [_doc/features/fact-norm-analysis.md](../features/fact-norm-analysis.md)
