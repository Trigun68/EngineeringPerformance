namespace EngineeringPerformance.Application.Analytics.FactNorm;

public static class PointSanitizer
{
    public static IReadOnlyList<FactPoint> Sanitize(
        IReadOnlyList<FactPoint> points,
        bool removeOutliers3Sigma,
        out int removedCount)
    {
        removedCount = 0;
        var clean = points
            .Where(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y) && !double.IsInfinity(p.X) && !double.IsInfinity(p.Y))
            .ToList();
        removedCount += points.Count - clean.Count;

        if (!removeOutliers3Sigma || clean.Count < 5)
            return clean;

        var mean = clean.Average(p => p.Y);
        var variance = clean.Sum(p => (p.Y - mean) * (p.Y - mean)) / clean.Count;
        var sigma = Math.Sqrt(variance);
        if (sigma <= 1e-12)
            return clean;

        var filtered = clean.Where(p => Math.Abs(p.Y - mean) <= 3 * sigma).ToList();
        removedCount += clean.Count - filtered.Count;
        return filtered;
    }

    public static IReadOnlyList<FactPoint> SampleIfNeeded(
        IReadOnlyList<FactPoint> points,
        int maxPoints,
        out bool sampled)
    {
        sampled = false;
        if (points.Count <= maxPoints)
            return points;

        sampled = true;
        var step = (double)points.Count / maxPoints;
        var result = new List<FactPoint>(maxPoints);
        for (var i = 0; i < maxPoints; i++)
        {
            var idx = Math.Min(points.Count - 1, (int)(i * step));
            result.Add(points[idx]);
        }

        return result;
    }
}
