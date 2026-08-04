using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
            new("role_id", user.RoleId.ToString())
        };

        return GenerateToken(claims, GetExpiration());
    }

    public string GenerateMfaSessionToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("purpose", "mfa_completion")
        };

        return GenerateToken(claims, DateTime.UtcNow.AddMinutes(5));
    }

    public int? ValidateMfaSessionToken(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
                ClockSkew = TimeSpan.Zero,
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
            var purpose = principal.FindFirst("purpose")?.Value;
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.Equals(purpose, "mfa_completion", StringComparison.Ordinal)
                && int.TryParse(userId, out var parsedUserId)
                ? parsedUserId
                : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public DateTime GetExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_settings.EffectiveAccessTokenLifetimeMinutes);
    }

    private string GenerateToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
