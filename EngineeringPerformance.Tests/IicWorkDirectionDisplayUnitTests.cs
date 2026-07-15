using EngineeringPerformance.Infrastructure.Data;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class IicWorkDirectionDisplayUnitTests
{
    [Fact]
    public void FormatCodeNameWithResolvedUnit_AppendsUnitFromGroup()
    {
        var root = new IicWorkDirection
        {
            Id = 1,
            Code = "IIC",
            Name = "Root",
            Unit = new UnitOfMeasurement { Id = 10, Name = "час" }
        };
        var leaf = new IicWorkDirection
        {
            Id = 2,
            Code = "IIC-01",
            Name = "Проектирование",
            ParentId = 1
        };
        var byId = new Dictionary<int, IicWorkDirection> { [1] = root, [2] = leaf };

        IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(leaf, byId)
            .Should().Be("IIC-01 — Проектирование (час)");
    }

    [Fact]
    public void ResolvePhysicalVolumeUnitName_ReturnsNomenclatureUnit()
    {
        var pv = new PhysicalVolume
        {
            Unit = new UnitOfMeasurement { Name = "м³" }
        };

        IicWorkDirectionDisplayUnit.ResolvePhysicalVolumeUnitName(pv).Should().Be("м³");
    }
}
