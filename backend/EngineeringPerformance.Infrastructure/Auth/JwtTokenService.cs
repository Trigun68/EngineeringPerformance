using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Settings;
using EngineeringPerformance.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly ISystemSettingsService _systemSettings;

    public JwtTokenService(IOptions<JwtSettings> settings, ISystemSettingsService systemSettings)
    {
        _settings = settings.Value;
        _systemSettings = systemSettings;
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = GetExpiration();

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpiration()
    {
        var minutes = _systemSettings.GetSessionExpirationMinutesAsync().GetAwaiter().GetResult();
        return DateTime.UtcNow.AddMinutes(minutes);
    }
}
