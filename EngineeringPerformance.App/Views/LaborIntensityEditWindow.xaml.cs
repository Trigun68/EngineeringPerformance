using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class LaborIntensityEditWindow : Window
{
    public LaborIntensityEditWindow(LaborIntensityEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
