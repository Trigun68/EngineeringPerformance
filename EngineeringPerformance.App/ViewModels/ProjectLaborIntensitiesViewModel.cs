using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectLaborIntensitiesViewModel : ObservableObject, ILoadableViewModel
{
    private readonly ProjectLaborIntensityService _service;

    [ObservableProperty]
    private ObservableCollection<ProjectLaborIntensity> _items = [];

    [ObservableProperty]
    private int _rowCount;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public ProjectLaborIntensitiesViewModel(ProjectLaborIntensityService service)
    {
        _service = service;
    }

    public Task LoadAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var list = await _service.GetAllForExplorerAsync();
            Items = new ObservableCollection<ProjectLaborIntensity>(list);
            RowCount = Items.Count;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Items = [];
            RowCount = 0;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
