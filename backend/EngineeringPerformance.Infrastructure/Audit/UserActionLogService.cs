using ClosedXML.Excel;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Audit;

/// <summary>
/// Журнал действий пользователей (порт из WPF). LogAsync — best-effort.
/// </summary>
public sealed class UserActionLogService : IUserActionLogService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<UserActionLogService> _logger;

    public UserActionLogService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<UserActionLogService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task LogAsync(
        int userId,
        UserActionType actionType,
        string? entityType = null,
        int? entityId = null,
        string? description = null,
        string? ipOrHost = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            context.UserActionLogs.Add(new UserActionLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ActionType = actionType.ToString(),
                EntityType = entityType,
                EntityId = entityId,
                Description = Truncate(description, 500),
                IpOrHost = Truncate(ipOrHost ?? Environment.MachineName, 64),
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to write user action log: userId={UserId}, action={Action}, entity={EntityType}/{EntityId}",
                userId, actionType, entityType, entityId);
        }
    }

    public async Task<IReadOnlyList<UserActionLogDto>> GetAsync(
        UserActionLogFilter filter,
        int skip = 0,
        int take = 1000,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = ApplyFilter(context.UserActionLogs.AsNoTracking().Include(l => l.User), filter);

        if (!includeArchived)
            query = query.Where(l => !l.IsArchived);

        var rows = await query
            .OrderByDescending(l => l.Timestamp)
            .ThenByDescending(l => l.Id)
            .Skip(skip)
            .Take(take)
            .Select(l => new
            {
                l.Id,
                l.UserId,
                UserName = l.User.Username,
                l.Timestamp,
                l.ActionType,
                l.EntityType,
                l.EntityId,
                l.Description,
                l.IpOrHost,
                l.IsArchived,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new UserActionLogDto(
                r.Id,
                r.UserId,
                r.UserName,
                r.Timestamp,
                ParseActionType(r.ActionType),
                r.EntityType,
                r.EntityId,
                r.Description,
                r.IpOrHost,
                r.IsArchived))
            .ToList();
    }

    public async Task<int> ArchiveAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var logs = await context.UserActionLogs
            .Where(l => ids.Contains(l.Id) && !l.IsArchived)
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
            log.IsArchived = true;

        await context.SaveChangesAsync(cancellationToken);
        return logs.Count;
    }

    public async Task<int> RestoreAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var logs = await context.UserActionLogs
            .Where(l => ids.Contains(l.Id) && l.IsArchived)
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
            log.IsArchived = false;

        await context.SaveChangesAsync(cancellationToken);
        return logs.Count;
    }

    public async Task<byte[]> ExportXlsxAsync(
        UserActionLogFilter filter,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = ApplyFilter(context.UserActionLogs.AsNoTracking().Include(l => l.User), filter);

        if (!includeArchived)
            query = query.Where(l => !l.IsArchived);

        var rows = await query
            .OrderByDescending(l => l.Timestamp)
            .ThenByDescending(l => l.Id)
            .Select(l => new
            {
                l.Id,
                UserName = l.User.Username,
                l.Timestamp,
                l.ActionType,
                l.EntityType,
                l.EntityId,
                l.Description,
                l.IpOrHost,
                l.IsArchived,
            })
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("UserActions");
        sheet.Cell(1, 1).Value = "Id";
        sheet.Cell(1, 2).Value = "User";
        sheet.Cell(1, 3).Value = "Timestamp";
        sheet.Cell(1, 4).Value = "ActionType";
        sheet.Cell(1, 5).Value = "EntityType";
        sheet.Cell(1, 6).Value = "EntityId";
        sheet.Cell(1, 7).Value = "Description";
        sheet.Cell(1, 8).Value = "IpOrHost";
        sheet.Cell(1, 9).Value = "IsArchived";

        var row = 2;
        foreach (var item in rows)
        {
            sheet.Cell(row, 1).Value = item.Id;
            sheet.Cell(row, 2).Value = item.UserName;
            sheet.Cell(row, 3).Value = item.Timestamp;
            sheet.Cell(row, 4).Value = item.ActionType;
            sheet.Cell(row, 5).Value = item.EntityType ?? string.Empty;
            sheet.Cell(row, 6).Value = item.EntityId?.ToString() ?? string.Empty;
            sheet.Cell(row, 7).Value = item.Description ?? string.Empty;
            sheet.Cell(row, 8).Value = item.IpOrHost ?? string.Empty;
            sheet.Cell(row, 9).Value = item.IsArchived ? "Yes" : "No";
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static IQueryable<UserActionLog> ApplyFilter(IQueryable<UserActionLog> query, UserActionLogFilter filter)
    {
        if (filter.FromUtc is not null)
            query = query.Where(l => l.Timestamp >= filter.FromUtc);
        if (filter.ToUtc is not null)
            query = query.Where(l => l.Timestamp <= filter.ToUtc);
        if (filter.UserId is not null)
            query = query.Where(l => l.UserId == filter.UserId);
        if (filter.ActionType is not null)
        {
            var actionName = filter.ActionType.Value.ToString();
            query = query.Where(l => l.ActionType == actionName);
        }
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(l => l.EntityType == filter.EntityType);
        if (filter.EntityId is not null)
            query = query.Where(l => l.EntityId == filter.EntityId);

        return query;
    }

    private static UserActionType ParseActionType(string raw) =>
        Enum.TryParse<UserActionType>(raw, out var parsed) ? parsed : default;

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..(maxLength - 1)] + "…";
}
