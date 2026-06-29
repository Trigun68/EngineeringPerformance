using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Win32;

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

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "Единицы_измерения.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var columns = new ReferenceExcelExporter.ColumnDef[]
        {
            new("Наименование", o => ((UnitOfMeasurement)o).Name,        FixedWidth: 30),
            new("Описание",     o => ((UnitOfMeasurement)o).Description, FixedWidth: 40),
            new("Архив",        o => ((UnitOfMeasurement)o).IsArchived.ToString()),
        };

        try
        {
            ReferenceExcelExporter.Export(
                "Единицы измерения",
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
