# Отчёт производительности проекта (вкладка «Отчет»)

**Дата:** 2026-07-22  
**Код:** `PerformanceReportService`, `PerformanceReportController`, `client/src/features/projects/report/`

## Назначение

Визуальный отчёт по инженерной производительности выбранных версий **плана** и **факта** проекта: средневзвешенный Z, шкала интерпретации, динамика по версиям, top-3 ± дисциплин, дерево направлений, PDF.

## API

| Метод | Путь | Право |
|-------|------|--------|
| GET | `/api/projects/{projectId}/performance-report?planVersionId=&factVersionId=` | `Reports` Read |
| GET | `/api/projects/{projectId}/performance-report/export.pdf` | `Reports` Read |

Z-математика только через существующий движок (`IPerformanceService` / `PerformanceCalculationEngine`). Паритет формул с демо: [performance-report-engine-parity.md](./performance-report-engine-parity.md).

## UI

- Вкладка **Отчет** в workspace проекта (`FullProjectsPage` + `useProjectWorkspace`).
- Пикеры версий план/факт (`ProjectVersionPickerDialog`).
- Стили: Tailwind-островок `src/features/projects/report/**` (без отдельного Next-приложения); остальной клиент — MUI.
