using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class UsersViewModel : DirectoryViewModelBase<User>
{
    public Func<User?, Task>? OpenEditWindow { get; set; }

    public UsersViewModel(IDirectoryService<User> service) : base(service)
    {
    }

    protected override void ApplyRefreshedItem(User existing, User updated)
    {
        base.ApplyRefreshedItem(existing, updated);
        existing.Username = updated.Username;
        existing.DisplayName = updated.DisplayName;
        existing.RoleId = updated.RoleId;
        existing.Role = updated.Role;
        existing.WindowsUsername = updated.WindowsUsername;
        existing.IsActive = updated.IsActive;
    }

    [RelayCommand]
    private async Task EditItemAsync()
    {
        if (SelectedItem is null) return;

        var itemId = SelectedItem.Id;
        if (OpenEditWindow is not null)
            await OpenEditWindow(SelectedItem);
        await RefreshItemFromServiceAsync(itemId);
    }

    [RelayCommand]
    private async Task CreateAndEditAsync()
    {
        if (OpenEditWindow is not null)
            await OpenEditWindow(null);

        if (SelectedItem?.Id is int id && id > 0)
            await RefreshItemFromServiceAsync(id);
    }
}
