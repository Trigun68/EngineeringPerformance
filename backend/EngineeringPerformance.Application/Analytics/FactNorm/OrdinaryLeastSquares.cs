namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>МНК для линейных моделей (нормальные уравнения).</summary>
public static class OrdinaryLeastSquares
{
    public static bool TrySolve(double[,] aTa, double[] aTy, out double[] coefficients)
    {
        var n = aTy.Length;
        coefficients = new double[n];
        var m = (double[,])aTa.Clone();
        var b = (double[])aTy.Clone();

        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            for (var row = col + 1; row < n; row++)
            {
                if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col]))
                    pivot = row;
            }

            if (Math.Abs(m[pivot, col]) < 1e-14)
                return false;

            if (pivot != col)
            {
                for (var j = 0; j < n; j++)
                    (m[col, j], m[pivot, j]) = (m[pivot, j], m[col, j]);
                (b[col], b[pivot]) = (b[pivot], b[col]);
            }

            var div = m[col, col];
            for (var j = col; j < n; j++)
                m[col, j] /= div;
            b[col] /= div;

            for (var row = 0; row < n; row++)
            {
                if (row == col) continue;
                var factor = m[row, col];
                for (var j = col; j < n; j++)
                    m[row, j] -= factor * m[col, j];
                b[row] -= factor * b[col];
            }
        }

        coefficients = b;
        return true;
    }

    public static bool TryFitDesign(IReadOnlyList<double[]> rows, IReadOnlyList<double> y, out double[] coeffs)
    {
        coeffs = [];
        if (rows.Count == 0 || rows.Count != y.Count)
            return false;

        var p = rows[0].Length;
        var aTa = new double[p, p];
        var aTy = new double[p];
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length != p) return false;
            for (var j = 0; j < p; j++)
            {
                aTy[j] += row[j] * y[i];
                for (var k = 0; k < p; k++)
                    aTa[j, k] += row[j] * row[k];
            }
        }

        return TrySolve(aTa, aTy, out coeffs);
    }
}
