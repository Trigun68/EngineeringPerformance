using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class EquipmentImportCatalogMatcherTests
{
    [Fact]
    public void ResolveOrCreate_ExistingName_ReturnsSameEquipment()
    {
        var existing = new Equipment { Id = 5, Name = "Насос Н-1" };
        var lookup = EquipmentImportCatalogMatcher.BuildNameLookup([existing]);
        var pending = new List<Equipment>();

        var (equipment, created) = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Насос Н-1", lookup, pending);

        created.Should().BeFalse();
        equipment.Id.Should().Be(5);
        pending.Should().BeEmpty();
    }

    [Fact]
    public void ResolveOrCreate_UnknownName_CreatesPendingItem()
    {
        var lookup = EquipmentImportCatalogMatcher.BuildNameLookup(Array.Empty<Equipment>());
        var pending = new List<Equipment>();

        var (equipment, created) = EquipmentImportCatalogMatcher.ResolveOrCreate(
            "Новая позиция", lookup, pending);

        created.Should().BeTrue();
        equipment.Name.Should().Be("Новая позиция");
        equipment.ClassifierId.Should().BeNull();
        equipment.IsAssessment.Should().BeNull();
        equipment.Coefficient.Should().BeNull();
        pending.Should().ContainSingle();
        lookup["Новая позиция"].Should().BeSameAs(equipment);
    }

    [Fact]
    public void BuildImportSummary_IncludesAutoCreatedCount()
    {
        var summary = EquipmentImportCatalogMatcher.BuildImportSummary(10, 3);

        summary.Should().Contain("Загружено: 10");
        summary.Should().Contain("Не найдено в справочнике: 3");
        summary.Should().Contain("требуется классификация");
    }
}
