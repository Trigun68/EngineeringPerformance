using EngineeringPerformance.Application.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Monitoring;

public sealed class ResourceCpuTests
{
    [Fact]
    public void CoresFromCpuSeconds_OneCoreFullyUsed_ReturnsOne()
    {
        var cores = ResourceCpu.CoresFromCpuSeconds(10d, 20d, TimeSpan.FromSeconds(10));
        cores.Should().Be(1d);
    }

    [Fact]
    public void CoresFromCpuSeconds_HalfCore_ReturnsPointFive()
    {
        var cores = ResourceCpu.CoresFromCpuSeconds(0d, 5d, TimeSpan.FromSeconds(10));
        cores.Should().Be(0.5d);
    }

    [Fact]
    public void CoresFromCpuSeconds_CounterReset_ReturnsZero()
    {
        ResourceCpu.CoresFromCpuSeconds(20d, 5d, TimeSpan.FromSeconds(10)).Should().Be(0d);
    }

    [Fact]
    public void CoresFromSqlProcessPercent_TwentyPercentOnFourCpus_IsPointEightCores()
    {
        ResourceCpu.CoresFromSqlProcessPercent(20, 4).Should().Be(0.8d);
    }

    [Fact]
    public void BytesToMib_OneMebibyte()
    {
        ResourceCpu.BytesToMib(1024 * 1024).Should().Be(1d);
    }
}
