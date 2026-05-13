using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class LaborIntensitiesViewModel : DirectoryViewModelBase<LaborIntensity>
{
    private readonly IDirectoryCache<LaborIntensity> _cache;

    public Func<LaborIntensity?, Task>? OpenEditWindow { get; set; }

    public LaborIntensitiesViewModel(
        IDirectoryService<LaborIntensity> service,
        IDirectoryCache<LaborIntensity> cache) : base(service)
    {
        _cache = cache;
    }

    protected override async Task<IReadOnlyList<LaborIntensity>> FetchAllAsync()
        => ShowArchived
            ? await DirectoryService.GetAllAsync(true)
            : await _cache.GetAllAsync();

    public override async Task ReloadFromDatabaseAsync()
    {
        _cache.Invalidate();
        await base.ReloadFromDatabaseAsync();
    }

    protected override void ApplyRefreshedItem(LaborIntensity existing, LaborIntensity updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Code = updated.Code;
        existing.DocumentTypeName = updated.DocumentTypeName;
        existing.NormPpr = updated.NormPpr;
        existing.NormOpr = updated.NormOpr;
        existing.NormPd = updated.NormPd;
        existing.NormRd = updated.NormRd;
        existing.IicWorkDirectionId = updated.IicWorkDirectionId;
        existing.Techas = updated.Techas;
        existing.IicWorkDirection = updated.IicWorkDirection;
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
