using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.ViewModels;

/// <summary>
/// ViewModel главной страницы: статический контент, без загрузки данных.
/// </summary>
public sealed class ProjectDashboardViewModel : ILoadableViewModel
{
    public Task LoadAsync() => Task.CompletedTask;
}
