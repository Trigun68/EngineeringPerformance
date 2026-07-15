namespace EngineeringPerformance.Application.Analytics;

/// <summary>Z-параметры сценариев прогноза по группе IIC (пессимист./базовый/оптимист.).</summary>
public sealed record PlanningZParameters(decimal ZPessimistic, decimal ZBase, decimal ZOptimistic)
{
    public static PlanningZParameters Default { get; } = new(0.5m, 0m, -0.5m);
}
