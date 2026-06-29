using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class EquipmentImportCatalogMatcherTests
{
    [Fact]
    public void ResolveOrCreate_ExistingNameAndUnit_ReturnsSameEquipment()
    {
        var existing = new Equipment { Id = 5, Name = "Насос Н-1", UnitId = 2 };
        var (lookup, ambiguous, duplicateCount) = EquipmentImportCatalogMatcher.BuildCatalogLookup([existing]);
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Насос Н-1", 2, lookup, ambiguous, pending);

        result.Created.Should().BeFalse();
        result.SkippedDuplicate.Should().BeFalse();
        result.Equipment.Id.Should().Be(5);
        pending.Should().BeEmpty();
        duplicateCount.Should().Be(0);
    }

    [Fact]
    public void ResolveOrCreate_SameNameDifferentUnit_CreatesSeparateItems()
    {
        var existing = new Equipment { Id = 5, Name = "Насос Н-1", UnitId = 2 };
        var (lookup, ambiguous, _) = EquipmentImportCatalogMatcher.BuildCatalogLookup([existing]);
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Насос Н-1", 3, lookup, ambiguous, pending);

        result.Created.Should().BeTrue();
        result.Equipment.UnitId.Should().Be(3);
        pending.Should().ContainSingle();
    }

    [Fact]
    public void ResolveOrCreate_UnknownName_CreatesPendingItemWithDefaults()
    {
        var (lookup, ambiguous, _) = EquipmentImportCatalogMatcher.BuildCatalogLookup(Array.Empty<Equipment>());
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Новая позиция", null, lookup, ambiguous, pending);

        result.Created.Should().BeTrue();
        result.Equipment.Name.Should().Be("Новая позиция");
        result.Equipment.ClassifierId.Should().BeNull();
        result.Equipment.IsAssessment.Should().BeFalse();
        result.Equipment.Coefficient.Should().Be(1m);
        result.Equipment.IsArchived.Should().BeFalse();
        pending.Should().ContainSingle();
    }

    [Fact]
    public void ResolveOrCreate_AmbiguousCatalogKey_SkipsDuplicate()
    {
        var rows = new[]
        {
            new Equipment { Id = 1, Name = "Насос", UnitId = 2 },
            new Equipment { Id = 2, Name = "Насос", UnitId = 2 },
        };
        var (lookup, ambiguous, duplicateCount) = EquipmentImportCatalogMatcher.BuildCatalogLookup(rows);
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Насос", 2, lookup, ambiguous, pending);

        result.SkippedDuplicate.Should().BeTrue();
        duplicateCount.Should().Be(1);
        pending.Should().BeEmpty();
    }

    [Fact]
    public void BuildImportSummary_IncludesAutoCreatedAndSkippedDuplicates()
    {
        var summary = EquipmentImportCatalogMatcher.BuildImportSummary(
            10,
            3,
            2,
            [(5, "Насос", "шт"), (7, "Клапан", "компл")]);

        summary.Should().Contain("Загружено: 10");
        summary.Should().Contain("Не найдено в справочнике: 3");
        summary.Should().Contain("Пропущено из-за дубликатов");
        summary.Should().Contain("строка 5");
    }

    [Fact]
    public void NormalizeUnitToken_MatchesWithAndWithoutDot()
    {
        EquipmentImportCatalogMatcher.NormalizeUnitToken("компл.")
            .Should().Be(EquipmentImportCatalogMatcher.NormalizeUnitToken("компл"));
    }

    [Fact]
    public void CatalogKeyHash_DistinguishesNameAndUnit()
    {
        var hash1 = EquipmentCatalogKeyHasher.Compute("Насос", 2);
        var hash2 = EquipmentCatalogKeyHasher.Compute("Насос", 3);
        var hash3 = EquipmentCatalogKeyHasher.Compute("Насос", null);

        hash1.Should().NotBe(hash2);
        hash1.Should().NotBe(hash3);
        hash1.Should().HaveLength(64);
    }
}

public sealed class EquipmentUnitNameMatcherTests
{
    [Fact]
    public void ResolveUnitId_MatchesCatalogUnitWithOptionalDot()
    {
        var units = new List<UnitOfMeasurement>
        {
            new() { Id = 7, Name = "компл." },
            new() { Id = 2, Name = "шт" },
        };

        EquipmentUnitNameMatcher.ResolveUnitId("компл", units).Should().Be(7);
        EquipmentUnitNameMatcher.ResolveUnitId("шт", units).Should().Be(2);
    }
}
