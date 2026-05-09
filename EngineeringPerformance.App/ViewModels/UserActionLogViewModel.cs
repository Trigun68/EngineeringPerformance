using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Win32;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class UserActionLogViewModel : ObservableObject, ILoadableViewModel
{
    private const int PageSize = 1000;
    private const string AnyUserMarker = "— Все —";
    private const string AnyActionMarker = "— Все —";

    private readonly IUserActionLogService _journal;
    private readonly IDirectoryService<User> _userService;
    private readonly CurrentUserHolder _currentUser;

    [ObservableProperty] private ObservableCollection<UserActionLogDto> _rows = [];
    [ObservableProperty] private ObservableCollection<UserOption> _users = [];
    [ObservableProperty] private ObservableCollection<string> _actionTypes = [];

    [ObservableProperty] private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _toDate = DateTime.Today;
    [ObservableProperty] private UserOption? _selectedUser;
    [ObservableProperty] private string? _selectedActionType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;

    public UserActionLogViewModel(
        IUserActionLogService journal,
        IDirectoryService<User> userService,
        CurrentUserHolder currentUser)
    {
        _journal = journal;
        _userService = userService;
        _currentUser = currentUser;
    }

    public async Task LoadAsync()
    {
        await EnsureLookupsLoadedAsync();
        await ReloadAsync();
    }

    [RelayCommand]
    private Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private async Task ExportAndPurgeAsync()
    {
        if (!ConfirmAdminAction("Сохранить журнал в CSV-файл и удалить все записи из БД?")) return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV (UTF-8);*.csv|*.csv",
            FileName = $"user-action-log-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Title = "Сохранение журнала действий",
        };
        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            var result = await _journal.ExportToFileAndPurgeAsync(dialog.FileName);
            StatusMessage = $"Экспортировано {result.ExportedCount} записей в «{Path.GetFileName(result.FilePath)}». Удалено из БД: {result.DeletedCount}.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка экспорта: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV (UTF-8);*.csv|*.csv|Все файлы (*.*)|*.*",
            Title = "Восстановление журнала действий из файла",
        };
        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            var result = await _journal.ImportFromFileAsync(dialog.FileName);
            var summary = new StringBuilder();
            summary.Append($"Прочитано: {result.TotalRows}, добавлено: {result.ImportedCount}, дубли: {result.SkippedDuplicates}, orphan: {result.SkippedOrphans}, невалидные: {result.SkippedInvalid}.");
            if (result.Errors.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("Подробности:");
                foreach (var err in result.Errors.Take(10))
                    summary.AppendLine($"• {err}");
                if (result.Errors.Count > 10)
                    summary.AppendLine($"… и ещё {result.Errors.Count - 10}.");
            }
            StatusMessage = summary.ToString();
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка импорта: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EnsureLookupsLoadedAsync()
    {
        if (Users.Count == 0)
        {
            var allUsers = await _userService.GetAllAsync(includeArchived: true);
            Users.Clear();
            Users.Add(new UserOption(null, AnyUserMarker));
            foreach (var u in allUsers.OrderBy(u => u.DisplayName))
                Users.Add(new UserOption(u.Id, $"{u.DisplayName} ({u.Username})"));
            SelectedUser = Users[0];
        }

        if (ActionTypes.Count == 0)
        {
            ActionTypes.Add(AnyActionMarker);
            foreach (var name in Enum.GetNames<UserActionType>().OrderBy(n => n))
                ActionTypes.Add(name);
            SelectedActionType = AnyActionMarker;
        }
    }

    private async Task ReloadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var filter = new UserActionLogFilter(
                FromUtc: FromDate?.ToUniversalTime(),
                ToUtc: ToDate?.AddDays(1).AddTicks(-1).ToUniversalTime(),
                UserId: SelectedUser?.Id,
                ActionType: TryParseAction(SelectedActionType));

            var rows = await _journal.GetAsync(filter, skip: 0, take: PageSize);
            Rows = new ObservableCollection<UserActionLogDto>(rows);
            StatusMessage = $"Загружено записей: {rows.Count} (лимит {PageSize}).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static UserActionType? TryParseAction(string? raw) =>
        string.IsNullOrEmpty(raw) || raw == AnyActionMarker
            ? null
            : Enum.TryParse<UserActionType>(raw, out var v) ? v : null;

    private bool ConfirmAdminAction(string message)
    {
        var role = _currentUser.User?.RoleName;
        if (role != nameof(UserRole.Administrator))
        {
            ErrorMessage = "Действие доступно только администратору.";
            return false;
        }
        var result = MessageBox.Show(
            message,
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }
}

public sealed record UserOption(int? Id, string DisplayName);
