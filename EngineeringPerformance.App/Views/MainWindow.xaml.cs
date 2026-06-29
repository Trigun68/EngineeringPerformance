using System.Windows;
using System.Windows.Controls;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        DataContext = viewModel;
        navigationService.SetContentHost(ContentHost);
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is NavigationItem item && DataContext is MainViewModel vm)
        {
            vm.NavigateCommand.Execute(item);
        }
    }
}
