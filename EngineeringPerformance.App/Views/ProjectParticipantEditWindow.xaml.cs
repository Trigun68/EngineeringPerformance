using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.Views;

public partial class ProjectParticipantEditWindow : Window
{
    public ProjectParticipantEditWindow(ProjectParticipantEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task<bool> ShowEditDialogAsync(Window owner, int projectId, User? existingUser = null)
    {
        Owner = owner;
        var vm = (ProjectParticipantEditViewModel)DataContext;
        await vm.InitializeAsync(projectId, existingUser);

        var result = ShowDialog();
        return result == true;
    }
}
