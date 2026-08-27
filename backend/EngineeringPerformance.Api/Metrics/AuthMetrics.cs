using Prometheus;

namespace EngineeringPerformance.Api.Metrics;

public static class AuthMetrics
{
    public static readonly Counter LoginSuccess = Prometheus.Metrics.CreateCounter(
        "auth_login_success_total",
        "Number of successful password login attempts.");

    public static readonly Counter LoginFailure = Prometheus.Metrics.CreateCounter(
        "auth_login_failure_total",
        "Number of failed password login attempts.");

    public static readonly Counter TokenRefresh = Prometheus.Metrics.CreateCounter(
        "auth_token_refresh_total",
        "Number of successfully refreshed access tokens.");
}
