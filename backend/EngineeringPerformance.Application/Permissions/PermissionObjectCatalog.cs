namespace EngineeringPerformance.Application.Permissions;

public static class PermissionObjectCatalog
{
    public const string AdministratorRoleName = "Administrator";

    /// <summary>Объект только для роли Administrator (мониторинг, admin API); в AccessRights не хранится.</summary>
    public const string AdministrationObjectName = "Administration";

    public static readonly string[] AllObjectNames =
    [
        "Dashboard", "Users", "Projects", "PhysicalVolumes", "PhysicalVolumeTypes",
        "Equipment", "IicWorkDirections", "ProjectAnalysisVersions",
        "IicWorkDirectionNorms", "UnitsOfMeasurement",
        "ProjectPhysicalVolumes", "ProjectEquipments", "ProjectLaborIntensities",
        "ProjectDataUpload", "Assessment", "Planning",
        "VersionComparison", "GroupAnalysis", "GroupTrend",
        "References", "DataPage", "SettingsPage", "HelpPage",
        "LaborIntensities", "ProjectCharacteristics", "UserActionLog",
        AdministrationObjectName
    ];

    /// <summary>Объекты, которые должны быть в таблице AccessRights для PM/Observer.</summary>
    public static readonly string[] MatrixObjectNames =
        AllObjectNames
            .Where(name => !string.Equals(name, AdministrationObjectName, StringComparison.Ordinal))
            .ToArray();
}
