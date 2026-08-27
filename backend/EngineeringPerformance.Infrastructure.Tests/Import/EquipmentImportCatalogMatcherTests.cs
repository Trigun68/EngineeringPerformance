using FluentAssertions;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Import;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Import;

/// <summary>
/// Тесты для EquipmentImportCatalogMatcher — проектный справочник и копирование признаков.
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
    public void BuildCatalogLookup_ForProject_IgnoresOtherProjects()
    {
        var rows = new List<Equipment>
        {
            new() { Id = 1, Name = "Pump A", UnitId = 1, ProjectId = 10 },
            new() { Id = 2, Name = "Pump A", UnitId = 1, ProjectId = 20 },
        };

        var (lookup, ambiguous, duplicateCount) =
            EquipmentImportCatalogMatcher.BuildCatalogLookup(rows, projectId: 10);

        lookup.Should().HaveCount(1);
        lookup[EquipmentImportCatalogMatcher.CreateKey("Pump A", 1)].Id.Should().Be(1);
        ambiguous.Should().BeEmpty();
        duplicateCount.Should().Be(0);
    }

    [Fact]
    public void BuildCatalogLookup_DuplicateActiveKeys_ReturnsAmbiguousSet()
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
    public void BuildCatalogLookup_ActivePlusArchivedTwin_UsesActiveNotAmbiguous()
    {
        var active = new Equipment { Id = 10, Name = "Pump A", UnitId = 1, IsArchived = false, IsAssessment = true };
        var archived = new Equipment { Id = 2, Name = "Pump A", UnitId = 1, IsArchived = true, IsAssessment = false };
        var rows = new List<Equipment> { archived, active };

        var (lookup, ambiguous, duplicateCount) = EquipmentImportCatalogMatcher.BuildCatalogLookup(rows);

        duplicateCount.Should().Be(0);
        ambiguous.Should().BeEmpty();
        lookup[EquipmentImportCatalogMatcher.CreateKey("Pump A", 1)].Should().BeSameAs(active);
    }

    [Fact]
    public void ResolveOrCreate_ExistingInProject_ReturnsExisting()
    {
        var equipment = new Equipment { Id = 1, Name = "Pump A", UnitId = 1, ProjectId = 10 };
        var lookup = new Dictionary<EquipmentImportCatalogMatcher.EquipmentCatalogKey, Equipment>
        {
            [EquipmentImportCatalogMatcher.CreateKey("Pump A", 1)] = equipment
        };

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            10,
            "Pump A",
            1,
            lookup,
            new HashSet<EquipmentImportCatalogMatcher.EquipmentCatalogKey>(),
            [equipment],
            new List<Equipment>());

        result.Equipment.Should().Be(equipment);
        result.Created.Should().BeFalse();
        result.SkippedDuplicate.Should().BeFalse();
        result.CopiedFromOtherProject.Should().BeFalse();
    }

    [Fact]
    public void ResolveOrCreate_MissingEverywhere_CreatesStub()
    {
        var lookup = new Dictionary<EquipmentImportCatalogMatcher.EquipmentCatalogKey, Equipment>();
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            10,
            "Pump B",
            2,
            lookup,
            new HashSet<EquipmentImportCatalogMatcher.EquipmentCatalogKey>(),
            [],
            pending);

        result.Created.Should().BeTrue();
        result.CopiedFromOtherProject.Should().BeFalse();
        pending.Should().HaveCount(1);
        pending[0].ProjectId.Should().Be(10);
        pending[0].Coefficient.Should().Be(1m);
        pending[0].IsAssessment.Should().BeFalse();
        pending[0].ClassifierId.Should().BeNull();
    }

    [Fact]
    public void ResolveOrCreate_FoundInOtherProject_CopiesFlagsFromFreshestDonor()
    {
        var older = new Equipment
        {
            Id = 1,
            Name = "Pump A",
            UnitId = 1,
            ProjectId = 20,
            Coefficient = 2m,
            IsAssessment = true,
            ClassifierId = 5,
            ModifiedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var fresher = new Equipment
        {
            Id = 2,
            Name = "Pump A",
            UnitId = 1,
            ProjectId = 30,
            Coefficient = 3.5m,
            IsAssessment = true,
            ClassifierId = 7,
            ModifiedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var lookup = new Dictionary<EquipmentImportCatalogMatcher.EquipmentCatalogKey, Equipment>();
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            10,
            "Pump A",
            1,
            lookup,
            new HashSet<EquipmentImportCatalogMatcher.EquipmentCatalogKey>(),
            [older, fresher],
            pending);

        result.Created.Should().BeTrue();
        result.CopiedFromOtherProject.Should().BeTrue();
        pending.Should().HaveCount(1);
        pending[0].ProjectId.Should().Be(10);
        pending[0].Coefficient.Should().Be(3.5m);
        pending[0].IsAssessment.Should().BeTrue();
        pending[0].ClassifierId.Should().Be(7);
        pending[0].ParentId.Should().BeNull();
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
            10,
            "Pump C",
            1,
            lookup,
            ambiguous,
            [],
            pending);

        result.SkippedDuplicate.Should().BeTrue();
        result.Created.Should().BeFalse();
        pending.Should().BeEmpty();
    }

    [Fact]
    public void ResolveOrCreate_LegacyOverload_CreatesStubWithoutProject()
    {
        var lookup = new Dictionary<EquipmentImportCatalogMatcher.EquipmentCatalogKey, Equipment>();
        var pending = new List<Equipment>();

        var result = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Pump B", 2, lookup, new HashSet<EquipmentImportCatalogMatcher.EquipmentCatalogKey>(), pending);

        result.Created.Should().BeTrue();
        pending[0].Name.Should().Be("Pump B");
        pending[0].UnitId.Should().Be(2);
    }
}
