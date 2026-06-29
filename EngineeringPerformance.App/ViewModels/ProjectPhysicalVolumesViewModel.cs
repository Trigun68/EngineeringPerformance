using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectPhysicalVolumesViewModel : ObservableObject, ILoadableViewModel
{
    private readonly ProjectPhysicalVolumeService _service;

    [ObservableProperty]
    private ObservableCollection<ProjectPhysicalVolume> _items = [];

    [ObservableProperty]
    private int _rowCount;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public ProjectPhysicalVolumesViewModel(ProjectPhysicalVolumeService service)
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
            Items = new ObservableCollection<ProjectPhysicalVolume>(list);
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
