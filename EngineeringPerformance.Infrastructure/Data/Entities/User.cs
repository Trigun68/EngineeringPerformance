using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

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

    public Role Role { get; set; } = null!;
}
