namespace EngineeringPerformance.Application.Monitoring;

public static class ResourceCost
{
    public static double? PerUser(double value, int activeUsers)
        => activeUsers <= 0 ? null : ResourceCpu.Round(value / activeUsers);
}
