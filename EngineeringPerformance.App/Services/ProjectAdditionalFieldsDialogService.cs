using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;

namespace EngineeringPerformance.App.Services;

public interface IProjectAdditionalFieldsDialogService
{
    void SetOwnerWindow(Window owner);

    bool ShowDialog(ProjectEditViewModel parent);
}

public sealed class ProjectAdditionalFieldsDialogService : IProjectAdditionalFieldsDialogService
{
    private Window? _owner;

    public void SetOwnerWindow(Window owner)
    {
        _owner = owner;
    }

    public bool ShowDialog(ProjectEditViewModel parent)
    {
        if (_owner is null)
        {
            return false;
        }

        var window = new ProjectAdditionalFieldsWindow();
        var viewModel = new ProjectAdditionalFieldsViewModel(parent);
        viewModel.Initialize(result => window.DialogResult = result);
        window.DataContext = viewModel;
        window.Owner = _owner;

        return window.ShowDialog() == true;
    }
}
