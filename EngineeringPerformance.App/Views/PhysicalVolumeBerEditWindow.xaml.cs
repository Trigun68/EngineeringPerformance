using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class PhysicalVolumeBerEditWindow : Window
{
    public PhysicalVolumeBerEditWindow(PhysicalVolumeBerEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
