using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IProjectParticipantsDialogService
{
    void SetOwnerWindow(Window owner);
    Task ShowAsync(int projectId);
}

public sealed class ProjectParticipantsDialogService : IProjectParticipantsDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ProjectParticipantsDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public async Task ShowAsync(int projectId)
    {
        if (_owner is null) return;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var vm = scope.ServiceProvider.GetRequiredService<ProjectParticipantsDialogViewModel>();
        var win = scope.ServiceProvider.GetRequiredService<ProjectParticipantsDialog>();
        var participantEditDialog = _serviceProvider.GetRequiredService<IProjectParticipantEditDialogService>();
        participantEditDialog.SetOwnerWindow(_owner);

        win.Owner = _owner;
        win.DataContext = vm;

        // При нажатии «Добавить» открывает стандартное окно выбора пользователя
        vm.AddParticipantRequested += async (_, _) =>
        {
            var saved = await participantEditDialog.ShowEditDialogAsync(projectId);
            if (saved) await vm.ReloadAsync();
        };

        await vm.InitializeAsync(projectId, result => win.Dispatcher.Invoke(() => win.DialogResult = result));
        win.ShowDialog();
    }
}
