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

        var equipmentLines = bundle.Equipment
            .Select(eq => new LaborIntensityCalculator.EquipmentLine(
                eq.Id,
                eq.Equipment?.ClassifierId,
                eq.QuantityInIicGroupUnits(),
                eq.Equipment?.IsAssessment == true,
                eq.FacilityCode))
            .ToList();

        var planHours = engine.RecalculatePlanHours(
            bundle.PlanningScenario,
            lines,
            equipmentLines,
            bundle.NormsByDirection,
            bundle.PlanningZByDirection);

        var ppvHours = planHours.Ppv.ToDictionary(
            r => r.PpvId,
            r => (r.Base, r.Optimistic, r.Pessimistic));

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

        foreach (var eq in bundle.Equipment)
        {
            if (planHours.Equipment.TryGetValue(eq.Id, out var hours))
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
