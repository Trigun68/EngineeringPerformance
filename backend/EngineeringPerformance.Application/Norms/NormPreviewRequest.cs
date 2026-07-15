using EngineeringPerformance.Application.Registers;

namespace EngineeringPerformance.Application.Norms;

public sealed record NormPreviewRequest(
    NormFormulaDto NormFormula,
    DeviationFormulaDto DeviationFormula,
    int? Points = null);
