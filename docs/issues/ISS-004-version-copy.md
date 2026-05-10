# ISS-004: Копирование версии анализа с данными

**Статус:** 🔄 in-progress
**Создано:** 2026-05-10
**Приоритет:** 🟡 Medium
**Связано:** `ProjectVersionPickerDialog`, `ProjectAnalysisVersion`, `ProjectPhysicalVolume`, `ProjectLaborIntensity`, `DataImportIteration`

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

- [ ] Кнопка «Копировать» в тулбаре `ProjectVersionPickerDialog` (enabled если выбрана версия).
- [ ] Мини-диалог: поля «Имя» (предзаполнено) и «Описание», кнопки «Сохранить» / «Отмена».
- [ ] Plan-версия: копируются PPV + DataImportIteration. PLI не копируются.
- [ ] Fact-версия: копируются PPV + PLI + DataImportIteration.
- [ ] `AnalysisVersionSnapshot` НЕ копируется.
- [ ] Новая версия появляется в гриде после Save, focus на ней.
- [ ] Логирование `UserActionType.VersionCopy` в `UserActionLog`.
- [ ] CHANGELOG `[Unreleased]` обновлён.
- [ ] `dotnet build` 0 ошибок 0 warnings.
- [ ] `dotnet test` 111+/111+ green.
- [ ] Запушено в `origin/master`.
- [ ] UI-подтверждение пользователем.

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
