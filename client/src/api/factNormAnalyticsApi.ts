import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';

export type AccuracyLevel = 'Low' | 'Medium' | 'High';
export type Verdict =
  | 'ChangeFormulaType'
  | 'AdjustParameters'
  | 'NormIsActual'
  | 'InsufficientData';
export type Confidence = 'Low' | 'Medium' | 'High';
export type SuggestionStatus = 'Pending' | 'Implemented' | 'Rejected';

export interface Accuracy {
  level: AccuracyLevel;
  dataPointsCount: number;
  periodLengthDays: number;
  hint: string | null;
}

export interface ChartPoint {
  x: number;
  y: number;
}

export interface NormComparison {
  normId: number;
  workDirectionId: number;
  groupName: string;
  currentFormula: string;
  proposedFormula: string;
  deltaMean: number;
  deltaSigma: number;
  currentR2: number;
  proposedR2: number;
  deltaR2: number;
  verdict: Verdict;
  requiresAttention: boolean;
  insufficientData: boolean;
  sigmaZero: boolean;
  suggestionId: number | null;
  chartPoints?: ChartPoint[] | null;
  proposedParameters?: Record<string, number> | null;
  warning?: string | null;
}

export interface NormSuggestion {
  id: number;
  normId: number;
  workDirectionId: number;
  groupName: string;
  suggestionType: Verdict;
  status: SuggestionStatus;
  proposedFormulaType: string;
  proposedParameters: Record<string, number>;
  proposedStdDeviation: number;
  proposedMeanPerformance: number;
  currentR2: number;
  proposedR2: number;
  deltaR2: number;
  confidence: Confidence;
  dataPointsCount: number;
  periodStart: string;
  periodEnd: string;
}

export interface FactNormAnalysisResponse {
  accuracy: Accuracy;
  comparisons: NormComparison[];
  suggestions: NormSuggestion[];
  warnings: string[];
  fromCache?: boolean;
}

export interface ReferenceFormula {
  code: string;
  displayName: string;
  formula: string;
  paramNames: string[];
  description: string;
}

export interface FactNormAnalysisRequest {
  periodStart: string;
  periodEnd: string;
  projectIds?: number[] | null;
  workDirectionIds?: number[] | null;
}

export interface CreateNormFromSuggestionRequest {
  suggestionId: number;
  effectiveFrom: string;
  formulaType?: string | null;
  parameters?: Record<string, number> | null;
  meanPerformance?: number | null;
  stdDeviation?: number | null;
}

function normalizeAccuracy(raw: Record<string, unknown>): Accuracy {
  return {
    level: String(raw.level ?? raw.Level ?? 'Low') as AccuracyLevel,
    dataPointsCount: Number(raw.dataPointsCount ?? raw.DataPointsCount ?? 0),
    periodLengthDays: Number(raw.periodLengthDays ?? raw.PeriodLengthDays ?? 0),
    hint: (raw.hint ?? raw.Hint ?? null) as string | null,
  };
}

function normalizeComparison(raw: Record<string, unknown>): NormComparison {
  const points = (raw.chartPoints ?? raw.ChartPoints) as Array<Record<string, unknown>> | null | undefined;
  const params = (raw.proposedParameters ?? raw.ProposedParameters) as Record<string, number> | null | undefined;
  return {
    normId: Number(raw.normId ?? raw.NormId),
    workDirectionId: Number(raw.workDirectionId ?? raw.WorkDirectionId),
    groupName: String(raw.groupName ?? raw.GroupName ?? ''),
    currentFormula: String(raw.currentFormula ?? raw.CurrentFormula ?? ''),
    proposedFormula: String(raw.proposedFormula ?? raw.ProposedFormula ?? ''),
    deltaMean: Number(raw.deltaMean ?? raw.DeltaMean ?? 0),
    deltaSigma: Number(raw.deltaSigma ?? raw.DeltaSigma ?? 0),
    currentR2: Number(raw.currentR2 ?? raw.CurrentR2 ?? 0),
    proposedR2: Number(raw.proposedR2 ?? raw.ProposedR2 ?? 0),
    deltaR2: Number(raw.deltaR2 ?? raw.DeltaR2 ?? 0),
    verdict: String(raw.verdict ?? raw.Verdict ?? 'NormIsActual') as Verdict,
    requiresAttention: Boolean(raw.requiresAttention ?? raw.RequiresAttention),
    insufficientData: Boolean(raw.insufficientData ?? raw.InsufficientData),
    sigmaZero: Boolean(raw.sigmaZero ?? raw.SigmaZero),
    suggestionId: (raw.suggestionId ?? raw.SuggestionId ?? null) as number | null,
    chartPoints: points?.map((p) => ({
      x: Number(p.x ?? p.X ?? 0),
      y: Number(p.y ?? p.Y ?? 0),
    })),
    proposedParameters: params ?? null,
    warning: (raw.warning ?? raw.Warning ?? null) as string | null,
  };
}

function normalizeSuggestion(raw: Record<string, unknown>): NormSuggestion {
  const params = (raw.proposedParameters ?? raw.ProposedParameters ?? {}) as Record<string, number>;
  return {
    id: Number(raw.id ?? raw.Id),
    normId: Number(raw.normId ?? raw.NormId),
    workDirectionId: Number(raw.workDirectionId ?? raw.WorkDirectionId),
    groupName: String(raw.groupName ?? raw.GroupName ?? ''),
    suggestionType: String(raw.suggestionType ?? raw.SuggestionType) as Verdict,
    status: String(raw.status ?? raw.Status) as SuggestionStatus,
    proposedFormulaType: String(raw.proposedFormulaType ?? raw.ProposedFormulaType ?? ''),
    proposedParameters: params,
    proposedStdDeviation: Number(raw.proposedStdDeviation ?? raw.ProposedStdDeviation ?? 0),
    proposedMeanPerformance: Number(raw.proposedMeanPerformance ?? raw.ProposedMeanPerformance ?? 0),
    currentR2: Number(raw.currentR2 ?? raw.CurrentR2 ?? 0),
    proposedR2: Number(raw.proposedR2 ?? raw.ProposedR2 ?? 0),
    deltaR2: Number(raw.deltaR2 ?? raw.DeltaR2 ?? 0),
    confidence: String(raw.confidence ?? raw.Confidence ?? 'Low') as Confidence,
    dataPointsCount: Number(raw.dataPointsCount ?? raw.DataPointsCount ?? 0),
    periodStart: String(raw.periodStart ?? raw.PeriodStart ?? ''),
    periodEnd: String(raw.periodEnd ?? raw.PeriodEnd ?? ''),
  };
}

export async function runFactNormAnalysis(
  request: FactNormAnalysisRequest,
  signal?: AbortSignal,
): Promise<FactNormAnalysisResponse> {
  const { data } = await apiClient.post<Record<string, unknown>>(
    '/analytics/fact-norm-analysis',
    {
      periodStart: request.periodStart,
      periodEnd: request.periodEnd,
      projectIds: request.projectIds ?? null,
      workDirectionIds: request.workDirectionIds ?? null,
    },
    { signal },
  );
  const comparisons = (data.comparisons ?? data.Comparisons ?? []) as Record<string, unknown>[];
  const suggestions = (data.suggestions ?? data.Suggestions ?? []) as Record<string, unknown>[];
  const warnings = (data.warnings ?? data.Warnings ?? []) as string[];
  return {
    accuracy: normalizeAccuracy((data.accuracy ?? data.Accuracy ?? {}) as Record<string, unknown>),
    comparisons: comparisons.map(normalizeComparison),
    suggestions: suggestions.map(normalizeSuggestion),
    warnings,
    fromCache: Boolean(data.fromCache ?? data.FromCache),
  };
}

export async function fetchReferenceFormulas(): Promise<ReferenceFormula[]> {
  const { data } = await apiClient.get<Record<string, unknown>[]>('/analytics/reference-formulas');
  return (data ?? []).map((raw) => ({
    code: String(raw.code ?? raw.Code ?? ''),
    displayName: String(raw.displayName ?? raw.DisplayName ?? ''),
    formula: String(raw.formula ?? raw.Formula ?? ''),
    paramNames: (raw.paramNames ?? raw.ParamNames ?? []) as string[],
    description: String(raw.description ?? raw.Description ?? ''),
  }));
}

export async function createNormFromSuggestion(
  request: CreateNormFromSuggestionRequest,
): Promise<unknown> {
  const { data } = await apiClient.post('/analytics/norms/from-suggestion', {
    suggestionId: request.suggestionId,
    effectiveFrom: request.effectiveFrom,
    formulaType: request.formulaType ?? null,
    parameters: request.parameters ?? null,
    meanPerformance: request.meanPerformance ?? null,
    stdDeviation: request.stdDeviation ?? null,
  });
  return data;
}

export async function exportFactNormAnalysis(
  request: FactNormAnalysisRequest,
  format: 'csv' | 'xlsx' = 'xlsx',
): Promise<void> {
  const params = new URLSearchParams({
    periodStart: request.periodStart,
    periodEnd: request.periodEnd,
    format,
  });
  if (request.projectIds?.length) {
    for (const id of request.projectIds) params.append('projectIds', String(id));
  }
  const { blob, filename } = await downloadBlob(
    `/analytics/fact-norm-analysis/export?${params.toString()}`,
    undefined,
    format === 'csv' ? 'fact-norm-analysis.csv' : 'fact-norm-analysis.xlsx',
  );
  triggerBrowserDownload(blob, filename);
}
