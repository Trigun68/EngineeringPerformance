namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Строит сводную таблицу вкладки «Детально»: объекты в строках, корневые группы IIC в столбцах.
/// </summary>
public static class DetailForecastSummaryTableBuilder
{
    private sealed record ColumnKey(int RootId, string UnitName);

    private sealed record FacilityKey(string Code, string Name);

    public static DetailForecastSummaryTableDto Build(
        IReadOnlyList<DetailForecastSummaryInputRow> rows,
        IReadOnlyList<IicDirectionNodeDto> directions,
        string projectCode,
        string versionName)
    {
        var byId = directions.ToDictionary(d => d.Id);
        var blockRoots = ResolveBlockRoots(directions);
        var blockRootIds = blockRoots.Select(r => r.Id).ToHashSet();
        var blockRootOrder = blockRoots.Select((r, i) => (r.Id, i)).ToDictionary(x => x.Id, x => x.i);

        int? ResolveBlockRootId(int classifierId)
        {
            if (!byId.TryGetValue(classifierId, out var current))
                return null;

            var guard = 0;
            while (guard++ < 64)
            {
                if (blockRootIds.Contains(current.Id))
                    return current.Id;

                if (current.ParentId is not int pid || !byId.TryGetValue(pid, out var parent))
                    return blockRootIds.Contains(current.Id) ? current.Id : null;

                current = parent;
            }

            return null;
        }

        string ResolveIicGroupUnitName(int classifierId)
        {
            if (!byId.TryGetValue(classifierId, out var direction))
                return "—";

            if (!string.IsNullOrWhiteSpace(direction.UnitName))
                return direction.UnitName.Trim();

            if (direction.ParentId is int pid && byId.TryGetValue(pid, out var parent))
            {
                var current = parent;
                var guard = 0;
                while (guard++ < 64)
                {
                    if (!string.IsNullOrWhiteSpace(current.UnitName))
                        return current.UnitName.Trim();

                    if (current.ParentId is not int ppid || !byId.TryGetValue(ppid, out var next))
                        break;

                    current = next;
                }
            }

            return "—";
        }

        var aggregates = new Dictionary<(FacilityKey Fac, ColumnKey Col), (decimal Vol, decimal Hours)>();

        foreach (var row in rows)
        {
            if (row.ClassifierId is not int classifierId)
                continue;

            var rootId = ResolveBlockRootId(classifierId);
            if (rootId is null)
                continue;

            var unit = ResolveIicGroupUnitName(classifierId);
            var fac = new FacilityKey(row.FacilityCode ?? string.Empty, row.FacilityName ?? string.Empty);
            var col = new ColumnKey(rootId.Value, unit);
            var key = (fac, col);

            var volume = row.Quantity * (row.Coefficient is > 0 ? row.Coefficient.Value : 1m);
            var hours = row.LaborHours ?? 0m;

            if (aggregates.TryGetValue(key, out var existing))
                aggregates[key] = (existing.Vol + volume, existing.Hours + hours);
            else
                aggregates[key] = (volume, hours);
        }

        var usedColumnKeys = aggregates.Keys
            .Select(k => k.Col)
            .Distinct()
            .OrderBy(k => blockRootOrder.GetValueOrDefault(k.RootId, int.MaxValue))
            .ThenBy(k => k.UnitName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitsByRoot = usedColumnKeys
            .GroupBy(k => k.RootId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UnitName).Distinct().ToList());

        var columns = new List<DetailForecastSummaryColumnDto>();
        foreach (var root in blockRoots)
        {
            if (!unitsByRoot.TryGetValue(root.Id, out var units) || units.Count == 0)
                continue;

            foreach (var unit in units)
            {
                var rootName = string.IsNullOrWhiteSpace(root.Code)
                    ? root.Name
                    : $"{root.Code} — {root.Name}";
                var title = string.IsNullOrWhiteSpace(unit) || unit == "—"
                    ? rootName
                    : $"{rootName} ({unit})";

                columns.Add(new DetailForecastSummaryColumnDto(root.Id, root.Name, unit, title));
            }
        }

        var columnKeys = columns
            .Select(c => new ColumnKey(c.RootId, c.UnitName))
            .ToList();

        var facilities = aggregates.Keys
            .Select(k => k.Fac)
            .Distinct()
            .OrderBy(f => f.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resultRows = new List<DetailForecastSummaryRowDto>();
        foreach (var fac in facilities)
        {
            var cells = new List<DetailForecastSummaryCellDto>();
            decimal laborTotal = 0m;
            foreach (var colKey in columnKeys)
            {
                if (aggregates.TryGetValue((fac, colKey), out var agg))
                {
                    var hours = Math.Round(agg.Hours, 0, MidpointRounding.AwayFromZero);
                    laborTotal += hours;
                    cells.Add(new DetailForecastSummaryCellDto(
                        Math.Round(agg.Vol, 0, MidpointRounding.AwayFromZero),
                        hours));
                }
                else
                    cells.Add(new DetailForecastSummaryCellDto(0m, 0m));
            }

            var label = string.IsNullOrWhiteSpace(fac.Code)
                ? fac.Name
                : string.IsNullOrWhiteSpace(fac.Name)
                    ? fac.Code
                    : $"{fac.Code} — {fac.Name}";

            resultRows.Add(new DetailForecastSummaryRowDto(
                fac.Code, fac.Name, label, cells, laborTotal));
        }

        return new DetailForecastSummaryTableDto(projectCode, versionName, columns, resultRows);
    }

    internal static IReadOnlyList<IicDirectionNodeDto> ResolveBlockRoots(IReadOnlyList<IicDirectionNodeDto> directions)
    {
        var metaRoots = directions
            .Where(d => d.ParentId is null)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (metaRoots.Count == 1)
        {
            var children = directions
                .Where(d => d.ParentId == metaRoots[0].Id)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (children.Count > 0)
                return children;
        }

        return metaRoots;
    }
}
