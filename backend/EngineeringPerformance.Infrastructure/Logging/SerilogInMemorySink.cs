using Serilog.Core;
using Serilog.Events;

namespace EngineeringPerformance.Infrastructure.Logging;

/// <summary>
/// Sink Serilog → in-memory store для админского Log Viewer.
/// </summary>
public sealed class SerilogInMemorySink : ILogEventSink
{
    private readonly SerilogLogStore _store;

    public SerilogInMemorySink(SerilogLogStore store)
    {
        _store = store;
    }

    public void Emit(LogEvent logEvent)
    {
        var exception = logEvent.Exception?.ToString();
        var sourceContext = logEvent.Properties.TryGetValue("SourceContext", out var ctx)
            ? ctx.ToString().Trim('"')
            : null;

        _store.Add(new SerilogLogEntry(
            logEvent.Timestamp.UtcDateTime,
            logEvent.Level.ToString(),
            logEvent.RenderMessage(),
            exception,
            sourceContext));
    }
}
