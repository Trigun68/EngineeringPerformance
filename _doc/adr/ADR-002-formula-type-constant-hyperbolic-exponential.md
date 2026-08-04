# ADR-002: Расширение FormulaType (Constant, Hyperbolic, Exponential)

**Дата:** 2026-08-04  
**Статус:** Accepted

## Контекст

Модуль «Анализ факта и норм» подгоняет референтные формулы (CONSTANT, HYPERBOLIC, EXPONENTIAL и др.) и создаёт записи `IicWorkDirectionNorm`. Существующий enum `FormulaType` не покрывал постоянную и гиперболическую модели, а экспоненциальный рост `a·e^(b·V)` отличается от `ExponentialDecay`.

## Решение

Добавлены значения:

- `Constant = 9` — `N(V) = a`
- `Hyperbolic = 10` — `N(V) = a + b/V`
- `Exponential = 11` — `N(V) = a · e^(b·V)`

Реализованы в `NormFormulaEvaluator` и `FormulaTemplates`. Референтные коды анализа маппятся через `FormulaTypeMapper`.

## Последствия

- Frontend `FormulaType` расширен до `1…11`.
- Старые нормы без изменений; новые типы доступны в шаблонах и в создании из предложения.
- `ExponentialDecay` сохраняется для затухания с смещением `c`.
