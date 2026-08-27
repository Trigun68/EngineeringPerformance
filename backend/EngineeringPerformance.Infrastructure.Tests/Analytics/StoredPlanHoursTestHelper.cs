using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Analytics;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>Копирует результат движка в колонки LaborIntensity* — имитация persist перед GET.</summary>
internal static class StoredPlanHoursTestHelper
{
    public static void ApplyEngineHours(ProjectsPageVersionBundle bundle, IPerformanceCalculationEngine engine)
    {
        var lines = bundle.PhysicalVolumes
            .Select(pv => new LaborIntensityCalculator.PpvLine(
                pv.Id,
                pv.GetClassifierId(),
                pv.FacilityCode,
                pv.Quantity,
                pv.GetCoefficient(),
                pv.GetIsAssessment() == true))
            .ToList();

        var ppvHours = engine.ComputePpvForecastHours(
            bundle.PlanningScenario,
            lines,
            bundle.NormsByDirection,
            bundle.PlanningZByDirection);

        foreach (var pv in bundle.PhysicalVolumes)
        {
            if (ppvHours.TryGetValue(pv.Id, out var hours))
            {
                pv.LaborIntensityBase = hours.Base;
                pv.LaborIntensityOptimistic = hours.Optimistic;
                pv.LaborIntensityPessimistic = hours.Pessimistic;
            }
            else
            {
                pv.LaborIntensityBase = null;
                pv.LaborIntensityOptimistic = null;
                pv.LaborIntensityPessimistic = null;
            }
        }

        var equipmentLines = bundle.Equipment
            .Select(eq => new LaborIntensityCalculator.EquipmentLine(
                eq.Id,
                eq.Equipment?.ClassifierId,
                eq.QuantityInIicGroupUnits(),
                eq.Equipment?.IsAssessment == true))
            .ToList();

        var equipmentHours = engine.ComputeEquipmentForecastHours(
            equipmentLines,
            bundle.NormsByDirection,
            bundle.PlanningZByDirection);

        foreach (var eq in bundle.Equipment)
        {
            if (equipmentHours.TryGetValue(eq.Id, out var hours))
            {
                eq.LaborIntensityBase = hours.Base;
                eq.LaborIntensityOptimistic = hours.Optimistic;
                eq.LaborIntensityPessimistic = hours.Pessimistic;
            }
            else
            {
                eq.LaborIntensityBase = null;
                eq.LaborIntensityOptimistic = null;
                eq.LaborIntensityPessimistic = null;
            }
        }
    }
}
