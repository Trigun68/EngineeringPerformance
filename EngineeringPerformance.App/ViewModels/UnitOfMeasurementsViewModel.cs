using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class UnitOfMeasurementsViewModel : DirectoryViewModelBase<UnitOfMeasurement>
{
    private readonly IDirectoryCache<UnitOfMeasurement> _cache;

    public Func<UnitOfMeasurement?, Task>? OpenEditWindow { get; set; }

    public UnitOfMeasurementsViewModel(
        IDirectoryService<UnitOfMeasurement> service,
        IDirectoryCache<UnitOfMeasurement> cache) : base(service)
    {
        _cache = cache;
    }

    protected override async Task<IReadOnlyList<UnitOfMeasurement>> FetchAllAsync()
        => ShowArchived
            ? await DirectoryService.GetAllAsync(true)
            : await _cache.GetAllAsync();

    public override async Task ReloadFromDatabaseAsync()
    {
        _cache.Invalidate();
        await base.ReloadFromDatabaseAsync();
    }

    protected override void ApplyRefreshedItem(UnitOfMeasurement existing, UnitOfMeasurement updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Description = updated.Description;
    }

    [RelayCommand]
    private async Task CreateAndEditAsync()
    {
        if (OpenEditWindow is not null)
            await OpenEditWindow(null);
    }

    [RelayCommand]
    private async Task EditItemAsync()
    {
        if (SelectedItem is null || OpenEditWindow is null) return;

        await OpenEditWindow(SelectedItem);
    }
}
