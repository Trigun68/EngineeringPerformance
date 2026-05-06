namespace EngineeringPerformance.App.ViewModels;

public sealed class NavigationGroup
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<NavigationItem> Items { get; init; } = [];

    public bool IsExpanded { get; init; } = true;
}

public sealed class NavigationItem
{
    public string Name { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public string ViewName { get; init; } = string.Empty;
}
