namespace EngineeringPerformance.Application.Analytics;

/// <summary>Какая из трёх колонок плановых часов идёт в Z плана и KPI.</summary>
public enum PlanHourKind
{
    Base = 0,
    Optimistic = 1,
    Pessimistic = 2,
}
