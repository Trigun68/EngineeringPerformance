using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App.Services;

public interface IConfirmDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowAsync(string title, string message, string confirmButtonText = "OK", string cancelButtonText = "Отмена");
}

public sealed class ConfirmDialogService : IConfirmDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _owner;

    public ConfirmDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetOwnerWindow(Window owner) => _owner = owner;

    public Task<bool> ShowAsync(string title, string message, string confirmButtonText = "OK", string cancelButtonText = "Отмена")
    {
        var viewModel = _serviceProvider.GetRequiredService<ConfirmDialogViewModel>();
        viewModel.Prepare(title, message, confirmButtonText, cancelButtonText);

        var window = _serviceProvider.GetRequiredService<ConfirmDialog>();
        window.DataContext = viewModel;
        window.Owner = _owner ?? Application.Current.MainWindow;

        var result = window.ShowDialog() == true;
        return Task.FromResult(result);
    }
}
