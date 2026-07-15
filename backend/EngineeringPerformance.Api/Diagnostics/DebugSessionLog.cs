using System.Net.Http.Json;

namespace EngineeringPerformance.Api.Diagnostics;

/// <summary>Временная отладочная отправка NDJSON (session 7d5e90). Удалить после верификации.</summary>
internal static class DebugSessionLog
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };
    private const string IngestUrl = "http://host.docker.internal:7788/ingest/268a4e0e-2c5f-41f4-b9f0-a07171c96c13";

    public static void Write(string hypothesisId, string location, string message, object? data = null)
    {
        // #region agent log
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["sessionId"] = "7d5e90",
                    ["hypothesisId"] = hypothesisId,
                    ["location"] = location,
                    ["message"] = message,
                    ["data"] = data,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };
                using var req = new HttpRequestMessage(HttpMethod.Post, IngestUrl);
                req.Headers.TryAddWithoutValidation("X-Debug-Session-Id", "7d5e90");
                req.Content = JsonContent.Create(payload);
                await Http.SendAsync(req);
            }
            catch
            {
                // ignore — debug ingest must not affect auth/API
            }
        });
        // #endregion
    }
}
