using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class DbSettingsWindow : Window
{
    public DbSettingsWindow(DbSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
