using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Analytics.NormRegistry;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Analytics.NormRegistry;

public sealed class NormRegistryService(
    AppDbContext db,
    INormUsageStatsService usageStats,
    IDistributedCache cache,
    IOptions<NormHealthOptions> options,
    IUserActionLogger actionLogger,
    IAnalysisVersionRecalculationOrchestrator recalcOrchestrator) : INormRegistryService, INormHealthCheckService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] Palette =
    [
        "#2563eb", "#16a34a", "#ca8a04", "#dc2626", "#7c3aed",
        "#0891b2", "#ea580c", "#4f46e5", "#059669", "#be185d",
    ];

    private TimeSpan CacheTtl => TimeSpan.FromMinutes(Math.Max(1, options.Value.CacheTtlMinutes));

    public Task<HealthSummaryDto> RunAsync(int[]? groupIds = null, CancellationToken cancellationToken = default) =>
        HealthCheckAsync(groupIds, cancellationToken);

    public async Task<HealthSummaryDto> HealthCheckAsync(
        int[]? groupIds = null,
        CancellationToken cancellationToken = default)
    {
        var stamp = await GetCacheStampAsync(cancellationToken);
        var key = $"norm-registry:health:{stamp}:{string.Join(",", groupIds ?? [])}";
        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return JsonSerializer.Deserialize<HealthSummaryDto>(cached, JsonOpts)!;

        var inputs = await LoadHealthInputsAsync(groupIds, cancellationToken);
        var hints = await usageStats.GetHintsAsync(inputs.Select(i => i.Id).ToList(), cancellationToken);
        var summary = NormHealthCheckEngine.Run(inputs, hints, options.Value);

        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(summary, JsonOpts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return summary;
    }

    public async Task<PagedResultDto<NormListItemDto>> ListAsync(
        NormFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize <= 0 ? 20 : filter.PageSize, 1, 200);
        var stamp = await GetCacheStampAsync(cancellationToken);
        var cacheKey =
            $"norm-registry:list:{stamp}:{page}:{pageSize}:{filter.SortBy}:{filter.SortDesc}:{filter.OnlyWithIssues}:{filter.Search}:{filter.EffectiveFrom:O}:{filter.EffectiveTo:O}:{string.Join("-", filter.GroupIds ?? [])}:{string.Join("-", filter.Statuses ?? [])}:{string.Join("-", filter.FormulaTypes ?? [])}";

        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return JsonSerializer.Deserialize<PagedResultDto<NormListItemDto>>(cached, JsonOpts)!;

        var today = DateTime.UtcNow.Date;
        var health = await HealthCheckAsync(filter.GroupIds, cancellationToken);
        var issuesByNorm = BuildIssueIndex(health);

        var hints = await usageStats.GetHintsAsync(null, cancellationToken);
        var entities = await db.IicWorkDirectionNorms
            .AsNoTracking()
            .Include(n => n.WorkDirection)
            .OrderBy(n => n.WorkDirection!.SortOrder)
            .ThenBy(n => n.WorkDirection!.Name)
            .ThenBy(n => n.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var items = entities.Select(n => MapListItem(n, today, hints, issuesByNorm)).ToList();
        items = ApplyFilters(items, filter, today);

        items = SortItems(items, filter.SortBy, filter.SortDesc);

        var total = items.Count;
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var result = new PagedResultDto<NormListItemDto>(pageItems, total, page, pageSize);

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(result, JsonOpts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return result;
    }

    public async Task<NormDetailsDto?> GetDetailsAsync(int normId, CancellationToken cancellationToken = default)
    {
        var entity = await db.IicWorkDirectionNorms
            .AsNoTracking()
            .Include(n => n.WorkDirection)
            .Include(n => n.Unit)
            .FirstOrDefaultAsync(n => n.Id == normId, cancellationToken);
        if (entity is null) return null;

        var today = DateTime.UtcNow.Date;
        var health = await HealthCheckAsync([entity.WorkDirectionId], cancellationToken);
        var issuesByNorm = BuildIssueIndex(health);
        var hints = await usageStats.GetHintsAsync([normId], cancellationToken);
        var listItem = MapListItem(entity, today, hints, issuesByNorm);
        var usage = await usageStats.GetUsageAsync(normId, cancellationToken);

        var siblings = await db.IicWorkDirectionNorms
            .AsNoTracking()
            .Where(n => n.WorkDirectionId == entity.WorkDirectionId)
            .OrderBy(n => n.EffectiveFrom)
            .ThenBy(n => n.Id)
            .Select(n => new { n.Id, n.EffectiveFrom })
            .ToListAsync(cancellationToken);

        var idx = siblings.FindIndex(s => s.Id == normId);
        int? prev = idx > 0 ? siblings[idx - 1].Id : null;
        int? next = idx >= 0 && idx < siblings.Count - 1 ? siblings[idx + 1].Id : null;

        var audit = await db.UserActionLogs
            .AsNoTracking()
            .Where(l => l.EntityType == "IicWorkDirectionNorm" && l.EntityId == normId)
            .OrderByDescending(l => l.Timestamp)
            .Take(50)
            .Select(l => new NormChangeAuditDto(
                l.Id,
                l.ActionType,
                l.User != null
                    ? (l.User.DisplayName != null && l.User.DisplayName != ""
                        ? l.User.DisplayName
                        : l.User.Username)
                    : l.UserId.ToString(),
                l.Timestamp,
                null,
                l.Description))
            .ToListAsync(cancellationToken);

        var normIssues = health.Issues.Where(i => i.NormIds.Contains(normId) || i.GroupId == entity.WorkDirectionId).ToList();

#pragma warning disable CS0618
        return new NormDetailsDto(
            listItem,
            usage,
            prev,
            next,
            audit,
            normIssues,
            entity.Unit?.Name ?? entity.WorkDirection?.Name,
            entity.CoefficientsJson,
            entity.DeviationCoefficientsJson,
            entity.DeviationModelType.ToString());
#pragma warning restore CS0618
    }

    public async Task<TimelineResponseDto> GetTimelineAsync(
        bool includeHistorical = true,
        bool includeFuture = true,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var health = await HealthCheckAsync(null, cancellationToken);
        var issueNormIds = health.Issues.SelectMany(i => i.NormIds).ToHashSet();

        // Полный справочник нужен, чтобы единица измерения резолвилась с предка по дереву IIC.
        var directions = await db.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        var norms = await db.IicWorkDirectionNorms
            .AsNoTracking()
            .Include(n => n.WorkDirection)
            .OrderBy(n => n.WorkDirection!.SortOrder)
            .ThenBy(n => n.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var bars = new List<TimelineBarDto>();
        var groups = new Dictionary<int, TimelineGroupDto>();

        foreach (var n in norms)
        {
            var status = NormDerivedStatus.Resolve(n.EffectiveFrom, n.EffectiveTo, today);
            if (!includeHistorical && status == NormDerivedStatus.Historical) continue;
            if (!includeFuture && status == NormDerivedStatus.Future) continue;

            var color = Palette[Math.Abs(n.WorkDirectionId) % Palette.Length];
            var groupName = directions.TryGetValue(n.WorkDirectionId, out var dir)
                ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(dir, directions)
                : $"#{n.WorkDirectionId}";
            groups[n.WorkDirectionId] = new TimelineGroupDto(n.WorkDirectionId, groupName, color);

            bars.Add(new TimelineBarDto(
                n.Id,
                n.WorkDirectionId,
                groupName,
                $"#{n.Id} {n.FormulaType}",
                color,
                status,
                n.EffectiveFrom,
                n.EffectiveTo,
                issueNormIds.Contains(n.Id)));
        }

        return new TimelineResponseDto(bars, groups.Values.OrderBy(g => g.Name).ToList());
    }

    public async Task<NormRegistryFiltersMetaDto> GetFiltersMetaAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new NormRegistryGroupOptionDto(
                d.Id,
                d.Name,
                d.Code,
                d.ParentId,
                d.Unit != null ? d.Unit.Name : null))
            .ToListAsync(cancellationToken);

        var formulaTypes = Enum.GetNames<FormulaType>().ToList();
        var statuses = new List<string>
        {
            NormDerivedStatus.Active,
            NormDerivedStatus.Historical,
            NormDerivedStatus.Future,
        };

        return new NormRegistryFiltersMetaDto(groups, formulaTypes, statuses);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportRegistryAsync(
        NormFilterDto filter,
        string format,
        CancellationToken cancellationToken = default)
    {
        var all = await ListAsync(filter with { Page = 1, PageSize = 200 }, cancellationToken);
        // Fetch remaining pages if needed
        var items = all.Items.ToList();
        while (items.Count < all.TotalCount && items.Count < 10_000)
        {
            var nextPage = items.Count / 200 + 1;
            var page = await ListAsync(filter with { Page = nextPage, PageSize = 200 }, cancellationToken);
            if (page.Items.Count == 0) break;
            items.AddRange(page.Items);
        }

        await actionLogger.LogAsync(
            UserActionType.IicNormEdit,
            "IicWorkDirectionNorm",
            null,
            $"export-registry format={format} count={items.Count}",
            cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id;Group;Formula;Performance;Mu;Sigma;From;To;Status;Versions;Issues");
            foreach (var r in items)
            {
                sb.AppendLine(string.Join(';',
                    r.Id,
                    Escape(r.GroupName),
                    r.FormulaType,
                    r.MeanPerformance,
                    r.Estimate,
                    r.StdDeviation,
                    r.EffectiveFrom.ToString("yyyy-MM-dd"),
                    r.EffectiveTo?.ToString("yyyy-MM-dd") ?? "",
                    r.Status,
                    r.TotalVersions,
                    r.IssueCount));
            }

            return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
                ExportFileNameBuilder.Build(
                    ExportFileNameBuilder.Modules.Analytics,
                    "csv",
                    ExportFileNameBuilder.Views.Norms));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Norms");
        sheet.Cell(1, 1).Value = "Id";
        sheet.Cell(1, 2).Value = "Group";
        sheet.Cell(1, 3).Value = "Formula";
        sheet.Cell(1, 4).Value = "Display";
        sheet.Cell(1, 5).Value = "Performance";
        sheet.Cell(1, 6).Value = "Mu";
        sheet.Cell(1, 7).Value = "Sigma";
        sheet.Cell(1, 8).Value = "From";
        sheet.Cell(1, 9).Value = "To";
        sheet.Cell(1, 10).Value = "Status";
        sheet.Cell(1, 11).Value = "Versions";
        sheet.Cell(1, 12).Value = "Issues";
        var row = 2;
        foreach (var r in items)
        {
            sheet.Cell(row, 1).Value = r.Id;
            sheet.Cell(row, 2).Value = r.GroupName;
            sheet.Cell(row, 3).Value = r.FormulaType;
            sheet.Cell(row, 4).Value = r.FormulaDisplay;
            sheet.Cell(row, 5).Value = r.MeanPerformance;
            sheet.Cell(row, 6).Value = r.Estimate;
            sheet.Cell(row, 7).Value = r.StdDeviation;
            sheet.Cell(row, 8).Value = r.EffectiveFrom;
            sheet.Cell(row, 9).Value = r.EffectiveTo;
            sheet.Cell(row, 10).Value = r.Status;
            sheet.Cell(row, 11).Value = r.TotalVersions;
            sheet.Cell(row, 12).Value = r.IssueCount;
            row++;
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFileNameBuilder.Build(
                ExportFileNameBuilder.Modules.Analytics,
                "xlsx",
                ExportFileNameBuilder.Views.Norms));
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportHealthReportAsync(
        string format,
        int[]? groupIds = null,
        CancellationToken cancellationToken = default)
    {
        var health = await HealthCheckAsync(groupIds, cancellationToken);
        await actionLogger.LogAsync(
            UserActionType.IicNormEdit,
            "IicWorkDirectionNorm",
            null,
            $"export-health format={format} issues={health.Issues.Count}",
            cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Rule;Severity;GroupId;NormIds;Message;DetectedAt");
            foreach (var i in health.Issues)
            {
                sb.AppendLine(string.Join(';',
                    i.RuleCode,
                    i.Severity,
                    i.GroupId,
                    string.Join('|', i.NormIds),
                    Escape(i.Message),
                    i.DetectedAt.ToString("O")));
            }

            return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
                ExportFileNameBuilder.Build(
                    ExportFileNameBuilder.Modules.Analytics,
                    "csv",
                    ExportFileNameBuilder.Views.NormHealth));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Health");
        sheet.Cell(1, 1).Value = "Rule";
        sheet.Cell(1, 2).Value = "Severity";
        sheet.Cell(1, 3).Value = "GroupId";
        sheet.Cell(1, 4).Value = "NormIds";
        sheet.Cell(1, 5).Value = "Message";
        sheet.Cell(1, 6).Value = "DetectedAt";
        var row = 2;
        foreach (var i in health.Issues)
        {
            sheet.Cell(row, 1).Value = i.RuleCode;
            sheet.Cell(row, 2).Value = i.Severity;
            sheet.Cell(row, 3).Value = i.GroupId;
            sheet.Cell(row, 4).Value = string.Join(',', i.NormIds);
            sheet.Cell(row, 5).Value = i.Message;
            sheet.Cell(row, 6).Value = i.DetectedAt;
            row++;
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFileNameBuilder.Build(
                ExportFileNameBuilder.Modules.Analytics,
                "xlsx",
                ExportFileNameBuilder.Views.NormHealth));
    }

    public async Task<NormListItemDto> ArchiveAsync(
        int normId,
        int userId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.IicWorkDirectionNorms.FirstOrDefaultAsync(n => n.Id == normId, cancellationToken)
            ?? throw new KeyNotFoundException($"Norm {normId} not found.");

        var today = DateTime.UtcNow.Date;
        var status = NormDerivedStatus.Resolve(entity.EffectiveFrom, entity.EffectiveTo, today);
        if (status == NormDerivedStatus.Historical)
            throw new InvalidOperationException("Норма уже закрыта (Historical).");

        var oldTo = entity.EffectiveTo;
        var newTo = today;
        if (entity.EffectiveFrom.Date > newTo)
            throw new InvalidOperationException("Нельзя закрыть период раньше EffectiveFrom.");

        entity.EffectiveTo = DateTime.SpecifyKind(newTo, DateTimeKind.Utc);
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await recalcOrchestrator.AfterNormChangedAsync(
            entity.WorkDirectionId,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            cancellationToken);

        await actionLogger.LogForUserAsync(
            userId,
            UserActionType.IicNormEdit,
            "IicWorkDirectionNorm",
            normId,
            JsonSerializer.Serialize(new { action = "Archived", oldEffectiveTo = oldTo, newEffectiveTo = newTo, changedBy }),
            cancellationToken: cancellationToken);

        await InvalidateCacheAsync(cancellationToken);

        var details = await GetDetailsAsync(normId, cancellationToken);
        return details!.Norm;
    }

    public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        // Stamp в ключах list/health/usage — без SCAN по Redis.
        await cache.SetStringAsync(
            "norm-registry:invalidate-marker",
            DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) },
            cancellationToken);
    }

    private async Task<string> GetCacheStampAsync(CancellationToken cancellationToken) =>
        await cache.GetStringAsync("norm-registry:invalidate-marker", cancellationToken) ?? "0";

    private async Task<List<NormHealthNormInput>> LoadHealthInputsAsync(
        int[]? groupIds,
        CancellationToken cancellationToken)
    {
        var query = db.IicWorkDirectionNorms
            .AsNoTracking()
            .Include(n => n.WorkDirection)
            .AsQueryable();

        if (groupIds is { Length: > 0 })
            query = query.Where(n => groupIds.Contains(n.WorkDirectionId));

        var list = await query.ToListAsync(cancellationToken);
        return list.Select(ToHealthInput).ToList();
    }

    private static NormHealthNormInput ToHealthInput(Domain.Entities.IicWorkDirectionNorm n)
    {
#pragma warning disable CS0618
        var formula = NormFormulaMapper.ToNormFormula(
            n.FormulaType,
            n.CoefficientsJson,
            n.MinVolume,
            n.MaxVolume,
            n.ExtrapolationMode,
            n.Performance);
        var deviation = NormFormulaMapper.ToDeviationFormula(
            n.DeviationModelType,
            n.DeviationCoefficientsJson,
            n.Performance,
            n.StandardDeviation);

        return new NormHealthNormInput(
            n.Id,
            n.WorkDirectionId,
            n.WorkDirection?.Name ?? $"#{n.WorkDirectionId}",
            n.WorkDirection?.Code ?? "",
            n.EffectiveFrom,
            n.EffectiveTo,
            n.FormulaType,
            formula.Coefficients,
            n.StandardDeviation,
            n.DeviationModelType,
            deviation.Coefficients,
            n.CreatedAt);
#pragma warning restore CS0618
    }

    private static Dictionary<int, (int Count, string? MaxSeverity)> BuildIssueIndex(HealthSummaryDto health)
    {
        var map = new Dictionary<int, (int Count, string? MaxSeverity)>();
        foreach (var issue in health.Issues)
        {
            foreach (var id in issue.NormIds)
            {
                if (!map.TryGetValue(id, out var cur))
                    cur = (0, null);
                cur.Count++;
                cur.MaxSeverity = MaxSeverity(cur.MaxSeverity, issue.Severity);
                map[id] = cur;
            }
        }

        return map;
    }

    private static string? MaxSeverity(string? a, string b)
    {
        int Rank(string? s) => s?.ToLowerInvariant() switch
        {
            "error" => 3,
            "warning" => 2,
            "info" => 1,
            _ => 0,
        };
        return Rank(b) >= Rank(a) ? b : a;
    }

    private static NormListItemDto MapListItem(
        Domain.Entities.IicWorkDirectionNorm n,
        DateTime today,
        IReadOnlyDictionary<int, NormUsageHint> hints,
        Dictionary<int, (int Count, string? MaxSeverity)> issuesByNorm)
    {
#pragma warning disable CS0618
        var formula = NormFormulaMapper.ToNormFormula(
            n.FormulaType,
            n.CoefficientsJson,
            n.MinVolume,
            n.MaxVolume,
            n.ExtrapolationMode,
            n.Performance);
        var status = NormDerivedStatus.Resolve(n.EffectiveFrom, n.EffectiveTo, today);
        hints.TryGetValue(n.Id, out var hint);
        issuesByNorm.TryGetValue(n.Id, out var iss);
        var performance = n.Performance ?? (formula.Coefficients.TryGetValue("a", out var a) ? a : (decimal?)null);
        var estimate = n.Estimate
            ?? (n.FormulaType == FormulaType.KimCurve && performance is > 0
                ? KimCurveMath.Ln(performance.Value)
                : null);
        var sigma = n.StandardDeviation;

        return new NormListItemDto(
            n.Id,
            n.WorkDirectionId,
            n.WorkDirection?.Name ?? $"#{n.WorkDirectionId}",
            n.WorkDirection?.Code,
            n.FormulaType.ToString(),
            NormFormulaDisplay.Format(n.FormulaType, formula.Coefficients),
            formula.Coefficients.ToDictionary(kv => kv.Key, kv => kv.Value),
            performance is null ? null : (double)performance.Value,
            estimate is null ? null : (double)estimate.Value,
            sigma is null ? null : (double)sigma.Value,
            n.EffectiveFrom,
            n.EffectiveTo,
            status,
            hint?.TotalVersions ?? 0,
            iss.Count,
            iss.MaxSeverity);
#pragma warning restore CS0618
    }

    private static List<NormListItemDto> ApplyFilters(
        List<NormListItemDto> items,
        NormFilterDto filter,
        DateTime today)
    {
        IEnumerable<NormListItemDto> q = items;

        if (filter.GroupIds is { Length: > 0 })
            q = q.Where(i => filter.GroupIds.Contains(i.GroupId));

        if (filter.Statuses is { Length: > 0 })
            q = q.Where(i => filter.Statuses.Contains(i.Status, StringComparer.OrdinalIgnoreCase));

        if (filter.FormulaTypes is { Length: > 0 })
            q = q.Where(i => filter.FormulaTypes.Contains(i.FormulaType, StringComparer.OrdinalIgnoreCase));

        if (filter.EffectiveFrom is DateTime from || filter.EffectiveTo is DateTime to)
        {
            var f = filter.EffectiveFrom?.Date ?? DateTime.MinValue;
            var t = filter.EffectiveTo?.Date ?? DateTime.MaxValue.Date;
            q = q.Where(i => NormDerivedStatus.PeriodsOverlap(i.EffectiveFrom, i.EffectiveTo, f, t));
        }

        if (filter.OnlyWithIssues == true)
            q = q.Where(i => i.IssueCount > 0);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = q.Where(i =>
                i.GroupName.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (i.GroupCode?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
                || i.Id.ToString().Contains(s, StringComparison.OrdinalIgnoreCase)
                || i.FormulaDisplay.Contains(s, StringComparison.OrdinalIgnoreCase)
                || i.FormulaType.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        _ = today;
        return q.ToList();
    }

    private static List<NormListItemDto> SortItems(List<NormListItemDto> items, string? sortBy, bool desc)
    {
        Func<NormListItemDto, object?> key = (sortBy ?? "group").ToLowerInvariant() switch
        {
            "effectivefrom" or "from" => i => i.EffectiveFrom,
            "status" => i => i.Status,
            "usage" or "totalversions" => i => i.TotalVersions,
            "issues" or "issuecount" => i => i.IssueCount,
            "formula" => i => i.FormulaType,
            _ => i => i.GroupName,
        };

        return desc
            ? items.OrderByDescending(key).ThenBy(i => i.Id).ToList()
            : items.OrderBy(key).ThenBy(i => i.Id).ToList();
    }

    private static string Escape(string value) =>
        value.Replace(";", ",", StringComparison.Ordinal).Replace('\n', ' ');
}
