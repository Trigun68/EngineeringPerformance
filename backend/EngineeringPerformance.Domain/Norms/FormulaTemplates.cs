namespace EngineeringPerformance.Domain.Norms;

/// <summary>Описание коэффициента формулы для UI и валидации.</summary>
public sealed record FormulaCoefficientDef(
    string Name,
    string Label,
    bool Required,
    decimal? Min = null,
    decimal? Max = null);

/// <summary>Шаблон формулы N(V) — единый справочник для backend и frontend.</summary>
public sealed record FormulaTemplate(
    FormulaType Type,
    string DisplayName,
    string FormulaLatex,
    IReadOnlyList<FormulaCoefficientDef> Coefficients,
    bool RequiresBounds);

/// <summary>Статический справочник формул нормы N(V).</summary>
public static class FormulaTemplates
{
    public static FormulaTemplate Get(FormulaType type) =>
        All.First(t => t.Type == type);

    public static decimal Calculate(NormFormula formula, decimal volume) =>
        NormFormulaEvaluator.Calculate(formula, volume);

    public static readonly FormulaTemplate Linear = new(
        FormulaType.Linear,
        "Линейная",
        @"N(V) = \max(0, a - b \cdot V)",
        [
            new("a", "Базовая составляющая", true, 0),
            new("b", "Коэффициент убывания (производительность)", true, 0),
        ],
        RequiresBounds: false);

    public static readonly FormulaTemplate Logarithmic = new(
        FormulaType.Logarithmic,
        "Логарифмическая",
        @"N(V) = \max(0, a - b \cdot \ln(V))",
        [
            new("a", "Базовая составляющая", true, 0),
            new("b", "Коэффициент логарифма", true, 0),
        ],
        RequiresBounds: true);

    public static readonly FormulaTemplate ShiftedLog = new(
        FormulaType.ShiftedLog,
        "Сдвинутая логарифмическая",
        @"N(V) = \max(0, a - b \cdot \ln(V + c))",
        [
            new("a", "Базовая составляющая", true, 0),
            new("b", "Коэффициент логарифма", true, 0),
            new("c", "Сдвиг объёма", true, 0),
        ],
        RequiresBounds: true);

    public static readonly FormulaTemplate Wright = new(
        FormulaType.Wright,
        "Уайт (степенная)",
        @"N(V) = a \cdot V^{-b}",
        [
            new("a", "Коэффициент a", true, 0),
            new("b", "Показатель b", true, 0, 1),
        ],
        RequiresBounds: true);

    public static readonly FormulaTemplate Power = new(
        FormulaType.Power,
        "Степенная",
        @"N(V) = a \cdot V^{b}",
        [
            new("a", "Коэффициент a", true, 0),
            new("b", "Показатель b", true),
        ],
        RequiresBounds: true);

    public static readonly FormulaTemplate ExponentialDecay = new(
        FormulaType.ExponentialDecay,
        "Экспоненциальное затухание",
        @"N(V) = a \cdot e^{-b \cdot V} + c",
        [
            new("a", "Коэффициент a", true, 0),
            new("b", "Коэффициент затухания b", true, 0),
            new("c", "Смещение c", true, 0),
        ],
        RequiresBounds: true);

    public static readonly FormulaTemplate PiecewiseLinear = new(
        FormulaType.PiecewiseLinear,
        "Кусочно-линейная",
        @"N(V) — ломаная по точкам (V_i, N_i)",
        [],
        RequiresBounds: true);

    public static readonly FormulaTemplate Polynomial = new(
        FormulaType.Polynomial,
        "Полиномиальная",
        @"N(V) = \max(0, \sum a_i V^i)",
        [],
        RequiresBounds: true);

    public static readonly FormulaTemplate Constant = new(
        FormulaType.Constant,
        "Постоянная",
        @"N(V) = a",
        [new("a", "Производительность", true, 0)],
        RequiresBounds: false);

    public static readonly FormulaTemplate Hyperbolic = new(
        FormulaType.Hyperbolic,
        "Гиперболическая",
        @"N(V) = \max(0, a + b / V)",
        [
            new("a", "Асимптота a", true),
            new("b", "Коэффициент b", true),
        ],
        RequiresBounds: true);

    public static readonly FormulaTemplate Exponential = new(
        FormulaType.Exponential,
        "Экспоненциальная",
        @"N(V) = a \cdot e^{b \cdot V}",
        [
            new("a", "Коэффициент a", true, 0),
            new("b", "Показатель b", true),
        ],
        RequiresBounds: true);

    public static readonly FormulaTemplate KimCurve = new(
        FormulaType.KimCurve,
        "Логнормальное распределение",
        @"P(z) = e^{\mu + z \cdot \sigma},\ \mu = \ln(P)",
        [
            new("a", "Производительность", true, 0),
            new("mu", "μ", false),
            new("sigma", "σ", true, 0),
        ],
        RequiresBounds: false);

    /// <summary>Все шаблоны — объявлен после полей, иначе static init даёт null в списке.</summary>
    public static IReadOnlyList<FormulaTemplate> All { get; } =
    [
        Linear,
        Logarithmic,
        ShiftedLog,
        Wright,
        Power,
        ExponentialDecay,
        PiecewiseLinear,
        Polynomial,
        Constant,
        Hyperbolic,
        Exponential,
        KimCurve,
    ];
}
