using System.Collections.ObjectModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class DataPageViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAccessControlService _accessControl;
    private readonly CurrentUserHolder _currentUserHolder;
    private readonly ILogger<DataPageViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ReferenceTabItem> _tabs = [];

    [ObservableProperty]
    private int _selectedTabIndex;

    public DataPageViewModel(
        IServiceProvider serviceProvider,
        IAccessControlService accessControl,
        CurrentUserHolder currentUserHolder,
        ILogger<DataPageViewModel> logger)
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
            ("Версии анализа проекта", typeof(ProjectAnalysisVersionsWindow), typeof(ProjectAnalysisVersionsViewModel), "ProjectAnalysisVersions"),
            ("Физ. объёмы по проекту", typeof(ProjectPhysicalVolumesWindow), typeof(ProjectPhysicalVolumesViewModel), "ProjectPhysicalVolumes"),
            ("Трудоемкость по проекту", typeof(ProjectLaborIntensitiesWindow), typeof(ProjectLaborIntensitiesViewModel), "ProjectLaborIntensities"),
        };

        foreach (var (name, viewType, viewModelType, accessObjectName) in tabConfigs)
        {
            if (!_accessControl.CanRead(user, accessObjectName)) continue;

            Tabs.Add(new ReferenceTabItem
            {
                Name = name,
                ViewType = viewType,
                ViewModelType = viewModelType,
            });
        }
    }

    public UserControl? GetOrCreateView(ReferenceTabItem tab)
    {
        if (tab.View is not null) return tab.View;

        var view = (UserControl)_serviceProvider.GetRequiredService(tab.ViewType);
        if (tab.ViewModelType is not null)
        {
            var viewModel = _serviceProvider.GetRequiredService(tab.ViewModelType);
            view.DataContext = viewModel;

            if (viewModel is ILoadableViewModel loadable)
            {
                var scheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
                _ = loadable.LoadAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception is not null)
                        _logger.LogError(t.Exception.InnerException, "LoadAsync failed for tab {TabName}", tab.Name);
                }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted, scheduler);
            }
        }

        tab.View = view;
        return view;
    }
}
