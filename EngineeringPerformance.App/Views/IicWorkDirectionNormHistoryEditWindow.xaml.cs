using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class IicWorkDirectionNormHistoryEditWindow : Window
{
    public IicWorkDirectionNormHistoryEditWindow(IicWorkDirectionNormHistoryEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
