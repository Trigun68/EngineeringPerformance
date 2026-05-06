using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel = viewModel;
        ViewModel.LoginSucceeded += OnLoginSucceeded;
    }

    public LoginViewModel? ViewModel { get; }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.LoginSucceeded -= OnLoginSucceeded;
        base.OnClosed(e);
    }
}
