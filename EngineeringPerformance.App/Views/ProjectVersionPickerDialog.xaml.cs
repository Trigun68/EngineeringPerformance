using System.Windows;
using System.Windows.Input;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.App.Views;

public partial class ProjectVersionPickerDialog : Window
{
    public ProjectVersionPickerDialog()
    {
        InitializeComponent();
    }

    public async Task<int?> ShowPickerAsync(
        Window owner,
        int projectId,
        string projectName,
        AnalysisDirection direction)
    {
        return await ShowPickerAsync(owner, projectId, projectName, (AnalysisDirection?)direction);
    }

    public async Task<int?> ShowPickerAsync(
        Window owner,
        int projectId,
        string projectName,
        AnalysisDirection? direction)
    {
        Owner = owner;
        var vm = (ProjectVersionPickerDialogViewModel)DataContext;

        await vm.InitializeAsync(
            projectId,
            projectName,
            direction,
            result => Dispatcher.Invoke(() => DialogResult = result));

        ShowDialog();

        return DialogResult == true ? vm.SelectedVersion?.Id : null;
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var vm = (ProjectVersionPickerDialogViewModel)DataContext;
        if (vm.SelectedVersion is not null && vm.SelectCommand.CanExecute(null))
            vm.SelectCommand.Execute(null);
    }
}
