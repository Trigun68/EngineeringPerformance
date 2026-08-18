using System.Globalization;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Чистая математика трендов Z: clip, same-day aggregate, сегментация по нормам, цвета.
/// Расчёт Z одной версии — через IPerformanceCalculationEngine / pipeline (SSOT).
/// </summary>
public sealed class ZTrendEngine : IZTrendEngine
{
    public static readonly string[] PlotPalette =
    [
        "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd",
        "#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf",
    ];

    public decimal? ClipZ(decimal? z, double zClipMax, out string flag)
    {
        flag = "ok";
        if (z is null)
        {
            flag = "no_norm";
            return null;
        }

        var max = (decimal)zClipMax;
        if (z.Value > max)
        {
            flag = "clipped";
            return max;
        }

        if (z.Value < -max)
        {
            flag = "clipped";
            return -max;
        }

        return z;
    }

    public IReadOnlyList<ZTrendRawPoint> AggregateSameDay(
        IReadOnlyList<ZTrendRawPoint> points,
        string weightMode)
    {
        if (points.Count == 0)
            return [];

        var uniform = string.Equals(weightMode, "uniform", StringComparison.OrdinalIgnoreCase);

        return points
            .GroupBy(p => (p.Date.Date, p.SegmentKey, p.Flag))
            .Select(g =>
            {
                var list = g.ToList();
                var projectCount = list.Select(x => x.ProjectId).Distinct().Count();
                var linePoints = list.Where(p => p.ZScore.HasValue && p.Flag is "ok" or "clipped").ToList();
                if (linePoints.Count == 0)
                {
                    var first = list[0];
                    return first with
                    {
                        Date = g.Key.Item1,
                        ProjectCount = projectCount,
                        Weight = uniform ? list.Count : list.Sum(x => x.Weight),
                        Contributions = list
                            .GroupBy(x => (x.ProjectId, x.VersionId))
                            .Select(cg =>
                            {
                                var p = cg.First();
                                return new ZTrendContributionDto(
                                    p.ProjectId,
                                    p.ProjectCode,
                                    p.ProjectName,
                                    p.VersionId,
                                    p.VersionName,
                                    p.ZScore.HasValue ? (double)p.ZScore.Value : null,
                                    (double)(uniform ? 1m : (p.Weight <= 0 ? 1m : p.Weight)));
                            })
                            .ToList(),
                    };
                }

                // Режим «uniform» — равные веса; при нулевых часах откат к среднему арифметическому.
                var aggregator = new WeightedZAggregator();
                foreach (var point in linePoints)
                    aggregator.Add(point.ZScore, uniform ? 1m : point.Weight);

                var zWeighted = aggregator.Result(2, arithmeticFallback: true) ?? 0m;
                var weightSum = aggregator.WeightSum > 0 ? aggregator.WeightSum : aggregator.SampleCount;

                var sample = linePoints[0];
                var contributions = list
                    .GroupBy(x => (x.ProjectId, x.VersionId))
                    .Select(cg =>
                    {
                        var p = cg.First();
                        return new ZTrendContributionDto(
                            p.ProjectId,
                            p.ProjectCode,
                            p.ProjectName,
                            p.VersionId,
                            p.VersionName,
                            p.ZScore.HasValue ? (double)p.ZScore.Value : null,
                            (double)(uniform ? 1m : (p.Weight <= 0 ? 1m : p.Weight)));
                    })
                    .ToList();
                return sample with
                {
                    Date = g.Key.Item1,
                    ZScore = zWeighted,
                    Weight = weightSum,
                    ProjectCount = projectCount,
                    Contributions = contributions,
                };
            })
            .OrderBy(p => p.Date)
            .ToList();
    }

    public ZTrendSeriesDto BuildSeries(
        IReadOnlyList<ZTrendRawPoint> points,
        string seriesType,
        AnalyticsTrendsOptions options,
        IReadOnlyDictionary<string, string>? colorBySegmentKey = null)
    {
        var linePoints = points
            .Where(p => p.ZScore.HasValue && p.Flag is "ok" or "clipped")
            .OrderBy(p => p.Date)
            .ToList();

        if (linePoints.Count == 0)
            return new ZTrendSeriesDto(seriesType, []);

        var segments = new List<ZTrendSegmentDto>();
        List<ZTrendRawPoint>? current = null;
        string? currentKey = null;

        void CloseCurrent(DateTime? effectiveTo)
        {
            if (current is null || currentKey is null || current.Count == 0)
                return;

            var first = current[0];
            var color = colorBySegmentKey is not null && colorBySegmentKey.TryGetValue(currentKey, out var c)
                ? c
                : ColorForSegment(Math.Abs(currentKey.GetHashCode()) % PlotPalette.Length, 0, options.LightenStep);

            segments.Add(new ZTrendSegmentDto(
                first.NormId,
                first.NormName,
                first.GroupId,
                first.GroupName,
                color,
                currentKey,
                first.Date,
                effectiveTo,
                current.Select(ToPointDto).ToList()));
        }

        foreach (var point in linePoints)
        {
            if (current is null || currentKey != point.SegmentKey)
            {
                if (current is not null && currentKey is not null)
                {
                    var last = current[^1];
                    var gapDays = (point.Date.Date - last.Date.Date).TotalDays;
                    var duplicateBoundary = gapDays <= options.GapThresholdDays;

                    CloseCurrent(point.Date);
                    current = [];
                    currentKey = point.SegmentKey;
                    if (duplicateBoundary)
                        current.Add(last with { SegmentKey = point.SegmentKey, NormId = point.NormId, NormName = point.NormName });
                    current.Add(point);
                }
                else
                {
                    current = [point];
                    currentKey = point.SegmentKey;
                }
            }
            else
            {
                current.Add(point);
            }
        }

        CloseCurrent(null);
        return new ZTrendSeriesDto(seriesType, segments);
    }

    public string LightenColor(string hexColor, double lightenStep, int shadeIndex)
    {
        if (shadeIndex <= 0)
            return NormalizeHex(hexColor);

        if (!TryParseHex(hexColor, out var r, out var g, out var b))
            return PlotPalette[0];

        RgbToHsl(r, g, b, out var h, out var s, out var l);
        var nextL = Math.Clamp(l + lightenStep * shadeIndex, 0.15, 0.92);
        HslToRgb(h, s, nextL, out r, out g, out b);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public string ColorForSegment(int paletteIndex, int shadeIndex, double lightenStep)
    {
        var baseColor = PlotPalette[Math.Abs(paletteIndex) % PlotPalette.Length];
        return LightenColor(baseColor, lightenStep, shadeIndex);
    }

    private static ZTrendPointDto ToPointDto(ZTrendRawPoint p)
    {
        var contributions = p.Contributions;
        if (contributions is null || contributions.Count == 0)
        {
            contributions =
            [
                new ZTrendContributionDto(
                    p.ProjectId,
                    p.ProjectCode,
                    p.ProjectName,
                    p.VersionId,
                    p.VersionName,
                    p.ZScore.HasValue ? (double)p.ZScore.Value : null,
                    (double)p.Weight),
            ];
        }

        return new(
            p.Date,
            p.ZScore.HasValue ? (double)p.ZScore.Value : null,
            (double)p.Weight,
            p.ProjectCount,
            p.NormId,
            p.Flag,
            contributions);
    }

    private static string NormalizeHex(string hex)
    {
        var t = hex.Trim();
        if (!t.StartsWith('#'))
            t = "#" + t;
        return t.Length == 7 ? t.ToLowerInvariant() : t;
    }

    private static bool TryParseHex(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        var t = hex.Trim().TrimStart('#');
        if (t.Length != 6)
            return false;
        return int.TryParse(t[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
               && int.TryParse(t[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
               && int.TryParse(t[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }

    private static void RgbToHsl(int r, int g, int b, out double h, out double s, out double l)
    {
        var rd = r / 255.0;
        var gd = g / 255.0;
        var bd = b / 255.0;
        var max = Math.Max(rd, Math.Max(gd, bd));
        var min = Math.Min(rd, Math.Min(gd, bd));
        l = (max + min) / 2.0;
        if (Math.Abs(max - min) < 1e-9)
        {
            h = 0;
            s = 0;
            return;
        }

        var d = max - min;
        s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
        if (Math.Abs(max - rd) < 1e-9)
            h = (gd - bd) / d + (gd < bd ? 6 : 0);
        else if (Math.Abs(max - gd) < 1e-9)
            h = (bd - rd) / d + 2;
        else
            h = (rd - gd) / d + 4;
        h /= 6.0;
    }

    private static void HslToRgb(double h, double s, double l, out int r, out int g, out int b)
    {
        double Hue(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }

        double rd, gd, bd;
        if (s <= 0)
        {
            rd = gd = bd = l;
        }
        else
        {
            var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            var p = 2 * l - q;
            rd = Hue(p, q, h + 1.0 / 3);
            gd = Hue(p, q, h);
            bd = Hue(p, q, h - 1.0 / 3);
        }

        r = (int)Math.Round(rd * 255);
        g = (int)Math.Round(gd * 255);
        b = (int)Math.Round(bd * 255);
    }
}
