namespace EngineeringPerformance.Api.Tests;

/// <summary>
/// В CI тесты с БД не должны молча пропускаться (C-04 / IMP-04).
/// Вне CI: <see cref="EnsureSqlAvailable"/> возвращает false → тесты делают early return.
/// </summary>
internal static class IntegrationTestSqlGate
{
    private static readonly bool InCi =
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    public static void RequireAvailable(bool sqlAvailable)
    {
        if (InCi && !sqlAvailable)
        {
            throw new InvalidOperationException(
                "Integration database is unavailable in CI. Ensure SQL Server is started (docker compose up sqlserver db-init).");
        }
    }
}
