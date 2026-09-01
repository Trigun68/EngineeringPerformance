using System.Diagnostics.Tracing;

namespace EngineeringPerformance.Infrastructure.Monitoring;

/// <summary>
/// Имена EventSource HTTP-сервера. Kestrel в ASP.NET Core — с дефисами
/// (<c>Microsoft-AspNetCore-Server-Kestrel</c>), не с точками.
/// </summary>
internal static class DotNetRuntimeCounterSources
{
    public static bool IsHttpServerSource(string? name) =>
        name is "Microsoft-AspNetCore-Server-Kestrel"
            or "Microsoft.AspNetCore.Server.Kestrel"
            or "Microsoft.AspNetCore.Hosting";
}

/// <summary>EventCounters Kestrel / Hosting: текущие HTTP-коннекты и запросы.</summary>
internal sealed class DotNetRuntimeCounters : EventListener
{
    private volatile int _kestrelConnections;
    private volatile int _currentRequests;

    public int KestrelConnections => _kestrelConnections;

    public int CurrentRequests => _currentRequests;

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (!DotNetRuntimeCounterSources.IsHttpServerSource(eventSource.Name))
            return;

        // EnableEvents из OnEventSourceCreated даёт deadlock (dotnet/runtime#43964).
        _ = Task.Factory.StartNew(
            () => EnableEvents(
                eventSource,
                EventLevel.Informational,
                EventKeywords.None,
                new Dictionary<string, string?> { ["EventCounterIntervalSec"] = "1" }),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventName != "EventCounters" || eventData.Payload is null || eventData.Payload.Count == 0)
            return;

        if (!TryReadPayload(eventData.Payload[0], out var payload) || payload is null)
            return;

        if (!payload.TryGetValue("Name", out var nameObj) || nameObj is null)
            return;

        var name = nameObj.ToString();
        var value = ReadCounterValue(payload);

        switch (name)
        {
            case "current-connections":
                _kestrelConnections = (int)Math.Round(value);
                break;
            case "current-requests":
                _currentRequests = (int)Math.Round(value);
                break;
        }
    }

    private static bool TryReadPayload(object? raw, out IDictionary<string, object>? payload)
    {
        switch (raw)
        {
            case IDictionary<string, object> typed:
                payload = typed;
                return true;
            case System.Collections.IDictionary untyped:
                payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (System.Collections.DictionaryEntry entry in untyped)
                {
                    if (entry.Key is string key && entry.Value is not null)
                        payload[key] = entry.Value;
                }

                return true;
            default:
                payload = null;
                return false;
        }
    }

    private static double ReadCounterValue(IDictionary<string, object> payload)
    {
        if (payload.TryGetValue("Mean", out var mean) && mean is not null)
            return Convert.ToDouble(mean);
        if (payload.TryGetValue("Increment", out var increment) && increment is not null)
            return Convert.ToDouble(increment);
        return 0d;
    }
}
