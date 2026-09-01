using System.Collections.Concurrent;
using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class ActiveUserTracker : IActiveUserTracker
{
    private readonly ConcurrentDictionary<int, DateTime> _lastSeen = new();

    public void Record(int userId, DateTime timestampUtc)
        => _lastSeen[userId] = timestampUtc;

    public int CountActive(TimeSpan window, DateTime nowUtc)
    {
        var cutoff = nowUtc - window;
        var count = 0;
        foreach (var pair in _lastSeen)
        {
            if (pair.Value >= cutoff)
                count++;
        }

        return count;
    }
}
