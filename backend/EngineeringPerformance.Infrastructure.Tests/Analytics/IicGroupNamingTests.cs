using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>Единый формат имён групп IIC: «код — наименование (ед.)».</summary>
public sealed class IicGroupNamingTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void ApplyFormattedWorkDirectionNames_UsesCodeEmDashNameAndUnit()
    {
        var unit = new UnitOfMeasurement { Id = 1, Name = "чел·ч" };
        var dir = new IicWorkDirection
        {
            Id = 10,
            Code = "IIC-2-1",
            Name = "Проектирование",
            Unit = unit,
        };
        var map = new Dictionary<int, IicWorkDirection> { [10] = dir };
        var rows = new List<PerformanceRowDto>
        {
            new(10, "Проектирование", "IIC-2-1", null, 0, "чел·ч", 1m, null, null, null, null, null, null, null, null),
        };

        var formatted = ProjectsPageGridBuilder.ApplyFormattedWorkDirectionNames(rows, map);

        formatted.Should().ContainSingle()
            .Which.WorkDirectionName.Should().Be("IIC-2-1 — Проектирование (чел·ч)");
    }

    [Fact]
    public void BuildFactGrids_PvIicGroupName_ContainsEmDashAndUnit()
    {
        const int dirId = 20;
        var unit = new UnitOfMeasurement { Id = 2, Name = "м²" };
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-3",
            Name = "Строительство",
            Unit = unit,
        };
        var kfo = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "KQ.01",
            Name = "Площадь",
            IsAssessment = true,
            ClassifierId = dirId,
            Coefficient = 1m,
        };
        var ppv = new ProjectPhysicalVolume
        {
            Id = 100,
            Quantity = 10m,
            FacilityCode = "01",
            FacilityName = "Объект",
            DocumentSet = "ДС-1",
            PhysicalVolumeKfoId = 5,
            PhysicalVolumeKfo = kfo,
        };

        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes = [ppv],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>(),
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [dirId] = classifier },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>(),
            NormIdByDirection = new Dictionary<int, int>(),
        };

        var performance = new List<PerformanceRowDto>
        {
            new(dirId, "Строительство", "IIC-3", null, 0, "м²", 10m, null, null, null, 1m, null, null, null, null),
        };

        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var pvRow = detail.Should().ContainSingle(r => !r.IsEquipment).Subject;
        pvRow.IicGroupName.Should().Contain("—");
        pvRow.IicGroupName.Should().Contain("(");
        pvRow.IicGroupName.Should().Be("IIC-3 — Строительство (м²)");
        pvRow.RowId.Should().Be(100);
    }
}
