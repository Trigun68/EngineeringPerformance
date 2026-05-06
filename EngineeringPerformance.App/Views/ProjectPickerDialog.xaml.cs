using System.Windows;
using System.Windows.Input;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class ProjectPickerDialog : Window
{
    public ProjectPickerDialog()
    {
        InitializeComponent();
    }

    public async Task<int?> ShowPickerAsync(Window owner)
    {
        Owner = owner;
        var vm = (ProjectPickerDialogViewModel)DataContext;

        await vm.InitializeAsync(result => Dispatcher.Invoke(() => DialogResult = result));

        ShowDialog();

        return DialogResult == true ? vm.SelectedProject?.Id : null;
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var vm = (ProjectPickerDialogViewModel)DataContext;
        if (vm.SelectedProject is not null && vm.SelectCommand.CanExecute(null))
            vm.SelectCommand.Execute(null);
    }
}
