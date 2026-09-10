using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Infrastructure.Tests.Persistence;

public sealed class SqlConnectionStringLaneTests
{
    [Fact]
    public void ForInteractive_SetsPoolAndAppName()
    {
        const string baseCs =
            "Server=localhost;Database=EngineeringPerformance;User Id=sa;Password=x;TrustServerCertificate=True";

        var parsed = new SqlConnectionStringBuilder(SqlConnectionStringLane.ForInteractive(baseCs));

        parsed.ApplicationName.Should().Be(SqlConnectionStringLane.InteractiveAppName);
        parsed.MinPoolSize.Should().Be(2);
        parsed.MaxPoolSize.Should().Be(32);
        parsed.ConnectTimeout.Should().Be(5);
    }

    [Fact]
    public void ForAnalytics_CapsPoolSeparatelyFromInteractive()
    {
        const string baseCs =
            "Server=localhost;Database=EngineeringPerformance;User Id=sa;Password=x;TrustServerCertificate=True";

        var parsed = new SqlConnectionStringBuilder(SqlConnectionStringLane.ForAnalytics(baseCs));

        parsed.ApplicationName.Should().Be(SqlConnectionStringLane.AnalyticsAppName);
        parsed.MaxPoolSize.Should().Be(8);
        parsed.ConnectTimeout.Should().Be(15);
        parsed.ApplicationName.Should().NotBe(SqlConnectionStringLane.InteractiveAppName);
    }

    [Fact]
    public void ForResourceMonitor_UsesTinyPoolAndDistinctAppName()
    {
        const string baseCs =
            "Server=localhost;Database=EngineeringPerformance;User Id=sa;Password=x;TrustServerCertificate=True";

        var parsed = new SqlConnectionStringBuilder(SqlConnectionStringLane.ForResourceMonitor(baseCs));

        parsed.ApplicationName.Should().Be(SqlConnectionStringLane.ResourceMonitorAppName);
        parsed.MaxPoolSize.Should().Be(2);
        parsed.ConnectTimeout.Should().Be(2);
    }
}

public sealed class SqlConnectionTimeoutClassifierTests
{
    [Fact]
    public void IsUnavailable_DetectsTimeoutMessage()
    {
        var ex = new InvalidOperationException(
            "A connection was successfully established with the server, but then an error occurred during the pre-login handshake. Connection Timeout Expired.");

        SqlConnectionTimeoutClassifier.IsUnavailable(ex).Should().BeTrue();
    }

    [Fact]
    public void IsUnavailable_IgnoresOrdinaryInvalidOperation()
    {
        SqlConnectionTimeoutClassifier.IsUnavailable(new InvalidOperationException("Version not found."))
            .Should().BeFalse();
    }
}
