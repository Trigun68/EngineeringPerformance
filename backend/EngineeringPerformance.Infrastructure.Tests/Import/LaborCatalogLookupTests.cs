using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Import;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Import;

public sealed class LaborCatalogLookupTests
{
    [Fact]
    public void BuildMap_MatchesNameWhenDocumentTypeNameEmpty()
    {
        var leaf = new LaborIntensity
        {
            Id = 7,
            Code = "27.AS_03",
            Name = "AS_03Схема структурная КТС (комплекс технических средств)",
            DocumentTypeName = null,
            IicWorkDirectionId = 12,
            IsArchived = false,
        };

        var map = LaborCatalogLookup.BuildMap([leaf]);
        map.Should().ContainKey("AS_03Схема структурная КТС (комплекс технических средств)");
        map["AS_03Схема структурная КТС (комплекс технических средств)"].Id.Should().Be(7);
    }

    [Fact]
    public void KnownKeys_IncludesNameAndDocumentTypeName()
    {
        var leaf = new LaborIntensity
        {
            Name = "AS_17Спецификация",
            DocumentTypeName = null,
        };

        LaborCatalogLookup.KnownKeys([leaf]).Should().Contain("AS_17Спецификация");
    }

    [Fact]
    public void BuildMap_PrefersDocumentTypeNameOverNameCollision()
    {
        var byDocType = new LaborIntensity
        {
            Id = 1,
            Name = "other",
            DocumentTypeName = "AS_03",
            IicWorkDirectionId = 10,
            IsArchived = false,
        };
        var byName = new LaborIntensity
        {
            Id = 2,
            Name = "AS_03",
            DocumentTypeName = null,
            IicWorkDirectionId = 11,
            IsArchived = false,
        };

        var map = LaborCatalogLookup.BuildMap([byDocType, byName]);
        map["AS_03"].Id.Should().Be(1);
    }
}
