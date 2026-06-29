using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Win32;

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

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "Виды_справочников_физ_объёмов.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var columns = new ReferenceExcelExporter.ColumnDef[]
        {
            new("Наименование", o => ((PhysicalVolumeType)o).Name,        FixedWidth: 30),
            new("Описание",     o => ((PhysicalVolumeType)o).Description, FixedWidth: 40),
            new("Архив",        o => ((PhysicalVolumeType)o).IsArchived.ToString()),
        };

        try
        {
            ReferenceExcelExporter.Export(
                "Виды справочников физ. объёмов",
                columns,
                Items.Cast<object>(),
                dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
