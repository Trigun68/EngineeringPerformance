using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class IterationDeleteDialogViewModel : ObservableObject
{
    private readonly IDataImportService _importService;
    private readonly IUserActionLogService _actionLog;
    private readonly CurrentUserHolder _currentUser;
    private Action<bool>? _closeDialog;
    // Любое удаление в текущем сеансе диалога делает результат «успешным» —
    // вызывающая сторона (ProjectEditViewModel) обновит таблицу, даже если
    // пользователь закрыл окно через «Отмена»/крестик.
    private bool _anyDeleted;

    public bool AnyDeleted => _anyDeleted;

    [ObservableProperty] private int _projectId;
    [ObservableProperty] private int _versionId;
    [ObservableProperty] private DataImportType _dataType;
    [ObservableProperty] private ObservableCollection<IterationItem> _iterations = [];
    [ObservableProperty] private IterationItem? _selectedIteration;
    [ObservableProperty] private string? _errorMessage;

    public string WindowTitle => DataType switch
    {
        DataImportType.PhysicalVolume => "Удаление итерации физических объёмов",
        DataImportType.Equipment => "Удаление итерации данных оборудования",
        _ => "Удаление итерации трудоёмкости",
    };

    public IterationDeleteDialogViewModel(
        IDataImportService importService,
        IUserActionLogService actionLog,
        CurrentUserHolder currentUser)
    {
        _importService = importService;
        _actionLog = actionLog;
        _currentUser = currentUser;
    }

    public async Task InitializeAsync(int projectId, int versionId, DataImportType dataType, Action<bool>? closeDialog = null)
    {
        _closeDialog = closeDialog;
        _anyDeleted = false;
        ProjectId = projectId;
        VersionId = versionId;
        DataType = dataType;
        ErrorMessage = null;
        SelectedIteration = null;

        var iterationInfos = await _importService.GetImportIterationsAsync(projectId, versionId, dataType);
        Iterations.Clear();
        foreach (var info in iterationInfos)
        {
            Iterations.Add(new IterationItem
            {
                ImportedAt = info.ImportedAt,
                RecordCount = info.RecordCount,
                Description = info.Description,
                SourceFileName = info.SourceFileName,
            });
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedIteration is null)
        {
            ErrorMessage = "Выберите итерацию для удаления";
            return;
        }

        try
        {
            ErrorMessage = null;
            var iterationToDelete = SelectedIteration;
            await _importService.DeleteIterationAsync(ProjectId, VersionId, DataType, iterationToDelete.ImportedAt);
            Iterations.Remove(iterationToDelete);
            SelectedIteration = null;
            _anyDeleted = true;

            var userId = _currentUser.User?.Id;
            if (userId is not null)
            {
                var actionType = DataType switch
                {
                    DataImportType.PhysicalVolume => UserActionType.PhysicalVolumeDelete,
                    DataImportType.Equipment => UserActionType.EquipmentDelete,
                    _ => UserActionType.LaborIntensityDelete,
                };
                var desc = $"ProjectId={ProjectId}, VersionId={VersionId}, importedAt={iterationToDelete.ImportedAt:O}, rows={iterationToDelete.RecordCount}";
                await _actionLog.LogAsync(userId.Value, actionType, entityType: "ProjectAnalysisVersion", entityId: VersionId, description: desc);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка удаления: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(_anyDeleted);

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

public sealed class IterationItem
{
    public DateTime ImportedAt { get; set; }

    public string DisplayText => $"{ImportedAt:dd.MM.yyyy HH:mm} — {RecordCount} записей";

    public int RecordCount { get; set; }

    public string? Description { get; set; }

    public string? SourceFileName { get; set; }
}
