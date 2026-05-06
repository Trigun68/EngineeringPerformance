using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class IicWorkDirectionEditWindow : Window
{
    public IicWorkDirectionEditWindow(IicWorkDirectionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
