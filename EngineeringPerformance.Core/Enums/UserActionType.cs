namespace EngineeringPerformance.Core.Enums;

// Категории действий пользователя для журнала аудита.
// Имя enum-значения сохраняется в БД как nvarchar(64) для читабельности.
public enum UserActionType
{
    // Аутентификация
    Login = 1,
    Logout = 2,
    LoginFailed = 3,

    // Проекты
    ProjectCreate = 10,
    ProjectEdit = 11,
    ProjectDelete = 12,

    // Версии анализа
    VersionCreate = 20,
    VersionEdit = 21,
    VersionDelete = 22,

    // Физические объёмы (ФО)
    PhysicalVolumeImport = 30,
    PhysicalVolumeDelete = 31,
    PhysicalVolumeEdit = 32,

    // Трудоёмкость
    LaborIntensityImport = 40,
    LaborIntensityDelete = 41,
    LaborIntensityEdit = 42,

    // Норма IIC (детальная сетка)
    IicNormEdit = 50,
}
