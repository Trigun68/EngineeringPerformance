using System.Globalization;
using EngineeringPerformance.Application.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class PhysicalVolumeByUnitAggregatorTests
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private static string N0(decimal value) => value.ToString("N0", Ru);

    [Fact]
    public void Aggregate_Empty_ReturnsEmpty()
    {
        PhysicalVolumeByUnitAggregator.Aggregate([]).Should().BeEmpty();
        PhysicalVolumeByUnitAggregator.Format([]).Should().BeEmpty();
        PhysicalVolumeByUnitAggregator.FormatOrNull([]).Should().BeNull();
    }

    [Fact]
    public void Aggregate_SingleUnit_SumsAndFormatsAsInteger()
    {
        var totals = PhysicalVolumeByUnitAggregator.Aggregate(
        [
            ("м²", 100m),
            ("м²", 50.5m),
            ("  м²  ", 0m),
            (null, 10m),
            ("", 5m),
        ]);

        totals.Should().ContainSingle()
            .Which.Should().Be(("м²", 151m));
        PhysicalVolumeByUnitAggregator.Format(totals).Should().Be($"{N0(151)} (м²)");
    }

    [Fact]
    public void Aggregate_MultipleUnits_SortedAndFormatted()
    {
        var totals = PhysicalVolumeByUnitAggregator.Aggregate(
        [
            ("шт.", 7m),
            ("м²", 100m),
            ("шт.", 0.4m),
        ]);

        totals.Should().HaveCount(2);
        totals[0].Unit.Should().Be("м²");
        totals[0].Total.Should().Be(100m);
        totals[1].Unit.Should().Be("шт.");
        totals[1].Total.Should().Be(7m);

        PhysicalVolumeByUnitAggregator.Format(totals)
            .Should().Be($"{N0(100)} (м²); {N0(7)} (шт.)");
    }

    [Fact]
    public void Aggregate_RoundsToWholeNumber_AwayFromZero()
    {
        var totals = PhysicalVolumeByUnitAggregator.Aggregate([("т", 1.234m), ("т", 0.006m)]);
        totals.Should().ContainSingle().Which.Total.Should().Be(1m);
    }

    [Fact]
    public void Format_UsesThousandsSeparator_NoFraction()
    {
        var totals = PhysicalVolumeByUnitAggregator.Aggregate([("м³", 12345.6m)]);
        PhysicalVolumeByUnitAggregator.Format(totals).Should().Be($"{N0(12346)} (м³)");
        PhysicalVolumeByUnitAggregator.Format(totals).Should().NotContain(",");
        PhysicalVolumeByUnitAggregator.Format(totals).Should().NotContain(".");
    }

    [Fact]
    public void FormatFromPerformance_UsesNormUnitAndVolume()
    {
        var rows = new List<PerformanceRowDto>
        {
            new(1, "A", "A", null, 0, "м²", 10m, null, null, null, null, null, null, null, null),
            new(2, "B", "B", 1, 1, "шт.", 3m, null, null, null, null, null, null, null, null),
            new(3, "C", "C", 1, 1, "м²", 5m, null, null, null, null, null, null, null, null),
        };

        PhysicalVolumeByUnitAggregator.FormatFromPerformance(rows)
            .Should().Be($"{N0(15)} (м²); {N0(3)} (шт.)");
    }
}
