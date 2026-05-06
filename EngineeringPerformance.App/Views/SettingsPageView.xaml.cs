using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class SettingsPageView : UserControl
{
    private bool _tabsInitialized;

    public SettingsPageView()
    {
        InitializeComponent();
        DataContextChanged += SettingsPageView_DataContextChanged;
    }

    private void SettingsPageView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_tabsInitialized) return;
        if (DataContext is not SettingsPageViewModel vm) return;

        _tabsInitialized = true;
        TabControl.SelectionChanged += TabControl_SelectionChanged;

        foreach (var tabItem in vm.Tabs)
        {
            var dxTab = new DXTabItem { Header = tabItem.Name };
            TabControl.Items.Add(dxTab);
        }

        if (TabControl.Items.Count > 0)
        {
            TabControl.SelectedIndex = 0;
        }
    }

    private void TabControl_SelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
    {
        if (e.NewSelectedItem is not DXTabItem dxTab) return;
        if (dxTab.Content is not null) return;
        if (DataContext is not SettingsPageViewModel vm) return;

        var tabIndex = TabControl.Items.IndexOf(dxTab);
        if (tabIndex < 0 || tabIndex >= vm.Tabs.Count) return;

        var tabItem = vm.Tabs[tabIndex];
        var view = vm.GetOrCreateView(tabItem);
        if (view is not null)
            dxTab.Content = view;
    }
}
