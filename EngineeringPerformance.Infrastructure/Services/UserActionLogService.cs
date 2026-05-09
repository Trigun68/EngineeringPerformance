using System.Globalization;
using System.Text;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Services;

// Реализация журнала действий пользователей.
// LogAsync — best-effort: ошибки записи не должны ломать основную операцию.
// Export/Import работают с CSV (UTF-8 BOM, разделитель `;`, RFC 4180-экранирование).
public sealed class UserActionLogService : IUserActionLogService
{
    private const string CsvHeader = "Id;UserId;UserName;Timestamp;ActionType;EntityType;EntityId;Description;IpOrHost";
    private const char Delimiter = ';';
    private static readonly string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

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
        CancellationToken cancellationToken = default)
    {
        // Best-effort: логирование не должно валить бизнес-операцию.
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var entry = new UserActionLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                ActionType = actionType.ToString(),
                EntityType = entityType,
                EntityId = entityId,
                Description = Truncate(description, 500),
                IpOrHost = Truncate(Environment.MachineName, 64),
            };
            context.UserActionLogs.Add(entry);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to write user action log entry: userId={UserId}, action={Action}, entity={EntityType}/{EntityId}",
                userId, actionType, entityType, entityId);
        }
    }

    public async Task<IReadOnlyList<UserActionLogDto>> GetAsync(
        UserActionLogFilter filter,
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.UserActionLogs
            .AsNoTracking()
            .Include(l => l.User)
            .AsQueryable();

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
                r.IpOrHost))
            .ToList();
    }

    public async Task<UserActionLogExportResult> ExportToFileAndPurgeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь файла не задан.", nameof(filePath));

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await context.UserActionLogs
            .AsNoTracking()
            .Include(l => l.User)
            .OrderBy(l => l.Timestamp)
            .ThenBy(l => l.Id)
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
            })
            .ToListAsync(cancellationToken);

        // Сначала пишем файл — если упадёт ввод-вывод, БД останется неизменной.
        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        await using (var writer = new StreamWriter(filePath, append: false, utf8Bom))
        {
            await writer.WriteLineAsync(CsvHeader);
            foreach (var r in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = string.Join(Delimiter, new[]
                {
                    r.Id.ToString(CultureInfo.InvariantCulture),
                    r.UserId.ToString(CultureInfo.InvariantCulture),
                    EscapeCsv(r.UserName),
                    r.Timestamp.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture),
                    EscapeCsv(r.ActionType),
                    EscapeCsv(r.EntityType),
                    r.EntityId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    EscapeCsv(r.Description),
                    EscapeCsv(r.IpOrHost),
                });
                await writer.WriteLineAsync(line);
            }
        }

        var deleted = await context.UserActionLogs.ExecuteDeleteAsync(cancellationToken);
        return new UserActionLogExportResult(filePath, rows.Count, deleted);
    }

    public async Task<UserActionLogImportResult> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь файла не задан.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл импорта не найден.", filePath);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var existingUserIds = await context.Users
            .AsNoTracking()
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var userSet = new HashSet<int>(existingUserIds);

        // Для дедупликации тянем ключи существующих записей:
        // (UserId, Timestamp, ActionType, EntityType, EntityId).
        var existingKeys = await context.UserActionLogs
            .AsNoTracking()
            .Select(l => new
            {
                l.UserId,
                l.Timestamp,
                l.ActionType,
                l.EntityType,
                l.EntityId,
            })
            .ToListAsync(cancellationToken);

        var keySet = new HashSet<string>(
            existingKeys.Select(k => BuildDedupKey(k.UserId, k.Timestamp, k.ActionType, k.EntityType, k.EntityId)));

        var total = 0;
        var imported = 0;
        var dupes = 0;
        var orphans = 0;
        var invalid = 0;
        var errors = new List<string>();

        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (headerLine is null || !headerLine.TrimStart('﻿').Equals(CsvHeader, StringComparison.Ordinal))
        {
            errors.Add($"Неверный заголовок CSV. Ожидается: {CsvHeader}");
            return new UserActionLogImportResult(0, 0, 0, 0, 0, errors);
        }

        var batch = new List<UserActionLog>();
        var lineNumber = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            total++;
            if (!TryParseCsvLine(line, out var fields) || fields.Count != 9)
            {
                invalid++;
                errors.Add($"Строка {lineNumber}: не удалось разобрать CSV.");
                continue;
            }

            if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId) ||
                !DateTime.TryParseExact(fields[3], TimestampFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ts))
            {
                invalid++;
                errors.Add($"Строка {lineNumber}: некорректные UserId или Timestamp.");
                continue;
            }

            var actionType = fields[4];
            if (string.IsNullOrWhiteSpace(actionType) || !Enum.TryParse<UserActionType>(actionType, out _))
            {
                invalid++;
                errors.Add($"Строка {lineNumber}: неизвестный ActionType «{actionType}».");
                continue;
            }

            var entityType = string.IsNullOrEmpty(fields[5]) ? null : fields[5];
            int? entityId = null;
            if (!string.IsNullOrEmpty(fields[6]))
            {
                if (!int.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedEntityId))
                {
                    invalid++;
                    errors.Add($"Строка {lineNumber}: некорректный EntityId «{fields[6]}».");
                    continue;
                }
                entityId = parsedEntityId;
            }
            var description = string.IsNullOrEmpty(fields[7]) ? null : fields[7];
            var ipOrHost = string.IsNullOrEmpty(fields[8]) ? null : fields[8];

            if (!userSet.Contains(userId))
            {
                orphans++;
                errors.Add($"Строка {lineNumber}: UserId={userId} отсутствует в БД — пропущено.");
                continue;
            }

            var key = BuildDedupKey(userId, ts, actionType, entityType, entityId);
            if (!keySet.Add(key))
            {
                dupes++;
                continue;
            }

            batch.Add(new UserActionLog
            {
                UserId = userId,
                Timestamp = ts,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Description = Truncate(description, 500),
                IpOrHost = Truncate(ipOrHost, 64),
            });
            imported++;
        }

        if (batch.Count > 0)
        {
            await context.UserActionLogs.AddRangeAsync(batch, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        return new UserActionLogImportResult(total, imported, dupes, orphans, invalid, errors);
    }

    private static UserActionType ParseActionType(string raw) =>
        Enum.TryParse<UserActionType>(raw, out var parsed) ? parsed : default;

    private static string BuildDedupKey(int userId, DateTime timestamp, string actionType, string? entityType, int? entityId) =>
        string.Join('|',
            userId.ToString(CultureInfo.InvariantCulture),
            timestamp.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture),
            actionType,
            entityType ?? string.Empty,
            entityId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..(maxLength - 1)] + "…";

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuotes = value.IndexOfAny([Delimiter, '"', '\r', '\n']) >= 0;
        if (!needsQuotes)
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    // Разбор одной CSV-строки: поддержка двойных кавычек для экранирования,
    // разделитель `;`. Переводы строк внутри quoted-полей в текущей реализации
    // не поддерживаются (простой line-by-line парсер — journal-строки коротки).
    private static bool TryParseCsvLine(string line, out List<string> fields)
    {
        fields = [];
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    // Двойные кавычки = экранированная кавычка.
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            else
            {
                if (ch == Delimiter)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else if (ch == '"' && sb.Length == 0)
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(ch);
                }
            }
        }

        if (inQuotes)
            return false;

        fields.Add(sb.ToString());
        return true;
    }
}
