using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.Views;

public partial class ProjectEditWindow : Window
{
    private bool _allowClose;

    public ProjectEditWindow(ProjectEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.PerformanceDataLoaded += (_, _) =>
            _ = Dispatcher.BeginInvoke(ExpandPerformanceTree, DispatcherPriority.Loaded);

        Closing += Window_Closing;
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;

        if (DataContext is not ProjectEditViewModel vm)
        {
            _allowClose = true;
            _ = Dispatcher.BeginInvoke(Close, DispatcherPriority.Background);
            return;
        }

        var ok = await vm.FinishClosingAsync();
        if (!ok)
        {
            MessageBox.Show(
                vm.ErrorMessage ?? "Не удалось сохранить изменения проекта.",
                "Сохранение",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _allowClose = true;
        _ = Dispatcher.BeginInvoke(Close, DispatcherPriority.Background);
    }

    private void ExpandPerformanceTree()
    {
        PerformanceTreeListView?.ExpandAllNodes();
    }

    private void AnalysisVersionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProjectEditViewModel { SelectedVersion: not null } vm)
        {
            vm.EditVersionCommand.Execute(null);
        }
    }

    private void ProjectParticipantsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProjectEditViewModel { SelectedProjectParticipant: not null } vm)
        {
            vm.EditParticipantCommand.Execute(null);
        }
    }

    public async Task<bool> ShowEditDialogAsync(Window owner, Project? item = null)
    {
        Owner = owner;
        var vm = (ProjectEditViewModel)DataContext;
        await vm.InitializeAsync(item);

        var result = ShowDialog();
        return result == true;
    }
}
