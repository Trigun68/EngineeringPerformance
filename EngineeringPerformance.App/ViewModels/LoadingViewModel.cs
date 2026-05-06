using CommunityToolkit.Mvvm.ComponentModel;

namespace EngineeringPerformance.App.ViewModels;

public partial class LoadingViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Загрузка...";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "Подготовка...";

    public void SetProgress(double value, string? status = null)
    {
        Progress = value;
        if (status is not null)
            StatusText = status;
    }

    public void SetTitle(string title)
    {
        Title = title;
    }
}