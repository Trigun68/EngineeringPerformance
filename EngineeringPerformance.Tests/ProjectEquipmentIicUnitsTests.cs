using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class ProjectEquipmentIicUnitsTests
{
    [Fact]
    public void QuantityInIicGroupUnits_MultipliesByCoefficient()
    {
        var row = new ProjectEquipment
        {
            Quantity = 10m,
            Equipment = new Equipment { Coefficient = 2.5m },
        };

        row.QuantityInIicGroupUnits().Should().Be(25m);
    }

    [Fact]
    public void QuantityInIicGroupUnits_NullCoefficient_UsesOne()
    {
        var row = new ProjectEquipment
        {
            Quantity = 4m,
            Equipment = new Equipment { Coefficient = null },
        };

        row.QuantityInIicGroupUnits().Should().Be(4m);
    }
}
