# Паритет формул Z: demo report vs PerformanceCalculationEngine

Дата: 2026-08-04.

## Вывод

Для leaf/subtree Z, средневзвешенной Z проекта, перцентиля и порогов интерпретации
TypeScript-демо `Trigun68/engineering-productivity-report` (`lib/performance/calc.ts`)
написано как порт бэкенда. **Изменения `PerformanceCalculationEngine` не требуются.**

| Алгоритм | Паритет |
|----------|---------|
| Leaf Z `(ln(prod)−normEstimate)/σ` | да |
| Subtree FactEstimate (часы×estimate) | да |
| Weighted Z по корням `Σ(Z×h)/Σ(h)` | да |
| Percentile bands | да |
| Interpretation thresholds | да |
| Forecast Opt/Base/Pess | нет (демо log-exp; SoT — multiplicative engine) |
| Insights / top-N | UI-слой, не engine |

Forecast и тексты рекомендаций остаются на стороне сервиса отчёта / UI.
