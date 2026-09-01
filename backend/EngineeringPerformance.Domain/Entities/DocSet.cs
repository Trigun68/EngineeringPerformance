using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

/// <summary>Справочник комплектов документации (DocSet) для распределения трудоёмкости факта.</summary>
public sealed class DocSet : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];
}
