# Session summary: V2-B AnalysisVersionSnapshot

**Дата:** 2026-05-04
**Сессия:** `orch-2026-05-03-v2-migration`, фаза **V2-B**
**Статус:** ✅ завершено

## Цель

Новая таблица `AnalysisVersionSnapshots` — снимок норм, действовавших на момент создания версии анализа проекта. Исторические Z-score-отчёты перестают «плыть» при правке справочника `IicWorkDirectionNorms`.

## Что сделано

### Сущность и конфигурация

**`Infrastructure/Data/Entities/AnalysisVersionSnapshot.cs`** — новая сущность:
- `Id`, `ProjectAnalysisVersionId`, `WorkDirectionId`, `NormId`
- Навигации: `ProjectAnalysisVersion`, `WorkDirection`, `Norm`

**`Infrastructure/Data/Configurations/AnalysisVersionSnapshotConfiguration.cs`** — новая конфигурация:
- Уникальный индекс `UX_AnalysisVersionSnapshots_Version_WorkDirection (ProjectAnalysisVersionId, WorkDirectionId)` — одна норма на узел в рамках версии
- FK `ProjectAnalysisVersion` → Cascade (удаление версии удаляет её снимок)
- FK `WorkDirection` → Restrict
- FK `Norm` → Restrict (не даёт удалить норму, если она кого-то снимком держит)

**`AppDbContext.cs`** — добавлен `DbSet<AnalysisVersionSnapshot> AnalysisVersionSnapshots` + `ApplyConfiguration`.

### Миграция

**`20260503213828_V2B_AnalysisVersionSnapshot`**:
- `CreateTable AnalysisVersionSnapshots`
- 3 индекса (уникальный + 2 FK-индекса)
- 3 FK с разными `DeleteBehavior`
- Кастомный `Sql()` в конце `Up()` — наполнение снимков для существующих версий:
  ```sql
  INSERT INTO AnalysisVersionSnapshots (ProjectAnalysisVersionId, WorkDirectionId, NormId)
  SELECT v.Id, n.WorkDirectionId, n.Id
  FROM ProjectAnalysisVersions v
  JOIN IicWorkDirectionNorms n
    ON n.EffectiveFrom <= v.CreatedAt
   AND (n.EffectiveTo IS NULL OR n.EffectiveTo >= v.CreatedAt);
  ```

### Верификация

- `dotnet build` — Success
- `dotnet test` — **71/71 PASSED**
- Данные после миграции:
  - 2 версии, 47 норм
  - **92 snapshot-строки** итого
  - Версия 1: 45 строк (2 нормы не действовали на её `CreatedAt` — это легитимно, т. к. добавлены после создания версии)
  - Версия 2: 47 строк (все нормы действовали)
  - Orphan-версий нет

## Что осталось из V2-B (вынесено в V2-B.1)

Переписать сервисы на использование снимка вместо runtime-match:

- **`PerformanceService.GetPerformanceDataAsync`** — сейчас делает runtime-join:
  ```
  Where(n.EffectiveFrom <= versionCreatedAt && (n.EffectiveTo == null || n.EffectiveTo >= versionCreatedAt))
  ```
  Должно стать:
  ```
  context.AnalysisVersionSnapshots
      .Where(s => s.ProjectAnalysisVersionId == versionId)
      .Include(s => s.Norm)
  ```

- **`ProjectForecastService.GenerateForecastAsync`** — аналогично (сейчас `g.First()` после фильтра по дате — недетерминировано).

- **Триггер наполнения снимка при создании новой версии**: добавить в `ProjectAnalysisVersionDirectoryService.CreateAsync` (или сервис `IAnalysisVersionSnapshotBuilder`) автосоздание снимков из актуальных норм на момент `CreatedAt`.

Без V2-B.1 таблица создана и наполнена, но приложение её пока не использует. Сервисы работают по-старому. Смысл миграции раскроется только после V2-B.1.

## Изменённые файлы

```
EngineeringPerformance.Infrastructure/
  Data/Entities/
    AnalysisVersionSnapshot.cs                 (new)
  Data/Configurations/
    AnalysisVersionSnapshotConfiguration.cs    (new)
  Data/AppDbContext.cs                         (+DbSet, +ApplyConfiguration)
  Migrations/
    20260503213828_V2B_AnalysisVersionSnapshot.cs  (new)
    + Designer.cs + AppDbContextModelSnapshot.cs

tmp/active/orch-2026-05-03-v2-migration/progress.json  (updated)
```

## Откат

```bash
dotnet ef database update 20260503213307_V2A1_WorkStartToDate --project EngineeringPerformance.Infrastructure --startup-project EngineeringPerformance.App
```
`Down()` дропает таблицу — снимки пропадают, но они были выведены из существующих данных и воспроизводимы.
