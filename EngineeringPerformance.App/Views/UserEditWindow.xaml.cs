using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.Views;

public partial class UserEditWindow : Window
{
    public UserEditWindow(UserEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task<bool> ShowEditDialogAsync(Window owner, User? user = null)
    {
        Owner = owner;
        var vm = (UserEditViewModel)DataContext;
        await vm.InitializeAsync(user);

        var result = ShowDialog();
        return result == true;
    }
}
