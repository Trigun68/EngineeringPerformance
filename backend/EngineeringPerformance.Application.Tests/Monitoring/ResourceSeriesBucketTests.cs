using EngineeringPerformance.Application.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Monitoring;

public sealed class ResourceSeriesBucketTests
{
    [Theory]
    [InlineData(60, 10)]
    [InlineData(180, 30)]
    [InlineData(360, 30)]
    [InlineData(720, 60)]
    [InlineData(1440, 120)]
    [InlineData(4320, 300)]
    [InlineData(10080, 600)]
    [InlineData(43200, 3600)]
    [InlineData(86400, 7200)]
    [InlineData(129600, 10800)]
    public void SecondsForWindow_MatchesPlan(int windowMinutes, int expectedSeconds)
        => ResourceSeriesBucket.SecondsForWindow(windowMinutes).Should().Be(expectedSeconds);

    [Fact]
    public void ClampWindowMinutes_CapsAtNinetyDays()
        => ResourceSeriesBucket.ClampWindowMinutes(500_000).Should().Be(ResourceSeriesBucket.MaxWindowMinutes);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(90, 90)]
    [InlineData(365, 90)]
    public void ClampHistoryRetentionDays_StaysWithinNinety(int days, int expected)
        => ResourceSeriesBucket.ClampHistoryRetentionDays(days).Should().Be(expected);
}
