using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class EquipmentVolumeAggregatorTests
{
    [Fact]
    public void MergeInto_AddsEquipmentVolumeToExistingDirection()
    {
        var volumeByDirection = new Dictionary<int, decimal> { [10] = 100m };
        var rows = new[]
        {
            new ProjectEquipment
            {
                Quantity = 5m,
                Equipment = new Equipment { ClassifierId = 10, IsAssessment = true, Coefficient = 2m },
            },
        };

        EquipmentVolumeAggregator.MergeInto(volumeByDirection, rows);

        volumeByDirection[10].Should().Be(110m);
    }

    [Fact]
    public void MergeInto_SkipsNonAssessmentOrMissingClassifier()
    {
        var volumeByDirection = new Dictionary<int, decimal>();
        var rows = new[]
        {
            new ProjectEquipment
            {
                Quantity = 3m,
                Equipment = new Equipment { ClassifierId = null, IsAssessment = true },
            },
            new ProjectEquipment
            {
                Quantity = 4m,
                Equipment = new Equipment { ClassifierId = 7, IsAssessment = false },
            },
        };

        EquipmentVolumeAggregator.MergeInto(volumeByDirection, rows);

        volumeByDirection.Should().BeEmpty();
    }
}
