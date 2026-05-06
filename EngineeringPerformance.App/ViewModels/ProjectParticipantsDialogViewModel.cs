using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectParticipantsDialogViewModel : ObservableObject
{
    private readonly IProjectVisibilityService _visibilityService;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private Action<bool>? _closeDialog;

    [ObservableProperty] private int _projectId;
    [ObservableProperty] private ObservableCollection<ProjectParticipantItem> _participants = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveParticipantCommand))]
    private ProjectParticipantItem? _selectedParticipant;
    [ObservableProperty] private string? _errorMessage;

    public ProjectParticipantsDialogViewModel(
        IProjectVisibilityService visibilityService,
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _visibilityService = visibilityService;
        _contextFactory = contextFactory;
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
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var list = await ctx.ProjectPermissions
                .AsNoTracking()
                .Include(pp => pp.User).ThenInclude(u => u.Role)
                .Where(pp => pp.ProjectId == ProjectId)
                .Select(pp => new ProjectParticipantItem
                {
                    Id = pp.User.Id,
                    UserName = pp.User.DisplayName,
                    Role = pp.User.Role.Name,
                    AccessLevel = pp.Level,
                })
                .ToListAsync();

            Participants = new ObservableCollection<ProjectParticipantItem>(list);
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
