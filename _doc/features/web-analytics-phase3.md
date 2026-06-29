# Web: аналитика и экспорт (Phase 3)

**Обновлено:** 2026-06-08  
**Связано:** `docs/migration_map.md` § Phase 3, `tmp/plan-phase3.md`, отчёт `_work/20260608_phase3_complete.md`

---

## Назначение

Веб-версия воспроизводит аналитический цикл WPF: после загрузки данных проекта пользователь может оценить факт, построить прогноз, сравнить версии и выгрузить отчёты. Администратор — просматривать журнал действий и настраивать матрицу прав.

---

## Точки входа (UI)

| URL | Описание |
|-----|----------|
| `/projects/:id` → вкладка «Оценка» | Дерево производительности, отчёт анализа версии, Excel/PDF |
| `/projects/:id` → вкладка «Прогноз» | Прогноз трудозатрат, редактирование плановых объёмов на листьях, Excel/PDF |
| `/projects/:id` → вкладка «Сравнение» | Версии A/B; подвкладки **Итого**, **По объектам**, **Детально**; фильтр «Только различия» |
| `/projects/:id` → вкладки «Группа», «Тренд» | Placeholder (сообщение API) |
| `/audit` | Журнал действий пользователей |
| `/settings/access-rights` | Матрица прав (только Administrator) |

Навигация: кнопки «Журнал» и «Права» в AppBar (`client/src/App.tsx`).

---

## Права доступа

Проверка на API через `RequireProjectAccess` и `HasPermission`. Типы прав — `PermissionType` в Domain (Assessment, Planning, VersionComparison, GroupAnalysis, GroupTrend). Страница прав — роль Administrator.

---

## Снимок норм при создании версии

При `POST /api/projects/{id}/versions` (и копировании) вызывается `AnalysisVersionSnapshotService`: фиксируются нормы на момент создания версии для корректных расчётов оценки и прогноза.

---

## Экспорт

- **Excel:** ClosedXML, endpoints `export/analysis.xlsx`, `export/forecast.xlsx`
- **PDF:** QuestPDF, endpoints `export/analysis.pdf`, `export/forecast.pdf`

Реализация: `AnalyticsExportService` в Infrastructure.

---

## Ограничения относительно WPF

- Групповой анализ и тренд — заглушки (как в desktop).
- PDF — упрощённая вёрстка; полный pixel-perfect перенос — техдолг.
- Сравнение версий — facility/detail портированы; экспорт compare в Excel/PDF — техдолг.

---

## Исходный код (ориентиры)

- Backend controllers: `ProjectAnalyticsController`, `ProjectLevelAnalyticsController`, `AuditController`, `AccessRightsController`
- Frontend tabs: `client/src/pages/projects/AssessmentTab.tsx`, `PlanningTab.tsx`, `CompareTab.tsx`
- API client: `client/src/api/analyticsApi.ts`
