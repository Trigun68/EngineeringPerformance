using System.Windows;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class ProjectAnalysisVersionEditWindow : Window
{
    public ProjectAnalysisVersionEditWindow(ProjectAnalysisVersionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
