using FluentAssertions;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Import;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Import;

/// <summary>
/// Тесты для EquipmentImportCatalogMatcher — разрешение коллизий по (Name, UnitId).
/// </summary>
public sealed class EquipmentImportCatalogMatcherTests
{
    [Fact]
    public void BuildCatalogLookup_EmptyList_ReturnsEmptyLookup()
    {
        var rows = new List<Equipment>();

        var (lookup, ambiguous, duplicateCount) = EquipmentImportCatalogMatcher.BuildCatalogLookup(rows);

        lookup.Should().BeEmpty();
        ambiguous.Should().BeEmpty();
        duplicateCount.Should().Be(0);
    }

    [Fact]
    public void BuildCatalogLookup_UniqueKeys_ReturnsLookupWithAllItems()
    {
        var rows = new List<Equipment>
        {
            new() { Id = 1, Name = "Pump A", UnitId = 1 },
            new() { Id = 2, Name = "Pump B", UnitId = 1 },
            new() { Id = 3, Name = "Pump A", UnitId = 2 },
        };

        var (lookup, ambiguous, duplicateCount) = EquipmentImportCatalogMatcher.BuildCatalogLookup(rows);

        lookup.Should().HaveCount(3);
        lookup.Should().ContainKey(EquipmentImportCatalogMatcher.CreateKey("Pump A", 1));
        lookup.Should().ContainKey(EquipmentImportCatalogMatcher.CreateKey("Pump B", 1));
        lookup.Should().ContainKey(EquipmentImportCatalogMatcher.CreateKey("Pump A", 2));
        ambiguous.Should().BeEmpty();
        duplicateCount.Should().Be(0);
    }

    [Fact]
    public void BuildCatalogLookup_DuplicateKeys_ReturnsAmbiguousSet()
    {
        var rows = new List<Equipment>
        {
            new() { Id = 1, Name = "Pump A", UnitId = 1, IsArchived = false },
            new() { Id = 2, Name = "Pump A", UnitId = 1, IsArchived = false },
            new() { Id = 3, Name = "Pump B", UnitId = 1, IsArchived = false },
        };

        var (lookup, ambiguous, duplicateCount) = EquipmentImportCatalogMatcher.BuildCatalogLookup(rows);

        duplicateCount.Should().Be(1);
        ambiguous.Should().Contain(EquipmentImportCatalogMatcher.CreateKey("Pump A", 1));
        lookup.Should().ContainKey(EquipmentImportCatalogMatcher.CreateKey("Pump B", 1));
    }

    [Fact]
    public void ResolveOrCreate_ExistingKey_ReturnsExistingEquipment()
    {
        var equipment = new Equipment { Id = 1, Name = "Pump A", UnitId = 1 };
        var lookup = new Dictionary<EquipmentImportCatalogMatcher.EquipmentCatalogKey, Equipment>
        {
            [EquipmentImportCatalogMatcher.CreateKey("Pump A", 1)] = equipment
        };

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Pump A", 1, lookup, new HashSet<EquipmentImportCatalogMatcher.EquipmentCatalogKey>(), new List<Equipment>());

        result.Equipment.Should().Be(equipment);
        result.Created.Should().BeFalse();
        result.SkippedDuplicate.Should().BeFalse();
    }

    [Fact]
    public void ResolveOrCreate_NewKey_CreatesNewEquipment()
    {
        var lookup = new Dictionary<EquipmentImportCatalogMatcher.EquipmentCatalogKey, Equipment>();
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Pump B", 2, lookup, new HashSet<EquipmentImportCatalogMatcher.EquipmentCatalogKey>(), pending);

        result.Created.Should().BeTrue();
        result.SkippedDuplicate.Should().BeFalse();
        pending.Should().HaveCount(1);
        pending[0].Name.Should().Be("Pump B");
        pending[0].UnitId.Should().Be(2);
    }

    [Fact]
    public void ResolveOrCreate_AmbiguousKey_ReturnsSkipped()
    {
        var lookup = new Dictionary<EquipmentImportCatalogMatcher.EquipmentCatalogKey, Equipment>();
        var ambiguous = new HashSet<EquipmentImportCatalogMatcher.EquipmentCatalogKey>
        {
            EquipmentImportCatalogMatcher.CreateKey("Pump C", 1)
        };
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Pump C", 1, lookup, ambiguous, pending);

        result.SkippedDuplicate.Should().BeTrue();
        result.Created.Should().BeFalse();
        pending.Should().BeEmpty();
    }
}
