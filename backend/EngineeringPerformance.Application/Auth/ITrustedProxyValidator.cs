namespace EngineeringPerformance.Application.Auth;

/// <summary>Единая проверка заголовка X-Trusted-Proxy-Token.</summary>
public interface ITrustedProxyValidator
{
    bool IsTrustedProxyToken(string? providedToken);
}
