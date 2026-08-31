# Сценарий внешнего модуля

Дата обновления: 2026-08-24.

Полный цикл без Excel. Все шаги — существующие маршруты плюс опциональный JSON-ingest.

1. `POST /api/auth/login` → Bearer.
2. Справочники: `GET /api/dictionaries/all`; при необходимости CRUD `/api/directories/{slug}` и нормы `/api/registers/iic-work-direction-norms`.
3. `POST /api/projects` (`code` + `name`).
4. `PUT /api/projects/{id}/characteristics`.
5. `POST /api/projects/{id}/versions` — `direction`, обязательный `physicalVolumeTypeId` (КФО/БЕР).
6. `PUT …/planning-parameters` — Z по ИИС.
7. Данные версии:
   - точечно: `POST …/physical-volumes|equipment|labor-intensities` (внутренние Id);
   - пакетом: `POST …/ingest/physical-volumes|equipment|labor-intensities` (шифры/имена, как Excel).
8. `POST …/recalculate` — если не сработал автопересчёт после ФО/оборудования.
9. `GET …/readiness` — пока нет критичных `issues`.
10. Чтение аналитики (по правам): `page-data`, `performance`, `compare`, тренды.

Excel остаётся для UI: те же таблицы, replace, итерации. Не смешивать в одной версии «тихий» upsert JSON и последующий Excel-replace — Excel сотрёт тип целиком.

Подробности: [projects.md](projects.md), [versions.md](versions.md), [row-crud.md](row-crud.md), ingest-*.md.
