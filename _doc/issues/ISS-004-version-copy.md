# ISS-004: Копирование версии анализа с данными

**Статус:** ✅ Closed (UI-подтверждение пользователя 2026-05-10 после фикса снапшота норм IIC)
**Создано:** 2026-05-10
**Закрыто:** 2026-05-10
**Приоритет:** 🟡 Medium
**Связано:** `ProjectVersionPickerDialog`, `ProjectAnalysisVersion`, `ProjectPhysicalVolume`, `ProjectLaborIntensity`, `DataImportIteration`, `AnalysisVersionSnapshot`


## Уточнение 2026-08-20 (web)

Продуктовое правило: копия версии **наследует** `AnalysisVersionSnapshot` источника (`AnalysisVersionSnapshotService.CopyFromVersionAsync` / `CreateCopies`). Fallback `PopulateSnapshotAsync` только если у источника снимка нет. Исторические снимки существующих версий не переписываются.

## Контекст

На странице **Проекты** при выборе версии анализа (как на вкладке «Прогнозирование», так и на вкладке «Анализ факта») открывается модальное окно `ProjectVersionPickerDialog`. Пользователь просит добавить кнопку **«Копировать»** для создания новой версии с полным копированием данных.

## Что нужно

### Общее поведение

В `ProjectVersionPickerDialog` (единый для Plan и Fact, фильтр по `Direction`) добавить кнопку **«Копировать»** в тулбар. Flow:
1. Выделить source-версию → нажать «Копировать».
2. Открывается мини-диалог с полями **Имя** (предзаполнено `«{sourceName} (копия)»`) и **Описание** (пустое).
3. **Сохранить** → сервис копирования создаёт новую версию + копии строк → грид перезагружается, focus на новой версии.
4. **Отмена** → ничего не меняется.

### Вкладка «Прогнозирование» (Direction = Plan)

Копируются:
- `ProjectAnalysisVersion` → новая запись с тем же `Direction`, `PhysicalVolumeTypeId`, `ProjectId`.
- `ProjectPhysicalVolume` → все строки source-версии, Id новые, `ProjectAnalysisVersionId` = новая версия.
- `DataImportIteration` → все записи source-версии 1-в-1 (те же `ImportedAt`, `SourceFileHash`, `SourceFileName`, `RecordCount`, `DataType`, `ImportedByUserId`), `ProjectAnalysisVersionId` = новая версия.

НЕ копируются:
- `AnalysisVersionSnapshot` — пересоздадутся при первом расчёте по актуальным нормам IIC.

### Вкладка «Анализ факта» (Direction = Fact)

То же, что Plan, плюс:
- `ProjectLaborIntensity` → все строки source-версии, Id новые, `ProjectAnalysisVersionId` = новая версия.

## Решения (утверждены пользователем 2026-05-10)

| Вопрос | Ответ | Обоснование |
|---|---|---|
| Копирование `DataImportIteration` | 1-в-1 с теми же полями | Пользователь выбрал вариант «б» — точная копия аудит-истории |
| Копирование `AnalysisVersionSnapshot` | Не копировать | Пользователь выбрал вариант «б» — пересоздадутся при расчёте по актуальным нормам |
| UX flow | Однокнопочный | Пользователь выбрал вариант «а» — source из выделения в picker, мини-диалог Имя/Описание |

## Acceptance criteria

- [x] Кнопка «Копировать» в тулбаре `ProjectVersionPickerDialog` (enabled если выбрана версия).
- [x] Мини-диалог: поля «Имя» (предзаполнено) и «Описание», кнопки «Сохранить» / «Отмена».
- [x] Plan-версия: копируются PPV + DataImportIteration. PLI не копируются.
- [x] Fact-версия: копируются PPV + PLI + DataImportIteration.
- [x] `AnalysisVersionSnapshot` источника НЕ копируется (новый строится по актуальным нормам IIC — см. «Известный дефект 2026-05-10»).
- [x] Новая версия появляется в гриде после Save, focus на ней.
- [x] Логирование `UserActionType.VersionCopy` в `UserActionLog`.
- [x] CHANGELOG `[Unreleased]` обновлён.
- [x] `dotnet build` 0 ошибок 0 warnings.
- [x] `dotnet test` 111/111 green.
- [x] Запушено в `origin/master`.
- [x] UI-подтверждение пользователем (2026-05-10 после фикса снапшота).

## Стартовые точки

1. `ProjectVersionPickerDialog.xaml` + `ProjectVersionPickerDialogViewModel.cs` — сюда добавляем кнопку + команду.
2. `ProjectAnalysisVersionEditDialogService.cs` — паттерн dialog-service.
3. `ConfirmDialogService.cs` — паттерн простого диалога.
4. `ProjectPhysicalVolumeCreateDialogService.cs` — паттерн Transient VM + dialog.
5. `DataTableRecord.cs` — базовый класс PPV/PLI (Id, ProjectAnalysisVersionId, CreatedAt, ModifiedAt).
6. `DataImportIteration.cs` — сущность итерации импорта.
7. `App.xaml.cs:350-448` — DI-регистрация.

## Создаваемые файлы

- `EngineeringPerformance.Core/Services/IProjectAnalysisVersionCopyService.cs` — интерфейс сервиса копирования.
- `EngineeringPerformance.Infrastructure/Services/ProjectAnalysisVersionCopyService.cs` — реализация (транзакция).
- `EngineeringPerformance.App/Views/ProjectAnalysisVersionCopyDialog.xaml(.cs)` — мини-диалог.
- `EngineeringPerformance.App/ViewModels/ProjectAnalysisVersionCopyViewModel.cs` — VM диалога.
- `EngineeringPerformance.App/Services/ProjectAnalysisVersionCopyDialogService.cs` — сервис диалога.

## Не делать

- ❌ Не копировать `AnalysisVersionSnapshot`.
- ❌ Не создавать DataImportIteration с новыми `ImportedAt`/`ImportedByUserId` — копировать 1-в-1.
- ❌ Не изменять существующий `ProjectAnalysisVersionEditWindow` — отдельный мини-диалог.
- ❌ Не добавлять новые пакеты.

---

## Известный дефект 2026-05-10 (handoff для следующей сессии)

**Симптом (UI-проверка пользователя 2026-05-10):**
В созданной копированием версии (Plan и Fact) **не подтягиваются нормы IIC и не выполняется расчёт производительности**. Грид расчёта пуст. В логах ошибок нет — расчёт молча возвращает пустой результат.

**Корневая причина:**
В `ProjectAnalysisVersionDirectoryService.CreateAsync` (`EngineeringPerformance.Infrastructure/Services/ProjectAnalysisVersionDirectoryService.cs:19-30`) после INSERT новой версии вызывается `PopulateSnapshotInternalAsync` — он создаёт записи `AnalysisVersionSnapshot` (заморозку норм IIC по `WorkDirectionId` на момент `CreatedAt`). Без этих снапшотов `PerformanceService` не находит нормы для расчёта.

`ProjectAnalysisVersionCopyService.CopyVersionAsync` добавляет новую версию напрямую через `context.ProjectAnalysisVersions.Add()` **в обход** `CreateAsync` directory-сервиса. Снапшоты не создаются → расчёт пуст.

Решение по вопросу 2 («не копировать AnalysisVersionSnapshot, пересоздадутся при первом расчёте по актуальным нормам IIC») было принято с ложной предпосылкой: в текущей архитектуре снапшоты автоматически при расчёте НЕ пересоздаются. Они создаются только в `CreateAsync` или явно через `RebuildSnapshotAsync`.

**Фикс:**
В `ProjectAnalysisVersionCopyService.CopyVersionAsync` после `await context.SaveChangesAsync()` (когда `copy.Id` уже получен), но **до** `tx.CommitAsync`, сгенерировать снапшоты норм для новой версии на момент `copy.CreatedAt`. Логика — копия `PopulateSnapshotInternalAsync` из `ProjectAnalysisVersionDirectoryService.cs:60-85`:

```csharp
// Снапшот норм IIC для новой версии на момент copy.CreatedAt — без него PerformanceService
// не находит нормы (см. ISS-004 «Известный дефект»). Решение 2б остаётся в силе:
// нормы берутся актуальные на момент создания копии, а не замороженные у source.
var activeNorms = await context.IicWorkDirectionNorms
    .AsNoTracking()
    .Where(n => n.EffectiveFrom <= copy.CreatedAt
                && (n.EffectiveTo == null || n.EffectiveTo >= copy.CreatedAt))
    .ToListAsync(cancellationToken);

var pickedByDirection = activeNorms
    .GroupBy(n => n.WorkDirectionId)
    .Select(g => g.OrderByDescending(n => n.EffectiveFrom).First());

foreach (var norm in pickedByDirection)
{
    context.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
    {
        ProjectAnalysisVersionId = copy.Id,
        WorkDirectionId = norm.WorkDirectionId,
        NormId = norm.Id
    });
}
```

Это **уточнение** ADR/решения 2б, не его отмена: «AnalysisVersionSnapshot источника не копируется, но новый снапшот по актуальным нормам IIC создаётся в момент копирования».

**Альтернатива (отвергнута):** делегировать создание снапшота через `IDirectoryService<ProjectAnalysisVersion>.CreateAsync()`, а потом догружать данные. Минус — два SaveChanges вместо одной транзакции, и часть полей (`Direction`, `PhysicalVolumeTypeId`) пришлось бы заполнять отдельно. Прямая копия `PopulateSnapshotInternalAsync` в той же транзакции — короче и атомарнее.

**Definition of Done после фикса:**
- [x] `ProjectAnalysisVersionCopyService.CopyVersionAsync` создаёт `AnalysisVersionSnapshot` для новой версии до `tx.CommitAsync` (коммит `9dc0469`).
- [x] UI: в скопированной Plan-версии расчёт производительности отрабатывает (Forecast).
- [x] UI: в скопированной Fact-версии расчёт фактической производительности отрабатывает.
- [x] CHANGELOG `[Unreleased]` обновлён (Fixed).
- [x] `dotnet build` 0 ошибок 0 warnings, `dotnet test` 111/111 green.
- [x] Коммит `fix(infra): populate AnalysisVersionSnapshot on version copy (ISS-004)` запушен в `origin/master` (`9dc0469`).
- [x] UI-подтверждение пользователя (2026-05-10).
- [x] Статус ISS-004 → ✅ Closed после подтверждения.
