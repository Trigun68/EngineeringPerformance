using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Auth;

/// <summary>
/// Сопоставление введённого логина с учётной записью приложения (Username / WindowsUsername).
/// </summary>
public static class LoginIdentityMatcher
{
    public static bool Matches(User user, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = input.Trim();

        if (string.Equals(user.Username, normalized, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(user.WindowsUsername))
            return false;

        if (string.Equals(user.WindowsUsername, normalized, StringComparison.OrdinalIgnoreCase))
            return true;

        var slash = user.WindowsUsername.LastIndexOf('\\');
        if (slash >= 0)
        {
            var account = user.WindowsUsername[(slash + 1)..];
            if (string.Equals(account, normalized, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
