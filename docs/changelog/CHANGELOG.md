# Changelog

## [Unreleased]

### Added (2026-05-06, Phase 3 — forecast export, version comparison, reports tab)
- `IProjectForecastExportService` / `ProjectForecastExportService` — экспорт прогноза трудозатрат в Excel (ClosedXML): 3 листа — «Сценарии», «Плановые объёмы», «ТОП дисциплин».
- `ProjectsPageViewModel.OpenForecastReportAsyncCommand` — реализован экспорт прогноза с `SaveFileDialog`; ранее была заглушка «Экспорт прогноза будет реализован в Фазе 3».
- `ProjectsPageViewModel`: команды `SelectCompareVersionACommand`, `SelectCompareVersionBCommand` — выбор двух версий для сравнения и расчёт Δ Z-score по всем направлениям ИИС в `CompareRows`.
- `ProjectsPageViewModel`: команды `ExportFactAnalysisToExcelCommand`, `ExportFactAnalysisToPdfCommand`, `ExportForecastToExcelCommand` — прямой экспорт через `SaveFileDialog` с вкладки «Отчёты».
- `VersionCompareRow` — модель строки сравнения версий (ZScoreA, ZScoreB, ZDelta, InterpretationA/B).
- `App.xaml.cs`: зарегистрирован `IProjectForecastExportService`.

### Changed (2026-05-06, Phase 3 — UI)
- `ProjectsPageView.xaml`, вкладка «Сравнение версий»: реализована полноценная вкладка — панель выбора двух версий (Версия А / Версия Б) + `TreeListView` с колонками Z-score А, Оценка А, Z-score Б, Оценка Б, Δ Z-score; заголовки версий динамические (берутся из ViewModel).
- `ProjectsPageView.xaml`, вкладка «Отчёты»: реализована полноценная вкладка — три карточки-группы: «Оценка факта» (просмотр на экране + Excel + PDF), «Прогноз трудозатрат» (Excel), «Сравнение версий» (ссылка на вкладку).

### Added (2026-05-06, Projects page improvements)
- `ProjectPickerDialog` / `ProjectPickerDialogViewModel` — новый диалог выбора проекта: только список проектов (без версий), тулбар с кнопками добавить/редактировать/удалить/в архив/из архива, чекбокс «Показывать архивные», поиск по коду и названию, двойной клик для выбора.
- `ProjectQuickEditDialog` / `ProjectQuickEditDialogViewModel` — вложенный диалог ввода кода и наименования проекта (вызывается из `ProjectPickerDialog`).
- `ProjectDataDialog` / `ProjectDataDialogViewModel` / `IProjectDataDialogService` — объединённый диалог «Основные данные»: содержит все поля из блока «Основные данные проекта» и все поля из «Дополнительных данных» в одном окне.
- `ProjectParticipantsDialog` / `ProjectParticipantsDialogViewModel` / `IProjectParticipantsDialogService` — диалог участников проекта с тулбаром добавления (через стандартный `ProjectParticipantEditWindow`) и кнопкой удаления.
- `IProjectPickerDialogService` / `ProjectPickerDialogService` — сервис показа нового диалога выбора проекта.

### Changed (2026-05-06, Projects page improvements)
- `ProjectsPageViewModel`: кнопка «Выбрать проект» теперь вызывает `ProjectPickerDialog` (только выбор проекта) вместо `ProjectSelectionDialog` (проект + версия); зависимость `IProjectEditDialogService` заменена на `IProjectPickerDialogService`, `IProjectDataDialogService`, `IProjectParticipantsDialogService`.
- `ProjectsPageViewModel.OpenProjectDataCommand`: открывает `ProjectDataDialog` (основные + доп. поля) вместо `ProjectEditWindow`.
- `ProjectsPageViewModel.OpenParticipantsCommand`: открывает `ProjectParticipantsDialog` вместо `IProjectParticipantEditDialogService.ShowEditDialogAsync`.
- `ProjectsPageView.xaml`: кнопка «Выбрать проект» получила `IsEnabled="True"` явно — теперь всегда активна и не блекнет.
- `ProjectCharacteristicSelectionEditWindow.xaml`: биндинг `SelectedCharacteristicId` добавлен `UpdateSourceTrigger=PropertyChanged` — исправлен баг, при котором список вариантов не подгружался при первом выборе характеристики.
- `App.xaml.cs`: зарегистрированы `ProjectPickerDialog`, `ProjectPickerDialogViewModel`, `IProjectPickerDialogService`, `ProjectDataDialog`, `ProjectDataDialogViewModel`, `IProjectDataDialogService`, `ProjectParticipantsDialog`, `ProjectParticipantsDialogViewModel`, `IProjectParticipantsDialogService`.

### Added (2026-05-06, Phase 2 — Projects page redesign)
- `ProjectsPageView.xaml` / `ProjectsPageView.xaml.cs` — новая страница «Проекты»: заменяет `ProjectsWindow` в навигации; содержит панель выбора проекта (код, название, отклонение), кнопки «Выбрать проект», «Основные данные», «Характеристики», «Участники», а ниже — внутренний `TabControl` с 4 вкладками.
- `ProjectsPageViewModel` — ViewModel страницы: управляет выбором проекта, версий (план/факт), загрузкой данных производительности и прогноза, вызовом всех диалогов.
- Вкладка «Прогнозирование»: кнопка выбора плановой версии + таблица с 7 колонками (Наименование, Ед.изм., Факт ФО, Норм.производит., Базовый, Оптимист., Пессимист.); вычисляется: Base=FactFO×NormP, Opt=FactFO×EXP(NormE-0.5·NormStd), Pes=FactFO×EXP(NormE+0.5·NormStd).
- Вкладка «Оценка факта»: кнопка выбора версии факта + текущая таблица производительности + кнопки импорта/удаления ФО и трудоёмкости + «Анализ версии проекта».
- `ForecastPerformanceItem` — модель строки прогноза.
- `NullConverters.cs` — 3 конвертера: `NullToCollapsed`, `NullToBool`, `BoolToVisibility`.
- `App.xaml` — регистрация новых конвертеров в ресурсах приложения.
- `NavigationService.CreateEntry`: «Projects» теперь создаёт `ProjectsPageView` + `ProjectsPageViewModel` вместо `ProjectsWindow`.
- `App.xaml.cs`: зарегистрированы `ProjectsPageViewModel` (Singleton) и `ProjectsPageView` (Transient).

### Changed (2026-05-06, Phase 1 — UI redesign)
- `App.xaml`: добавлены стили `PageTabItemStyle` и `PageTabControlStyle` — плоские вкладки в стиле референса (активная — синяя `#3498DB`, иконка + текст, hover-эффект).
- `SettingsPageView.xaml` / `SettingsPageView.xaml.cs`: `DXTabControl` заменён на стандартный WPF `TabControl` с новым стилем.
- `ReferencesPageView.xaml` / `ReferencesPageView.xaml.cs`: аналогичная замена; при смене вкладки с уже загруженным контентом вызывается `LoadAsync` для обновления данных.
- `MainViewModel.BuildNavigation`: убраны пункты «Отчет» (`VersionComparison`) и «Данные» (`DataPage`); порядок пунктов меняется на: Главная, Проекты, Справочники, Настройки, Анализ проекта, Прогноз по проекту.
- `ReferencesPageViewModel.BuildTabs`: добавлена вкладка «Нормы ИИС» (`IicWorkDirectionNormsWindow`) — норм групп физ. объёмов (IIC) перенесена из DataPage в Справочники.
- `DataPageViewModel.BuildTabs`: вкладка «Нормы групп физ. объёмов (IIC)» удалена.
- `ProjectCharacteristicSelectionEditViewModel.OnSelectedCharacteristicIdChanged`: исправлен баг — после замены коллекции `Options` теперь явно вызывается `OnPropertyChanged(nameof(IsOptionsAvailable))`, что немедленно включает второй выпадающий список в диалоге характеристик.
- `ProjectCharacteristicSelectionEditViewModel.InitializeAsync`: `OnPropertyChanged(nameof(IsOptionsAvailable))` также вызывается после установки `SelectedCharacteristicId` при редактировании существующей строки.

### Changed (2026-05-06, physical volume import)
- `ProjectPhysicalVolume`: добавлены 4 новых поля — `WorkId` (nvarchar 200, «ID работы полный»), `WorkName` (nvarchar 1000, «Наименование работы»), `CatalogCode` (nvarchar 100, шифр из справочника), `CatalogName` (nvarchar 1000, наименование из справочника).
- `DataImportService.PhysicalVolumeColumnMap`: добавлено поле `WorkId`; маппинги обновлены — `SpisokRabot` (col H=8), `FromEpCatalogColumn` (e+2), `LegacyTemplate` (0 = нет колонки).
- `DataImportService.PhysicalVolumeImportDraft`: добавлены поля `WorkId`, `WorkName`, `CatalogCode`, `CatalogName`; `FromRow` принимает `PhysicalVolume catalogEntry` для денормализации `CatalogCode`/`CatalogName`; поля участвуют в `MergeFrom`, `ApplyTruncation`, `ApplyTo`, `ToNewEntity`.
- `DataImportService.PhysicalVolumeImportDraft.FromRow`: `FacilityName` больше не перегружается значением «Наименование работы» — теперь хранится отдельно в `WorkName`; `FacilityName` хранит только название объекта из FBS.
- `ProjectPhysicalVolumeConfiguration`: добавлены ограничения `HasMaxLength` для всех четырёх новых полей.
- EF-миграция `20260505212529_AddProjectPhysicalVolumeWorkFields` — добавляет 4 колонки в таблицу `ProjectPhysicalVolumes`.

### Changed (2026-05-06)
- `PerformanceService`: фильтр агрегации `volumeByDirection` и `unallocatedPhysicalVolume` ограничен строками с `PhysicalVolume.IsAssessment == true` — строки «Нет» и «Не выбрано» исключены из всех Z-score расчётов и итоговой строки «(пусто)».
- `ProjectForecastService`: аналогичный фильтр `IsAssessment == true` применён к `volumeByDirection` — в прогнозы трудоёмкости попадают только строки с отметкой «Оценка = Да».
- `ProjectVersionAnalysisService`: фильтр `IsAssessment == true` добавлен к `pvByDirection` — в детальную таблицу физических объёмов отчёта «Анализ версии проекта» включаются только строки с отметкой «Оценка = Да».
- `PerformanceServiceTests`, `ProjectVersionAnalysisServiceTests`, `ProjectForecastServiceTests`, `ProjectAnalysisServiceTests`, `AnalysisVersionSnapshotTests`: seed-данные физических объёмов дополнены `IsAssessment = true` — все 103 теста проходят.
- `docs/features/physical-volume-aggregation.md`: обновлено правило агрегации — добавлен раздел «Фильтр IsAssessment» с описанием логики и типовым C#-паттерном; карта точек вывода расширена колонкой «Фильтр IsAssessment»; заголовок файла обновлён.

### Changed
- `ProjectVersionAnalysisWindow`: название проекта (`ProjectNameWithVersion`) уменьшено с 13 до 16 pt (на 2 пункта меньше заголовка «Анализ версии проекта» — 18 pt).
- `ProjectVersionAnalysisWindow`, секция IIC: добавлена строка-заголовок колонок и итоговая строка «ИТОГО по проекту» с суммами ФО и трудозатрат по корневым узлам.
- `ProjectVersionAnalysisWindow`, карточка направления: блок физ. объёмов и трудоёмкости переработан — два грида рядом (50%/50%); таблица ФО дополнена колонками «Коэфф.» и «Итого»; пропорции колонок ФО: Код 7% / Номенклатурная позиция 60% / Ед. изм. 7% / Кол-во 7% / Коэфф. 7% / Итого 12%; пропорции колонок трудоёмкости: Шифр документации 75% / План, ч 25%. Подсветка жёлтым: «Факт ФО» если ФО нет но трудоёмкость есть; «Факт труд., ч» если трудоёмкость нет но ФО есть.
- `ProjectVersionAnalysisWindow`: блок «Виды работ (трудоёмкость проекта)» внизу окна удалён — трудоёмкость теперь отображается в карточке каждого направления.
- `ProjectVersionAnalysisWindow`, таблица ФО: пропорции колонок скорректированы через `Width="N*"` — Код 7% / Номенклатурная позиция 60% / Ед. изм. 7% / Кол-во 7% / Коэфф. 7% / Итого 12%.
- `ProjectVersionAnalysisWindow`, таблица трудоёмкости: пропорции колонок скорректированы — Шифр документации 75% / План, ч 25%.

### UX / Интерпретация / Прогноз (2026-05-05, итерация 2)
- `AssessmentForm`, блок «Точность оценки»: под шкалой точности добавлен `GridControl` с характеристиками проекта (имя + выбранная опция + PD%) и средним по PD в подвале. Пусто при отсутствии характеристик.
- `ProjectAnalysisReportDto`: новое поле `AccuracyCharacteristics: IReadOnlyList<AccuracyCharacteristicDto>`. `ProjectAnalysisService.GenerateReportAsync` формирует список из `ProjectCharacteristicSelectionService.GetByProjectAsync`, среднее PD% вычисляется из того же набора.
- Перцентили PEPM: шкала переведена на линейный маппинг `[-1, 1] → [0, 100]%` (10 равных корзин по 0,2 + клэмп за границами). Обновлены: `ProjectAnalysisService.GetPercentile`, `InterpretationSettingsView.xaml` (раздел 3), `docs/features/project-analysis.md`.
- Прогноз: `ProjectForecastService.ComputeBaseHours` / `ComputeScenarioHours` явно работают через `NormEstimate` в лог-пространстве — `base = vol × exp(NormEstimate)`, `opt/pes = base × exp(∓0.5 × NormStdDev)`. Поведение алгебраически совпадает с прежним, но формула и комментарии теперь отражают «оценка — это натуральный логарифм производительности». Добавлен раздел «Forecast Scenarios (since 2026-05-05)» в `docs/features/performance-analytics.md`.
- Тесты: новый `ProjectAnalysisServiceTests` (13 параметризованных + 3 факт-теста по перцентилям и характеристикам) и регрессия `ProjectForecastServiceTests.GenerateForecast_ScenarioHours_FollowNormEstimateWithHalfStdDeviation`. Итог: **103/103** пройден.

### UX / Reports (2026-05-05)
- Главная вкладка: стартовая заглушка с крупной KPI-плиткой «Активные проекты» (проекты со статусом `«В работе»`, учёт прав текущего пользователя через `IProjectVisibilityService`).
- Настройки → вкладка «Интерпретация»: справочник порогов PEPM / Z-оценки / перцентиля / точности сравнения / рекомендаций (`InterpretationSettingsView` + `InterpretationSettingsViewModel`).
- Отчёт «Анализ версии проекта»: кнопка на `ProjectEditWindow` под блоком «Производительность» + модальное окно `ProjectVersionAnalysisWindow` с полной иерархией IIC (шифр, название, ед. изм.), ненулевыми физ. объёмами по направлениям (номенклатура, ед. изм., количество), блоком видов работ из трудоёмкости проекта и показателями производительности / Z-оценкой; сервис `IProjectVersionAnalysisService` + DTO `ProjectVersionAnalysisDto`.
- `AssessmentForm` — реструктуризация «Информация о проекте»: добавлены крупные KPI-плитки PEPM / Перцентиль / Общие трудозатраты, перенесено «Количество дисциплин», удалены дубли «Точность сравнения» / «Оценка точности»; блок «Точность оценки» — только шкала точности + пояснение.
- `PerformanceService.BuildHierarchy` — `FactEstimate` для каждого узла теперь средне-взвешенное по трудозатратам по всему поддереву; `Z-score` и `Interpretation` пересчитываются по агрегированной оценке. `NormEstimate` остаётся напрямую из снимка норм, без доп. расчётов. Изменение прозрачно распространяется и на блок «Производительность» в `ProjectEditWindow`, и на дисциплины AssessmentForm (один источник данных).
- Документация: `docs/features/project-version-analysis.md`; раздел «Weighted-Average FactEstimate (since 2026-05-05)» в `docs/features/performance-analytics.md`; `docs/ui/dashboard.md` (новая); обновлены `docs/features/project-analysis.md` (Accuracy Score → PD%, новая ASCII-схема AssessmentForm) и `docs/features/project-modal-redesign.md` (упоминание кнопки «Анализ версии проекта»).
- Тесты: `PerformanceServiceTests` дополнен регрессиями на средне-взвешенную `FactEstimate` родительских узлов и пересчёт `Z-score` по агрегированной оценке; добавлен новый `ProjectVersionAnalysisServiceTests` (заголовок отчёта, фильтрация плейсхолдера, группировка физ. объёмов по направлениям с отбросом нулевых / без `ClassifierId`, сортировка видов работ, ошибка при неизвестной версии). Итог: **86/86** пройден.

### Changed (2026-05-05, UI)
- `MainWindow.xaml`: Sidebar Header — иконка приложения (`logo.png`, та же что на заставке) заменила символ «☰»; добавлена подпись «Управление производительностью»; высота блока выровнена с правым Header через `Grid.IsSharedSizeScope="True"` + `SharedSizeGroup="HeaderRow"` на обеих строках.
- `MainWindow.xaml`: иконка окна/трея изменена с `logo.png` (JPEG) на `logo.ico` — корректный формат для панели задач Windows; то же для `SplashWindow.xaml`.

### Changed (2026-05-05, tab state)
- `NavigationService`: кэш View-экземпляров (`_viewCache`) — при повторном переходе на вкладку возвращается тот же `UserControl`, а не создаётся новый; `LoadAsync()` вызывается только при первом посещении вкладки за сеанс (`_loaded` HashSet); при ошибке `LoadAsync` запись из `_loaded` удаляется, чтобы следующий переход повторил попытку.
- `App.xaml.cs`: `AssessmentFormViewModel`, `PlanningFormViewModel`, `ProjectDashboardViewModel` переведены с `AddTransient` на `AddSingleton` — ViewModel живёт весь сеанс, состояние (выбранный проект, загруженный отчёт, несохранённые объёмы) сохраняется при уходе с вкладки.

### Added (2026-05-05)
- `ProjectVersionAnalysisWindow`: кнопки «Excel» (зелёная) и «PDF» (красная) в шапке окна рядом с «Обновить» и «Закрыть»; экспортируют полный отчёт версии проекта через `SaveFileDialog`.
- `IProjectVersionAnalysisExportService` / `ProjectVersionAnalysisExportService` — сервис экспорта: Excel (ClosedXML, 3 листа: «Направления IIC», «Физ. объёмы», «Трудоёмкость»); PDF (PdfSharp 6.2.0, сводная страница + детальная страница на каждое направление с ненулевыми данными).
- `PDFsharp 6.2.0` добавлен в `EngineeringPerformance.Infrastructure`.
- `ProjectVersionAnalysisViewModel`: команды `ExportToExcelCommand` / `ExportToPdfCommand` с `CanExecute` (активны после загрузки отчёта, блокируются во время экспорта).
- `docs/features/physical-volume-aggregation.md` — документация правила агрегации физических объёмов: все аналитические точки вывода обязаны использовать `Quantity × Coefficient` (через `QuantityInIicGroupUnits()`), не сырое `Quantity`; карта всех точек вывода с указанием статуса.

### Added (2026-04-30)
- `ProjectCharacteristicSelection` — сущность выбора характеристик проекта (ProjectId, CharacteristicId, OptionId, уникальный индекс на пару); EF-миграция `20260429235517_AddProjectCharacteristicSelections`, SQL `013_AddProjectCharacteristicSelections.sql`.
- `ProjectCharacteristicSelectionService` — CRUD + `GetByProjectAsync` → `ProjectCharacteristicSelectionRow`.
- `ProjectCharacteristicsDialogViewModel` / `ProjectCharacteristicSelectionEditViewModel` — диалог выбора характеристик с delta-сохранением и фильтрацией уже выбранных.
- `ProjectCharacteristicsDialog.xaml`, `ProjectCharacteristicSelectionEditWindow.xaml` — Views для управления характеристиками проекта.
- `IProjectCharacteristicsDialogService` / `ProjectCharacteristicsDialogService` — сервисы диалогов с корректной цепочкой owner.
- `ProjectEditWindow.xaml`: кнопка «Характеристики» + `Border` «Отклонение по проекту: X%»; `ProjectEditViewModel`: `ProjectDeviationPercent`, `LoadProjectDeviationAsync`, `OpenCharacteristicsAsync`.
- `ProjectCharacteristic` / `ProjectCharacteristicOption` — сущности справочника характеристик; сид `migrations/data/008_ProjectCharacteristics.sql` (6 характеристик, 13 опций, UTF-16-LE-BOM); вкладка «Характеристики проекта» на странице справочников.
- `ComparisonAccuracy.cs` (Core) — helper расчёта точности сравнения по PD%: <7% Очень высокая / <9% Высокая / <11% Стандартная / ≥11% Пониженная.
- `IDirectoryCache<T>` — обёртка кэша для всех справочников (включая LaborIntensity); EF Compiled Model (`UseModel(AppDbContextModel.Instance)`).

### Changed (2026-04-30)
- `ApplyFilters` во всех справочниках: bulk-replace через `Items = new ObservableCollection<>()` вместо поэлементной вставки → загрузка вкладок 3–260 ms (было до 1671 ms, ускорение 88×).
- `MergeItemsPreservingReferences` / `RefreshItemInCollection`: замена `Items[i] = updated` (Replace event) вместо in-place мутации — корректное обновление грида во всех справочниках.
- Паттерн «(не выбрано)» через sentinel `Id = 0` в `ComboBoxEdit` применён ко всем диалогам справочников с иерархическими списками; в «Номенклатуре физ. объёмов» — для единиц измерения.
- Сид-файлы БД переведены в кодировку UTF-16-LE-BOM (sqlcmd авто-детектирует) — исправлено mojibake при чтении UTF-8 как CP1251.
- `ProjectCharacteristicOption`: удалена колонка `ComparisonAccuracy` (теперь расчётная через `ComparisonAccuracy.cs`); миграция `20260429223256_DropProjectCharacteristicOptionAccuracy`, SQL `012_DropProjectCharacteristicOptionAccuracy.sql`.

### Removed
- `LaborIntensity.NodeLevel`, `LaborIntensity.DocumentTypeName`, `LaborIntensity.TexasCode` — удалены из сущности, конфигурации, EF-миграции `20260429132548_AddLaborIntensity` (+ Designer/Snapshot), SQL-схемы `migrations/schema/010_AddLaborIntensity.sql`, сидов `migrations/data/004_LaborIntensity.sql`, `LaborIntensityEditViewModel`, `LaborIntensitiesViewModel`. Индекс `IX_LaborIntensities_NodeLevel` удалён; сортировка в `LaborIntensityDirectoryService.GetAllAsync` переведена на `SortOrder`/`Code`/`Name`.

### Added
- `PhysicalVolumes.Coefficient` — перевод количества по проекту в единицы группы физ. объёмов ИИС; SQL `migrations/schema/007_PhysicalVolumeCoefficient.sql`; EF `20260419213543_AddPhysicalVolumeCoefficient`
- Документация: раздел про `007` в `docs/database/migration.md`; поле `Coefficient` в `docs/database/entities/PhysicalVolume.md`; уточнение формулы в `docs/features/performance-analytics.md`
- Папка `install/` — инструкция по развёртыванию БД (`README.md`) и скрипт `Migrate.ps1` для запуска миграций из корректного каталога `migrations/`
- Двухчастная полная миграция: `migrations/FullMigration_01_Structure.sql` (DDL), `migrations/FullMigration_02_DataImport.sql` (данные); `000_MasterMigration.sql` подключает обе части
- Каталоги `migrations/schema/` (начальная схема и консолидированный DDL) и `migrations/data/` (сид и крупные импорты)
- `ProjectEditWindow` — redesigned project modal with three-column layout (project data, analysis versions, project managers)
- Auto-save functionality for project data fields with 500ms debounce timer
- Project Managers CRUD block with add/edit/delete via `ProjectManagerEditWindow` dialog
- Performance tab with Z-score analysis (`PerformanceService`) — hierarchical tree display of work directions
- Excel import/export for Physical Volumes (`DataImportService.ImportPhysicalVolumesAsync`)
- Excel import/export for Labor Intensities (`DataImportService.ImportLaborIntensitiesAsync`)
- `DataImportIteration` entity — tracks import batches for versioned data management and batch-level deletion
- `DataImportType` enum — PhysicalVolume (1), LaborIntensity (2)
- `PerformanceRowDto` record — Z-score calculation result with interpretation
- `PerformanceInterpretation` enum — Excellent, Good, Satisfactory, NeedsAttention, Critical with Russian localization
- `ProjectEditViewModel` — comprehensive ViewModel with auto-save, version management, manager CRUD, import/delete commands
- `ProjectManagerEditViewModel` — user selection dialog with search filter for project manager assignment
- `ProjectManagerItem` and `PerformanceItem` display models
- Entity documentation for `DataImportIteration`
- Feature documentation: `project-modal-redesign.md`, `performance-analytics.md`, `data-import.md`
- `PhysicalVolume.DocumentSet` property — document package/kit field (nullable)
- `PhysicalVolume.ProjectPhysicalVolumes` navigation collection
- `PhysicalVolume.LaborIntensities` navigation collection
- `ProjectLaborIntensity.PhysicalVolumeId` foreign key column → `PhysicalVolumes.Id`
- `ProjectLaborIntensity.PhysicalVolume` nullable navigation property
- Entity relationships documentation (`docs/database/entity-relationships.md`)
- Entity documentation for `PhysicalVolume`, `ProjectPhysicalVolume`, `ProjectLaborIntensity`

### Changed
- `install/README.md` — состав цепочек `FullMigration_01` / `02`, генератор `005`, `dotnet ef` как альтернатива
- `install/Migrate.ps1` — расширенная справка `.DESCRIPTION` / доп. `.EXAMPLE`
- `docs/database/migration.md` — актуальные сиды (`002`–`005`), `003` как заглушка, `schema` с `007`, исправления в описании индексов `AddPhysicalVolumeRelationships`
- Документация `002`: голые проекты PRJ-25-* без версий анализа и без демо `ProjectUserAccess`
- Структура SQL-миграций: файлы `001`/`006_Consolidated` перенесены в `migrations/schema/`, скрипты данных `002`–`007` — в `migrations/data/`; обновлён `docs/database/migration.md`
- `ProjectPhysicalVolumeConfiguration` — updated `WithMany` to use named inverse property, added index on `CatalogId`
- `PhysicalVolumeDto` — added `DocumentSet` parameter
- `ProjectLaborIntensity` relationship: FK changed from `Brand` → `DocumentSet` (alternate key) to `PhysicalVolumeId` → `PhysicalVolumes.Id` (standard FK); `DocumentSet` ↔ `Brand` is now a business-level match only

### Database
- Migration `20260419213543_AddPhysicalVolumeCoefficient`: column `PhysicalVolumes.Coefficient` (decimal 18,4, default 1)
- Migration `20260415193900_AddPhysicalVolumeRelationships`:
  - Added `PhysicalVolumeId` column (int, nullable) to `ProjectLaborIntensities` table
  - Added `DocumentSet` column (nvarchar(100), nullable) to `PhysicalVolumes` table
  - Added foreign key `FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId` (`PhysicalVolumeId` → `PhysicalVolumes.Id`)
  - Added indexes on `ProjectPhysicalVolumes.CatalogId`, `ProjectLaborIntensities.Brand`, `ProjectLaborIntensities.PhysicalVolumeId`
