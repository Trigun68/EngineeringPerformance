using EngineeringPerformance.Application.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Кодирование грида «Детально» (ISS-019) обязано быть обратимым: клиент восстанавливает
/// ровно те же строки, иначе в гриде окажутся чужие значения.
/// </summary>
public sealed class DetailGridEncoderTests
{
    private static ProjectsPageDetailRowDto Row(
        int id,
        string catalogName,
        string facilityCode = "25-310",
        decimal? baseHours = null,
        string itemName = "") =>
        new(
            RowId: id,
            FacilityCode: facilityCode,
            FacilityName: "Объект",
            DocumentSet: "",
            CatalogCode: $"code-{id}",
            CatalogName: catalogName,
            WorkId: "w",
            WorkName: "Работа",
            IicGroupName: "IIC-1-3 — Бетонные конструкции (м³)",
            IicUnitName: "м³",
            Quantity: 10.5m,
            BaseHours: baseHours,
            OptimisticHours: null,
            PessimisticHours: null,
            FactLaborHours: null,
            FactProductivity: null,
            ZScore: null,
            Interpretation: "",
            IsEquipment: false,
            ItemName: itemName);

    /// <summary>Декодер клиента: те же правила, что в decodeEncodedGrid.ts.</summary>
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
    public void Encode_RoundTrip_RestoresEveryFieldValue()
    {
        var rows = Enumerable.Range(0, 60)
            .Select(i => Row(i, $"Наименование {i % 4}", baseHours: i % 3 == 0 ? null : i))
            .ToList();

        var decoded = Decode(DetailGridEncoder.Encode(rows));

        decoded.Should().HaveCount(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            decoded[i]["rowId"].Should().Be(rows[i].RowId);
            decoded[i]["catalogName"].Should().Be(rows[i].CatalogName);
            decoded[i]["catalogCode"].Should().Be(rows[i].CatalogCode);
            decoded[i]["quantity"].Should().Be(rows[i].Quantity);
            decoded[i]["baseHours"].Should().Be(rows[i].BaseHours);
            decoded[i]["isEquipment"].Should().Be(rows[i].IsEquipment);
        }
    }

    [Fact]
    public void Encode_InternsRepeatedStrings_AndKeepsUniqueOnesInline()
    {
        // 60 строк на 4 наименования — словарь выгоден; catalogCode уникален в каждой строке.
        var rows = Enumerable.Range(0, 60).Select(i => Row(i, $"Наименование {i % 4}")).ToList();

        var encoded = DetailGridEncoder.Encode(rows);

        encoded.Strings.Should().ContainKey("catalogName");
        encoded.Strings["catalogName"].Should().HaveCount(4);
        encoded.Strings.Should().NotContainKey("catalogCode", "уникальные значения словарь не сжимает");
    }

    [Fact]
    public void Encode_KeepsDuplicatedColumnsIndependent()
    {
        // itemName обычно совпадает с catalogName, но совпадение не гарантировано:
        // кодирование не должно выводить одно поле из другого.
        var rows = new List<ProjectsPageDetailRowDto>
        {
            Row(1, "Общее наименование", itemName: "Особое наименование"),
            Row(2, "Общее наименование", itemName: "Общее наименование"),
        };

        var decoded = Decode(DetailGridEncoder.Encode(rows));

        decoded[0]["itemName"].Should().Be("Особое наименование");
        decoded[0]["catalogName"].Should().Be("Общее наименование");
        decoded[1]["itemName"].Should().Be("Общее наименование");
    }

    [Fact]
    public void Encode_EmptyGrid_ProducesColumnsWithoutRows()
    {
        var encoded = DetailGridEncoder.Encode([]);

        encoded.Rows.Should().BeEmpty();
        encoded.Columns.Should().BeEquivalentTo(DetailGridEncoder.ColumnNames);
        encoded.Strings.Should().BeEmpty();
    }

    [Fact]
    public void ColumnNames_MatchCellCount()
    {
        var encoded = DetailGridEncoder.Encode([Row(1, "Наименование")]);

        encoded.Rows[0].Should().HaveCount(encoded.Columns.Count);
    }

    [Fact]
    public void ColumnNames_CoverEveryPropertyOfTheRow()
    {
        // Поле, добавленное в DTO, но забытое в кодировщике, молча не доедет до клиента.
        var expected = typeof(ProjectsPageDetailRowDto)
            .GetProperties()
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..]);

        DetailGridEncoder.ColumnNames.Should().BeEquivalentTo(expected);
    }
}
