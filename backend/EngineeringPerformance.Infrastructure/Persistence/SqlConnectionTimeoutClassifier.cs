namespace EngineeringPerformance.Infrastructure.Persistence;

/// <summary>
/// Обёртка: классификатор живёт в Application, чтобы Api не импортировал Persistence.
/// </summary>
public static class SqlConnectionTimeoutClassifier
{
    public static bool IsUnavailable(Exception exception) =>
        Application.Diagnostics.SqlConnectionTimeoutClassifier.IsUnavailable(exception);
}
