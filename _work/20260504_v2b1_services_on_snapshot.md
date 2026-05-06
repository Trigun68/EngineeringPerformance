# Session summary: V2-B.1 Сервисы на снимке норм

**Дата:** 2026-05-04
**Сессия:** `orch-2026-05-03-v2-migration`, фаза **V2-B.1**
**Статус:** ✅ завершено

## Цель

Подключить таблицу `AnalysisVersionSnapshots` (созданную в V2-B) к расчётным сервисам. До этой фазы таблица была наполнена, но ни один сервис её не читал — V2-B миграция «висела вхолостую».

## Что сделано

### Сервисы

**`Infrastructure/Services/PerformanceService.cs`**
Заменил runtime-фильтр по справочнику норм на чтение из снимка версии:

```csharp
// было
var norms = await context.IicWorkDirectionNorms.AsNoTracking()
    .Where(n => n.EffectiveFrom <= versionCreatedAt
                && (n.EffectiveTo == null || n.EffectiveTo >= versionCreatedAt))
    .ToListAsync();
// группировка с OrderByDescending(EffectiveFrom).First() для перекрытий

// стало
var snapshotNorms = await context.AnalysisVersionSnapshots.AsNoTracking()
    .Where(s => s.ProjectAnalysisVersionId == versionId)
    .Include(s => s.Norm)
    .Select(s => s.Norm)
    .ToListAsync();
var normByDirection = snapshotNorms.ToDictionary(n => n.WorkDirectionId);
```

Уникальный индекс `UX_AnalysisVersionSnapshots_Version_WorkDirection` гарантирует одну норму на узел в рамках версии — `GroupBy + OrderByDescending(EffectiveFrom).First()` больше не нужен.

**`Infrastructure/Services/ProjectForecastService.cs`**
Аналогично: убран `versionCreatedAt`, runtime-фильтр и недетерминированный `g.First()` в группировке норм. Чтение из `AnalysisVersionSnapshots`.

### Автонаполнение снимка при создании версии

**`Infrastructure/Services/ProjectAnalysisVersionDirectoryService.cs`** — переопределён `CreateAsync`:

1. Сохраняет `ProjectAnalysisVersion` (как делал базовый класс).
2. Запускает `PopulateSnapshotInternalAsync`: выбирает из `IicWorkDirectionNorms` все записи, где `EffectiveFrom ≤ CreatedAt AND (EffectiveTo IS NULL OR EffectiveTo ≥ CreatedAt)`, группирует по `WorkDirectionId`, оставляет одну с самой свежей `EffectiveFrom` (детерминизм при перекрытиях), вставляет в `AnalysisVersionSnapshots`.
3. Один `SaveChangesAsync` в конце — обе вставки в одной транзакции контекста.

Дополнительно: публичный `RebuildSnapshotAsync(versionId)` — удаляет старые строки снимка и пересоздаёт их по справочнику. Используется в интеграционных тестах и при ручном восстановлении данных.

### Регрессионные тесты

**`Tests/AnalysisVersionSnapshotTests.cs`** — 4 новых теста:

| Тест | Что проверяет |
|---|---|
| `CreateAsync_PopulatesSnapshotForActiveNorms` | После `CreateAsync` снимок содержит ровно одну строку для направления с действующей нормой; направление без норм пропущено без падений |
| `CreateAsync_PicksMostRecentEffectiveFrom_WhenOverlappingNormsExist` | При двух перекрытых нормах для одного `WorkDirectionId` снимок берёт ту, у которой свежее `EffectiveFrom` (не `g.First()`) |
| `PerformanceService_UsesSnapshot_NotLatestNormAddedAfterVersion` | Создание новой нормы (B) после версии с нормой (A) в снимке не меняет `NormProductivity` исторического Z-score-отчёта |
| `ProjectForecastService_UsesSnapshot_NotLatestNorm` | Тот же сценарий для forecast — `PlannedVolumes[].NormProductivity` остаётся «зафиксированным» |

### Верификация

- `dotnet build EngineeringPerformance.slnx` — Success
- `dotnet test` — **75/75 PASSED** (было 71, +4 новых)

## Известные ограничения (residual drift risk)

Снимок хранит `NormId` (FK), а не значения `Performance/Estimate/StandardDeviation`. Поэтому:

- ✅ **Защищено**: добавление новой нормы (новый ряд `IicWorkDirectionNorms`), закрытие старой (`EffectiveTo`), любые изменения справочника, не затрагивающие конкретный ряд.
- ❌ **Не защищено**: in-place редактирование значений в существующем ряду через `IicWorkDirectionNormService.UpdateAsync` — `Include(s => s.Norm)` следует FK к тому же ряду и подтягивает свежие значения.

Это совпадает с дизайн-решением V2-B (дешёвый снимок только по FK). Если нужна полная иммутабельность исторических отчётов, есть два пути для отдельной фазы:

1. Запретить in-place edit в `IicWorkDirectionNormService.UpdateAsync` — любое изменение значения создаёт новый ряд (`EffectiveFrom`/`EffectiveTo` history-only) и в UI остаётся `IicWorkDirectionNormHistoryEditViewModel`.
2. Расширить `AnalysisVersionSnapshot` колонками `Performance/Estimate/StandardDeviation/UnitId` и копировать значения при создании снимка (отдельная V2-B.2 миграция).

Рекомендация: вариант 1 — соответствует уже существующей идее «нормы версионируются по EffectiveFrom», без новой миграции.

## Изменённые файлы

```
EngineeringPerformance.Infrastructure/
  Services/
    PerformanceService.cs                           (norms ← snapshot)
    ProjectForecastService.cs                       (norms ← snapshot)
    ProjectAnalysisVersionDirectoryService.cs       (CreateAsync override + RebuildSnapshotAsync)

EngineeringPerformance.Tests/
  AnalysisVersionSnapshotTests.cs                   (new, 4 тестов)

tmp/active/orch-2026-05-03-v2-migration/progress.json  (status: v2b1-completed)
```

## Откат

Откат не затрагивает БД (миграция V2-B не менялась). Чтобы вернуть прежнее поведение сервисов:

```bash
git revert <commit-with-v2b1>
```

Снимок версий в БД остаётся — он совместим со старыми сервисами (просто не читается).

## Что дальше

Из плана `orch-2026-05-03-v2-migration` остались pending:

- **V2C** — `ProjectPermission` (замена `ProjectUserAccess` + `Visibility`).
- **V2D** — FK `PhysicalVolumeId` (PPV NOT NULL, PLI NULL).

Опционально перед V2C:
- **V2-B.2** (если решим закрыть residual drift) — запретить in-place edit норм или расширить снимок значениями.
