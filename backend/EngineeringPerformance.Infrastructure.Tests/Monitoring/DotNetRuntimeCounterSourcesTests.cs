using EngineeringPerformance.Infrastructure.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Monitoring;

public sealed class DotNetRuntimeCounterSourcesTests
{
    [Theory]
    [InlineData("Microsoft-AspNetCore-Server-Kestrel", true)]
    [InlineData("Microsoft.AspNetCore.Server.Kestrel", true)]
    [InlineData("Microsoft.AspNetCore.Hosting", true)]
    [InlineData("System.Runtime", false)]
    [InlineData(null, false)]
    public void IsHttpServerSource_MatchesKestrelAndHosting(string? name, bool expected)
        => DotNetRuntimeCounterSources.IsHttpServerSource(name).Should().Be(expected);
}
