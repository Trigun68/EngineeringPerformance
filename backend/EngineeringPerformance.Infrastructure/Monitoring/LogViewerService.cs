using System.Text.Json;
using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Logging;
using Serilog.Events;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class LogViewerService : ILogViewerService
{
    private readonly SerilogLogStore _store;

    public LogViewerService(SerilogLogStore store)
    {
        _store = store;
    }

    public Task<LogViewerResultDto> GetRecentLogsAsync(
        int take = 1000,
        string? level = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var clampedTake = Math.Clamp(take, 1, SerilogLogStore.MaxEntries);
        var memoryEntries = _store.GetAll()
            .Select(MapToDto)
            .ToList();

        var fileEntries = ReadJsonLogFileEntries(_store.JsonLogDirectory, clampedTake);
        var merged = MergeEntries(memoryEntries, fileEntries)
            .OrderByDescending(e => e.TimestampUtc)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(level))
        {
            merged = merged.Where(e =>
                string.Equals(e.Level, level.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            merged = merged.Where(e =>
                e.Message.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (e.Exception?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (e.SourceContext?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var list = merged.Take(clampedTake).ToList();
        return Task.FromResult(new LogViewerResultDto(list, list.Count));
    }

    private static IEnumerable<SerilogLogEntryDto> ReadJsonLogFileEntries(string? directory, int maxLines)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            yield break;
        }

        var files = Directory.GetFiles(directory, "app-*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(3);

        var parsed = new List<SerilogLogEntryDto>();
        foreach (var file in files)
        {
            foreach (var line in File.ReadLines(file).Reverse().Take(maxLines))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var entry = TryParseCompactJsonLine(line);
                if (entry is not null)
                {
                    parsed.Add(entry);
                }

                if (parsed.Count >= maxLines)
                {
                    break;
                }
            }

            if (parsed.Count >= maxLines)
            {
                break;
            }
        }

        foreach (var entry in parsed)
        {
            yield return entry;
        }
    }

    private static SerilogLogEntryDto? TryParseCompactJsonLine(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            var timestamp = root.TryGetProperty("@t", out var t)
                ? t.GetString() ?? DateTime.UtcNow.ToString("o")
                : DateTime.UtcNow.ToString("o");

            var level = root.TryGetProperty("@l", out var l)
                ? l.GetString() ?? LogEventLevel.Information.ToString()
                : LogEventLevel.Information.ToString();

            var message = root.TryGetProperty("@m", out var m)
                ? m.GetString() ?? string.Empty
                : root.TryGetProperty("@mt", out var mt)
                    ? mt.GetString() ?? string.Empty
                    : string.Empty;

            string? exception = null;
            if (root.TryGetProperty("@x", out var x))
            {
                exception = x.GetString();
            }

            string? sourceContext = null;
            if (root.TryGetProperty("SourceContext", out var sc))
            {
                sourceContext = sc.GetString();
            }

            return new SerilogLogEntryDto(timestamp, level, message, exception, sourceContext);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<SerilogLogEntryDto> MergeEntries(
        IEnumerable<SerilogLogEntryDto> memory,
        IEnumerable<SerilogLogEntryDto> file)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in memory.Concat(file))
        {
            var key = $"{entry.TimestampUtc}|{entry.Level}|{entry.Message}";
            if (seen.Add(key))
            {
                yield return entry;
            }
        }
    }

    private static SerilogLogEntryDto MapToDto(SerilogLogEntry entry) =>
        new(
            entry.TimestampUtc.ToString("o"),
            entry.Level,
            entry.Message,
            entry.Exception,
            entry.SourceContext);
}
