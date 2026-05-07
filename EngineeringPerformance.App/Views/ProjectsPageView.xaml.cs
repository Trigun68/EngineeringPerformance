using System.ComponentModel;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.App.Services;
using System.Windows.Controls;

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
    }

    public void SetSettingsHelper(UserSettingsHelper helper) => _settingsHelper = helper;

    private GridControl[] GetAllGrids() =>
    [
        ForecastGrid, FacilityForecastGrid, DetailForecastGrid,
        FactPerformanceGrid, FactFacilityGrid, FactDetailGrid, CompareGrid,
    ];

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_settingsHelper is null) return;

        var savedTab = await _settingsHelper.GetIntAsync(KeyTab);
        if (savedTab.HasValue && savedTab.Value < TabControl.Items.Count)
            TabControl.SelectedIndex = savedTab.Value;

        var grids = GetAllGrids();
        for (var i = 0; i < grids.Length; i++)
            await _settingsHelper.RestoreGridLayoutAsync(grids[i], GridNames[i]);

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

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        TabControl.SelectionChanged -= OnTabSelectionChanged;
    }

    private void OnTabSelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
    {
        if (_settingsHelper is null) return;
        _ = _settingsHelper.SaveIntAsync(KeyTab, TabControl.SelectedIndex);
    }
}
