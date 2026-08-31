using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Application.Export;

/// <summary>KPI трёх сценариев: часы на контенте, часы + ₽ только на титуле.</summary>
public static class PdfScenarioKpis
{
    public static ReportKpiItem[] HoursOnly(decimal optimistic, decimal baseHours, decimal pessimistic) =>
    [
        new(PdfScenarioLabels.Optimistic, PdfNumberFormat.HoursWithUnit(optimistic)),
        new(PdfScenarioLabels.Base, PdfNumberFormat.HoursWithUnit(baseHours)),
        new(PdfScenarioLabels.Pessimistic, PdfNumberFormat.HoursWithUnit(pessimistic)),
    ];

    public static ReportKpiItem[] HoursWithCost(
        decimal optimistic,
        decimal baseHours,
        decimal pessimistic,
        ScenarioWorkCostResult? costs) =>
    [
        new(
            PdfScenarioLabels.Optimistic,
            PdfNumberFormat.HoursWithUnit(optimistic),
            PdfNumberFormat.CurrencyRub(costs?.Optimistic)),
        new(
            PdfScenarioLabels.Base,
            PdfNumberFormat.HoursWithUnit(baseHours),
            PdfNumberFormat.CurrencyRub(costs?.Base)),
        new(
            PdfScenarioLabels.Pessimistic,
            PdfNumberFormat.HoursWithUnit(pessimistic),
            PdfNumberFormat.CurrencyRub(costs?.Pessimistic)),
    ];
}
