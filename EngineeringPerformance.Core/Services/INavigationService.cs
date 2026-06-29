namespace EngineeringPerformance.Core.Services;

public interface INavigationService
{
    void NavigateTo(string viewName);

    void GoBack();

    string CurrentView { get; }

    void SetContentHost(object host);
}
