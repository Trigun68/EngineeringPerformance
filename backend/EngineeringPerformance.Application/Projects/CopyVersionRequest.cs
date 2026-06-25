namespace EngineeringPerformance.Application.Projects;

public sealed record CopyVersionRequest(string NewVersionName, string? Description = null);
