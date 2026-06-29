using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Win32;

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

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Сохранить как Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "Справочник_трудоёмкости.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var columns = new ReferenceExcelExporter.ColumnDef[]
        {
            new("Код",               o => ((LaborIntensity)o).Code,                   FixedWidth: 20),
            new("Наименование",      o => ((LaborIntensity)o).Name,                   FixedWidth: 40),
            new("Тип документа TF",  o => ((LaborIntensity)o).DocumentTypeName,       FixedWidth: 35),
            new("Направление IIC",   o => ((LaborIntensity)o).IicWorkDirectionDisplay, FixedWidth: 35),
            new("Норма ПИР",         o => ((LaborIntensity)o).NormPpr?.ToString("G")),
            new("Норма ОПР",         o => ((LaborIntensity)o).NormOpr?.ToString("G")),
            new("Норма ПД",          o => ((LaborIntensity)o).NormPd?.ToString("G")),
            new("Норма РД",          o => ((LaborIntensity)o).NormRd?.ToString("G")),
            new("Архив",             o => ((LaborIntensity)o).IsArchived.ToString()),
        };

        try
        {
            ReferenceExcelExporter.Export(
                "Справочник трудоёмкости",
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
