using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class ConfirmDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Подтверждение";
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _confirmButtonText = "OK";
    [ObservableProperty] private string _cancelButtonText = "Отмена";

    public void Prepare(string title, string message, string confirmButtonText, string cancelButtonText = "Отмена")
    {
        Title = title;
        Message = message;
        ConfirmButtonText = confirmButtonText;
        CancelButtonText = cancelButtonText;
    }

    [RelayCommand]
    private void Confirm() => CloseDialog(true);

    [RelayCommand]
    private void Cancel() => CloseDialog(false);

    private void CloseDialog(bool result)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window window)
                window.DialogResult = result;
        });
    }
}
