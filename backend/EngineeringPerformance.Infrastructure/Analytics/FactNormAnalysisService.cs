using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Analytics.FactNorm;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Analytics;

public sealed class FactNormAnalysisService : IFactNormAnalysisService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string GenerationKey = "fact-norm:gen";

    private readonly AppDbContext _db;
    private readonly IFactAssessmentPipeline _pipeline;
    private readonly IFactNormAnalysisEngine _engine;
    private readonly IIicNormService _normService;
    private readonly IDistributedCache _cache;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly AnalyticsOptions _options;
    private readonly ICurrentUserService _currentUser;

    public FactNormAnalysisService(
        AppDbContext db,
        IFactAssessmentPipeline pipeline,
        IFactNormAnalysisEngine engine,
        IIicNormService normService,
        IDistributedCache cache,
        ICacheInvalidator cacheInvalidator,
        IOptions<AnalyticsOptions> options,
        ICurrentUserService currentUser)
    {
        _db = db;
        _pipeline = pipeline;
        _engine = engine;
        _normService = normService;
        _cache = cache;
        _cacheInvalidator = cacheInvalidator;
        _options = options.Value;
        _currentUser = currentUser;
    }

    public IReadOnlyList<ReferenceFormulaDto> ListReferenceFormulas() =>
        ReferenceFormulaCatalog.All
            .Select(f => new ReferenceFormulaDto(f.Code, f.DisplayName, f.Formula, f.ParamNames.ToArray(), f.Description))
            .ToList();

    public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        var gen = await GetGenerationAsync(cancellationToken);
        await _cache.SetStringAsync(
            GenerationKey,
            (gen + 1).ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) },
            cancellationToken);
    }

    public async Task<FactNormAnalysisResponse> AnalyzeAsync(
        FactNormAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request);

        var cacheKey = await BuildCacheKeyAsync(request, cancellationToken);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            var fromCache = JsonSerializer.Deserialize<FactNormAnalysisResponse>(cached);
            if (fromCache is not null)
                return fromCache with { FromCache = true };
        }

        var periodStart = DateTime.SpecifyKind(request.PeriodStart.Date, DateTimeKind.Utc);
        var periodEndExclusive = DateTime.SpecifyKind(request.PeriodEnd.Date.AddDays(1), DateTimeKind.Utc);

        var versionsQuery = _db.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => !v.IsArchived
                        && v.Direction == AnalysisDirection.Fact
                        && v.CreatedAt >= periodStart
                        && v.CreatedAt < periodEndExclusive);

        if (request.ProjectIds is { Length: > 0 } projectIds)
            versionsQuery = versionsQuery.Where(v => projectIds.Contains(v.ProjectId));

        var versions = await versionsQuery
            .OrderBy(v => v.CreatedAt)
            .Select(v => new { v.Id, v.ProjectId, v.CreatedAt })
            .ToListAsync(cancellationToken);

        if (versions.Count == 0)
        {
            var emptyAccuracy = AccuracyLevelCalculator.Compute(
                request.PeriodStart, request.PeriodEnd, 0, _options.Accuracy);
            return new FactNormAnalysisResponse(
                ToAccuracyDto(emptyAccuracy),
                [],
                [],
                ["Нет данных за период"]);
        }

        // Точки по WorkDirectionId
        var pointsByDirection = new Dictionary<int, List<FactPoint>>();
        var normIdByDirection = new Dictionary<int, int>();
        var warnings = new List<string>();

        foreach (var version in versions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AssessmentResultDto assessment;
            try
            {
                assessment = await _pipeline.EvaluateAsync(
                    new AssessmentRequest(version.ProjectId, version.Id),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                warnings.Add($"Версия {version.Id}: {ex.Message}");
                continue;
            }

            var snapshotNorms = await _db.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => s.ProjectAnalysisVersionId == version.Id)
                .Select(s => new { s.WorkDirectionId, s.NormId })
                .ToListAsync(cancellationToken);
            var snapshotMap = snapshotNorms.ToDictionary(s => s.WorkDirectionId, s => s.NormId);

            foreach (var row in assessment.Performance)
            {
                if (row.IsTotal || row.WorkDirectionId <= 0)
                    continue;
                if (request.WorkDirectionIds is { Length: > 0 }
                    && !request.WorkDirectionIds.Contains(row.WorkDirectionId))
                    continue;
                if (row.FactPhysicalVolume is not > 0 || row.FactProductivity is not { } productivity)
                    continue;

                if (!pointsByDirection.TryGetValue(row.WorkDirectionId, out var list))
                {
                    list = [];
                    pointsByDirection[row.WorkDirectionId] = list;
                }

                list.Add(new FactPoint((double)row.FactPhysicalVolume.Value, (double)productivity));

                if (!normIdByDirection.ContainsKey(row.WorkDirectionId)
                    && snapshotMap.TryGetValue(row.WorkDirectionId, out var normId))
                {
                    normIdByDirection[row.WorkDirectionId] = normId;
                }
            }
        }

        var directionIds = pointsByDirection.Keys.ToList();
        if (directionIds.Count == 0)
        {
            var emptyAccuracy = AccuracyLevelCalculator.Compute(
                request.PeriodStart, request.PeriodEnd, 0, _options.Accuracy);
            return new FactNormAnalysisResponse(
                ToAccuracyDto(emptyAccuracy),
                [],
                [],
                warnings.Count > 0 ? warnings : ["Нет фактических точек производительности за период"]);
        }

        var directions = await _db.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => directionIds.Contains(d.Id))
            .ToListAsync(cancellationToken);
        var directionMap = directions.ToDictionary(d => d.Id);

        // Нормы: из снапшота или temporal на конец периода
        var neededNormIds = normIdByDirection.Values.Distinct().ToList();
        var missingDirections = directionIds.Where(id => !normIdByDirection.ContainsKey(id)).ToList();
        if (missingDirections.Count > 0)
        {
            var asOf = DateTime.SpecifyKind(request.PeriodEnd.Date, DateTimeKind.Utc);
            var temporal = await _db.IicWorkDirectionNorms
                .AsNoTracking()
                .Where(n => missingDirections.Contains(n.WorkDirectionId)
                            && n.EffectiveFrom <= asOf
                            && (n.EffectiveTo == null || n.EffectiveTo >= asOf))
                .OrderByDescending(n => n.EffectiveFrom)
                .ThenByDescending(n => n.Id)
                .ToListAsync(cancellationToken);
            foreach (var g in temporal.GroupBy(n => n.WorkDirectionId))
                normIdByDirection[g.Key] = g.First().Id;
            neededNormIds = normIdByDirection.Values.Distinct().ToList();
        }

        var norms = await _db.IicWorkDirectionNorms
            .AsNoTracking()
            .Where(n => neededNormIds.Contains(n.Id))
            .ToListAsync(cancellationToken);
        var normsById = norms.ToDictionary(n => n.Id);

        var inputs = new List<NormGroupComparisonInput>();
        foreach (var (workDirectionId, points) in pointsByDirection)
        {
            if (!normIdByDirection.TryGetValue(workDirectionId, out var normId)
                || !normsById.TryGetValue(normId, out var norm))
            {
                warnings.Add($"Направление {workDirectionId}: норма не найдена — точки исключены.");
                continue;
            }

            if (!directionMap.TryGetValue(workDirectionId, out var direction))
                continue;

            var groupName = IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(direction, directionMap);
            var formula = NormFormulaMapper.ToNormFormula(
                norm.FormulaType,
                norm.CoefficientsJson,
                norm.MinVolume,
                norm.MaxVolume,
                norm.ExtrapolationMode,
                norm.Performance);
            var refCode = FormulaTypeMapper.ToReferenceCode(norm.FormulaType);
            var parameters = ExtractFitParameters(norm, formula);
            var mean = 0.0;
#pragma warning disable CS0618
            if (norm.Performance is { } perf)
                mean = (double)perf;
            else if (norm.Estimate is { } est)
                mean = Math.Exp((double)est);
#pragma warning restore CS0618
            var sigma = ExtractSigma(norm);

            inputs.Add(new NormGroupComparisonInput(
                norm.Id,
                workDirectionId,
                groupName,
                new CurrentNormFitContext(refCode, parameters, mean, sigma, null),
                points));
        }

        var engineResult = _engine.Run(request.PeriodStart, request.PeriodEnd, inputs, _options);
        warnings.AddRange(engineResult.Warnings);

        // Persist pending suggestions (replace pending for same norm+period)
        var suggestionDtos = new List<NormSuggestionDto>();
        var comparisonDtos = new List<NormComparisonDto>();
        var projectFilterJson = request.ProjectIds is { Length: > 0 }
            ? JsonSerializer.Serialize(request.ProjectIds.OrderBy(x => x).ToArray())
            : null;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var cmp in engineResult.Comparisons)
        {
            int? suggestionId = null;
            NormSuggestionDto? suggestionDto = null;
            if (cmp.Suggestion is { } draft)
            {
                var existing = await _db.NormSuggestions
                    .Where(s => s.NormId == cmp.NormId
                                && s.Status == NormSuggestionStatus.Pending
                                && s.PeriodStart == periodStart
                                && s.PeriodEnd == request.PeriodEnd.Date)
                    .ToListAsync(cancellationToken);
                _db.NormSuggestions.RemoveRange(existing);

                var entity = new NormSuggestion
                {
                    NormId = cmp.NormId,
                    SuggestionType = draft.SuggestionType switch
                    {
                        NormVerdict.ChangeFormulaType => NormSuggestionType.ChangeFormulaType,
                        NormVerdict.AdjustParameters => NormSuggestionType.AdjustParameters,
                        _ => NormSuggestionType.NormIsActual,
                    },
                    Status = NormSuggestionStatus.Pending,
                    ProposedFormulaType = draft.ProposedFormulaType,
                    ProposedParametersJson = JsonSerializer.Serialize(draft.ProposedParameters),
                    ProposedStdDeviation = draft.ProposedStdDeviation,
                    ProposedMeanPerformance = draft.ProposedMeanPerformance,
                    CurrentR2 = draft.CurrentR2,
                    ProposedR2 = draft.ProposedR2,
                    DeltaR2 = draft.DeltaR2,
                    Confidence = draft.Confidence switch
                    {
                        SuggestionConfidence.High => NormSuggestionConfidence.High,
                        SuggestionConfidence.Medium => NormSuggestionConfidence.Medium,
                        _ => NormSuggestionConfidence.Low,
                    },
                    DataPointsCount = draft.DataPointsCount,
                    PeriodStart = periodStart,
                    PeriodEnd = DateTime.SpecifyKind(request.PeriodEnd.Date, DateTimeKind.Utc),
                    ProjectFilterJson = projectFilterJson,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.NormSuggestions.Add(entity);
                await _db.SaveChangesAsync(cancellationToken);
                suggestionId = entity.Id;
                suggestionDto = MapSuggestion(entity, cmp.WorkDirectionId, cmp.GroupName);
                suggestionDtos.Add(suggestionDto);
            }

            comparisonDtos.Add(new NormComparisonDto(
                cmp.NormId,
                cmp.WorkDirectionId,
                cmp.GroupName,
                cmp.CurrentFormula,
                cmp.ProposedFormula,
                cmp.DeltaMean,
                cmp.DeltaSigma,
                cmp.CurrentR2,
                cmp.ProposedR2,
                cmp.DeltaR2,
                cmp.Verdict.ToString(),
                cmp.RequiresAttention,
                cmp.InsufficientData,
                cmp.SigmaZero,
                suggestionId,
                cmp.ChartPoints.Select(p => new ChartPointDto(p.X, p.Y)).ToList(),
                cmp.BestFit?.Parameters.ToDictionary(kv => kv.Key, kv => kv.Value),
                cmp.Warning));
        }

        await tx.CommitAsync(cancellationToken);

        var response = new FactNormAnalysisResponse(
            ToAccuracyDto(engineResult.Accuracy),
            comparisonDtos,
            suggestionDtos,
            warnings);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return response;
    }

    public async Task<IicNormDto> CreateNormFromSuggestionAsync(
        CreateNormFromSuggestionRequest request,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _db.NormSuggestions
            .FirstOrDefaultAsync(s => s.Id == request.SuggestionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Suggestion {request.SuggestionId} not found.");

        if (suggestion.Status != NormSuggestionStatus.Pending)
            throw new InvalidOperationException("Предложение уже обработано.");

        var sourceNorm = await _db.IicWorkDirectionNorms
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == suggestion.NormId, cancellationToken)
            ?? throw new KeyNotFoundException($"Norm {suggestion.NormId} not found.");

        var formulaCode = request.FormulaType ?? suggestion.ProposedFormulaType;
        var parameters = request.Parameters
            ?? JsonSerializer.Deserialize<Dictionary<string, double>>(suggestion.ProposedParametersJson)
            ?? new Dictionary<string, double>();
        var mean = request.MeanPerformance ?? suggestion.ProposedMeanPerformance;
        var sigma = request.StdDeviation ?? suggestion.ProposedStdDeviation;

        if (sigma <= 0)
            throw new ArgumentException("StdDeviation must be > 0.");

        if (!_options.NormCreation.AllowBackdatedEffectiveFrom
            && request.EffectiveFrom.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("EffectiveFrom cannot be in the past.");

        var (formulaType, coeffs, poly) = FormulaTypeMapper.ToNormStorage(formulaCode, parameters);
        var coeffDecimals = coeffs.ToDictionary(
            kv => kv.Key,
            kv => (decimal)kv.Value,
            StringComparer.OrdinalIgnoreCase);

        var write = new IicNormWriteRequest(
            WorkDirectionId: sourceNorm.WorkDirectionId,
            Performance: (decimal)mean,
            Estimate: mean > 0 ? (decimal)Math.Log(mean) : 0m,
            StandardDeviation: (decimal)sigma,
            UnitId: sourceNorm.UnitId,
            EffectiveFrom: request.EffectiveFrom,
            EffectiveTo: null,
            NormFormula: new NormFormulaDto(
                formulaType,
                coeffDecimals,
                sourceNorm.MinVolume,
                sourceNorm.MaxVolume,
                sourceNorm.ExtrapolationMode,
                null,
                poly?.Select(x => (decimal)x).ToList()),
            DeviationFormula: new DeviationFormulaDto(
                DeviationModelType.Multiplicative,
                new Dictionary<string, decimal> { ["sigma"] = (decimal)(sigma / Math.Max(mean, 1e-9)) }));

        // Закрытие предыдущей нормы + создание — внутри CreateAsync (AutoClose).
        // Дополнительно пишем audit и обновляем suggestion в одной явной транзакции вокруг Create.
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var previousOpen = await _db.IicWorkDirectionNorms
            .Where(n => n.WorkDirectionId == sourceNorm.WorkDirectionId
                        && (n.EffectiveTo == null || n.EffectiveTo >= request.EffectiveFrom.Date))
            .OrderByDescending(n => n.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        var oldEffectiveTo = previousOpen?.EffectiveTo;

        var created = await _normService.CreateAsync(write, cancellationToken);

        if (previousOpen is not null)
        {
            _db.NormChangeAudits.Add(new NormChangeAudit
            {
                NormId = previousOpen.Id,
                Action = NormChangeAction.EffectiveToChanged,
                ChangedBy = changedBy,
                OldValueJson = JsonSerializer.Serialize(new { EffectiveTo = oldEffectiveTo }),
                NewValueJson = JsonSerializer.Serialize(new { EffectiveTo = request.EffectiveFrom.Date }),
                SourceSuggestionId = suggestion.Id,
                Timestamp = DateTime.UtcNow,
            });
        }

        _db.NormChangeAudits.Add(new NormChangeAudit
        {
            NormId = created.Id,
            Action = NormChangeAction.Created,
            ChangedBy = changedBy,
            NewValueJson = JsonSerializer.Serialize(new
            {
                created.Id,
                FormulaType = formulaCode,
                Parameters = parameters,
                Mean = mean,
                Sigma = sigma,
                request.EffectiveFrom,
            }),
            SourceSuggestionId = suggestion.Id,
            Timestamp = DateTime.UtcNow,
        });

        var trackedSuggestion = await _db.NormSuggestions
            .FirstAsync(s => s.Id == suggestion.Id, cancellationToken);
        trackedSuggestion.Status = NormSuggestionStatus.Implemented;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await InvalidateCacheAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(cancellationToken);

        return created;
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(
        FactNormAnalysisRequest request,
        string format,
        CancellationToken cancellationToken = default)
    {
        var analysis = await AnalyzeAsync(request, cancellationToken);
        var fmt = (format ?? "xlsx").Trim().ToLowerInvariant();

        if (fmt is "csv")
        {
            var sb = new StringBuilder();
            sb.AppendLine("GroupName;CurrentFormula;ProposedFormula;DeltaMean;DeltaSigma;CurrentR2;ProposedR2;DeltaR2;Verdict");
            foreach (var c in analysis.Comparisons)
            {
                sb.Append(EscapeCsv(c.GroupName)).Append(';')
                    .Append(c.CurrentFormula).Append(';')
                    .Append(c.ProposedFormula).Append(';')
                    .Append(c.DeltaMean.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(c.DeltaSigma.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(c.CurrentR2.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(c.ProposedR2.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(c.DeltaR2.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(c.Verdict)
                    .AppendLine();
            }

            return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "fact-norm-analysis.csv");
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Предложения");
        var headers = new[]
        {
            "Группа", "Тек.формула", "Факт.тип", "Δmean", "Δσ", "R² тек.", "R² предл.", "ΔR²", "Вердикт",
        };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var c in analysis.Comparisons)
        {
            sheet.Cell(row, 1).Value = c.GroupName;
            sheet.Cell(row, 2).Value = c.CurrentFormula;
            sheet.Cell(row, 3).Value = c.ProposedFormula;
            sheet.Cell(row, 4).Value = c.DeltaMean;
            sheet.Cell(row, 5).Value = c.DeltaSigma;
            sheet.Cell(row, 6).Value = c.CurrentR2;
            sheet.Cell(row, 7).Value = c.ProposedR2;
            sheet.Cell(row, 8).Value = c.DeltaR2;
            sheet.Cell(row, 9).Value = c.Verdict;
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return (stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "fact-norm-analysis.xlsx");
    }

    private void ValidatePeriod(FactNormAnalysisRequest request)
    {
        if (request.PeriodStart >= request.PeriodEnd)
            throw new ArgumentException("PeriodStart must be before PeriodEnd.");
        if (request.PeriodEnd.Date > DateTime.UtcNow.Date)
            throw new ArgumentException("PeriodEnd cannot be in the future.");
        var days = (request.PeriodEnd.Date - request.PeriodStart.Date).TotalDays;
        if (days > _options.FactNormAnalysis.MaxPeriodDays)
            throw new ArgumentException($"Period exceeds max {_options.FactNormAnalysis.MaxPeriodDays} days.");
    }

    private async Task<string> BuildCacheKeyAsync(FactNormAnalysisRequest request, CancellationToken ct)
    {
        var gen = await GetGenerationAsync(ct);
        var projects = request.ProjectIds is { Length: > 0 }
            ? string.Join(',', request.ProjectIds.OrderBy(x => x))
            : "all";
        var groups = request.WorkDirectionIds is { Length: > 0 }
            ? string.Join(',', request.WorkDirectionIds.OrderBy(x => x))
            : "all";
        return $"fact-norm:{gen}:{request.PeriodStart:yyyyMMdd}:{request.PeriodEnd:yyyyMMdd}:{projects}:{groups}";
    }

    private async Task<long> GetGenerationAsync(CancellationToken ct)
    {
        var raw = await _cache.GetStringAsync(GenerationKey, ct);
        return long.TryParse(raw, out var g) ? g : 0;
    }

    private static AccuracyDto ToAccuracyDto(AccuracyResult r) =>
        new(r.Level.ToString(), r.DataPointsCount, r.PeriodLengthDays, r.Hint);

    private static NormSuggestionDto MapSuggestion(NormSuggestion entity, int workDirectionId, string groupName)
    {
        var parameters = JsonSerializer.Deserialize<Dictionary<string, double>>(entity.ProposedParametersJson)
                         ?? new Dictionary<string, double>();
        return new NormSuggestionDto(
            entity.Id,
            entity.NormId,
            workDirectionId,
            groupName,
            entity.SuggestionType.ToString(),
            entity.Status.ToString(),
            entity.ProposedFormulaType,
            parameters,
            entity.ProposedStdDeviation,
            entity.ProposedMeanPerformance,
            entity.CurrentR2,
            entity.ProposedR2,
            entity.DeltaR2,
            entity.Confidence.ToString(),
            entity.DataPointsCount,
            entity.PeriodStart,
            entity.PeriodEnd);
    }

    private static Dictionary<string, double> ExtractFitParameters(IicWorkDirectionNorm norm, NormFormula formula)
    {
        if (formula.Type == FormulaType.Polynomial && formula.PolynomialCoeffs.Count > 0)
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (formula.PolynomialCoeffs.Count > 0) dict["a"] = (double)formula.PolynomialCoeffs[0];
            if (formula.PolynomialCoeffs.Count > 1) dict["b"] = (double)formula.PolynomialCoeffs[1];
            if (formula.PolynomialCoeffs.Count > 2) dict["c"] = (double)formula.PolynomialCoeffs[2];
            return dict;
        }

        // EPMS Linear N=a-bV → референс y=a+(-b)x
        if (formula.Type == FormulaType.Linear)
        {
            var a = (double)formula.Coefficients.GetValueOrDefault("a");
            var b = (double)formula.Coefficients.GetValueOrDefault("b");
            return new Dictionary<string, double> { ["a"] = a, ["b"] = -b };
        }

        if (formula.Type == FormulaType.Logarithmic)
        {
            var a = (double)formula.Coefficients.GetValueOrDefault("a");
            var b = (double)formula.Coefficients.GetValueOrDefault("b");
            return new Dictionary<string, double> { ["a"] = a, ["b"] = -b };
        }

        return formula.Coefficients.ToDictionary(kv => kv.Key, kv => (double)kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static double ExtractSigma(IicWorkDirectionNorm norm)
    {
        if (!string.IsNullOrWhiteSpace(norm.DeviationCoefficientsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(norm.DeviationCoefficientsJson);
                if (doc.RootElement.TryGetProperty("sigma", out var sigmaEl)
                    && sigmaEl.TryGetDouble(out var sigmaRatio))
                {
                    var basePerf = (double)(norm.Performance ?? 0);
                    if (basePerf > 0)
                        return Math.Abs(sigmaRatio * basePerf);
                    return Math.Abs(sigmaRatio);
                }
            }
            catch
            {
                // fall through
            }
        }

#pragma warning disable CS0618
        return (double)(norm.StandardDeviation ?? 0);
#pragma warning restore CS0618
    }

    private static string EscapeCsv(string value) =>
        value.Contains(';') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
