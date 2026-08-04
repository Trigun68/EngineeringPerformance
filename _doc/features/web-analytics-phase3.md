# Web: аналитика и экспорт (Phase 3)

**Обновлено:** 2026-07-14  
**Политика:** только web — см. [web-only-development-plan.md](../plans/web-only-development-plan.md)

---

## Назначение

Веб-приложение обеспечивает полный аналитический цикл: после загрузки данных проекта пользователь может оценить факт, построить прогноз, сравнить версии и выгрузить отчёты. Администратор — просматривать журнал действий и настраивать матрицу прав.

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

Реализация: `AnalyticsExportService` и `ProjectExportService` в `Export/` (Infrastructure).

---

## Ограничения и техдолг (web)

- Групповой анализ и групповой тренд — заглушки (см. [business-logic.md](../architecture/business-logic.md) §8).
- Внутреннее расхождение прогноза: `ForecastPanel` vs отчёт планирования (задача A-01).
- Сравнение версий — facility/detail реализованы; PDF compare — в [pdf-visual-reports.md](./pdf-visual-reports.md).

---

## Исходный код (ориентиры)

- Backend controllers: `ProjectAnalyticsController`, `ProjectLevelAnalyticsController`, `AuditController`, `AccessRightsController`
- Frontend tabs: `client/src/pages/projects/AssessmentTab.tsx`, `PlanningTab.tsx`, `CompareTab.tsx`
- API client: `client/src/api/analyticsApi.ts`
