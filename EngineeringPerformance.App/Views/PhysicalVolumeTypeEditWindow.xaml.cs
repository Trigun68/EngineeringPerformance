using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.Views;

public partial class PhysicalVolumeTypeEditWindow : Window
{
    public PhysicalVolumeTypeEditWindow(PhysicalVolumeTypeEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task<bool> ShowEditDialogAsync(Window owner, PhysicalVolumeType? item = null)
    {
        Owner = owner;
        var vm = (PhysicalVolumeTypeEditViewModel)DataContext;
        await vm.InitializeAsync(item);

        var result = ShowDialog();
        return result == true;
    }
}
