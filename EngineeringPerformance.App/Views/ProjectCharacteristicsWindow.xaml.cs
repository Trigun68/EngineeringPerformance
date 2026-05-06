using System.Windows.Controls;
using System.Windows.Input;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class ProjectCharacteristicsWindow : UserControl
{
    public ProjectCharacteristicsWindow()
    {
        InitializeComponent();
    }

    private void GridControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProjectCharacteristicsViewModel vm && vm.SelectedItem is not null)
        {
            vm.EditItemCommand.Execute(null);
        }
    }
}
