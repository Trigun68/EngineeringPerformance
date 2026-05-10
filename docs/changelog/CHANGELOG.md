# Changelog

## [Unreleased]

### Changed (2026-05-10, ADR-003 — расширение уникального ключа ProjectPhysicalVolume)
- `EngineeringPerformance.Infrastructure/Data/Configurations/ProjectPhysicalVolumeConfiguration.cs`: уникальный составной индекс `ProjectPhysicalVolumes` расширен с 3 колонок `(ProjectAnalysisVersionId, ProjectId, PhysicalVolumeId)` до 6 `(… + FacilityCode, FacilityName, WorkId)`. Бизнес-причина: одна номенклатура ФО может встречаться на разных объектах/работах одного проекта — старый ключ блокировал сценарий «Добавить копированием» из ISS-003. Решение зафиксировано в `docs/adr/ADR-003-project-physical-volume-unique-key.md`. Строгий UNIQUE без filtered index (NULL = NULL → блокирует дубликаты).
- `EngineeringPerformance.App/ViewModels/ProjectPhysicalVolumeCreateViewModel.cs`: сообщение об ошибке при нарушении уникального индекса обновлено — теперь указывает на комбинацию «номенклатура ФО + код/наименование объекта + ID работы» и предлагает изменить любое из этих полей.
- `docs/issues/ISS-003-detail-grid-context-menu-add-delete.md`: статус → ✅ Closed с резюме реализации.

### Fixed (2026-05-09, ISS-003 unique violation message)
- `EngineeringPerformance.App/ViewModels/ProjectPhysicalVolumeCreateViewModel.cs`: при попытке сохранить копию `ProjectPhysicalVolume` с той же номенклатурой ФО в той же версии проекта пользователь видел сырое сообщение EF Core «An error occurred while saving the entity changes. See the inner exception for details.» — без указания причины. Бизнес-правило БД: уникальный индекс `(ProjectAnalysisVersionId, ProjectId, PhysicalVolumeId)` запрещает дубликаты номенклатуры в одной версии проекта. Добавлен метод `IsUniqueViolation`, который проходит по цепочке `InnerException` и ищет ключевые слова `UNIQUE`/`duplicate key`. При срабатывании показывается понятное русское сообщение: «Строка с такой номенклатурой ФО в данной версии проекта уже существует. Выберите другую номенклатуру в выпадающем списке.». Для прочих ошибок остаётся общий fallback с текстом исключения.

### Added (2026-05-09, ISS-003 detail grid context menu CRUD)
- `EngineeringPerformance.App/Views/ProjectsPageView.xaml.cs`: в `OnDetailForecastShowGridMenu` добавлены 3 пункта после разделителя — «Удалить выбранную строку», «Добавить строку копированием выбранной», «Добавить строку». Подключены к новым командам VM.
- `EngineeringPerformance.App/Views/ConfirmDialog.xaml(.cs)` + `EngineeringPerformance.App/ViewModels/ConfirmDialogViewModel.cs`: универсальный confirm-диалог проектного стиля (header `#2C3E50`, белый body, кнопки Confirm/Cancel) с биндингом `Title`/`Message`/`ConfirmButtonText`/`CancelButtonText`. Заменяет паттерн `MessageBox.Show` для подтверждений.
- `EngineeringPerformance.App/Services/ConfirmDialogService.cs`: `IConfirmDialogService.ShowAsync(title, message, confirmText, cancelText)` → `Task<bool>`. Owner-окно через `SetOwnerWindow` с фолбэком на `Application.Current.MainWindow`.
- `EngineeringPerformance.App/Views/ProjectPhysicalVolumeCreateDialog.xaml(.cs)` + `EngineeringPerformance.App/ViewModels/ProjectPhysicalVolumeCreateViewModel.cs`: модальное окно создания `ProjectPhysicalVolume` с двумя режимами (`forCopy`/пусто) через `Prepare(forCopy, source, projectId, versionId)`. Поля: `FacilityCode/Name`, `BindingType`, `DocumentSet`, `CatalogId`, `Tag`, `WorkId/Name/Comment`, `EstimationBasis`, `Quantity`. Иерархический lookup `PhysicalVolume` (по `Parent/Children`, отступы `└` по depth, фильтр `IsArchived=false`). Валидация: `PhysicalVolumeId != null && Quantity > 0`. Связанные/вычисляемые поля (`IsAssessment`, `Coefficient`, `IicGroupName/UnitName`, `NormProductivity/StdDev`, `*Hours`) намеренно не показываются.
- `EngineeringPerformance.App/Services/ProjectPhysicalVolumeCreateDialogService.cs`: `IProjectPhysicalVolumeCreateDialogService.ShowAsync(forCopy, source, projectId, versionId)` → `Task<bool>`. Шаблон фабрики аналогичен `IIterationDeleteDialogService`.
- `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs`: 3 новые команды — `DeleteProjectPhysicalVolumeCommand` (confirm → `IDataTableService.DeleteAsync` → reload с `savedState`), `CreateProjectPhysicalVolumeFromCopyCommand`, `CreateProjectPhysicalVolumeCommand`. Все используют паттерн `state = SaveDetailForecastStateCallback?.Invoke(); ... await LoadForecastDataAsync(pid, vid, state)` — focus и scroll сохраняются после операций. Логирование в `UserActionLog`: `PhysicalVolumeDelete` и `PhysicalVolumeCreate`. Ошибки удаления (FK-ограничения) показываются через `IConfirmDialogService` как информационное окно с текстом СУБД.
- `EngineeringPerformance.Core/Enums/UserActionType.cs`: добавлено значение `PhysicalVolumeCreate = 33` для аудита создания строки `ProjectPhysicalVolume` (`PhysicalVolumeDelete = 31` уже существовало).
- `EngineeringPerformance.App/App.xaml.cs`: DI — `ConfirmDialogViewModel`/`ConfirmDialog` (Transient) + `IConfirmDialogService` (Singleton); `ProjectPhysicalVolumeCreateViewModel`/`ProjectPhysicalVolumeCreateDialog` (Transient) + `IProjectPhysicalVolumeCreateDialogService` (Singleton).

### Added (2026-05-09, ISS-003 backlog)
- `docs/issues/ISS-003-detail-grid-context-menu-add-delete.md`: backlog-задача — 3 новых пункта в контекстное меню `DetailForecastGrid` (Удалить выбранную строку / Добавить копированием / Добавить пустую). Включает требования пользователя, acceptance criteria, стартовые точки для новой сессии (паттерны для копирования: `PhysicalVolumeQuickEditDialog`, `IterationDeleteDialog`, `EditProjectPhysicalVolumeAsync`), список «не делать» (не редактировать связанные поля, не использовать `MessageBox`, не терять focus/scroll).

### Chore (2026-05-09, ProjectsPageViewModel.cs encoding + warnings)
- `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs`: файл перекодирован UTF-8-mojibake → корректный UTF-8 без BOM (раньше был с 9 повторяющимися BOM-маркерами и двойной перекодировкой UTF-8 → CP1251 → UTF-8, из-за чего русские строки отображались как `РћС€РёР±РєР° Р·Р°РіСЂСѓР·РєРё`). Перекодировка через strict `cp1251.GetBytes()` подтвердила нулевую потерю данных (`lost_chars: 0`). Размер файла 68436 → 64129 байт. Теперь error messages, контекстные комментарии и разделители в UI рендерятся корректно (например, «Ошибка загрузки данных прогноза» вместо mojibake).
- `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs`: добавлен `_ =` перед `Dispatcher.BeginInvoke` в `LoadForecastDataAsync` (строка 515) и `LoadFactDataAsync` (строка 767). Гасит 4 пред-существующих warning CS4014 («вызов не ожидается»). Сборка теперь даёт **0 предупреждений**.

### Changed (2026-05-09, ISS-002 closed)
- `docs/issues/ISS-002-detail-grid-flicker-revisit.md`: статус → ✅ Closed после UI-подтверждения пользователя. Сценарии редактирования (Forecast/Fact, IIC-норма, удаление строки) проходят без вздрагиваний и потери фокуса. Микро-оптимизации (`BeginDataUpdate/EndDataUpdate`, in-place `Add/Remove`) переведены в backlog как не требующиеся.
- `docs/features/detail-grid-state-preservation.md`: раздел «Что осталось решить» заменён на «Итог» с фиксацией закрытия ISS-002.

### Fixed (2026-05-09, ISS-002 iteration 2 — detail grid flicker)
- `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs`: в `LoadFactDataAsync` убран дублирующий restore-pass детальной сетки факта. Раньше внутри `Load*` повторно снимался `factState = SaveFactDetailStateCallback?.Invoke()` и через второй `Dispatcher.BeginInvoke` перетирал восстановление, переданное вызывающей командой (`EditProjectPhysicalVolumeAsync` и т.п.). Это был остаток от отклонённого 2026-05-08 анти-паттерна (см. таблицу истории в feature-doc, строка 01db0b7). Теперь Fact-путь идентичен Forecast-пути — единственный restore идёт от `savedState`, переданного из команды редактирования. Убирает один из источников «вздрагивания» на вкладке «Оценка факта → Детально».
- `EngineeringPerformance.App/Views/ProjectsPageView.xaml.cs`: `RestoreGridState` стал устойчив к уменьшению `VisibleRowCount`. Если `FocusedRowId` отсутствует в новом снимке (строка удалена/отфильтрована/перешла под другую группу), вместо безусловного `tv.TopRowIndex = state.TopRowIndex` теперь выполняется `Math.Clamp` к диапазону `[0, VisibleRowCount-1]`. Дополнительно: при `VisibleRowCount == 0` ставится `0` без поиска — экран не «прыгает» в случайную позицию.
- `EngineeringPerformance.App/Views/ProjectsPageView.xaml`: к `CompareGrid` добавлены `RestoreStateOnSourceChange="True"` и `RestoreStateKeyFieldName="Id"`. При смене Версии А или Б на вкладке «Сравнение версий» сохраняется focus и раскрытие узлов TreeListView (ключ — `WorkDirectionId`, стабильный между версиями одного проекта).

### Added (2026-05-09, detail grid state preservation docs)
- `docs/features/detail-grid-state-preservation.md`: документация метода сохранения focus/scroll/групп в детальных сетках Forecast/Fact — комбинация `RestoreStateOnSourceChange="True"` + `RestoreStateKeyFieldName="ProjectPhysicalVolumeId"` (DevExpress нативно для focus и групп) и ручного `SaveGridState`/`RestoreGridState` (только вертикальный скролл через якорь `FocusedRowId` + `FocusedVisibleOffset`). Включена таблица истории эволюции — что пробовали и почему отклоняли.
- `docs/issues/ISS-002-detail-grid-flicker-revisit.md`: backlog-задача на следующую сессию — устранить остаточные «вздрагивания» и возможную потерю фокуса. План проверок: видеозапись сценария, альтернативы смене `ItemsSource` (`Add/Remove`, `BeginDataUpdate`/`EndDataUpdate`), fallback при удалённой строке, унификация с `CompareGrid`. Definition of Done и список «не делать».

### Fixed (2026-05-09, populate empty AddUserActionLogs migration)
- `EngineeringPerformance.Infrastructure/Migrations/20260509112303_AddUserActionLogs.cs`: предыдущая ревизия имела пустой `Up()`/`Down()` — `dotnet ef database update` не создавал таблицу `UserActionLogs`, и при первом обращении приложение падало `Invalid object name 'UserActionLog'`. Заполнено: `CreateTable("UserActionLogs")` с PK, FK на `Users` (Restrict) и тремя индексами (`UserId+Timestamp`, `ActionType`, `EntityType+EntityId`); `Down()` делает `DropTable`. Если миграция уже отмечена применённой в `__EFMigrationsHistory` — применить SQL-скрипт `migrations/schema/015_AddUserActionLogs.sql` напрямую (на dev-БД уже применено).

### Added (2026-05-09, user action log feature docs and tests)
- `docs/features/user-action-journal.md`: документация фичи — схема БД, категории действий, точки внедрения логирования (таблица VM/сервисов), контракт `IUserActionLogService`, формат CSV, правила доступа, что НЕ реализовано (авто-архив, old/new value, Logout).
- `EngineeringPerformance.Tests/UserActionLogServiceTests.cs`: 8 unit-тестов на сервис журнала — `LogAsync` (вставка, no-throw), `GetAsync` (фильтр по типу/пользователю/периоду, сортировка по `Timestamp DESC`), `ExportToFileAndPurge` (CSV-заголовок, экранирование `;`/`"`, удаление записей), `ImportFromFile` (дедупликация, orphan UserId, неверный заголовок).

### Changed (2026-05-09, user action log purge implementation)
- `EngineeringPerformance.Infrastructure/Services/UserActionLogService.cs`: метод `ExportToFileAndPurgeAsync` переписан с `ExecuteDeleteAsync` на `RemoveRange` + `SaveChangesAsync` — обеспечивает совместимость с EF InMemory provider в тестах. Журнал короткий (ограничен ручной архивацией), производительность приемлема.

### Added (2026-05-09, user action log UI tab)
- `EngineeringPerformance.Core/AccessControlConstants.cs`: добавлен объект доступа `UserActionLog` (родитель `SettingsPage`, отображение «Журнал действий пользователей»).
- `EngineeringPerformance.App/ViewModels/UserActionLogViewModel.cs`: VM журнала. Фильтры (период по умолчанию −30 дней, пользователь, тип действия), пагинация на 1000 строк, команды `RefreshAsync`, `ExportAndPurgeAsync` (Save+Delete с подтверждением и проверкой роли Administrator), `ImportAsync` с агрегированным отчётом (всего/добавлено/дубли/orphan/невалидные + первые 10 ошибок). Свойство `IsNotBusy` для блокировки тулбара.
- `EngineeringPerformance.App/Views/UserActionLogView.xaml(.cs)`: UserControl с панелью фильтров (DateEdit ×2, ComboBoxEdit ×2), тулбаром (Обновить / Сохранить и удалить / Восстановить / статус-сообщение), DevExpress GridControl с колонками Time UTC, User, Action, Entity, Id, Description, Host. Текстовое сообщение об ошибке внизу (NullToCollapsed-конвертер).
- `EngineeringPerformance.App/App.xaml.cs`: DI-регистрация `UserActionLogView` (Transient) + `UserActionLogViewModel` (Singleton).
- `EngineeringPerformance.App/ViewModels/SettingsPageViewModel.cs`: третья вкладка «Журнал действий» в `BuildTabs` после «Пользователи» и «Права пользователей»; видна только при `CanRead(user, "UserActionLog")`.

### Added (2026-05-09, log detail-grid quick edits)
- `EngineeringPerformance.App/ViewModels/ProjectsPageViewModel.cs`: внедрены зависимости `IUserActionLogService` и `CurrentUserHolder`. После успешного закрытия трёх диалогов быстрого редактирования детальной сетки пишется журнал: `EditProjectPhysicalVolumeAsync` → `PhysicalVolumeEdit` (EntityType=ProjectPhysicalVolume), `EditPhysicalVolumeNomenclatureAsync` → `LaborIntensityEdit` (EntityType=PhysicalVolume), `EditIicNormAsync` → `IicNormEdit` (EntityType=IicWorkDirectionNorm). Helper `LogDetailEditAsync` молчит для неавторизованного пользователя.

### Added (2026-05-09, log Version CRUD and Project edit/create from project edit dialog)
- `EngineeringPerformance.App/ViewModels/ProjectAnalysisVersionEditViewModel.cs`: после успешного `Save` пишет `VersionCreate` или `VersionEdit` (EntityType=ProjectAnalysisVersion, описание содержит `ProjectId`, имя версии и направление). Внедрены `IUserActionLogService` и `CurrentUserHolder`.
- `EngineeringPerformance.App/ViewModels/ProjectEditViewModel.cs`: добавлен helper `LogAsync` и логирование `ProjectCreate`/`ProjectEdit` в `Save`-команде, `VersionDelete` — в команде `DeleteVersion`. Подключена зависимость `IUserActionLogService` к существующему `CurrentUserHolder`.

### Added (2026-05-09, log Project CRUD and import-iteration delete)
- `EngineeringPerformance.App/ViewModels/ProjectPickerDialogViewModel.cs`: пишет `ProjectCreate` / `ProjectEdit` / `ProjectDelete` в журнал после соответствующих операций (EntityType=Project, EntityId=projectId, описание `Code Name`). Внедрены `IUserActionLogService` и `CurrentUserHolder`. Helper `LogProjectActionAsync` молчит, если пользователь не авторизован.
- `EngineeringPerformance.App/ViewModels/IterationDeleteDialogViewModel.cs`: после удаления итерации импорта пишет `PhysicalVolumeDelete` или `LaborIntensityDelete` (EntityType=ProjectAnalysisVersion, EntityId=VersionId, описание содержит `importedAt` и количество строк).

### Added (2026-05-09, log PhysicalVolume / LaborIntensity imports)
- `EngineeringPerformance.App/ViewModels/DataImportDialogViewModel.cs`: после успешного импорта пишется `PhysicalVolumeImport` или `LaborIntensityImport` (зависит от `DataType`) с указанием `EntityType=ProjectAnalysisVersion`, `EntityId=VersionId` и описанием (project/version/rows/filename). Внедрены зависимости `IUserActionLogService` и `CurrentUserHolder`.

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
