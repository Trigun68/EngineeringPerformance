using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class User : DirectoryItem
{
    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public int RoleId { get; set; }

    public string? WindowsUsername { get; set; }

    /// <summary>
    /// SID Windows-учётной записи. Используется для устойчивого авто-логина
    /// после переименования учётки в AD/локально (имя меняется, SID — нет).
    /// Если null — fallback на сравнение по <see cref="WindowsUsername"/>.
    /// </summary>
    public string? WindowsSid { get; set; }

    public bool IsActive { get; set; } = true;

    public int FailedLoginAttempts { get; set; }

    public bool IsLockedOut { get; set; }

    public DateTime? LockoutEndsAt { get; set; }

    public DateTime? LastFailedLoginAt { get; set; }

    public bool MfaEnabled { get; set; }

    public string? MfaSecret { get; set; }

    public bool MustChangePassword { get; set; }

    public DateTime? AdSyncedAt { get; set; }

    public string? AdGroups { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public Role Role { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
