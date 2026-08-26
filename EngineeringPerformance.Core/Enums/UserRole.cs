namespace EngineeringPerformance.Core.Enums;

public enum UserRole
{
    SuperAdmin = 1,
    Admin = 2,
    Engineer = 3,
    Gip = 4,

    [Obsolete("Use SuperAdmin.")]
    Administrator = SuperAdmin,

    [Obsolete("Use Admin.")]
    ProjectManager = Admin,

    [Obsolete("Use Engineer.")]
    Observer = Engineer
}
