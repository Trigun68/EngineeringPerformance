# ISS-016: Tooling качества и добивка покрытия тестами

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** Tooling / Tests
**Приоритет:** P3 (Low-Medium)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §5

## Контекст
- Client: ESLint 9 (`npm run lint` = `eslint src`); типы — `npm run typecheck`; bundle 4.16 MB одним чанком.
- Backend: `TreatWarningsAsErrors=false`, нет .NET-анализаторов/StyleCop; в тестах предупреждения CS8604.
- Покрытие backend перекошено: Analytics — сильное; Import (2-3 файла), Permissions (нет матрицы тестов), Export (smoke) — тонкие; цель >80% на Import/PerformanceService/PermissionAuthorization не подтверждена.
- Client: нет unit-тестов на крупные панели (`ForecastPanel`/`FactPanel`/`ComparePanel`/`ReportPanel`, `gridColumns`, `compareColumns`), `api/` покрыт только `projectsApi`, сторы без тестов. E2E — 37 spec (широко).

## Решение (направление)
1. ESLint (typescript + react-hooks + import-order) в client; шаг в CI.
2. Backend: включить анализаторы (Microsoft.CodeAnalysis.NetAnalyzers), постепенно поднять `TreatWarningsAsErrors`.
3. Тесты: матрица прав (PermissionAuthorization), импорт (PPV/LI/Equipment процессоры), экспорт (Excel/PDF smoke+контент), unit на god-панели после ISS-014, `analyticsApi`/сторы.
4. Code-splitting bundle по маршрутам (React.lazy) — цель <1 MB initial chunk.

## Acceptance Criteria
- [x] `npm run lint` запускает ESLint (`eslint src`); ошибки линта дают ненулевой код. Типы — `npm run typecheck`.
- [ ] Анализаторы включены; новые предупреждения не растут.
- [ ] Покрытие Import/Permissions/Export измеримо и растёт (отчёт coverage в CI).
- [ ] Initial bundle <1 MB (gzip-ориентир зафиксировать в docs).

## Связанные
- ISS-008 (CI Application.Tests — часть P0), ISS-013, ISS-014
