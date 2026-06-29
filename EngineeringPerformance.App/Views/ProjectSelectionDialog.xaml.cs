using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class ProjectSelectionDialog : Window
{
    public ProjectSelectionDialog(ProjectSelectionDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task<(int ProjectId, int VersionId)?> ShowSelectionDialogAsync(Window owner)
    {
        Owner = owner;
        var vm = (ProjectSelectionDialogViewModel)DataContext;

        bool? dialogResult = null;
        Closed += (_, _) => dialogResult = DialogResult;
        await vm.InitializeAsync(result => Dispatcher.Invoke(() => DialogResult = result));

        ShowDialog();

        if (dialogResult == true && vm.SelectedProject is not null && vm.SelectedVersionId is int versionId)
        {
            return (vm.SelectedProject.Id, versionId);
        }

        return null;
    }
}
