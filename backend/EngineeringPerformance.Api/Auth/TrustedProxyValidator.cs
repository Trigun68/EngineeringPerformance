using EngineeringPerformance.Api.Configuration;
using EngineeringPerformance.Application.Auth;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Api.Auth;

public sealed class TrustedProxyValidator(IOptions<SpnegoOptions> options) : ITrustedProxyValidator
{
    public bool IsTrustedProxyToken(string? providedToken)
    {
        var secret = options.Value.TrustedProxySecret;
        return !string.IsNullOrWhiteSpace(secret)
            && !string.IsNullOrEmpty(providedToken)
            && string.Equals(providedToken, secret, StringComparison.Ordinal);
    }
}
