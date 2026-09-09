using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.LaborIicBindings;

public sealed class LaborIntensityIicBindingPeriodTests
{
    [Fact]
    public void AutoClosePrevious_ClosesOpenPeriodDayBeforeNewFrom()
    {
        var previous = new LaborIntensityIicBinding
        {
            EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
        };
        var from = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        LaborIntensityIicBindingService.AutoClosePrevious([previous], from, DateTime.UtcNow);

        previous.EffectiveTo.Should().Be(new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ResolveAsOf_UsesPeriodCoveringVersionDate()
    {
        var old = new LaborIntensityIicBinding
        {
            IicWorkDirectionId = 1,
            EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc),
        };
        var next = new LaborIntensityIicBinding
        {
            IicWorkDirectionId = 11,
            EffectiveFrom = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
        };

        LaborIntensityIicBindingService.ResolveAsOf([old, next], new DateTime(2024, 3, 1))
            .Should().Be(1);
        LaborIntensityIicBindingService.ResolveAsOf([old, next], new DateTime(2024, 6, 2))
            .Should().Be(11);
    }
}
