namespace EngineeringPerformance.Domain.Entities;

/// <summary>Единый доступ к полям каталога КФО/БЭР через ProjectPhysicalVolume.</summary>
public static class ProjectPhysicalVolumeExtensions
{
    public static string GetVolumeCode(this ProjectPhysicalVolume ppv) =>
        ppv.PhysicalVolumeKfo?.Code ?? ppv.PhysicalVolumeBer?.Code ?? string.Empty;

    public static string GetVolumeName(this ProjectPhysicalVolume ppv) =>
        ppv.PhysicalVolumeKfo?.Name ?? ppv.PhysicalVolumeBer?.Name ?? string.Empty;

    public static decimal GetCoefficient(this ProjectPhysicalVolume ppv) =>
        ppv.PhysicalVolumeKfo?.Coefficient ?? ppv.PhysicalVolumeBer?.Coefficient ?? 1m;

    public static int? GetClassifierId(this ProjectPhysicalVolume ppv) =>
        ppv.PhysicalVolumeKfo?.ClassifierId ?? ppv.PhysicalVolumeBer?.ClassifierId;

    public static bool? GetIsAssessment(this ProjectPhysicalVolume ppv) =>
        ppv.PhysicalVolumeKfo?.IsAssessment ?? ppv.PhysicalVolumeBer?.IsAssessment;

    public static int? GetCatalogVolumeId(this ProjectPhysicalVolume ppv) =>
        ppv.PhysicalVolumeKfoId ?? ppv.PhysicalVolumeBerId;
}
