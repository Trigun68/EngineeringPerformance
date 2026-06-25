namespace EngineeringPerformance.Infrastructure.Logging;

/// <summary>
/// Кольцевой буфер последних записей Serilog для Log Viewer (до 1000).
/// </summary>
public sealed class SerilogLogStore
{
    public const int MaxEntries = 1000;

    private readonly LinkedList<SerilogLogEntry> _entries = new();
    private readonly object _sync = new();

    public string? JsonLogDirectory { get; set; }

    public void Add(SerilogLogEntry entry)
    {
        lock (_sync)
        {
            _entries.AddFirst(entry);
            while (_entries.Count > MaxEntries)
            {
                _entries.RemoveLast();
            }
        }
    }

    public IReadOnlyList<SerilogLogEntry> GetAll()
    {
        lock (_sync)
        {
            return _entries.ToList();
        }
    }
}

public sealed record SerilogLogEntry(
    DateTime TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string? SourceContext);
