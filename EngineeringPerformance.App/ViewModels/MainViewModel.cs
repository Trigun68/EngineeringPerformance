using System.Collections.ObjectModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAccessControlService _accessControl;
    private readonly IProjectVisibilityService _projectVisibility;
    private readonly IServiceProvider _serviceProvider;
    private readonly CurrentUserHolder _currentUserHolder;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private UserDto _currentUser = null!;

    [ObservableProperty]
    private string _currentViewTitle = "Главная";

    [ObservableProperty]
    private ObservableCollection<NavigationGroup> _navigationGroups = [];

    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    public MainViewModel(
        INavigationService navigationService,
        IAccessControlService accessControl,
        IProjectVisibilityService projectVisibility,
        IServiceProvider serviceProvider,
        CurrentUserHolder currentUserHolder,
        ILogger<MainViewModel> logger)
    {
        _navigationService = navigationService;
        _accessControl = accessControl;
        _projectVisibility = projectVisibility;
        _serviceProvider = serviceProvider;
        _currentUserHolder = currentUserHolder;
        _logger = logger;
        _currentUser = currentUserHolder.User!;

        BuildNavigation();
        ShowDashboard();
    }

    private async void ShowDashboard()
    {
        CurrentViewTitle = "Главная";
        var dashboard = _serviceProvider.GetRequiredService<ProjectDashboardView>();
        var dashboardVm = _serviceProvider.GetRequiredService<ProjectDashboardViewModel>();
        dashboard.DataContext = dashboardVm;
        CurrentView = dashboard;
        try
        {
            await dashboardVm.LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard LoadAsync failed");
        }
    }

    private void BuildNavigation()
    {
        var items = new List<NavigationItem>();

        AddIfAccessible(items, "\U0001F3E0", "Главная",            "Dashboard");
        AddIfAccessible(items, "\U0001F4C1", "Проекты",            "Projects");
        AddIfAccessible(items, "\U0001F4D6", "Справочники",        "References");
        AddIfAccessible(items, "\U0001F4DC", "Журнал действий",    "UserActionLog");
        AddIfAccessible(items, "\U00002699", "Настройки",          "SettingsPage");
        AddIfAccessible(items, "\U00002753", "Справка",            "HelpPage");

        NavigationGroups = new ObservableCollection<NavigationGroup>
        {
            new NavigationGroup { Name = "Навигация", Items = items }
        };
    }

    private void AddIfAccessible(List<NavigationItem> list, string icon, string name, string viewName)
    {
        if (_accessControl.CanRead(CurrentUser, viewName))
        {
            list.Add(new NavigationItem { Icon = icon, Name = name, ViewName = viewName });
        }
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        if (item is null) return;
        if (SelectedNavigationItem == item) return;
        SelectedNavigationItem = item;
        CurrentViewTitle = item.Name;
        _navigationService.NavigateTo(item.ViewName);
    }

    [RelayCommand]
    private void NavigateByViewName(string viewName)
    {
        var item = FindNavigationItem(viewName);
        if (item is not null)
        {
            if (SelectedNavigationItem == item) return;
            SelectedNavigationItem = item;
            CurrentViewTitle = item.Name;
            _navigationService.NavigateTo(item.ViewName);
        }
    }

    [RelayCommand]
    private void NavigateHome()
    {
        SelectedNavigationItem = null;
        ShowDashboard();
    }

    private NavigationItem? FindNavigationItem(string viewName)
    {
        foreach (var group in NavigationGroups)
        {
            foreach (var item in group.Items)
            {
                if (item.ViewName == viewName) return item;
            }
        }
        return null;
    }
}
