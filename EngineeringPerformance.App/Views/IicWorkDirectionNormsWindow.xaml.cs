using System.Windows.Controls;
using System.Windows.Input;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class IicWorkDirectionNormsWindow : UserControl
{
    public IicWorkDirectionNormsWindow()
    {
        InitializeComponent();
    }

    private void GridControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is IicWorkDirectionNormsViewModel vm && vm.SelectedItem is not null)
        {
            vm.EditItemCommand.Execute(null);
        }
    }
}
