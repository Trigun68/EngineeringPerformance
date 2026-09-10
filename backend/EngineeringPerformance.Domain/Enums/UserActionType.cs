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
    /// <summary>Восстановление версии анализа.</summary>
    VersionRestore = 24,
    /// <summary>Назначение версии основной.</summary>
    VersionSetPrimary = 25,
    /// <summary>Пересчёт версии анализа.</summary>
    VersionRecalculate = 26,
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
    /// <summary>Создание записи трудоёмкости.</summary>
    LaborIntensityCreate = 43,
    /// <summary>Откат импорта данных.</summary>
    ImportRollback = 44,
    IicNormEdit = 50,
    PermissionMatrixUpdate = 51,
    UserSettingUpdate = 52,
    /// <summary>Изменение стоимости работ.</summary>
    WorkCostEdit = 53,
    /// <summary>Удаление стоимости работ.</summary>
    WorkCostDelete = 54,
    /// <summary>Создание записи справочника.</summary>
    DirectoryCreate = 60,
    /// <summary>Редактирование записи справочника.</summary>
    DirectoryEdit = 61,
    /// <summary>Архивирование записи справочника.</summary>
    DirectoryArchive = 62,
    /// <summary>Восстановление записи справочника из архива.</summary>
    DirectoryRestore = 63,
    /// <summary>Удаление записи справочника.</summary>
    DirectoryDelete = 64,
    /// <summary>Создание пользователя.</summary>
    UserCreate = 70,
    /// <summary>Редактирование пользователя.</summary>
    UserEdit = 71,
    /// <summary>Удаление пользователя.</summary>
    UserDelete = 72,
    /// <summary>Архивирование пользователя.</summary>
    UserArchive = 73,
    /// <summary>Восстановление пользователя из архива.</summary>
    UserRestore = 74,
    /// <summary>Смена пароля пользователя.</summary>
    UserPasswordChange = 75,
    /// <summary>Разблокировка пользователя.</summary>
    UserUnlock = 76,
    /// <summary>Сброс MFA пользователя.</summary>
    UserMfaReset = 77,
    /// <summary>Включение MFA.</summary>
    MfaEnable = 78,
    /// <summary>Отключение MFA.</summary>
    MfaDisable = 79,
    /// <summary>Изменение прав доступа.</summary>
    AccessRightUpdate = 80,
    /// <summary>Изменение прав на проект.</summary>
    ProjectPermissionUpdate = 81,
    /// <summary>Редактирование параметра планирования.</summary>
    PlanningParameterEdit = 82,
    /// <summary>Редактирование планового объёма.</summary>
    PlannedVolumeEdit = 83,
    /// <summary>Изменение системной настройки.</summary>
    SystemSettingUpdate = 84,
    /// <summary>Редактирование характеристики проекта.</summary>
    ProjectCharacteristicEdit = 85,
    /// <summary>Архивирование записи журнала аудита.</summary>
    AuditLogArchive = 86,
    /// <summary>Восстановление записи журнала аудита из архива.</summary>
    AuditLogRestore = 87,
}
