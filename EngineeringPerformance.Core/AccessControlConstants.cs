namespace EngineeringPerformance.Core;

public static class AccessControlConstants
{
    public static readonly string[] AllObjectNames =
    [
        "Dashboard", "Users", "Projects", "PhysicalVolumes", "PhysicalVolumeTypes",
        "Equipment", "IicWorkDirections", "ProjectAnalysisVersions",
        "IicWorkDirectionNorms", "UnitsOfMeasurement",
        "ProjectPhysicalVolumes", "ProjectEquipments", "ProjectLaborIntensities",
        "ProjectDataUpload", "Assessment", "Planning",
        "VersionComparison", "GroupAnalysis", "GroupTrend",
        "References", "DataPage", "SettingsPage", "HelpPage",
        "LaborIntensities", "ProjectCharacteristics", "UserActionLog"
    ];

    // objectName → parent section name (null = top-level)
    public static readonly Dictionary<string, string?> ObjectParent = new()
    {
        ["Dashboard"]                  = null,
        ["Projects"]                   = null,
        ["Assessment"]                 = null,
        ["Planning"]                   = null,
        ["VersionComparison"]          = null,
        ["GroupAnalysis"]              = null,
        ["GroupTrend"]                 = null,
        ["DataPage"]                   = null,
        ["ProjectAnalysisVersions"]    = "DataPage",
        ["IicWorkDirectionNorms"]      = "DataPage",
        ["ProjectPhysicalVolumes"]     = "DataPage",
        ["ProjectEquipments"]          = "DataPage",
        ["ProjectLaborIntensities"]    = "DataPage",
        ["ProjectDataUpload"]          = "DataPage",
        ["References"]                 = null,
        ["PhysicalVolumeTypes"]        = "References",
        ["PhysicalVolumes"]            = "References",
        ["Equipment"]                  = "References",
        ["LaborIntensities"]           = "References",
        ["IicWorkDirections"]          = "References",
        ["UnitsOfMeasurement"]         = "References",
        ["ProjectCharacteristics"]     = "References",
        ["HelpPage"]                    = null,
        ["SettingsPage"]               = null,
        ["Users"]                      = "SettingsPage",
        ["UserActionLog"]              = "SettingsPage",
    };

    private static readonly Dictionary<string, string> ObjectNameRussianDisplayNames = new()
    {
        ["Users"] = "Пользователи",
        ["Projects"] = "Проекты",
        ["PhysicalVolumes"] = "Номенклатура физических объёмов",
        ["Equipment"] = "Номенклатура оборудования",
        ["PhysicalVolumeTypes"] = "Виды справочников физ. объёмов",
        ["IicWorkDirections"] = "Группы физ. объёмов (IIC)",
        ["LaborIntensities"] = "Трудоёмкость",
        ["ProjectAnalysisVersions"] = "Версии анализа проекта",
        ["IicWorkDirectionNorms"] = "Нормы групп физ. объёмов (IIC)",
        ["UnitsOfMeasurement"] = "Единицы измерения",
        ["ProjectCharacteristics"] = "Характеристики проекта",
        ["ProjectPhysicalVolumes"] = "Физ. объёмы по проекту",
        ["ProjectEquipments"] = "Оборудование по проекту",
        ["ProjectLaborIntensities"] = "Трудоёмкость по проекту",
        ["ProjectDataUpload"] = "Загрузка данных проекта",
        ["Assessment"] = "Оценка",
        ["Planning"] = "Планирование",
        ["VersionComparison"] = "Сравнение версий",
        ["GroupAnalysis"] = "Анализ по группам",
        ["GroupTrend"] = "Тренд по группам",
        ["References"] = "Справочники",
        ["DataPage"] = "Данные",
        ["HelpPage"] = "Справка",
        ["SettingsPage"] = "Настройки",
        ["Dashboard"] = "Главная",
        ["UserActionLog"] = "Журнал действий пользователей",
    };

    public static string ToRussianObjectName(string objectName)
    {
        return ObjectNameRussianDisplayNames.TryGetValue(objectName, out var display)
            ? display
            : objectName;
    }
}
