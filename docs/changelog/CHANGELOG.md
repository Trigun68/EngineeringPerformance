# Changelog

## [Unreleased]

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
