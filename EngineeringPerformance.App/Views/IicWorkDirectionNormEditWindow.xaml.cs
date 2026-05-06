using System.Windows;
using System.Windows.Input;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class IicWorkDirectionNormEditWindow : Window
{
    public IicWorkDirectionNormEditWindow(IicWorkDirectionNormEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is IicWorkDirectionNormEditViewModel vm)
        {
            vm.EditHistoryItemCommand.Execute(null);
        }
    }
}
