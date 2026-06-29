using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectParticipantsDialogViewModel : ObservableObject
{
    private readonly IProjectVisibilityService _visibilityService;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private int _projectId;
    [ObservableProperty] private ObservableCollection<ProjectParticipantItem> _participants = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveParticipantCommand))]
    private ProjectParticipantItem? _selectedParticipant;
    [ObservableProperty] private string? _errorMessage;

    public ProjectParticipantsDialogViewModel(IProjectVisibilityService visibilityService)
    {
        _visibilityService = visibilityService;
    }

    public async Task InitializeAsync(int projectId, Action<bool>? closeDialog = null)
    {
        ProjectId = projectId;
        _closeDialog = closeDialog;
        ErrorMessage = null;
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            var list = await _visibilityService.GetProjectPermissionsAsync(ProjectId);
            Participants = new ObservableCollection<ProjectParticipantItem>(
                list.Select(MapParticipant));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки участников: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RequestAddParticipant()
    {
        // Вызывающий код (ProjectsPageViewModel) подписывается на это событие
        AddParticipantRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? AddParticipantRequested;

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private async Task RemoveParticipantAsync()
    {
        if (SelectedParticipant is null) return;

        var confirm = MessageBox.Show(
            $"Удалить участника \"{SelectedParticipant.UserName}\" из проекта?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _visibilityService.RemoveUserFromProjectAsync(ProjectId, SelectedParticipant.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка удаления: {ex.Message}";
        }
    }

    private bool CanRemove() => SelectedParticipant is not null;

    [RelayCommand]
    private void Close() => CloseDialog(false);

    private static ProjectParticipantItem MapParticipant(ProjectPermissionDto dto) => new()
    {
        Id = dto.UserId,
        UserName = dto.DisplayName,
        Role = dto.RoleName,
        AccessLevel = dto.Level,
    };

    private void CloseDialog(bool result)
    {
        if (_closeDialog is not null)
            _closeDialog(result);
        else
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this) is Window win)
                    win.DialogResult = result;
            });
    }
}
