using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class EquipmentEditWindow : Window
{
    public EquipmentEditWindow(EquipmentEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
