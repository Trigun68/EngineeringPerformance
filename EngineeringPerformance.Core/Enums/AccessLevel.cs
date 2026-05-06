namespace EngineeringPerformance.Core.Enums;

public enum AccessLevel
{
    None = 0,
    Read = 1,
    Edit = 2
}

public static class AccessLevelExtensions
{
    public static string ToRussian(this AccessLevel level) => level switch
    {
        AccessLevel.None => "Нет доступа",
        AccessLevel.Read => "Чтение",
        AccessLevel.Edit => "Редактирование",
        _ => level.ToString(),
    };
}
