namespace EngineeringPerformance.Application.Monitoring;

/// <summary>
/// Окно графиков вкладки «Ресурсы» и шаг даунсэмпла (~360–1000 точек).
/// </summary>
public static class ResourceSeriesBucket
{
    public const int MinWindowMinutes = 1;

    public const int MaxWindowMinutes = 90 * 24 * 60;

    public const int MaxHistoryRetentionDays = 90;

    public static int ClampWindowMinutes(int windowMinutes)
        => Math.Clamp(windowMinutes, MinWindowMinutes, MaxWindowMinutes);

    public static int ClampHistoryRetentionDays(int days)
        => Math.Clamp(days, 1, MaxHistoryRetentionDays);

    /// <summary>Длина корзины в секундах для запрошенного окна.</summary>
    public static int SecondsForWindow(int windowMinutes)
    {
        var minutes = ClampWindowMinutes(windowMinutes);
        if (minutes <= 60)
            return 10;
        if (minutes <= 360)
            return 30;
        if (minutes <= 720)
            return 60;
        if (minutes <= 1440)
            return 120;
        if (minutes <= 4320)
            return 300;
        if (minutes <= 10080)
            return 600;
        if (minutes <= 43200)
            return 3600;
        if (minutes <= 86400)
            return 7200;
        return 10800;
    }
}
