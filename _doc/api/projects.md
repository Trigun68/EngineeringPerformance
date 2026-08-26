# Проекты

Дата обновления: 2026-08-24.  
Код: [`ProjectsController`](../../backend/EngineeringPerformance.Api/Controllers/ProjectsController.cs), [`CreateProjectRequest`](../../backend/EngineeringPerformance.Application/Projects/DTOs/CreateProjectRequest.cs).

Права: `Projects` Read/Edit. Карточка одного проекта: ещё `[RequireProjectAccess]`.

| Метод | Путь | Назначение |
|-------|------|------------|
| GET | `/api/projects?includeArchived=` | Список |
| GET | `/api/projects/{id}` | Карточка |
| POST | `/api/projects` | Создать |
| PUT | `/api/projects/{id}` | Изменить |
| POST | `/api/projects/{id}/archive` | Архив |
| POST | `/api/projects/{id}/unarchive` | Восстановить |
| DELETE | `/api/projects/{id}` | Удалить |
| GET | `/api/projects/export/excel\|pdf` | Каталог |
| GET | `/api/projects/{id}/export/excel\|pdf` | Отчёт версии |
| GET | `/api/projects/{id}/export/combined.pdf` | Сводный PDF |
| POST | `/api/projects/import/excel` | **501** — не реализовано |

## Тело create/update

`code` (уникален), `name`. Опционально: `workNumber`, `contractNumber`, `calendarPlanNumber`, `type`, `customer`, `contractor`, `gip`, `workStatus`, `workStart` (date-only), `program`.

Конфликт кода: **409**.

## Характеристики проекта

`GET|PUT /api/projects/{id}/characteristics` — выборы `{ characteristicId, optionId }`. Справочник характеристик: slug `project-characteristics`, опции `/api/characteristics/{id}/options`.

## ACL

`GET|PUT|DELETE /api/projects/{id}/permissions` — кто видит проект.
