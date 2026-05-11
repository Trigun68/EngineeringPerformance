using System.Windows;
using System.Windows.Controls;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.App.Views.Forms;
using EngineeringPerformance.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.App.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAccessControlService _accessControl;
    private readonly CurrentUserHolder _currentUserHolder;
    private readonly ILogger<NavigationService> _logger;
    private readonly Stack<string> _navigationHistory = [];

    // Кэш: viewName → (View, ViewModel). Заполняется при первом посещении вкладки.
    private readonly Dictionary<string, (UserControl View, object? ViewModel)> _viewCache = [];
    // Множество вкладок, для которых LoadAsync уже был вызван успешно.
    private readonly HashSet<string> _loaded = [];

    private ContentControl? _contentHost;

    public string CurrentView { get; private set; } = string.Empty;

    public NavigationService(IServiceProvider serviceProvider, IAccessControlService accessControl, CurrentUserHolder currentUserHolder, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _accessControl = accessControl;
        _currentUserHolder = currentUserHolder;
        _logger = logger;
    }

    public void SetContentHost(object host)
    {
        _contentHost = (ContentControl)host;
    }

    public void NavigateTo(string viewName)
    {
        if (_currentUserHolder.User is null || !_accessControl.CanRead(_currentUserHolder.User, viewName))
            return;

        if (!string.IsNullOrEmpty(CurrentView))
            _navigationHistory.Push(CurrentView);

        CurrentView = viewName;
        ShowView(viewName);
    }

    public void GoBack()
    {
        if (_navigationHistory.Count > 0)
        {
            CurrentView = _navigationHistory.Pop();
            ShowView(CurrentView);
        }
    }

    // Сбрасывает кэш при выходе пользователя, чтобы следующий логин начинал чисто.
    public void ResetCache()
    {
        _viewCache.Clear();
        _loaded.Clear();
        _navigationHistory.Clear();
        CurrentView = string.Empty;
    }

    private void ShowView(string viewName)
    {
        if (_contentHost is null)
            return;

        if (!_viewCache.TryGetValue(viewName, out var entry))
        {
            entry = CreateEntry(viewName);
            if (entry.View is null)
                return;

            if (entry.ViewModel is not null)
                entry.View.DataContext = entry.ViewModel;

            _viewCache[viewName] = entry;
        }

        var mainWindow = Application.Current?.MainWindow;
        switch (entry.ViewModel)
        {
            case AssessmentFormViewModel assessmentVm:
                assessmentVm.OwnerWindow = mainWindow;
                break;
            case PlanningFormViewModel planningVm:
                planningVm.OwnerWindow = mainWindow;
                break;
        }

        _contentHost.Content = entry.View;

        // LoadAsync вызывается только при первом показе вкладки.
        if (!_loaded.Contains(viewName) && entry.ViewModel is ILoadableViewModel loadable)
        {
            _loaded.Add(viewName);
            _ = loadable.LoadAsync().ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception is not null)
                {
                    _loaded.Remove(viewName);
                    _logger.LogError(t.Exception.Flatten(), "LoadAsync failed after navigation to {View}", viewName);
                }
            }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    private (UserControl View, object? ViewModel) CreateEntry(string viewName)
    {
        return viewName switch
        {
            "Dashboard"               => (_serviceProvider.GetRequiredService<ProjectDashboardView>(),         _serviceProvider.GetRequiredService<ProjectDashboardViewModel>()),
            "Users"                   => (_serviceProvider.GetRequiredService<UsersWindow>(),                  _serviceProvider.GetRequiredService<UsersViewModel>()),
            "Projects"                => (_serviceProvider.GetRequiredService<ProjectsPageView>(),             _serviceProvider.GetRequiredService<ProjectsPageViewModel>()),
            "PhysicalVolumes"         => (_serviceProvider.GetRequiredService<PhysicalVolumesWindow>(),        _serviceProvider.GetRequiredService<PhysicalVolumesViewModel>()),
            "PhysicalVolumeTypes"     => (_serviceProvider.GetRequiredService<PhysicalVolumeTypesWindow>(),    _serviceProvider.GetRequiredService<PhysicalVolumeTypesViewModel>()),
            "IicWorkDirections"       => (_serviceProvider.GetRequiredService<IicWorkDirectionsWindow>(),      _serviceProvider.GetRequiredService<IicWorkDirectionsViewModel>()),
            "UnitOfMeasurements"      => (_serviceProvider.GetRequiredService<UnitOfMeasurementsWindow>(),     _serviceProvider.GetRequiredService<UnitOfMeasurementsViewModel>()),
            "ProjectAnalysisVersions" => (_serviceProvider.GetRequiredService<ProjectAnalysisVersionsWindow>(), _serviceProvider.GetRequiredService<ProjectAnalysisVersionsViewModel>()),
            "IicWorkDirectionNorms"   => (_serviceProvider.GetRequiredService<IicWorkDirectionNormsWindow>(),  _serviceProvider.GetRequiredService<IicWorkDirectionNormsViewModel>()),
            "ProjectPhysicalVolumes"  => (_serviceProvider.GetRequiredService<ProjectPhysicalVolumesWindow>(), _serviceProvider.GetRequiredService<ProjectPhysicalVolumesViewModel>()),
            "ProjectLaborIntensities" => (_serviceProvider.GetRequiredService<ProjectLaborIntensitiesWindow>(), _serviceProvider.GetRequiredService<ProjectLaborIntensitiesViewModel>()),
            "ProjectDataUpload"       => (_serviceProvider.GetRequiredService<ProjectDataUploadForm>(),        null),
            "Assessment"              => (_serviceProvider.GetRequiredService<AssessmentForm>(),               _serviceProvider.GetRequiredService<AssessmentFormViewModel>()),
            "Planning"                => (_serviceProvider.GetRequiredService<PlanningForm>(),                 _serviceProvider.GetRequiredService<PlanningFormViewModel>()),
            "VersionComparison"       => (_serviceProvider.GetRequiredService<VersionComparisonForm>(),        null),
            "GroupAnalysis"           => (_serviceProvider.GetRequiredService<GroupAnalysisForm>(),            null),
            "GroupTrend"              => (_serviceProvider.GetRequiredService<GroupTrendForm>(),               null),
            "References"              => (_serviceProvider.GetRequiredService<ReferencesPageView>(),           _serviceProvider.GetRequiredService<ReferencesPageViewModel>()),
            "DataPage"                => (_serviceProvider.GetRequiredService<DataPageView>(),                 _serviceProvider.GetRequiredService<DataPageViewModel>()),
            "HelpPage"                => (_serviceProvider.GetRequiredService<InterpretationSettingsView>(),   _serviceProvider.GetRequiredService<InterpretationSettingsViewModel>()),
            "SettingsPage"            => (_serviceProvider.GetRequiredService<SettingsPageView>(),             _serviceProvider.GetRequiredService<SettingsPageViewModel>()),
            "UserActionLog"           => (_serviceProvider.GetRequiredService<UserActionLogView>(),            _serviceProvider.GetRequiredService<UserActionLogViewModel>()),
            _                         => (null!, null)
        };
    }
}
