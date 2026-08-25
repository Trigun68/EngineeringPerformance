# ISS-008: Устаревшие красные тесты + Application.Tests вне CI

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** Tests / CI
**Приоритет:** P0 (Critical)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §1, §5

## Контекст
Baseline-прогон 2026-08-16:
- `Application.Tests`: `FormulaTemplatesTests.All_ContainsEightNonNullTemplates` — ожидает 8 шаблонов, в `ReferenceFormulaCatalog` уже 11 (добавлены Constant/Hyperbolic/Exponential 2026-08-04).
- `Infrastructure.Tests`: `PerformanceCalculationTests.GetPercentile(null)` — ожидает «Н/Д», код возвращает «—`.
- Workflow `.github/workflows/api-tests.yml` запускает только Api.Tests + Infrastructure.Tests → `Application.Tests` не гоняется нигде, а красный Infrastructure.Tests уронит CI при ближайшем push с `backend/**`.

## Acceptance Criteria
- [ ] Ожидания обоих тестов обновлены под текущее поведение (11 шаблонов; «—»), прогон зелёный.
- [ ] `Application.Tests` добавлен шагом в `api-tests.yml` (или отдельный workflow).
- [ ] CI зелёный на master после push.

## Файлы
- `backend/EngineeringPerformance.Application.Tests/Norms/FormulaTemplatesTests.cs`
- `backend/EngineeringPerformance.Infrastructure.Tests/Analytics/PerformanceCalculationTests.cs`
- `.github/workflows/api-tests.yml`

## Связанные
- Отчёт аудита `_work/20260816_221500_code_architecture_review.md`
