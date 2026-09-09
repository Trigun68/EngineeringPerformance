using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.LaborIntensity;

/// <summary>
/// Чистая логика пересчёта LI по сценариям планирования (без БД).
/// Реализация для IPerformanceCalculationEngine.RecalculatePlanHours;
/// Infrastructure вызывает только через движок.
/// </summary>
public static class LaborIntensityCalculator
{
    public sealed record PpvLine(
        int Id,
        int? ClassifierId,
        string? FacilityCode,
        decimal Quantity,
        decimal Coefficient,
        bool IsAssessment);

    public sealed record RecalcResult(int PpvId, decimal? Base, decimal? Pessimistic, decimal? Optimistic);

    /// <summary>
    /// Строка оборудования: объём уже в единицах группы IIC.
    /// FacilityCode нужен для ByObjectIicGroup; пустой — отдельная корзина.
    /// </summary>
    public sealed record EquipmentLine(
        int Id,
        int? ClassifierId,
        decimal VolumeInIicUnits,
        bool IsAssessment,
        string? FacilityCode = null);

    /// <summary>Совместный пересчёт ФО и оборудования в одном V_g.</summary>
    public sealed record PlanHoursResult(
        IReadOnlyList<RecalcResult> Ppv,
        Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> Equipment);

    private readonly record struct UnifiedLine(
        bool IsPpv,
        int Id,
        int? ClassifierId,
        string? FacilityCode,
        decimal Volume,
        bool IsAssessment);

    private readonly record struct HoursTriple(decimal? Base, decimal? Pessimistic, decimal? Optimistic);

    // Метод internal: расчёт доступен только через IPerformanceCalculationEngine,
    // наружу остаются публичные DTO-строки (PpvLine/EquipmentLine/RecalcResult).
    internal static IReadOnlyList<RecalcResult> Recalculate(
        PlanningScenario scenario,
        IReadOnlyList<PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        IDeviationCalculator calculator) =>
        Recalculate(
            scenario,
            lines,
            Array.Empty<EquipmentLine>(),
            normsByDirection,
            planningZByDirection,
            calculator).Ppv;

    internal static PlanHoursResult Recalculate(
        PlanningScenario scenario,
        IReadOnlyList<PpvLine> ppv,
        IReadOnlyList<EquipmentLine> equipment,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        IDeviationCalculator calculator)
    {
        var unified = new List<UnifiedLine>(ppv.Count + equipment.Count);
        foreach (var line in ppv)
        {
            unified.Add(new UnifiedLine(
                true,
                line.Id,
                line.ClassifierId,
                line.FacilityCode,
                line.Quantity * line.Coefficient,
                line.IsAssessment));
        }

        foreach (var line in equipment)
        {
            unified.Add(new UnifiedLine(
                false,
                line.Id,
                line.ClassifierId,
                line.FacilityCode,
                line.VolumeInIicUnits,
                line.IsAssessment));
        }

        var hours = scenario switch
        {
            PlanningScenario.ByEachValue => RecalculateByEachValue(unified, normsByDirection, planningZByDirection, calculator),
            PlanningScenario.ByProjectIicGroup => RecalculateByGroup(unified, normsByDirection, planningZByDirection, calculator, groupByFacility: false),
            PlanningScenario.ByObjectIicGroup => RecalculateByGroup(unified, normsByDirection, planningZByDirection, calculator, groupByFacility: true),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        var ppvResults = new List<RecalcResult>();
        var equipmentHours = new Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)>(equipment.Count);

        foreach (var line in unified)
        {
            hours.TryGetValue((line.IsPpv, line.Id), out var triple);
            if (line.IsPpv)
            {
                if (hours.ContainsKey((true, line.Id)))
                    ppvResults.Add(new RecalcResult(line.Id, triple.Base, triple.Pessimistic, triple.Optimistic));
            }
            else
            {
                equipmentHours[line.Id] = (triple.Base, triple.Optimistic, triple.Pessimistic);
            }
        }

        return new PlanHoursResult(ppvResults, equipmentHours);
    }

    private static Dictionary<(bool IsPpv, int Id), HoursTriple> RecalculateByEachValue(
        IReadOnlyList<UnifiedLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> norms,
        IReadOnlyDictionary<int, PlanningZParameters> zParams,
        IDeviationCalculator calculator)
    {
        var result = new Dictionary<(bool IsPpv, int Id), HoursTriple>();
        foreach (var line in lines)
        {
            if (!IsEligible(line, requirePositiveVolume: !line.IsPpv))
            {
                if (!line.IsPpv)
                    result[(false, line.Id)] = new HoursTriple(null, null, null);
                continue;
            }

            if (!norms.TryGetValue(line.ClassifierId!.Value, out var snap))
            {
                if (!line.IsPpv)
                    result[(false, line.Id)] = new HoursTriple(null, null, null);
                continue;
            }

            var z = ForecastCalculation.ResolvePlanningZ(zParams, line.ClassifierId.Value);
            var (b, o, p) = ForecastCalculation.ComputeForecastHours(
                snap.NormFormula, snap.DeviationFormula, line.Volume, line.Volume, z, calculator);
            result[(line.IsPpv, line.Id)] = new HoursTriple(b, p, o);
        }

        return result;
    }

    private static Dictionary<(bool IsPpv, int Id), HoursTriple> RecalculateByGroup(
        IReadOnlyList<UnifiedLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> norms,
        IReadOnlyDictionary<int, PlanningZParameters> zParams,
        IDeviationCalculator calculator,
        bool groupByFacility)
    {
        var result = new Dictionary<(bool IsPpv, int Id), HoursTriple>();
        foreach (var line in lines.Where(l => !l.IsPpv))
            result[(false, line.Id)] = new HoursTriple(null, null, null);

        var eligible = lines.Where(l => IsEligible(l, requirePositiveVolume: true)).ToList();
        var groups = eligible.GroupBy(l => (
            Dir: l.ClassifierId!.Value,
            Facility: groupByFacility ? l.FacilityCode ?? string.Empty : string.Empty));

        foreach (var group in groups)
        {
            if (!norms.TryGetValue(group.Key.Dir, out var snap))
                continue;

            var vGroup = group.Sum(l => l.Volume);
            if (vGroup <= 0)
                continue;

            var z = ForecastCalculation.ResolvePlanningZ(zParams, group.Key.Dir);
            var (bG, oG, pG) = ForecastCalculation.ComputeForecastHours(
                snap.NormFormula, snap.DeviationFormula, vGroup, vGroup, z, calculator);

            foreach (var line in group)
            {
                var share = line.Volume / vGroup;
                result[(line.IsPpv, line.Id)] = new HoursTriple(
                    bG.HasValue ? Math.Round(bG.Value * share, 2) : null,
                    pG.HasValue ? Math.Round(pG.Value * share, 2) : null,
                    oG.HasValue ? Math.Round(oG.Value * share, 2) : null);
            }
        }

        return result;
    }

    private static bool IsEligible(UnifiedLine line, bool requirePositiveVolume)
    {
        if (!line.IsAssessment || !line.ClassifierId.HasValue)
            return false;
        if (requirePositiveVolume && line.Volume <= 0)
            return false;
        return true;
    }
}
