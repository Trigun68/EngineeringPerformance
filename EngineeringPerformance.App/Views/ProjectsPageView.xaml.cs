using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class ProjectsPageView : UserControl
{
    private UserSettingsHelper? _settingsHelper;
    private bool _gridLayoutsInitialized;
    private const string KeyTab = "ProjectsPage.ActiveTab";

    private static readonly string[] GridNames =
    [
        "ForecastGrid", "FacilityForecastGrid", "DetailForecastGrid",
        "FactPerformanceGrid", "FactFacilityGrid", "FactDetailGrid",
        "CompareGrid", "CompareFacilityGrid", "CompareDetailGrid",
    ];

    public ProjectsPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DetailForecastView.ShowGridMenu += OnDetailForecastShowGridMenu;
        FactDetailView.ShowGridMenu += OnFactDetailShowGridMenu;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ProjectsPageViewModel vm)
        {
            vm.SaveDetailForecastStateCallback    = () => DetailGridStateHelper.Save(DetailForecastGrid);
            vm.RestoreDetailForecastStateCallback = s  => DetailGridStateHelper.Restore(DetailForecastGrid, s);
            vm.SaveFactDetailStateCallback        = () => DetailGridStateHelper.Save(FactDetailGrid);
            vm.RestoreFactDetailStateCallback     = s  => DetailGridStateHelper.Restore(FactDetailGrid, s);
        }
    }

    public void SetSettingsHelper(UserSettingsHelper helper) => _settingsHelper = helper;

    /// <summary>Сохраняет раскладки гридов перед закрытием приложения (если страница загружена).</summary>
    public void FlushGridLayoutsForShutdown()
    {
        if (_settingsHelper is null || !_gridLayoutsInitialized) return;
        // FlushAllSync: фаза 1 на UI-потоке (сбор XML), фаза 2 через Task.Run (запись в БД без дедлока)
        _settingsHelper.FlushAllSync();
    }

    private GridControl[] GetAllGrids() =>
    [
        ForecastGrid, FacilityForecastGrid, DetailForecastGrid,
        FactPerformanceGrid, FactFacilityGrid, FactDetailGrid,
        CompareGrid, CompareFacilityGrid, CompareDetailGrid,
    ];

    private void OnDetailForecastShowGridMenu(object sender, GridMenuEventArgs e)
    {
        if (e.MenuInfo.MenuType != GridMenuType.RowCell) return;
        if (DataContext is not ProjectsPageViewModel vm) return;

        var rowHandle = DetailForecastView.FocusedRowHandle;
        var row = DetailForecastGrid.GetRow(rowHandle) as DetailForecastItem;
        if (row is null || row.IsTotal || !IsActionableDetailRow(row))
        {
            e.Handled = true;
            return;
        }

        e.MenuInfo.Menu.Items.Clear();
        AddDetailForecastMenuItems(e.MenuInfo.Menu.Items, vm, row);
    }

    private void OnFactDetailShowGridMenu(object sender, GridMenuEventArgs e)
    {
        if (e.MenuInfo.MenuType != GridMenuType.RowCell) return;
        if (DataContext is not ProjectsPageViewModel vm) return;

        var rowHandle = FactDetailView.FocusedRowHandle;
        var row = FactDetailGrid.GetRow(rowHandle) as FactDetailRow;
        if (row is null || !IsActionableFactDetailRow(row))
        {
            e.Handled = true;
            return;
        }

        e.MenuInfo.Menu.Items.Clear();
        AddFactDetailMenuItems(e.MenuInfo.Menu.Items, vm, row);
    }

    private static bool IsActionableDetailRow(DetailForecastItem row) =>
        row.IsEquipmentRow || row.ProjectPhysicalVolumeId > 0;

    private static bool IsActionableFactDetailRow(FactDetailRow row) =>
        row.IsEquipmentRow || row.ProjectPhysicalVolumeId > 0;

    private static void AddDetailForecastMenuItems(CommonBarItemCollection items, ProjectsPageViewModel vm, DetailForecastItem row)
    {
        if (row.IsEquipmentRow)
        {
            AddMenuItem(items, "Редактировать оборудование", () => vm.EditProjectEquipmentCommand.Execute(row));
            AddMenuItem(items, "Редактировать номенклатуру оборудования", () => vm.EditEquipmentNomenclatureCommand.Execute(row));
        }
        else
        {
            AddMenuItem(items, "Редактировать физ. объём", () => vm.EditProjectPhysicalVolumeCommand.Execute(row));
            AddMenuItem(items, "Редактировать номенклатуру физ. объёма", () => vm.EditPhysicalVolumeNomenclatureCommand.Execute(row));
        }

        AddMenuItem(items, "Редактировать норму EAC", () => vm.EditIicNormCommand.Execute(row), row.NormId.HasValue);
        items.Add(new BarItemSeparator());

        if (row.IsEquipmentRow)
        {
            AddMenuItem(items, "Удалить выбранную строку", () => vm.DeleteProjectEquipmentCommand.Execute(row));
            AddMenuItem(items, "Добавить строку копированием выбранной", () => vm.CreateProjectEquipmentFromCopyCommand.Execute(row));
            AddMenuItem(items, "Добавить строку", () => vm.CreateProjectEquipmentCommand.Execute(null));
        }
        else
        {
            AddMenuItem(items, "Удалить выбранную строку", () => vm.DeleteProjectPhysicalVolumeCommand.Execute(row));
            AddMenuItem(items, "Добавить строку копированием выбранной", () => vm.CreateProjectPhysicalVolumeFromCopyCommand.Execute(row));
            AddMenuItem(items, "Добавить строку", () => vm.CreateProjectPhysicalVolumeCommand.Execute(null));
        }
    }

    private static void AddFactDetailMenuItems(CommonBarItemCollection items, ProjectsPageViewModel vm, FactDetailRow row)
    {
        if (row.IsEquipmentRow)
        {
            AddMenuItem(items, "Редактировать оборудование", () => vm.EditFactProjectEquipmentCommand.Execute(row));
            AddMenuItem(items, "Редактировать номенклатуру оборудования", () => vm.EditFactEquipmentNomenclatureCommand.Execute(row));
        }
        else
        {
            AddMenuItem(items, "Редактировать физ. объём", () => vm.EditFactProjectPhysicalVolumeCommand.Execute(row));
            AddMenuItem(items, "Редактировать номенклатуру физ. объёма", () => vm.EditFactPhysicalVolumeNomenclatureCommand.Execute(row));
        }

        AddMenuItem(items, "Редактировать норму EAC", () => vm.EditFactIicNormCommand.Execute(row), row.NormId.HasValue);
        items.Add(new BarItemSeparator());

        if (row.IsEquipmentRow)
        {
            AddMenuItem(items, "Удалить выбранную строку", () => vm.DeleteFactProjectEquipmentCommand.Execute(row));
            AddMenuItem(items, "Добавить строку копированием выбранной", () => vm.CreateFactProjectEquipmentFromCopyCommand.Execute(row));
            AddMenuItem(items, "Добавить строку", () => vm.CreateFactProjectEquipmentCommand.Execute(null));
        }
        else
        {
            AddMenuItem(items, "Удалить выбранную строку", () => vm.DeleteFactProjectPhysicalVolumeCommand.Execute(row));
            AddMenuItem(items, "Добавить строку копированием выбранной", () => vm.CreateFactProjectPhysicalVolumeFromCopyCommand.Execute(row));
            AddMenuItem(items, "Добавить строку", () => vm.CreateFactProjectPhysicalVolumeCommand.Execute(null));
        }
    }

    private static void AddMenuItem(CommonBarItemCollection items, string caption, Action onClick, bool isEnabled = true)
    {
        var item = new BarButtonItem { Content = caption, IsEnabled = isEnabled };
        item.ItemClick += (_, _) => onClick();
        items.Add(item);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_settingsHelper is null || _gridLayoutsInitialized) return;

        var savedTab = await _settingsHelper.GetIntAsync(KeyTab);
        if (savedTab.HasValue && savedTab.Value < TabControl.Items.Count)
            TabControl.SelectedIndex = savedTab.Value;

        // Дождаться layout-прохода — иначе RestoreLayoutFromStream может не применить ширины star-колонок.
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);

        var grids = GetAllGrids();
        for (var i = 0; i < grids.Length; i++)
        {
            var grid = grids[i];
            var savedSummary = grid.TotalSummary.OfType<GridSummaryItem>().ToList();
            await _settingsHelper.RestoreGridLayoutAsync(grid, GridNames[i]);
            if (grid.TotalSummary.Count == 0 && savedSummary.Count > 0)
            {
                foreach (var item in savedSummary)
                    grid.TotalSummary.Add(item);
            }
        }

        TabControl.SelectionChanged += OnTabSelectionChanged;

        for (var i = 0; i < grids.Length; i++)
            _settingsHelper.AttachGridLayoutAutoSave(grids[i], GridNames[i]);

        _gridLayoutsInitialized = true;
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        TabControl.SelectionChanged -= OnTabSelectionChanged;

        if (_settingsHelper is null || !_gridLayoutsInitialized) return;

        // Сбрасываем флаг до await, чтобы повторный OnLoaded не пропустил восстановление
        // при быстрой смене страниц (пока async-цепочка ещё не завершилась).
        _gridLayoutsInitialized = false;

        foreach (var grid in GetAllGrids())
            await _settingsHelper.DetachAndSaveGridLayoutAsync(grid);
    }

    private void OnTabSelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
    {
        if (_settingsHelper is null) return;
        _ = _settingsHelper.SaveIntAsync(KeyTab, TabControl.SelectedIndex);
    }
}
