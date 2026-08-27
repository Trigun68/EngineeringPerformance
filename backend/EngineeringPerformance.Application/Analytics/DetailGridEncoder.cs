namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Упаковка грида «Детально» в <see cref="EncodedGridDto"/> (ISS-019).
/// Кодирование чисто транспортное: набор и значения полей не меняются, поэтому дублирующие
/// по данным колонки (например itemName и catalogName) остаются — их схлопывает словарь строк,
/// а не предположение об их равенстве.
/// </summary>
public static class DetailGridEncoder
{
    /// <summary>
    /// Единый список колонок: имя и доступ к значению рядом, поэтому порядок ячеек
    /// не может разойтись с порядком имён.
    /// </summary>
    private static readonly (string Name, Func<ProjectsPageDetailRowDto, object?> Get)[] Columns =
    [
        ("rowId", r => r.RowId),
        ("facilityCode", r => r.FacilityCode),
        ("facilityName", r => r.FacilityName),
        ("documentSet", r => r.DocumentSet),
        ("catalogCode", r => r.CatalogCode),
        ("catalogName", r => r.CatalogName),
        ("workId", r => r.WorkId),
        ("workName", r => r.WorkName),
        ("iicGroupName", r => r.IicGroupName),
        ("iicUnitName", r => r.IicUnitName),
        ("quantity", r => r.Quantity),
        ("baseHours", r => r.BaseHours),
        ("optimisticHours", r => r.OptimisticHours),
        ("pessimisticHours", r => r.PessimisticHours),
        ("factLaborHours", r => r.FactLaborHours),
        ("factProductivity", r => r.FactProductivity),
        ("zScore", r => r.ZScore),
        ("interpretation", r => r.Interpretation),
        ("isEquipment", r => r.IsEquipment),
        ("bindingType", r => r.BindingType),
        ("catalogId", r => r.CatalogId),
        ("tag", r => r.Tag),
        ("itemName", r => r.ItemName),
        ("workComment", r => r.WorkComment),
        ("estimationBasis", r => r.EstimationBasis),
        ("pvCode", r => r.PvCode),
        ("pvName", r => r.PvName),
        ("isAssessment", r => r.IsAssessment),
        ("coefficient", r => r.Coefficient),
        ("normProductivity", r => r.NormProductivity),
        ("normStdDev", r => r.NormStdDev),
        ("classifierId", r => r.ClassifierId),
        ("normId", r => r.NormId),
        ("catalogVolumeId", r => r.CatalogVolumeId),
        ("catalogEquipmentId", r => r.CatalogEquipmentId),
        ("catalogResource", r => r.CatalogResource),
    ];

    /// <summary>Имена колонок в порядке ячеек — для тестов и клиентских контрактов.</summary>
    public static IReadOnlyList<string> ColumnNames { get; } = Columns.Select(c => c.Name).ToArray();

    public static EncodedGridDto Empty { get; } = GridColumnEncoder.Empty(ColumnNames);

    public static EncodedGridDto Encode(IReadOnlyList<ProjectsPageDetailRowDto> rows) =>
        GridColumnEncoder.Encode(rows, Columns);
}
