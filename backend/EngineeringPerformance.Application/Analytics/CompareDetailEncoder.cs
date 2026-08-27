namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Колоночная упаковка грида сравнения «Детально» — тот же транспорт, что ISS-019,
/// другой набор полей (<see cref="VersionCompareDetailRowDto"/>).
/// </summary>
public static class CompareDetailEncoder
{
    private static readonly (string Name, Func<VersionCompareDetailRowDto, object?> Get)[] Columns =
    [
        ("facilityCode", r => r.FacilityCode),
        ("facilityName", r => r.FacilityName),
        ("documentSet", r => r.DocumentSet),
        ("pvCode", r => r.PvCode),
        ("pvName", r => r.PvName),
        ("workId", r => r.WorkId),
        ("workName", r => r.WorkName),
        ("quantityA", r => r.QuantityA),
        ("quantityB", r => r.QuantityB),
        ("quantityDelta", r => r.QuantityDelta),
        ("plannedLaborHoursA", r => r.PlannedLaborHoursA),
        ("plannedLaborHoursB", r => r.PlannedLaborHoursB),
        ("plannedLaborHoursOptimisticA", r => r.PlannedLaborHoursOptimisticA),
        ("plannedLaborHoursOptimisticB", r => r.PlannedLaborHoursOptimisticB),
        ("plannedLaborHoursPessimisticA", r => r.PlannedLaborHoursPessimisticA),
        ("plannedLaborHoursPessimisticB", r => r.PlannedLaborHoursPessimisticB),
        ("factLaborHoursA", r => r.FactLaborHoursA),
        ("factLaborHoursB", r => r.FactLaborHoursB),
        ("hoursDelta", r => r.HoursDelta),
        ("plannedProductivityA", r => r.PlannedProductivityA),
        ("plannedProductivityB", r => r.PlannedProductivityB),
        ("plannedProductivityOptimisticA", r => r.PlannedProductivityOptimisticA),
        ("plannedProductivityOptimisticB", r => r.PlannedProductivityOptimisticB),
        ("plannedProductivityPessimisticA", r => r.PlannedProductivityPessimisticA),
        ("plannedProductivityPessimisticB", r => r.PlannedProductivityPessimisticB),
        ("factProductivityA", r => r.FactProductivityA),
        ("factProductivityB", r => r.FactProductivityB),
        ("productivityDelta", r => r.ProductivityDelta),
        ("zScoreA", r => r.ZScoreA),
        ("zScoreB", r => r.ZScoreB),
        ("zDelta", r => r.ZDelta),
        ("iicGroupName", r => r.IicGroupName),
        ("iicUnitName", r => r.IicUnitName),
        ("presence", r => r.Presence),
        ("bindingType", r => r.BindingType),
        ("catalogId", r => r.CatalogId),
        ("tag", r => r.Tag),
        ("itemName", r => r.ItemName),
        ("workComment", r => r.WorkComment),
        ("estimationBasis", r => r.EstimationBasis),
        ("isAssessment", r => r.IsAssessment),
        ("coefficient", r => r.Coefficient),
        ("normProductivity", r => r.NormProductivity),
        ("normStdDev", r => r.NormStdDev),
        ("isEquipment", r => r.IsEquipment),
    ];

    public static IReadOnlyList<string> ColumnNames { get; } = Columns.Select(c => c.Name).ToArray();

    public static EncodedGridDto Empty { get; } = GridColumnEncoder.Empty(ColumnNames);

    public static EncodedGridDto Encode(IReadOnlyList<VersionCompareDetailRowDto> rows) =>
        GridColumnEncoder.Encode(rows, Columns);
}
