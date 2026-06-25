namespace EngineeringPerformance.Application.Users;

public sealed record PutPreferencesRequest(
    IReadOnlyDictionary<string, string>? Preferences = null,
    string? Key = null,
    string? Value = null);
