namespace EngineeringPerformance.Application.Analytics.FactNorm;

public interface IReferenceFormula
{
    string Code { get; }

    string DisplayName { get; }

    string Formula { get; }

    string Description { get; }

    IReadOnlyList<string> ParamNames { get; }

    int ParameterCount { get; }

    bool RequiresPositiveX { get; }

    bool RequiresPositiveY { get; }

    IReadOnlyDictionary<string, double> GetDefaultParams();

    bool CanFit(IReadOnlyList<FactPoint> points, int minPoints);

    FormulaFitResult Fit(IReadOnlyList<FactPoint> points);

    double Predict(double x, IReadOnlyDictionary<string, double> parameters);
}

public abstract class ReferenceFormulaBase : IReferenceFormula
{
    public abstract string Code { get; }
    public abstract string DisplayName { get; }
    public abstract string Formula { get; }
    public abstract string Description { get; }
    public abstract IReadOnlyList<string> ParamNames { get; }
    public virtual int ParameterCount => ParamNames.Count;
    public virtual bool RequiresPositiveX => false;
    public virtual bool RequiresPositiveY => false;

    public abstract IReadOnlyDictionary<string, double> GetDefaultParams();

    public virtual bool CanFit(IReadOnlyList<FactPoint> points, int minPoints)
    {
        if (points.Count < minPoints || points.Count < ParameterCount + 1)
            return false;
        if (RequiresPositiveX && points.Any(p => p.X <= 0))
            return false;
        if (RequiresPositiveY && points.Any(p => p.Y <= 0))
            return false;
        return true;
    }

    public abstract FormulaFitResult Fit(IReadOnlyList<FactPoint> points);

    public abstract double Predict(double x, IReadOnlyDictionary<string, double> parameters);

    protected FormulaFitResult FromPredictions(
        IReadOnlyList<FactPoint> points,
        IReadOnlyDictionary<string, double> parameters,
        Func<FactPoint, double> predict)
    {
        var y = points.Select(p => p.Y).ToList();
        var yHat = points.Select(predict).ToList();
        if (yHat.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
        {
            return new FormulaFitResult(Code, parameters, FitStatistics.Compute(y, y, ParameterCount), false, "non-finite prediction");
        }

        var metrics = FitStatistics.Compute(y, yHat, ParameterCount);
        return new FormulaFitResult(Code, parameters, metrics, true);
    }

    protected static double P(IReadOnlyDictionary<string, double> parameters, string name) =>
        parameters.TryGetValue(name, out var v) ? v : 0;
}

public sealed class ConstantReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.Constant;
    public override string DisplayName => "Постоянная";
    public override string Formula => "y = a";
    public override string Description => "Производительность не зависит от объёма";
    public override IReadOnlyList<string> ParamNames => ["a"];

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 1 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var a = points.Average(p => p.Y);
        var parameters = new Dictionary<string, double> { ["a"] = a };
        return FromPredictions(points, parameters, _ => a);
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) => P(parameters, "a");
}

/// <summary>Только для R² текущей нормы; не входит в BIC-каталог подгонки.</summary>
public sealed class KimCurveReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.KimCurve;
    public override string DisplayName => "Кривая Кима";
    public override string Formula => "P(z) = e^(μ + z·σ)";
    public override string Description => "Логнормаль: μ = ln(P), производительность не зависит от объёма";
    public override IReadOnlyList<string> ParamNames => ["a"];

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 1 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var a = points.Average(p => p.Y);
        var parameters = new Dictionary<string, double> { ["a"] = a };
        return FromPredictions(points, parameters, _ => a);
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) => P(parameters, "a");
}

public sealed class LinearReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.Linear;
    public override string DisplayName => "Линейная";
    public override string Formula => "y = a + b·x";
    public override string Description => "Линейная зависимость (базовая норма)";
    public override IReadOnlyList<string> ParamNames => ["a", "b"];

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 0, ["b"] = 1 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var rows = points.Select(p => new[] { 1.0, p.X }).ToList();
        var y = points.Select(p => p.Y).ToList();
        if (!OrdinaryLeastSquares.TryFitDesign(rows, y, out var coeffs))
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "OLS failed");

        var parameters = new Dictionary<string, double> { ["a"] = coeffs[0], ["b"] = coeffs[1] };
        return FromPredictions(points, parameters, p => coeffs[0] + coeffs[1] * p.X);
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) =>
        P(parameters, "a") + P(parameters, "b") * x;
}

public sealed class PowerReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.Power;
    public override string DisplayName => "Степенная";
    public override string Formula => "y = a · x^b";
    public override string Description => "Кривая обучения (Wright's Law / степенная)";
    public override IReadOnlyList<string> ParamNames => ["a", "b"];
    public override bool RequiresPositiveX => true;
    public override bool RequiresPositiveY => true;

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 1, ["b"] = -0.2 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var filtered = points.Where(p => p.X > 0 && p.Y > 0).ToList();
        var y = filtered.Select(p => p.Y).ToList();
        if (filtered.Count < 3)
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "insufficient positive points");

        // ln(y) = ln(a) + b·ln(x)
        var rows = filtered.Select(p => new[] { 1.0, Math.Log(p.X) }).ToList();
        var lnY = filtered.Select(p => Math.Log(p.Y)).ToList();
        if (!OrdinaryLeastSquares.TryFitDesign(rows, lnY, out var coeffs))
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "OLS failed");

        var a = Math.Exp(coeffs[0]);
        var b = coeffs[1];
        var parameters = new Dictionary<string, double> { ["a"] = a, ["b"] = b };
        return FromPredictions(filtered, parameters, p => a * Math.Pow(p.X, b));
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) =>
        x <= 0 ? double.NaN : P(parameters, "a") * Math.Pow(x, P(parameters, "b"));
}

public sealed class LogarithmicReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.Logarithmic;
    public override string DisplayName => "Логарифмическая";
    public override string Formula => "y = a + b·ln(x)";
    public override string Description => "Логарифмический рост/спад";
    public override IReadOnlyList<string> ParamNames => ["a", "b"];
    public override bool RequiresPositiveX => true;

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 0, ["b"] = 1 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var filtered = points.Where(p => p.X > 0).ToList();
        var y = filtered.Select(p => p.Y).ToList();
        if (filtered.Count < 3)
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "insufficient positive x");

        var rows = filtered.Select(p => new[] { 1.0, Math.Log(p.X) }).ToList();
        if (!OrdinaryLeastSquares.TryFitDesign(rows, y, out var coeffs))
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "OLS failed");

        var parameters = new Dictionary<string, double> { ["a"] = coeffs[0], ["b"] = coeffs[1] };
        return FromPredictions(filtered, parameters, p => coeffs[0] + coeffs[1] * Math.Log(p.X));
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) =>
        x <= 0 ? double.NaN : P(parameters, "a") + P(parameters, "b") * Math.Log(x);
}

public sealed class ExponentialReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.Exponential;
    public override string DisplayName => "Экспоненциальная";
    public override string Formula => "y = a · e^(b·x)";
    public override string Description => "Экспоненциальная динамика";
    public override IReadOnlyList<string> ParamNames => ["a", "b"];
    public override bool RequiresPositiveY => true;

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 1, ["b"] = 0 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var filtered = points.Where(p => p.Y > 0).ToList();
        var y = filtered.Select(p => p.Y).ToList();
        if (filtered.Count < 3)
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "insufficient positive y");

        // ln(y) = ln(a) + b·x
        var rows = filtered.Select(p => new[] { 1.0, p.X }).ToList();
        var lnY = filtered.Select(p => Math.Log(p.Y)).ToList();
        if (!OrdinaryLeastSquares.TryFitDesign(rows, lnY, out var coeffs))
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "OLS failed");

        var a = Math.Exp(coeffs[0]);
        var b = coeffs[1];
        var parameters = new Dictionary<string, double> { ["a"] = a, ["b"] = b };
        return FromPredictions(filtered, parameters, p => a * Math.Exp(b * p.X));
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) =>
        P(parameters, "a") * Math.Exp(P(parameters, "b") * x);
}

public sealed class Polynomial2ReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.Polynomial2;
    public override string DisplayName => "Квадратичная";
    public override string Formula => "y = a + b·x + c·x²";
    public override string Description => "Квадратичная (с перегибом)";
    public override IReadOnlyList<string> ParamNames => ["a", "b", "c"];

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 0, ["b"] = 1, ["c"] = 0 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var rows = points.Select(p => new[] { 1.0, p.X, p.X * p.X }).ToList();
        var y = points.Select(p => p.Y).ToList();
        if (!OrdinaryLeastSquares.TryFitDesign(rows, y, out var coeffs))
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 3), false, "OLS failed");

        var parameters = new Dictionary<string, double> { ["a"] = coeffs[0], ["b"] = coeffs[1], ["c"] = coeffs[2] };
        return FromPredictions(points, parameters, p => coeffs[0] + coeffs[1] * p.X + coeffs[2] * p.X * p.X);
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) =>
        P(parameters, "a") + P(parameters, "b") * x + P(parameters, "c") * x * x;
}

public sealed class HyperbolicReferenceFormula : ReferenceFormulaBase
{
    public override string Code => ReferenceFormulaCodes.Hyperbolic;
    public override string DisplayName => "Гиперболическая";
    public override string Formula => "y = a + b/x";
    public override string Description => "Асимптотическое приближение";
    public override IReadOnlyList<string> ParamNames => ["a", "b"];
    public override bool RequiresPositiveX => true;

    public override IReadOnlyDictionary<string, double> GetDefaultParams() =>
        new Dictionary<string, double> { ["a"] = 0, ["b"] = 1 };

    public override FormulaFitResult Fit(IReadOnlyList<FactPoint> points)
    {
        var filtered = points.Where(p => p.X > 0).ToList();
        var y = filtered.Select(p => p.Y).ToList();
        if (filtered.Count < 3)
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "insufficient positive x");

        var rows = filtered.Select(p => new[] { 1.0, 1.0 / p.X }).ToList();
        if (!OrdinaryLeastSquares.TryFitDesign(rows, y, out var coeffs))
            return new FormulaFitResult(Code, GetDefaultParams(), FitStatistics.Compute(y, y, 2), false, "OLS failed");

        var parameters = new Dictionary<string, double> { ["a"] = coeffs[0], ["b"] = coeffs[1] };
        return FromPredictions(filtered, parameters, p => coeffs[0] + coeffs[1] / p.X);
    }

    public override double Predict(double x, IReadOnlyDictionary<string, double> parameters) =>
        x <= 0 ? double.NaN : P(parameters, "a") + P(parameters, "b") / x;
}

public static class ReferenceFormulaCatalog
{
    public static IReadOnlyList<IReferenceFormula> All { get; } =
    [
        new ConstantReferenceFormula(),
        new LinearReferenceFormula(),
        new PowerReferenceFormula(),
        new LogarithmicReferenceFormula(),
        new ExponentialReferenceFormula(),
        new Polynomial2ReferenceFormula(),
        new HyperbolicReferenceFormula(),
    ];

    public static IReferenceFormula Get(string code) =>
        TryGet(code) ?? throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown reference formula.");

    public static IReferenceFormula? TryGet(string code)
    {
        if (string.Equals(code, ReferenceFormulaCodes.KimCurve, StringComparison.OrdinalIgnoreCase))
            return KimCurve;
        return All.FirstOrDefault(f => string.Equals(f.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly IReferenceFormula KimCurve = new KimCurveReferenceFormula();
}
