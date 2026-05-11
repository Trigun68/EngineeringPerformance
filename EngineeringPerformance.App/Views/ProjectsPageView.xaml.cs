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

        e.MenuInfo.Menu.Items.Add(new BarItemSeparator());

        var item4 = new BarButtonItem { Content = "Удалить выбранную строку" };
        item4.ItemClick += (_, _) => vm.DeleteProjectPhysicalVolumeCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item4);

        var item5 = new BarButtonItem { Content = "Добавить строку копированием выбранной" };
        item5.ItemClick += (_, _) => vm.CreateProjectPhysicalVolumeFromCopyCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item5);

        var item6 = new BarButtonItem { Content = "Добавить строку" };
        item6.ItemClick += (_, _) => vm.CreateProjectPhysicalVolumeCommand.Execute(null);
        e.MenuInfo.Menu.Items.Add(item6);
    }

    private void OnFactDetailShowGridMenu(object sender, GridMenuEventArgs e)
    {
        if (e.MenuInfo.MenuType != GridMenuType.RowCell) return;
        if (DataContext is not ProjectsPageViewModel vm) return;

        var rowHandle = FactDetailView.FocusedRowHandle;
        var row = FactDetailGrid.GetRow(rowHandle) as FactDetailRow;
        if (row is null)
        {
            e.Handled = true;
            return;
        }

        e.MenuInfo.Menu.Items.Clear();

        var item1 = new BarButtonItem { Content = "Редактировать физ. объём" };
        item1.ItemClick += (_, _) => vm.EditFactProjectPhysicalVolumeCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item1);

        var item2 = new BarButtonItem { Content = "Редактировать номенклатуру физ. объёма" };
        item2.ItemClick += (_, _) => vm.EditFactPhysicalVolumeNomenclatureCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item2);

        var item3 = new BarButtonItem
        {
            Content = "Редактировать норму IIC",
            IsEnabled = row.NormId.HasValue,
        };
        item3.ItemClick += (_, _) => vm.EditFactIicNormCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item3);

        e.MenuInfo.Menu.Items.Add(new BarItemSeparator());

        var item4 = new BarButtonItem { Content = "Удалить выбранную строку" };
        item4.ItemClick += (_, _) => vm.DeleteFactProjectPhysicalVolumeCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item4);

        var item5 = new BarButtonItem { Content = "Добавить строку копированием выбранной" };
        item5.ItemClick += (_, _) => vm.CreateFactProjectPhysicalVolumeFromCopyCommand.Execute(row);
        e.MenuInfo.Menu.Items.Add(item5);

        var item6 = new BarButtonItem { Content = "Добавить строку" };
        item6.ItemClick += (_, _) => vm.CreateFactProjectPhysicalVolumeCommand.Execute(null);
        e.MenuInfo.Menu.Items.Add(item6);
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
