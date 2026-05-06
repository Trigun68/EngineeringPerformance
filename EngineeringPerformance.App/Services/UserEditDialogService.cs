using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IUserEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(User? user);
}

public sealed class UserEditDialogService : IUserEditDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public UserEditDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ShowEditDialogAsync(User? user)
    {
        if (_owner is null)
            return false;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<UserEditViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<UserEditWindow>();

        window.Owner = _owner;
        window.DataContext = viewModel;

        bool? dialogResult = null;
        window.Closed += (_, _) => dialogResult = window.DialogResult;
        await viewModel.InitializeAsync(user, result => window.Dispatcher.Invoke(() => window.DialogResult = result));

        window.ShowDialog();
        return dialogResult == true;
    }
}
