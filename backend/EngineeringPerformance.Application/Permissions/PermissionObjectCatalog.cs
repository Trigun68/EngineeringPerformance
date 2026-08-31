using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Permissions;

public static class PermissionObjectCatalog
{
    public const string SuperAdminRoleName = "SuperAdmin";

    /// <summary>Legacy DB/JWT name after Corporate Auth rename (Administrator → SuperAdmin).</summary>
    public const string LegacySuperAdminRoleName = "Administrator";

    /// <summary>Объект только для роли SuperAdmin (мониторинг, admin API); в AccessRights не хранится.</summary>
    public const string AdministrationObjectName = "Administration";

    public static readonly string[] AllObjectNames =
    [
        "Dashboard", "Users", "Projects", "PhysicalVolumes", "PhysicalVolumeTypes",
        "Equipment", "IicWorkDirections", "ProjectAnalysisVersions",
        "IicWorkDirectionNorms", "EngineeringWorkCosts", "UnitsOfMeasurement",
        "ProjectPhysicalVolumes", "ProjectEquipments", "ProjectLaborIntensities",
        "ProjectDataUpload", "Assessment", "Planning",
        "VersionComparison", "GroupAnalysis", "GroupTrend",
        "References", "DataPage", "SettingsPage", "HelpPage",
        "LaborIntensities", "ProjectCharacteristics", "UserActionLog",
        "Reports",
        AdministrationObjectName
    ];

    /// <summary>Объекты, которые должны быть в таблице AccessRights для PM/Observer.</summary>
    public static readonly string[] MatrixObjectNames =
        AllObjectNames
            .Where(name => !string.Equals(name, AdministrationObjectName, StringComparison.Ordinal))
            .ToArray();

    /// <summary>
    /// SuperAdmin bypass: текущее имя, legacy Administrator, или RoleId == 1.
    /// </summary>
    public static bool IsSuperAdmin(string? roleName, int? roleId = null)
    {
        if (roleId is (int)UserRole.SuperAdmin)
            return true;

        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        return string.Equals(roleName, SuperAdminRoleName, StringComparison.Ordinal)
            || string.Equals(roleName, LegacySuperAdminRoleName, StringComparison.Ordinal);
    }
}
