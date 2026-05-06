using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineeringPerformance.Core;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed partial class AccessRightsViewModel : ObservableObject, ILoadableViewModel
{
    private readonly IAccessControlService _accessControl;
    private Dictionary<(string, string), AccessRightDto> _rightsMap = [];

    public ObservableCollection<AccessLevelOption> AccessLevelOptions { get; } =
    [
        new AccessLevelOption { Value = AccessLevel.None, Display = "Нет доступа" },
        new AccessLevelOption { Value = AccessLevel.Read, Display = "Чтение" },
        new AccessLevelOption { Value = AccessLevel.Edit, Display = "Редактирование" },
    ];

    [ObservableProperty] private ObservableCollection<AccessRightMatrixRow> _rows = [];
    [ObservableProperty] private List<string> _roleNames = [];
    [ObservableProperty] private bool _hasChanges;

    public AccessRightsViewModel(IAccessControlService accessControl)
    {
        _accessControl = accessControl;
    }

    public async Task LoadAsync()
    {
        var rights = await _accessControl.GetAllAccessRightsAsync();
        _rightsMap = rights.ToDictionary(r => (r.ObjectName, r.RoleName));
        RoleNames = rights.Select(r => r.RoleName).Distinct().Order().ToList();

        // Build ordered list: section header rows + child rows
        var sectionChildren = AccessControlConstants.ObjectParent
            .Where(kv => kv.Value is not null)
            .GroupBy(kv => kv.Value!)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

        var ordered = new List<AccessRightMatrixRow>();

        // Top-level items (no parent) — emit as section header if they have children, else as plain row
        foreach (var objectName in AccessControlConstants.AllObjectNames)
        {
            if (!AccessControlConstants.ObjectParent.TryGetValue(objectName, out var parent) || parent is null)
            {
                bool hasChildren = sectionChildren.ContainsKey(objectName);
                var headerRow = new AccessRightMatrixRow(
                    objectName,
                    AccessControlConstants.ToRussianObjectName(objectName),
                    isHeader: hasChildren,
                    indent: 0);

                if (!hasChildren)
                    FillCells(headerRow);

                ordered.Add(headerRow);

                if (hasChildren)
                {
                    foreach (var child in sectionChildren[objectName])
                    {
                        var childRow = new AccessRightMatrixRow(
                            child,
                            AccessControlConstants.ToRussianObjectName(child),
                            isHeader: false,
                            indent: 1);
                        FillCells(childRow);
                        ordered.Add(childRow);
                    }
                }
            }
        }

        Rows.Clear();
        foreach (var row in ordered)
        {
            row.CellChanged += () => HasChanges = true;
            Rows.Add(row);
        }

        HasChanges = false;
    }

    private void FillCells(AccessRightMatrixRow row)
    {
        foreach (var role in RoleNames)
        {
            if (_rightsMap.TryGetValue((row.ObjectName, role), out var dto))
                row.SetCell(role, dto.Id, dto.RoleId, dto.AccessLevel);
        }
    }

    [RelayCommand] private async Task RefreshAsync() => await LoadAsync();

    public async Task SaveAsync() => await SaveAllAsync();

    [RelayCommand]
    private async Task SaveAllAsync()
    {
        var modified = Rows
            .SelectMany(r => r.Cells.Where(c => c.Value.IsModified).Select(c => (r, c.Key, c.Value)))
            .ToList();

        foreach (var (row, role, cell) in modified)
        {
            if (!_rightsMap.TryGetValue((row.ObjectName, role), out var dto)) continue;
            await _accessControl.UpdateAccessRightAsync(dto with { AccessLevel = cell.AccessLevel });
            cell.IsModified = false;
        }

        _accessControl.ClearCache();
        HasChanges = false;
    }
}

public sealed class AccessLevelOption
{
    public AccessLevel Value { get; set; }
    public string Display { get; set; } = string.Empty;
}

public sealed class AccessRightMatrixRow : ObservableObject
{
    public string ObjectName { get; }
    public string ObjectNameDisplay { get; }
    public bool IsHeader { get; }
    public int Indent { get; }
    public Dictionary<string, AccessRightCell> Cells { get; } = [];
    public event Action? CellChanged;

    public AccessRightMatrixRow(string objectName, string display, bool isHeader, int indent)
    {
        ObjectName = objectName;
        ObjectNameDisplay = display;
        IsHeader = isHeader;
        Indent = indent;
    }

    public void SetCell(string role, int id, int roleId, AccessLevel level)
    {
        var cell = new AccessRightCell(id, roleId, level);
        cell.Changed += () => CellChanged?.Invoke();
        Cells[role] = cell;
    }

    public AccessRightCell? GetCell(string role) =>
        Cells.TryGetValue(role, out var c) ? c : null;
}

public sealed class AccessRightCell : ObservableObject
{
    public int Id { get; }
    public int RoleId { get; }
    public bool IsModified { get; set; }
    public event Action? Changed;

    private AccessLevel _accessLevel;
    public AccessLevel AccessLevel
    {
        get => _accessLevel;
        set { if (SetProperty(ref _accessLevel, value)) { IsModified = true; Changed?.Invoke(); } }
    }

    public AccessRightCell(int id, int roleId, AccessLevel level)
    {
        Id = id; RoleId = roleId; _accessLevel = level;
    }
}
