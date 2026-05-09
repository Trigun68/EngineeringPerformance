# Changelog

## [Unreleased]

### Added (2026-05-09, log Login / LoginFailed events)
- `EngineeringPerformance.Infrastructure/Services/AuthenticationService.cs`: внедрён `IUserActionLogService`. `LoginAsync` логирует `Login` (успех) и `LoginFailed` (известный пользователь, неверный пароль; для неизвестных имён — только Serilog, т.к. UserId FK Restrict). `TryAutoLoginAsync` логирует `Login` для авто-входа по Windows-учётке.

### Added (2026-05-09, user action log service)
- `EngineeringPerformance.Infrastructure/Services/UserActionLogService.cs`: реализация `IUserActionLogService`. `LogAsync` best-effort (ошибки пишутся в Serilog и не бросаются), `GetAsync` с фильтром (период UTC, UserId, ActionType, EntityType, EntityId) и пагинацией, `ExportToFileAndPurgeAsync` (CSV UTF-8 BOM, разделитель `;`, RFC 4180-экранирование; файл пишется перед `ExecuteDeleteAsync` — при ошибке IO БД нетронута), `ImportFromFileAsync` с дедупликацией по `(UserId, Timestamp, ActionType, EntityType, EntityId)` и пропуском orphan-записей (UserId отсутствует в БД).
- `EngineeringPerformance.App/App.xaml.cs`: регистрация `IUserActionLogService → UserActionLogService` как Singleton.

### Added (2026-05-09, user action log core contracts)
- `EngineeringPerformance.Core/Enums/UserActionType.cs`: enum категорий действий (Login/Logout/LoginFailed, Project CRUD, Version CRUD, PhysicalVolume Import/Delete/Edit, LaborIntensity Import/Delete/Edit, IicNormEdit) — имена сохраняются в БД как nvarchar(64).
- `Core/DTOs/UserActionLogDto.cs`: DTO записи журнала (включает `UserName` для отображения в гриде).
- `Core/DTOs/UserActionLogFilter.cs`: фильтр выборки (период UTC, пользователь, тип действия, сущность).
- `Core/DTOs/UserActionLogIoResults.cs`: `UserActionLogExportResult` и `UserActionLogImportResult` (счётчики дублей/orphan/невалидных + список ошибок).
- `Core/Services/IUserActionLogService.cs`: контракт сервиса — `LogAsync` (best-effort, не бросает наружу), `GetAsync` (с пагинацией), `ExportToFileAndPurgeAsync` (атомарно), `ImportFromFileAsync` (с дедупликацией).

### Added (2026-05-09, user action log schema)
- `EngineeringPerformance.Infrastructure/Data/Entities/UserActionLog.cs`: новая сущность журнала действий — `Id`, `UserId` (FK Users, Restrict), `Timestamp` (UTC), `ActionType` nvarchar(64), `EntityType` nvarchar(100) NULL, `EntityId` int NULL, `Description` nvarchar(500) NULL, `IpOrHost` nvarchar(64) NULL.
- `Infrastructure/Data/Configurations/UserActionLogConfiguration.cs`: EF-конфигурация с тремя индексами (`UserId+Timestamp`, `ActionType`, `EntityType+EntityId`).
- `Infrastructure/Data/AppDbContext.cs`: `DbSet<UserActionLog> UserActionLogs` + регистрация конфигурации.
- EF миграция `AddUserActionLogs` + SQL-дубль `migrations/schema/015_AddUserActionLogs.sql`.
- ADR `docs/adr/ADR-002-user-action-journal.md` (схема + UI + правила экспорта/импорта в CSV).
- Backlog `docs/issues/ISS-001-user-action-journal-auto-archival.md` — авто-архивация на будущее.

### Fixed (2026-05-09, detail grid: focus, scroll and groups preserved on edit reload)
- `ProjectsPageView.xaml`, `DetailForecastGrid` и `FactDetailGrid`: добавлены `RestoreStateOnSourceChange="True"` и `RestoreStateKeyFieldName="ProjectPhysicalVolumeId"` — DevExpress теперь сам сохраняет фокусированную строку и раскрытие групп при смене `ItemsSource` (нативный механизм, исключает мигание из-за двойного рендера и потерю фокуса при ручном `FocusedRowHandle`).
- `ProjectsPageView.xaml.cs`, `RestoreGridState`: переписан — убран ранний выход `if (state.ExpandedGroupTexts.Count == 0) return;` (скрытый баг — позиция скролла не восстанавливалась без группировки), убраны ручные операции `ExpandAllGroups` / `CollapseGroupRow` (управляются DevExpress), убрано ручное присваивание `FocusedRowHandle`. Метод теперь восстанавливает только `TopRowIndex` — относительно фокусированной строки или абсолютно.
- `ProjectsPageView.xaml.cs`, `SaveGridState`: убран сбор `ExpandedGroupTexts` (DevExpress сам сохраняет состояние групп).
- `ProjectsPageViewModel.cs`, `DetailGridState`: удалено поле `ExpandedGroupTexts` (больше не используется).

### Fixed (2026-05-08, preserve detail forecast grid state on reload after edit)
- `ProjectsPageViewModel.LoadForecastDataAsync`: убран лишний `SaveDetailForecastStateCallback?.Invoke()` внутри метода и второй `BeginInvoke` — они выполнялись после смены `ItemsSource` (состояние всегда пустое) и перетирали корректный `savedState` из команды редактирования.
- `ProjectsPageView.xaml.cs`, `RestoreGridState`: логика восстановления изменена с «CollapseAll → раскрыть нужные» на «ExpandAll → схлопнуть лишние» — после `CollapseAllGroups` `VisibleRowCount` резко падал и часть групп не находилась для раскрытия.

### Added (2026-05-08, preserve detail grid state on reload)
- `ProjectsPageViewModel`: делегаты `SaveDetailForecastStateCallback`/`RestoreDetailForecastStateCallback` и аналогичные для `FactDetailData` — вызываются вокруг замены `ItemsSource` при перезагрузке данных после редактирования.
- `ProjectsPageView.xaml.cs`: методы `SaveGridState`/`RestoreGridState` — сохраняют свёрнутые группы (по значению) и позицию скролла (`TopRowIndex`) перед перезагрузкой, восстанавливают через `Dispatcher.BeginInvoke` после привязки новых данных.
- `DetailGridState`: класс-контейнер состояния (множество свёрнутых значений групп + позиция скролла).

### Fixed (2026-05-08, version label format)
- `ProjectsPageViewModel.BuildVersionLabel`: исправлены артефакты кодировки в строковых литералах — метка версии теперь отображается в формате `(Дата) Название: Комментарий` на всех вкладках (Прогнозирование, Оценка факта, Сравнение версий); исправлен fallback `"-"` для интерпретации Z-score на вкладке Сравнение.

### Changed (2026-05-08, hide analysis direction field in version edit dialog)
- `ProjectAnalysisVersionEditDialogService`: метод `ShowEditDialogAsync` получил параметр `direction` — передаёт направление анализа напрямую в VM, исключая ручной выбор пользователем.
- `ProjectAnalysisVersionEditViewModel.PrepareSync`: принимает `direction?` и устанавливает `SelectedDirection` автоматически; при редактировании существующей версии сохраняет переданное направление.
- `ProjectVersionPickerDialogViewModel`: `AddVersionAsync` и `EditVersionAsync` передают `Direction` в `ShowEditDialogAsync` — диалог открывается всегда с правильным направлением (Plan/Fact) в зависимости от вкладки.
- `ProjectAnalysisVersionEditWindow.xaml`: убраны поле «Направление анализа» (label + ComboBox); высота окна уменьшена с 560 до 490.

### Fixed (2026-05-07, detail tab total row via grid TotalSummary)
- `ProjectsPageViewModel`, `LoadForecastDataAsync` / `LoadFactDataAsync`: убраны ручные итоговые строки (`IsTotal=true`) из `DetailForecastData` и `FactDetailData`.
- `ProjectsPageView.xaml`, `DetailForecastGrid` и `FactDetailGrid`: итог реализован через `GridControl.TotalSummary` (Sum по трём колонкам часов + Count строк) и `TableView.ShowTotalSummary="True"` — нативный механизм DevExpress, не искажает данные при сортировке/группировке/фильтрации.

### Fixed (2026-05-07, forecast modal: PV tables INotifyPropertyChanged + PDF pagination)
- `ForecastDirectionViewModel`: убран `SetPhysicalVolumes` — `PhysicalVolumes` теперь передаётся напрямую в конструктор, что исключает проблему `INotifyPropertyChanged` (WPF не видел изменение `private set` свойства после асинхронной загрузки).
- `ProjectForecastReportViewModel`: `SetForecastData` теперь сначала загружает ФО из БД (`LoadAndBuildAsync`), затем создаёт `ForecastDirectionViewModel` с уже готовыми данными — таблицы ФО в карточках отображаются корректно.
- `BuildForecastSummaryPdf`: исправлен `break` при переполнении страницы — теперь добавляется новая страница с повтором заголовка колонок (`EnsureSpace`); `gfx.Dispose()` вызывается явно перед `doc.Save`.

### Fixed (2026-05-07, forecast modal: PV tables, PDF export, excel alignment)
- `ProjectForecastReportViewModel`: добавлена асинхронная загрузка физических объёмов (`LoadPhysicalVolumesAsync`) после `SetForecastData` — таблицы ФО в карточках направлений теперь заполнены данными.
- `ForecastDirectionViewModel`: добавлены `Id` и метод `SetPhysicalVolumes` — позволяет заполнить `PhysicalVolumes` после загрузки из БД.
- `IProjectForecastExportService` / `ProjectForecastExportService`: добавлен метод `ExportForecastSummaryToPdfAsync` — PDF-экспорт из модального окна «Прогноз трудозатрат» теперь работает.
- `BuildForecastSummaryExcel`: исправлено смещение итоговых строк — часы теперь пишутся в колонки 6,7,8 (совпадают с заголовками «Базовый», «Оптимист.», «Пессимист.»); заголовочный merge скорректирован с 7 до 8 колонок.

### Fixed (2026-05-07, forecast summary modal and excel align with tab)
- `ProjectForecastReportViewModel`: переписан — больше не выполняет самостоятельную загрузку данных через `PerformanceService`; принимает уже готовую коллекцию `ForecastData` через `SetForecastData()` из `ProjectsPageViewModel`. Итоговые часы берутся из той же итоговой строки `Level=-1`.
- `ProjectsPageViewModel.OpenForecastReport`: передаёт `ForecastData` в модальное окно вместо `LoadAsync()` — данные всегда совпадают с вкладкой «Итого».
- `ForecastPerformanceItem`: добавлено поле `Code` (шифр направления); заполняется в `BuildSubtree` из `WorkDirectionCode`.
- `ForecastSummaryRowDto` (новый DTO): передаёт строки вкладки «Итого» в сервис экспорта.
- `IProjectForecastExportService` / `ProjectForecastExportService`: добавлен метод `ExportForecastSummaryToExcelAsync` — строит Excel из `ForecastSummaryRowDto`, отражая ту же иерархию и итоги что на вкладке «Итого».
- `ExportForecastToExcelAsync`: переключён на `ExportForecastSummaryToExcelAsync(ForecastData)` вместо `GenerateReportAsync` — Excel теперь совпадает с вкладкой.

### Fixed (2026-05-07, facility forecast total row)
- `ProjectsPageViewModel`: убрана ручная итоговая строка (`IsTotal=true`) из `FacilityForecastData` и `FactFacilityData` — она попадала в Excel и удваивала итог (Excel сам считал сумму по всем строкам включая итоговую).
- `ProjectsPageView.xaml`, `FacilityForecastGrid` и `FactFacilityGrid`: итог реализован через `GridControl.TotalSummary` (Sum по трём колонкам часов + Count по объектам) и `TableView.ShowTotalSummary="True"` — нативный механизм DevExpress, не искажает данные при сортировке/группировке.
