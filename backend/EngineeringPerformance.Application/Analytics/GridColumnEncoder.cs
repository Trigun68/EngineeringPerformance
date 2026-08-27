namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Общая упаковка грида в <see cref="EncodedGridDto"/>: имена колонок один раз,
/// повторяющиеся строки — словарём. Не знает бизнес-смысла полей.
/// </summary>
public static class GridColumnEncoder
{
    /// <summary>
    /// Интернировать колонку, если уникальных значений хотя бы втрое меньше числа строк:
    /// иначе словарь весит больше, чем экономит.
    /// </summary>
    private const int MinRepeatFactor = 3;

    public static EncodedGridDto Encode<T>(
        IReadOnlyList<T> rows,
        (string Name, Func<T, object?> Get)[] columns)
    {
        var names = columns.Select(c => c.Name).ToArray();
        var cells = new object?[rows.Count][];
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var cell = new object?[columns.Length];
            for (var c = 0; c < columns.Length; c++)
                cell[c] = columns[c].Get(row);
            cells[i] = cell;
        }

        var tables = new Dictionary<string, IReadOnlyList<string>>();
        for (var c = 0; c < columns.Length; c++)
            InternColumn(cells, c, columns[c].Name, tables);

        return new EncodedGridDto(names, tables, cells);
    }

    public static EncodedGridDto Empty(IReadOnlyList<string> columns) =>
        new(columns, new Dictionary<string, IReadOnlyList<string>>(), []);

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
