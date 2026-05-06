using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    public static async Task ShowAsync(Window owner, Func<LoadingViewModel, Task> loadAction)
    {
        var viewModel = new LoadingViewModel();
        var window = new LoadingWindow
        {
            Owner = owner,
            DataContext = viewModel
        };

        window.Show();

        var ownerWasEnabled = owner.IsEnabled;
        owner.IsEnabled = false;
        try
        {
            await loadAction(viewModel);
        }
        finally
        {
            owner.IsEnabled = ownerWasEnabled;
            window.Close();
        }
    }
}