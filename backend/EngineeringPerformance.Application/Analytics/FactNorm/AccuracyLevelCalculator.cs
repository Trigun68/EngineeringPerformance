namespace EngineeringPerformance.Application.Analytics.FactNorm;

public static class AccuracyLevelCalculator
{
    public const string LowHint =
        "Короткие периоды (например, квартал) дают низкую точность. " +
        "Данная величина более подходит для периода год и более.";

    public static AccuracyResult Compute(
        DateTime periodStart,
        DateTime periodEnd,
        int dataPointsCount,
        AccuracyOptions options)
    {
        var days = Math.Max(0, (int)(periodEnd.Date - periodStart.Date).TotalDays);
        AccuracyLevel level;
        string? hint = null;

        if (days < options.LowMaxDays || dataPointsCount < options.LowMinPoints)
        {
            level = AccuracyLevel.Low;
            hint = LowHint;
        }
        else if (days >= options.MediumMaxDays && dataPointsCount >= options.HighMinPoints)
        {
            level = AccuracyLevel.High;
        }
        else
        {
            level = AccuracyLevel.Medium;
        }

        return new AccuracyResult(level, dataPointsCount, days, hint);
    }

    public static SuggestionConfidence ToConfidence(
        AccuracyLevel accuracy,
        int points,
        double adjustedR2,
        AccuracyOptions options)
    {
        if (accuracy == AccuracyLevel.High && adjustedR2 >= 0.85 && points >= options.HighMinPoints)
            return SuggestionConfidence.High;
        if (accuracy == AccuracyLevel.Medium && adjustedR2 >= 0.75)
            return SuggestionConfidence.Medium;
        return SuggestionConfidence.Low;
    }
}
