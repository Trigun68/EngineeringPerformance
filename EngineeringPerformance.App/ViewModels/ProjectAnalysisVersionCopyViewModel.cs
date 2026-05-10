using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectAnalysisVersionCopyViewModel : ObservableObject
{
    private readonly IProjectAnalysisVersionCopyService _copyService;
    private readonly IUserActionLogService _actionLog;
    private readonly CurrentUserHolder _currentUser;
    private Action<int?>? _closeDialog;
    private ProjectAnalysisVersion? _source;

    [ObservableProperty]
    private string _windowTitle = "Копировать версию";

    [ObservableProperty]
    private string _sourceVersionLabel = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _versionName = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    public ProjectAnalysisVersionCopyViewModel(
        IProjectAnalysisVersionCopyService copyService,
        IUserActionLogService actionLog,
        CurrentUserHolder currentUser)
    {
        _copyService = copyService;
        _actionLog = actionLog;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Подготавливает VM к показу. <paramref name="source"/> — версия, которую копируем;
    /// <paramref name="closeDialog"/> вызывается при Save (с Id новой версии) или Cancel (с null).
    /// </summary>
    public void Prepare(ProjectAnalysisVersion source, Action<int?>? closeDialog = null)
    {
        _source = source;
        _closeDialog = closeDialog;
        ErrorMessage = null;
        IsBusy = false;

        var directionLabel = source.Direction switch
        {
            AnalysisDirection.Plan => "план",
            AnalysisDirection.Fact => "факт",
            _ => source.Direction.ToString().ToLowerInvariant(),
        };

        WindowTitle = $"Копировать версию ({directionLabel})";
        SourceVersionLabel = $"Источник: «{source.VersionName}»";
        // По умолчанию — суффикс «(копия)» для понятности в гриде; пользователь может переписать.
        VersionName = $"{source.VersionName} (копия)";
        Description = source.Description;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_source is null) return;
        if (string.IsNullOrWhiteSpace(VersionName))
        {
            ErrorMessage = "Имя новой версии обязательно.";
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var newId = await _copyService.CopyVersionAsync(_source.Id, VersionName.Trim(), Description);

            var userId = _currentUser.User?.Id;
            if (userId is not null)
            {
                await _actionLog.LogAsync(
                    userId.Value,
                    UserActionType.VersionCopy,
                    entityType: "ProjectAnalysisVersion",
                    entityId: newId,
                    description: $"ProjectId={_source.ProjectId}, source={_source.Id} «{_source.VersionName}», direction={_source.Direction}, new=«{VersionName.Trim()}»");
            }

            CloseDialog(newId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка копирования: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !IsBusy && !string.IsNullOrWhiteSpace(VersionName);

    [RelayCommand]
    private void Cancel() => CloseDialog(null);

    private void CloseDialog(int? newVersionId)
    {
        if (_closeDialog is not null)
        {
            _closeDialog(newVersionId);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                {
                    window.DialogResult = newVersionId.HasValue;
                }
            });
        }
    }
}
