using System.Windows;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.Views;

public partial class UnitOfMeasurementEditWindow : Window
{
    public UnitOfMeasurementEditWindow(UnitOfMeasurementEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task<bool> ShowEditDialogAsync(Window owner, UnitOfMeasurement? item = null)
    {
        Owner = owner;
        var vm = (UnitOfMeasurementEditViewModel)DataContext;
        await vm.InitializeAsync(item);

        var result = ShowDialog();
        return result == true;
    }
}
