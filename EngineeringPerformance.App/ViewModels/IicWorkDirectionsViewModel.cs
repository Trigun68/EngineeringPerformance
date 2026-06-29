using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Win32;

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

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "Группы_физ_объёмов_IIC.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var columns = new ReferenceExcelExporter.ColumnDef[]
        {
            new("Код",           o => ((IicWorkDirection)o).Code,             FixedWidth: 15),
            new("Наименование",  o => ((IicWorkDirection)o).Name,             FixedWidth: 40),
            new("Описание",      o => ((IicWorkDirection)o).Description,      FixedWidth: 30),
            new("Ед. измерения", o => ((IicWorkDirection)o).Unit?.Name),
            new("Архив",         o => ((IicWorkDirection)o).IsArchived.ToString()),
        };

        try
        {
            ReferenceExcelExporter.Export(
                "Группы физических объёмов (IIC)",
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
