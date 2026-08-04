namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сводная таблица факта «Детально»: объекты × корневые группы IIC,
/// ячейки — ФО, план. часы (база), факт. часы, Z.
/// </summary>
public static class DetailFactSummaryTableBuilder
{
    private sealed record ColumnKey(int RootId, string UnitName);

    private sealed record FacilityKey(string Code, string Name);

    public static DetailFactSummaryTableDto Build(
        IReadOnlyList<DetailFactSummaryInputRow> rows,
        IReadOnlyList<IicDirectionNodeDto> directions,
        string projectCode,
        string versionName)
    {
        var byId = directions.ToDictionary(d => d.Id);
        var blockRoots = DetailForecastSummaryTableBuilder.ResolveBlockRoots(directions);
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

        var aggregates = new Dictionary<(FacilityKey Fac, ColumnKey Col), (decimal Vol, decimal Plan, decimal Fact, decimal ZWeighted, decimal ZHours)>();

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
            var plan = row.PlanHours ?? 0m;
            var fact = row.FactHours ?? 0m;
            var zPart = row.ZScore is decimal z && fact > 0 ? z * fact : 0m;
            var zHours = row.ZScore.HasValue && fact > 0 ? fact : 0m;

            if (aggregates.TryGetValue(key, out var existing))
            {
                aggregates[key] = (
                    existing.Vol + volume,
                    existing.Plan + plan,
                    existing.Fact + fact,
                    existing.ZWeighted + zPart,
                    existing.ZHours + zHours);
            }
            else
            {
                aggregates[key] = (volume, plan, fact, zPart, zHours);
            }
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

        var resultRows = new List<DetailFactSummaryRowDto>();
        foreach (var fac in facilities)
        {
            var cells = new List<DetailFactSummaryCellDto>();
            decimal factLaborTotal = 0m;
            foreach (var colKey in columnKeys)
            {
                if (aggregates.TryGetValue((fac, colKey), out var agg))
                {
                    decimal? z = agg.ZHours > 0
                        ? Math.Round(agg.ZWeighted / agg.ZHours, 4, MidpointRounding.AwayFromZero)
                        : null;
                    var factHours = Math.Round(agg.Fact, 0, MidpointRounding.AwayFromZero);
                    factLaborTotal += factHours;
                    cells.Add(new DetailFactSummaryCellDto(
                        Math.Round(agg.Vol, 0, MidpointRounding.AwayFromZero),
                        Math.Round(agg.Plan, 0, MidpointRounding.AwayFromZero),
                        factHours,
                        z));
                }
                else
                    cells.Add(new DetailFactSummaryCellDto(0m, 0m, 0m, null));
            }

            var label = string.IsNullOrWhiteSpace(fac.Code)
                ? fac.Name
                : string.IsNullOrWhiteSpace(fac.Name)
                    ? fac.Code
                    : $"{fac.Code} — {fac.Name}";

            resultRows.Add(new DetailFactSummaryRowDto(
                fac.Code, fac.Name, label, cells, factLaborTotal));
        }

        return new DetailFactSummaryTableDto(projectCode, versionName, columns, resultRows);
    }
}
