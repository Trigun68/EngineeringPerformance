using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class IicNormQuickEditViewModel : ObservableObject
{
    private readonly IRegisterService<IicWorkDirectionNorm> _service;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private int _normId;

    [ObservableProperty] private string _directionLabel = string.Empty;
    [ObservableProperty] private string _effectiveFromLabel = string.Empty;
    [ObservableProperty] private decimal? _performance;
    [ObservableProperty] private decimal? _estimate;
    [ObservableProperty] private decimal? _standardDeviation;
    [ObservableProperty] private string? _errorMessage;

    public IicNormQuickEditViewModel(
        IRegisterService<IicWorkDirectionNorm> service,
        IDbContextFactory<AppDbContext> contextFactory)
    {
        _service = service;
        _contextFactory = contextFactory;
    }

    public async Task PrepareAsync(int normId)
    {
        _normId = normId;
        ErrorMessage = null;

        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var norm = await ctx.IicWorkDirectionNorms
            .AsNoTracking()
            .Include(n => n.WorkDirection)
            .FirstOrDefaultAsync(n => n.Id == normId);

        if (norm is null)
        {
            ErrorMessage = "Норма не найдена";
            return;
        }

        DirectionLabel = $"{norm.WorkDirection?.Code} {norm.WorkDirection?.Name}".Trim();
        EffectiveFromLabel = norm.EffectiveFrom.ToLocalTime().ToString("dd.MM.yyyy");
        Performance = norm.Performance;
        Estimate = norm.Estimate;
        StandardDeviation = norm.StandardDeviation;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var entity = await _service.GetByIdAsync(_normId);
            if (entity is null)
            {
                ErrorMessage = "Норма не найдена";
                return;
            }
            entity.Performance = Performance;
            entity.Estimate = Estimate;
            entity.StandardDeviation = StandardDeviation;
            await _service.UpdateAsync(entity);
            CloseDialog(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                window.DialogResult = result;
        });
    }
}
