using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class DataImportDialogViewModel : ObservableObject
{
    private readonly IDataImportService _importService;
    private readonly IUserActionLogService _actionLog;
    private readonly CurrentUserHolder _currentUser;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private int _projectId;
    [ObservableProperty] private int _versionId;
    [ObservableProperty] private DataImportType _dataType;
    [ObservableProperty] private string? _selectedFilePath;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private DataImportResult? _importResult;

    public string WindowTitle => DataType switch
    {
        DataImportType.PhysicalVolume => "Импорт физических объёмов",
        DataImportType.Equipment => "Импорт данных оборудования",
        _ => "Импорт трудоёмкости",
    };

    public string DataTypeDisplayText => DataType switch
    {
        DataImportType.PhysicalVolume => "Физические объёмы",
        DataImportType.Equipment => "Оборудование",
        _ => "Трудоёмкость",
    };

    public DataImportDialogViewModel(
        IDataImportService importService,
        IUserActionLogService actionLog,
        CurrentUserHolder currentUser)
    {
        _importService = importService;
        _actionLog = actionLog;
        _currentUser = currentUser;
    }

    public void Initialize(int projectId, int versionId, DataImportType dataType, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        ProjectId = projectId;
        VersionId = versionId;
        DataType = dataType;
        ErrorMessage = null;
        ImportResult = null;
        SelectedFilePath = null;
        Description = null;
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*",
            Title = DataType switch
            {
                DataImportType.PhysicalVolume => "Выберите файл с физическими объёмами",
                DataImportType.Equipment => "Выберите файл с данными оборудования",
                _ => "Выберите файл с трудоёмкостью",
            },
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            if (string.IsNullOrWhiteSpace(Description))
                Description = Path.GetFileName(dialog.FileName);
            ErrorMessage = null;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            ErrorMessage = "Выберите файл для импорта";
            return;
        }

        var ownerWindow = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => ReferenceEquals(w.DataContext, this));

        if (ownerWindow is null)
        {
            ErrorMessage = "Не удалось определить окно импорта";
            return;
        }

        try
        {
            ErrorMessage = null;

            // Для трудоёмкости — предпросмотр несовпадений до записи в БД. Если в файле
            // есть «Типы документа», отсутствующие в справочнике, запрашиваем у пользователя
            // решение: добавить ли эти позиции в справочник (Yes), импортировать без
            // добавления (No) или отменить импорт (Cancel).
            var addMissingToCatalog = false;
            if (DataType == DataImportType.LaborIntensity)
            {
                var preview = await _importService.PreviewLaborImportAsync(SelectedFilePath);
                if (preview.UnmatchedDocumentTypes.Count > 0)
                {
                    var namesBlock = string.Join(Environment.NewLine, preview.UnmatchedDocumentTypes);
                    var promptText =
                        $"В файле найдено {preview.UnmatchedDocumentTypes.Count} «Типов документа», отсутствующих в справочнике «Трудоёмкость»:"
                        + Environment.NewLine + Environment.NewLine
                        + namesBlock
                        + Environment.NewLine + Environment.NewLine
                        + "Добавить эти позиции в справочник?"
                        + Environment.NewLine
                        + "    Да — добавить в справочник и выполнить импорт с привязкой,"
                        + Environment.NewLine
                        + "    Нет — импортировать без добавления в справочник,"
                        + Environment.NewLine
                        + "    Отмена — не выполнять импорт.";

                    var answer = MessageBox.Show(
                        ownerWindow,
                        promptText,
                        "Несовпадения со справочником «Трудоёмкость»",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (answer)
                    {
                        case MessageBoxResult.Yes:
                            addMissingToCatalog = true;
                            break;
                        case MessageBoxResult.No:
                            addMissingToCatalog = false;
                            break;
                        default:
                            // Cancel или закрытие окна крестиком — не производим запись в БД и закрываем диалог.
                            CloseDialog(false);
                            return;
                    }
                }
            }

            await LoadingWindow.ShowAsync(ownerWindow, async vm =>
            {
                vm.SetTitle(WindowTitle);
                vm.SetProgress(5, "Чтение файла Excel…");
                vm.SetProgress(25, "Сохранение в базу данных…");

                ImportResult = await _importService.ImportFromFileAsync(
                    ProjectId,
                    VersionId,
                    DataType,
                    SelectedFilePath,
                    Description?.Trim() ?? string.Empty,
                    addMissingToCatalog);

                vm.SetProgress(
                    100,
                    ImportResult.Success ? "Готово" : "Ошибка сохранения");
            });

            if (ImportResult is null)
            {
                ErrorMessage = "Результат импорта не получен";
                return;
            }

            if (ImportResult.Success)
            {
                var userId = _currentUser.User?.Id;
                if (userId is not null)
                {
                    var actionType = DataType switch
                    {
                        DataImportType.PhysicalVolume => UserActionType.PhysicalVolumeImport,
                        DataImportType.Equipment => UserActionType.EquipmentImport,
                        _ => UserActionType.LaborIntensityImport,
                    };
                    var desc = $"ProjectId={ProjectId}, VersionId={VersionId}, rows={ImportResult.RecordsImported}, file={Path.GetFileName(SelectedFilePath)}";
                    await _actionLog.LogAsync(userId.Value, actionType, entityType: "ProjectAnalysisVersion", entityId: VersionId, description: desc);
                }

                var text = ImportResult.RecordsImported > 0
                    ? $"Импортировано строк: {ImportResult.RecordsImported}."
                    : string.IsNullOrWhiteSpace(ImportResult.SuccessDetails)
                        ? $"Импортировано строк: {ImportResult.RecordsImported}."
                        : ImportResult.SuccessDetails;
                if (ImportResult.RecordsImported > 0 && !string.IsNullOrWhiteSpace(ImportResult.SuccessDetails))
                {
                    text += Environment.NewLine + ImportResult.SuccessDetails;
                }

                MessageBox.Show(
                    ownerWindow,
                    text,
                    "Импорт завершён",
                    MessageBoxButton.OK,
                    ImportResult.RecordsImported > 0
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);

                CloseDialog(true);
            }
            else
            {
                ErrorMessage = ImportResult.ErrorMessage ?? "Ошибка импорта данных";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка импорта: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        if (_closeDialog is not null)
        {
            _closeDialog(result);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                {
                    window.DialogResult = result;
                }
            });
        }
    }
}
