using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class PhysicalVolumeTypesViewModel : DirectoryViewModelBase<PhysicalVolumeType>
{
    private readonly IDirectoryCache<PhysicalVolumeType> _cache;

    public Func<PhysicalVolumeType?, Task>? OpenEditWindow { get; set; }

    public PhysicalVolumeTypesViewModel(
        IDirectoryService<PhysicalVolumeType> service,
        IDirectoryCache<PhysicalVolumeType> cache) : base(service)
    {
        _cache = cache;
    }

    protected override async Task<IReadOnlyList<PhysicalVolumeType>> FetchAllAsync()
        => ShowArchived
            ? await DirectoryService.GetAllAsync(true)
            : await _cache.GetAllAsync();

    public override async Task ReloadFromDatabaseAsync()
    {
        _cache.Invalidate();
        await base.ReloadFromDatabaseAsync();
    }

    protected override void ApplyRefreshedItem(PhysicalVolumeType existing, PhysicalVolumeType updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Code = updated.Code;
        existing.Description = updated.Description;
        existing.NotifyRowRefreshed();
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
