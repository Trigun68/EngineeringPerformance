using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class PhysicalVolumeKfoEditWindow : Window
{
    public PhysicalVolumeKfoEditWindow(PhysicalVolumeKfoEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
