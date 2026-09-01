namespace EngineeringPerformance.Domain.Enums;

/// <summary>
/// Типы объектов доступа — соответствуют ключам AccessControlConstants (Projects, PhysicalVolumes и т.д.).
/// </summary>
public enum PermissionType
{
    Dashboard,
    Users,
    Projects,
    PhysicalVolumes,
    PhysicalVolumeTypes,
    Equipment,
    IicWorkDirections,
    ProjectAnalysisVersions,
    IicWorkDirectionNorms,
    UnitsOfMeasurement,
    ProjectPhysicalVolumes,
    ProjectEquipments,
    ProjectLaborIntensities,
    ProjectDataUpload,
    Assessment,
    Planning,
    VersionComparison,
    GroupAnalysis,
    GroupTrend,
    References,
    DataPage,
    SettingsPage,
    HelpPage,
    LaborIntensities,
    ProjectCharacteristics,
    UserActionLog,
    Reports,
    Administration,
    EngineeringWorkCosts,
    DocSets
}

public static class PermissionTypeExtensions
{
    public static string ToObjectName(this PermissionType permission) =>
        permission switch
        {
            PermissionType.UnitsOfMeasurement => "UnitsOfMeasurement",
            _ => permission.ToString()
        };

    public static PermissionType? FromObjectName(string objectName) =>
        Enum.TryParse<PermissionType>(objectName, out var value) ? value : null;
}
