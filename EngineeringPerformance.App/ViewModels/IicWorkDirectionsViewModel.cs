using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class IicWorkDirectionsViewModel : DirectoryViewModelBase<IicWorkDirection>
{
    private readonly IDirectoryCache<IicWorkDirection> _cache;

    public Func<IicWorkDirection?, Task>? OpenEditWindow { get; set; }

    public IicWorkDirectionsViewModel(
        IDirectoryService<IicWorkDirection> service,
        IDirectoryCache<IicWorkDirection> cache) : base(service)
    {
        _cache = cache;
    }

    protected override async Task<IReadOnlyList<IicWorkDirection>> FetchAllAsync()
        => ShowArchived
            ? await DirectoryService.GetAllAsync(true)
            : await _cache.GetAllAsync();

    public override async Task ReloadFromDatabaseAsync()
    {
        _cache.Invalidate();
        await base.ReloadFromDatabaseAsync();
    }

    protected override void ApplyRefreshedItem(IicWorkDirection existing, IicWorkDirection updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Code = updated.Code;
        existing.Description = updated.Description;
        existing.UnitId = updated.UnitId;
        existing.Unit = updated.Unit;
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
