using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.Views;

public partial class DataPageView : UserControl
{
    private bool _tabsInitialized;

    public DataPageView()
    {
        InitializeComponent();
        DataContextChanged += DataPageView_DataContextChanged;
        Loaded += DataPageView_Loaded;
    }

    private void DataPageView_Loaded(object sender, RoutedEventArgs e)
    {
        if (TabControl.SelectedItem is DXTabItem dxTab)
        {
            LoadTabContentIfNeeded(dxTab);
        }
    }

    private void DataPageView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_tabsInitialized) return;
        if (DataContext is not DataPageViewModel vm) return;

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
            // DXTabControl не всегда вызывает SelectionChanged при программной установке SelectedIndex — контент первой вкладки иначе остаётся пустым.
            if (TabControl.SelectedItem is DXTabItem selected)
            {
                LoadTabContentIfNeeded(selected);
            }
        }
    }

    private void TabControl_SelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
    {
        if (e.NewSelectedItem is not DXTabItem dxTab) return;

        var hadContentAlready = dxTab.Content is not null;
        LoadTabContentIfNeeded(dxTab);

        // Вкладка уже была открыта: контент не пересоздаётся, а данные в БД могли измениться
        // (импорт из редактирования проекта и т.д.) — перезагрузка справочника.
        if (hadContentAlready
            && dxTab.Content is UserControl { DataContext: ILoadableViewModel vm })
        {
            _ = vm.LoadAsync().ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception is not null)
                        System.Diagnostics.Debug.WriteLine($"LoadAsync после смены вкладки: {t.Exception.GetBaseException().Message}");
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    private void LoadTabContentIfNeeded(DXTabItem? dxTab)
    {
        if (dxTab is null || dxTab.Content is not null) return;
        if (DataContext is not DataPageViewModel vm) return;

        var tabIndex = TabControl.Items.IndexOf(dxTab);
        if (tabIndex < 0 || tabIndex >= vm.Tabs.Count) return;

        var tabItem = vm.Tabs[tabIndex];
        var view = vm.GetOrCreateView(tabItem);
        if (view is not null)
        {
            dxTab.Content = view;
        }
    }
}
