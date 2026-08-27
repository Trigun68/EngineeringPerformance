using EngineeringPerformance.Application.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class CompareDetailEncoderTests
{
    private static VersionCompareDetailRowDto Row(int i, string pvName) =>
        new(
            FacilityCode: "25-310",
            FacilityName: "Объект",
            DocumentSet: "",
            PvCode: $"code-{i}",
            PvName: pvName,
            WorkId: "w",
            WorkName: "Работа",
            QuantityA: i,
            QuantityB: i + 1);

    private static IReadOnlyList<Dictionary<string, object?>> Decode(EncodedGridDto encoded)
    {
        var result = new List<Dictionary<string, object?>>(encoded.Rows.Count);
        foreach (var cells in encoded.Rows)
        {
            var row = new Dictionary<string, object?>();
            for (var c = 0; c < encoded.Columns.Count; c++)
            {
                var name = encoded.Columns[c];
                row[name] = encoded.Strings.TryGetValue(name, out var table) && cells[c] is int index
                    ? table[index]
                    : cells[c];
            }

            result.Add(row);
        }

        return result;
    }

    [Fact]
    public void Encode_RoundTrip_RestoresPvNameAndHours()
    {
        var rows = Enumerable.Range(0, 60).Select(i => Row(i, $"Позиция {i % 4}")).ToList();
        var decoded = Decode(CompareDetailEncoder.Encode(rows));

        decoded.Should().HaveCount(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            decoded[i]["pvName"].Should().Be(rows[i].PvName);
            decoded[i]["pvCode"].Should().Be(rows[i].PvCode);
            decoded[i]["quantityA"].Should().Be(rows[i].QuantityA);
            decoded[i]["isEquipment"].Should().Be(rows[i].IsEquipment);
        }
    }

    [Fact]
    public void ColumnNames_CoverEveryPropertyOfTheRow()
    {
        var expected = typeof(VersionCompareDetailRowDto)
            .GetProperties()
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..]);

        CompareDetailEncoder.ColumnNames.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Encode_EmptyGrid_ProducesColumnsWithoutRows()
    {
        var encoded = CompareDetailEncoder.Encode([]);
        encoded.Rows.Should().BeEmpty();
        encoded.Columns.Should().BeEquivalentTo(CompareDetailEncoder.ColumnNames);
    }
}
