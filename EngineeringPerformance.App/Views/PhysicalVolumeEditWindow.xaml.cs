using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class PhysicalVolumeEditWindow : Window
{
    public PhysicalVolumeEditWindow(PhysicalVolumeEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
