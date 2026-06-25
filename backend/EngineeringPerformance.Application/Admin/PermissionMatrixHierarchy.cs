namespace EngineeringPerformance.Application.Admin;

/// <summary>
/// Иерархия объектов матрицы прав — зеркало WPF AccessControlConstants + группа «Проекты».
/// </summary>
public static class PermissionMatrixHierarchy
{
    public sealed record MatrixObjectNode(
        string ObjectName,
        string DisplayName,
        string? ParentGroup,
        int Level,
        IReadOnlyList<string> HierarchyPath,
        bool IsGroupHeader);

    private static readonly Dictionary<string, string> RussianNames = new(StringComparer.Ordinal)
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
        ["HelpPage"] = "Справка",
        ["SettingsPage"] = "Настройки",
        ["Dashboard"] = "Главная",
        ["UserActionLog"] = "Журнал действий",
        ["Administration"] = "Администрирование",
        ["ProjectsGroup"] = "Проекты",
    };

    private static readonly string[] ProjectsGroupChildren =
    [
        "Projects",
        "Assessment",
        "Planning",
        "VersionComparison",
        "GroupAnalysis",
        "GroupTrend",
        "ProjectAnalysisVersions",
        "ProjectPhysicalVolumes",
        "ProjectEquipments",
        "ProjectLaborIntensities",
        "ProjectDataUpload",
    ];

    private static readonly string[] ReferencesChildren =
    [
        "PhysicalVolumes",
        "Equipment",
        "LaborIntensities",
        "IicWorkDirections",
        "IicWorkDirectionNorms",
        "UnitsOfMeasurement",
        "ProjectCharacteristics",
    ];

    private static readonly string[] SettingsChildren = ["Users"];

    public static IReadOnlyList<MatrixObjectNode> Build()
    {
        var nodes = new List<MatrixObjectNode>();

        AddLeaf(nodes, "Dashboard");

        AddGroup(nodes, "ProjectsGroup", "Проекты", ProjectsGroupChildren);
        AddGroup(nodes, "References", ToRussian("References"), ReferencesChildren);
        AddGroup(nodes, "SettingsPage", ToRussian("SettingsPage"), SettingsChildren);

        AddLeaf(nodes, "UserActionLog");
        AddLeaf(nodes, "Administration");
        AddLeaf(nodes, "HelpPage");

        return nodes;
    }

    private static void AddLeaf(List<MatrixObjectNode> nodes, string objectName)
    {
        nodes.Add(new MatrixObjectNode(
            objectName,
            ToRussian(objectName),
            null,
            Level: 0,
            HierarchyPath: [objectName],
            IsGroupHeader: false));
    }

    private static void AddGroup(
        List<MatrixObjectNode> nodes,
        string groupId,
        string displayName,
        IReadOnlyList<string> children)
    {
        nodes.Add(new MatrixObjectNode(
            groupId,
            displayName,
            null,
            Level: 0,
            HierarchyPath: [groupId],
            IsGroupHeader: true));

        foreach (var child in children)
        {
            nodes.Add(new MatrixObjectNode(
                child,
                ToRussian(child),
                groupId,
                Level: 1,
                HierarchyPath: [groupId, child],
                IsGroupHeader: false));
        }
    }

    private static string ToRussian(string objectName) =>
        RussianNames.TryGetValue(objectName, out var display) ? display : objectName;
}
