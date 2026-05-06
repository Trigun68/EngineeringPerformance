using CommunityToolkit.Mvvm.ComponentModel;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ProjectDashboardViewModel : ObservableObject, ILoadableViewModel
{
    // Признак «активного» проекта — его статус работ в справочнике.
    // Соответствует seed-значению DatabaseSeeder ("В работе").
    private const string ActiveStatus = "В работе";

    private readonly IProjectVisibilityService _projectVisibility;
    private readonly CurrentUserHolder _currentUserHolder;

    [ObservableProperty]
    private int _activeProjectsCount;

    [ObservableProperty]
    private bool _isBusy;

    public ProjectDashboardViewModel(IProjectVisibilityService projectVisibility, CurrentUserHolder currentUserHolder)
    {
        _projectVisibility = projectVisibility;
        _currentUserHolder = currentUserHolder;
    }

    public async Task LoadAsync()
    {
        if (_currentUserHolder.User is null)
            return;

        IsBusy = true;
        try
        {
            var projects = await _projectVisibility.GetAccessibleProjectsAsync(_currentUserHolder.User);
            ActiveProjectsCount = projects.Count(p => string.Equals(p.WorkStatus, ActiveStatus, StringComparison.Ordinal));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
