using System.Collections.ObjectModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAccessControlService _accessControl;
    private readonly CurrentUserHolder _currentUserHolder;
    private readonly ILogger<SettingsPageViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SettingsTabItem> _tabs = [];

    [ObservableProperty]
    private int _selectedTabIndex;

    public SettingsPageViewModel(
        IServiceProvider serviceProvider,
        IAccessControlService accessControl,
        CurrentUserHolder currentUserHolder,
        ILogger<SettingsPageViewModel> logger)
    {
        _serviceProvider = serviceProvider;
        _accessControl = accessControl;
        _currentUserHolder = currentUserHolder;
        _logger = logger;
        BuildTabs();
    }

    private void BuildTabs()
    {
        var user = _currentUserHolder.User;
        if (user is null) return;

        var tabConfigs = new[]
        {
            ("Пользователи", typeof(UsersWindow), typeof(UsersViewModel), "Users"),
            ("Права пользователей", typeof(AccessRightsWindow), typeof(AccessRightsViewModel), "Settings"),
        };

        foreach (var (name, viewType, viewModelType, accessObjectName) in tabConfigs)
        {
            if (!_accessControl.CanRead(user, accessObjectName)) continue;

            Tabs.Add(new SettingsTabItem
            {
                Name = name,
                ViewType = viewType,
                ViewModelType = viewModelType,
            });
        }
    }

    public UserControl? GetOrCreateView(SettingsTabItem tab)
    {
        if (tab.View is not null) return tab.View;

        var view = (UserControl)_serviceProvider.GetRequiredService(tab.ViewType);
        var viewModel = _serviceProvider.GetRequiredService(tab.ViewModelType);
        view.DataContext = viewModel;
        tab.View = view;

        if (viewModel is ILoadableViewModel loadable)
        {
            var scheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
            _ = loadable.LoadAsync().ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception is not null)
                    _logger.LogError(t.Exception.InnerException, "LoadAsync failed for settings tab {TabName}", tab.Name);
            }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted, scheduler);
        }

        return view;
    }
}

public sealed class SettingsTabItem
{
    public string Name { get; init; } = string.Empty;

    public Type ViewType { get; init; } = null!;

    public Type ViewModelType { get; init; } = null!;

    public UserControl? View { get; set; }
}
