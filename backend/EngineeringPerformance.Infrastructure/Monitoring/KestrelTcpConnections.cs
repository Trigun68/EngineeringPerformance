using System.Net.NetworkInformation;

namespace EngineeringPerformance.Infrastructure.Monitoring;

/// <summary>
/// Established TCP на портах Kestrel. EventCounter <c>current-connections</c> в контейнере
/// часто остаётся 0 (подписка после старта источника); в network namespace бэкенда все
/// ESTABLISHED на 8080 — это Nginx → API.
/// </summary>
internal static class KestrelTcpConnections
{
    internal static readonly int[] DefaultListenPorts = [8080, 5080];

    public static int CountEstablished(IReadOnlyCollection<int>? listenPorts = null)
    {
        var ports = listenPorts is { Count: > 0 } ? listenPorts : DefaultListenPorts;
        try
        {
            return CountEstablished(
                IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections(),
                ports);
        }
        catch
        {
            return 0;
        }
    }

    internal static int CountEstablished(
        IEnumerable<TcpConnectionInformation> connections,
        IReadOnlyCollection<int> listenPorts)
    {
        var ports = listenPorts as HashSet<int> ?? [.. listenPorts];
        var count = 0;
        foreach (var connection in connections)
        {
            if (connection.State == TcpState.Established
                && ports.Contains(connection.LocalEndPoint.Port))
                count++;
        }

        return count;
    }
}
