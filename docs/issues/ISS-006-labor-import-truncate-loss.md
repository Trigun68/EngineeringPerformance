# ISS-006: Импорт LI теряет часы из-за truncate DocumentationCode + затирающего merge

**Статус:** 🟡 In progress (фикс реализован 2026-05-11, ожидает пере-импорта файла фактических данных и UI-подтверждения)
**Категория:** Bug / DataImport / LaborIntensity
**Приоритет:** High

## Контекст
Импорт трудоёмкости (`25-310 Трудоемкость прокта.xlsx`, проект 4, версия v2) недосчитывал **~3 122 ч planned** (≈ 2 830 эффективных) из 61 725 ч.

Диагностика подтвердила:
- В файле 1 008 строк с непустым типом документа, **0 дублей** по исходным `(DocType, Brand)`.
- После `DocumentationCode[..100]` — 860 уникальных ключей. Совпадает с `LiCount` в БД.
- 267 строк имеют `DocType+|+Brand` длиннее 100 символов → после обрезки коллизии.
- 27 групп ≥2 строки. Топ потерь:
  - `VS_04Определение количества и типа приборов…` — 22 строки, потеря 344 ч.
  - `HS_02Тепловой расчет помещений…` — 22 строки, потеря 192 ч.
  - `IE_02Общие принципы защиты и обогрева…` — 15 строк, потеря 164 ч.

## Причины
1. **Truncate `DocumentationCode` до 100 символов** в `DataImportService.cs` (строки 533-539).
   Префикс «тип документа» уже занимает >100 символов, поэтому разные «марки» (`3U0000-ОП-АТХ1` vs `4L0000-…`) после обрезки совпадали по натуральному ключу.
2. **Затирающий merge** в `mergedByNaturalKey[(key)] = entity;` — последняя строка с этим ключом перезаписывала предыдущую вместо суммирования часов.
3. То же поведение в блоке update-existing-in-db — но после фикса (1) и (2) merge подаёт уже суммированные значения.

## Решение (вариант: truncate + суммирование)
1. **Схема БД:** `nvarchar(100)` → `nvarchar(450)` для `ProjectLaborIntensities.DocumentationCode`.
   Лимит индекса SQL Server 1700 байт ≈ 850 nvarchar — 450 безопасно.
   - EF миграция `20260511204756_ExpandLaborDocumentationCodeTo450`.
   - SQL-скрипт `migrations/schema/016_ExpandLaborDocumentationCodeTo450.sql`.
2. **Импортёр** (`EngineeringPerformance.Infrastructure/Services/DataImportService.cs`):
   - Лимит truncate: 100 → 450.
   - Извлечён метод `MergeLaborRowsByNaturalKey(IEnumerable<ProjectLaborIntensity>)`,
     который на коллизии суммирует `PlannedHours`/`ActualHours`, оставляя Brand/Notes/LaborIntensityId от первой строки.
   - Сообщение в `details` переписано на «дубли суммируются по плановым и фактическим часам».
3. **Test:** `DataImportServiceTests.MergeLaborRowsByNaturalKey_DuplicateKey_SumsPlannedAndActualHours` —
   три строки с одним натуральным ключом → одна запись с суммой плановых (24.75) и фактических (5).
   Доп. тест на различные ключи и `WorkDirectionId = NULL`.

## Acceptance Criteria
- [x] Схема `ProjectLaborIntensities.DocumentationCode` расширена до `nvarchar(450)` (EF миграция + SQL-скрипт).
- [x] `DataImportService` суммирует часы на коллизии по натуральному ключу, тесты зелёные.
- [x] Сообщение пользователю отражает суммирование, а не «схлопывание».
- [ ] После пере-импорта `25-310 Трудоемкость прокта.xlsx` в Факт v2:
      `SELECT SUM(PlannedHours) FROM ProjectLaborIntensities WHERE ProjectId=4 AND ProjectAnalysisVersionId=2` = **61 725.40**
      (сейчас 58 895.24).
- [ ] В UI вкладка «Итого» Факта v2 отображает 61 725 ч плана.

## Файлы
- `EngineeringPerformance.Infrastructure/Data/Configurations/ProjectLaborIntensityConfiguration.cs` — длина 450.
- `EngineeringPerformance.Infrastructure/Services/DataImportService.cs` — truncate 450, статический `MergeLaborRowsByNaturalKey`, обновлённое сообщение.
- `EngineeringPerformance.Infrastructure/Migrations/20260511204756_ExpandLaborDocumentationCodeTo450.cs` (+ Designer).
- `migrations/schema/016_ExpandLaborDocumentationCodeTo450.sql`.
- `EngineeringPerformance.Tests/DataImportServiceTests.cs` — два теста merge-логики.

## Не делать
- Не возвращать truncate до 100 — потеряем уникальность снова.
- Не суммировать строки с `WorkDirectionId = NULL` — уникальный индекс отфильтрован, исторические записи без направления уйдут как отдельные.

## Связанные
- `tmp/ISS-006-handoff.md` — handoff-контекст диагностики.
