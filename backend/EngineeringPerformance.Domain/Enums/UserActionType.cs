namespace EngineeringPerformance.Domain.Enums;

/// <summary>
/// Категории действий пользователя для журнала аудита.
/// Имя enum-значения сохраняется в БД как nvarchar(64).
/// </summary>
public enum UserActionType
{
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    ProjectCreate = 10,
    ProjectEdit = 11,
    ProjectDelete = 12,
    VersionCreate = 20,
    VersionEdit = 21,
    VersionDelete = 22,
    VersionCopy = 23,
    PhysicalVolumeImport = 30,
    PhysicalVolumeDelete = 31,
    PhysicalVolumeEdit = 32,
    PhysicalVolumeCreate = 33,
    EquipmentImport = 34,
    EquipmentDelete = 35,
    EquipmentEdit = 36,
    EquipmentCreate = 37,
    LaborIntensityImport = 40,
    LaborIntensityDelete = 41,
    LaborIntensityEdit = 42,
    IicNormEdit = 50,
    PermissionMatrixUpdate = 51,
    UserSettingUpdate = 52,
}
