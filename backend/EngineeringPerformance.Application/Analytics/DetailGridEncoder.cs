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
    /// Интернировать колонку, если уникальных значений хотя бы втрое меньше числа строк:
    /// иначе словарь весит больше, чем экономит.
    /// </summary>
    private const int MinRepeatFactor = 3;

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

    public static EncodedGridDto Encode(IReadOnlyList<ProjectsPageDetailRowDto> rows)
    {
        var cells = new object?[rows.Count][];
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var cell = new object?[Columns.Length];
            for (var c = 0; c < Columns.Length; c++)
                cell[c] = Columns[c].Get(row);
            cells[i] = cell;
        }

        var tables = new Dictionary<string, IReadOnlyList<string>>();
        for (var c = 0; c < Columns.Length; c++)
            InternColumn(cells, c, Columns[c].Name, tables);

        return new EncodedGridDto(ColumnNames, tables, cells);
    }

    /// <summary>Заменяет строки колонки на индексы в словаре, если это уменьшает объём.</summary>
    private static void InternColumn(
        object?[][] cells,
        int column,
        string name,
        Dictionary<string, IReadOnlyList<string>> tables)
    {
        var distinct = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in cells)
        {
            if (row[column] is not string value)
            {
                // Не строковая колонка (или null в строковой) — null допустим, число нет.
                if (row[column] is not null)
                    return;

                continue;
            }

            if (!distinct.ContainsKey(value))
                distinct[value] = distinct.Count;
        }

        if (distinct.Count == 0 || distinct.Count * MinRepeatFactor >= cells.Length)
            return;

        foreach (var row in cells)
        {
            if (row[column] is string value)
                row[column] = distinct[value];
        }

        var table = new string[distinct.Count];
        foreach (var (value, index) in distinct)
            table[index] = value;

        tables[name] = table;
    }
}
