# ISS-014: Удаление мёртвых UI-путей и декомпозиция god-панелей

**Статус:** 🟠 Частично (2026-09-09)
**Категория:** Frontend / Dead code / SRP
**Приоритет:** P1 (High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §3 (M-05), §4 (C-05)

## Контекст
Мёртвый код (не подключён к роутеру `App.tsx`, ~2000 строк):
- `client/src/pages/projects/ProjectDetailPage.tsx` (476)
- `client/src/pages/projects/ProjectWorkspacePage.tsx` (453)
- `client/src/features/forecast/ForecastPage.tsx` (730; импортируется только мёртвым `ProjectDetailPage`)
- `client/src/features/projects/ProjectsPage.tsx` (339; импортируется только собственным тестом `__tests__/ProjectsPage.test.tsx`)

Живые пути: `pages/projects/ProjectsPage` (список) + `FullProjectsPage` (workspace). Мёртвые страницы ссылаются на те же API иначе, чем живые панели — риск поведенческого дрейфа и лишняя цена рефакторингов.

God-панели (после удаления мёртвых): `ForecastPanel` 946 строк / 24 хука, `FactPanel` 706 / 22, `ComparePanel` 697 / 25, `ProjectVersionPickerDialog` 718, `PlanningParametersDialog` 638.

**2026-09-09.** Живые панели разобраны без смены поведения: `ForecastPanel` 232, `FactPanel` 223, `ComparePanel` 240; гриды `*SummaryGrid` / `*FacilityGrid` / `*DetailGrid`, диалоги и хуки `use*PageData` / `use*PanelDialogs`. `ProjectVersionPickerDialog`, `PlanningParametersDialog`, `IicNormsPage`, `ProductivityTrendPanel`, `FullProjectsPage` — ранее. Мёртвые файлы из списка выше **ещё в дереве**.

## Решение (направление)
1. Удалить 4 мёртвых файла + их тесты (git history сохраняет код при необходимости возврата).
2. Декомпозиция панелей: состояние/загрузка → кастомные хуки (`useForecastData`, `useCompareData`), секции UI → подкомпоненты; цель ≤300 строк на компонент.
3. После удаления — прогнать Vitest + Playwright (мёртвые пути могли маскировать регрессии).

## Acceptance Criteria
- [ ] Мёртвые файлы удалены; `npm run build` и `npm test` зелёные; bundle уменьшился.
- [x] Панели разбиты на хуки/подкомпоненты (контейнеры ≤250 строк). Поведение: Vitest панелей зелёный; Playwright **144 passed, 1 skipped**, `http://127.0.0.1:8081`, frontend `--build` (2026-09-09).
- [x] В отчёте: 144 passed Playwright на `http://127.0.0.1:8081` (полный suite; `project-workspace`, `forecast-module`, `fact-compare-module` зелёные).

## Файлы
- `client/src/pages/projects/ProjectDetailPage.tsx`, `ProjectWorkspacePage.tsx`
- `client/src/features/forecast/ForecastPage.tsx`, `client/src/features/projects/ProjectsPage.tsx` (+ `__tests__/ProjectsPage.test.tsx`)
- `client/src/features/projects/{forecast,fact,compare}/*.tsx`

## Связанные
- Отчёт аудита; `playwright-e2e-verify.mdc`
