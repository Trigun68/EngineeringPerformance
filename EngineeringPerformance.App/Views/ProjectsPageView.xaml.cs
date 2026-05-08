﻿﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class ProjectsPageView : UserControl
{
    private UserSettingsHelper? _settingsHelper;
    private const string KeyTab = "ProjectsPage.ActiveTab";

    private static readonly string[] GridNames =
    [
        "ForecastGrid", "FacilityForecastGrid", "DetailForecastGrid",
        "FactPerformanceGrid", "FactFacilityGrid", "FactDetailGrid", "CompareGrid",
    ];

    public ProjectsPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DetailForecastView.ShowGridMenu += OnDetailForecastShowGridMenu;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ProjectsPageViewModel vm)
        {
            vm.SaveDetailForecastStateCallback    = () => SaveGridState(DetailForecastGrid);
            vm.RestoreDetailForecastStateCallback = s  => RestoreGridState(DetailForecastGrid, s);
            vm.SaveFactDetailStateCallback        = () => SaveGridState(FactDetailGrid);
            vm.RestoreFactDetailStateCallback     = s  => RestoreGridState(FactDetailGrid, s);
        }
    }

    public void SetSettingsHelper(UserSettingsHelper helper) => _settingsHelper = helper;

    private static DetailGridState SaveGridState(GridControl grid)
    {
        var tv = grid.View as TableView;
        var state = new DetailGridState();
        var topIndex = tv?.TopRowIndex ?? 0;
        for (var i = topIndex; i < grid.VisibleRowCount; i++)
        {
            var rh = grid.GetRowHandleByVisibleIndex(i);
            if (!grid.IsGroupRowHandle(rh))
            {
                state.TopRowHandle = rh;
                break;
            }
        }
        for (var i = 0; i < grid.VisibleRowCount; i++)
        {
            var rh = grid.GetRowHandleByVisibleIndex(i);
            if (grid.IsGroupRowHandle(rh) && grid.IsGroupRowExpanded(rh))
            {
                var val = grid.GetGroupRowValue(rh)?.ToString();
                if (val is not null) state.ExpandedGroupTexts.Add(val);
            }
        }
        return state;
    }

    private static void RestoreGridState(GridControl grid, DetailGridState state)
    {
        if (state.ExpandedGroupTexts.Count == 0) return;
        grid.CollapseAllGroups();
        for (var i = 0; i < grid.VisibleRowCount; i++)
        {
            var rh = grid.GetRowHandleByVisibleIndex(i);
            if (!grid.IsGroupRowHandle(rh)) continue;
            var text = grid.GetGroupRowValue(rh)?.ToString();
            if (text is not null && state.ExpandedGroupTexts.Contains(text))
                grid.ExpandGroupRow(rh, false);
        }
        if (state.TopRowHandle != GridControl.InvalidRowHandle && grid.View is TableView tv)
            tv.ScrollIntoView(state.TopRowHandle);
    }

    private GridControl[] GetAllGrids() =>
    [
        ForecastGrid, FacilityForecastGrid, DetailForecastGrid,
        FactPerformanceGrid, FactFacilityGrid, FactDetailGrid, CompareGrid,
    ];

    private void OnDetailForecastShowGridMenu(object sender, GridMenuEventArgs e)
    {
        if (e.MenuInfo.MenuType != GridMenuType.RowCell) return;
        if (DataContext is not ProjectsPageViewModel vm) return;

        var rowHandle = DetailForecastView.FocusedRowHandle;
        var row = DetailForecastGrid.GetRow(rowHandle) as DetailForecastItem;
        if (row is null || row.IsTotal)
        {
            e.Handled = true;
            return;
        }

        e.MenuInfo.Menu.Items.Clear();

        var item1 = new BarButtonItem { Content = "Редактировать физ. объём" };
        item1.ItemClick += (_, _) => vm.EditProjectPhysicalVolumeCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item1);

        var item2 = new BarButtonItem { Content = "Редактировать номенклатуру физ. объёма" };
        item2.ItemClick += (_, _) => vm.EditPhysicalVolumeNomenclatureCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item2);

        var item3 = new BarButtonItem
        {
            Content = "Редактировать норму IIC",
            IsEnabled = row.NormId.HasValue,
        };
        item3.ItemClick += (_, _) => vm.EditIicNormCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item3);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_settingsHelper is null) return;

        var savedTab = await _settingsHelper.GetIntAsync(KeyTab);
        if (savedTab.HasValue && savedTab.Value < TabControl.Items.Count)
            TabControl.SelectedIndex = savedTab.Value;

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

        var dpd = DependencyPropertyDescriptor.FromProperty(GridColumn.VisibleProperty, typeof(GridColumn));
        for (var i = 0; i < grids.Length; i++)
        {
            var grid = grids[i];
            var name = GridNames[i];
            grid.EndSorting += (_, _) => _ = _settingsHelper?.SaveGridLayoutAsync(grid, name);
            grid.EndGrouping += (_, _) => _ = _settingsHelper?.SaveGridLayoutAsync(grid, name);
            foreach (var col in grid.Columns)
                dpd.AddValueChanged(col, (_, _) => _ = _settingsHelper?.SaveGridLayoutAsync(grid, name));
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        TabControl.SelectionChanged -= OnTabSelectionChanged;
    }

    private void OnTabSelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
    {
        if (_settingsHelper is null) return;
        _ = _settingsHelper.SaveIntAsync(KeyTab, TabControl.SelectedIndex);
    }
}
