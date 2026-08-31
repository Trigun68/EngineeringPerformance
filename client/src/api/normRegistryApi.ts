import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';

export type NormDerivedStatus = 'Active' | 'Historical' | 'Future';

export interface NormFilter {
  groupIds?: number[] | null;
  statuses?: string[] | null;
  formulaTypes?: string[] | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  onlyWithIssues?: boolean | null;
  search?: string | null;
  page?: number;
  pageSize?: number;
  sortBy?: string | null;
  sortDesc?: boolean;
}

export interface NormListItem {
  id: number;
  groupId: number;
  groupName: string;
  groupCode: string | null;
  formulaType: string;
  formulaDisplay: string;
  parameters: Record<string, number>;
  meanPerformance: number | null;
  estimate: number | null;
  stdDeviation: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  status: NormDerivedStatus | string;
  totalVersions: number;
  issueCount: number;
  maxIssueSeverity: string | null;
}

export interface PagedNorms {
  items: NormListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface NormHealthIssue {
  id: string;
  ruleCode: string;
  severity: string;
  normIds: number[];
  groupId: number | null;
  message: string;
  details: unknown;
  detectedAt: string;
  recommendation: string | null;
}

export interface HealthSummary {
  errorCount: number;
  warningCount: number;
  infoCount: number;
  issues: NormHealthIssue[];
}

export interface MonthlyVersionCount {
  month: string;
  count: number;
}

export interface NormUsageStats {
  totalVersions: number;
  planVersions: number;
  factVersions: number;
  primaryVersions: number;
  firstVersionDate: string | null;
  lastVersionDate: string | null;
  monthlyHistogram: MonthlyVersionCount[];
}

export interface NormChangeAudit {
  id: number;
  action: string;
  changedBy: string;
  timestamp: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface NormDetails {
  norm: NormListItem;
  usage: NormUsageStats;
  previousNormId: number | null;
  nextNormId: number | null;
  auditHistory: NormChangeAudit[];
  issues: NormHealthIssue[];
  unitName: string | null;
  coefficientsJson: string | null;
  deviationCoefficientsJson: string | null;
  deviationModelType: string;
}

export interface TimelineBar {
  normId: number;
  groupId: number;
  groupName: string;
  label: string;
  color: string;
  status: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  hasIssue: boolean;
}

export interface TimelineGroup {
  id: number;
  name: string;
  color: string;
}

export interface TimelineResponse {
  bars: TimelineBar[];
  groups: TimelineGroup[];
}

export interface NormRegistryFiltersMeta {
  groups: {
    id: number;
    name: string;
    code: string | null;
    parentId: number | null;
    unitName: string | null;
  }[];
  formulaTypes: string[];
  statuses: string[];
}

function asArray<T>(value: unknown): T[] {
  return Array.isArray(value) ? (value as T[]) : [];
}

function mapListItem(raw: Record<string, unknown>): NormListItem {
  return {
    id: Number(raw.id ?? raw.Id ?? 0),
    groupId: Number(raw.groupId ?? raw.GroupId ?? 0),
    groupName: String(raw.groupName ?? raw.GroupName ?? ''),
    groupCode: (raw.groupCode ?? raw.GroupCode ?? null) as string | null,
    formulaType: String(raw.formulaType ?? raw.FormulaType ?? ''),
    formulaDisplay: String(raw.formulaDisplay ?? raw.FormulaDisplay ?? ''),
    parameters: (raw.parameters ?? raw.Parameters ?? {}) as Record<string, number>,
    meanPerformance: (raw.meanPerformance ?? raw.MeanPerformance ?? null) as number | null,
    estimate: (raw.estimate ?? raw.Estimate ?? null) as number | null,
    stdDeviation: (raw.stdDeviation ?? raw.StdDeviation ?? null) as number | null,
    effectiveFrom: String(raw.effectiveFrom ?? raw.EffectiveFrom ?? ''),
    effectiveTo: (raw.effectiveTo ?? raw.EffectiveTo ?? null) as string | null,
    status: String(raw.status ?? raw.Status ?? ''),
    totalVersions: Number(raw.totalVersions ?? raw.TotalVersions ?? 0),
    issueCount: Number(raw.issueCount ?? raw.IssueCount ?? 0),
    maxIssueSeverity: (raw.maxIssueSeverity ?? raw.MaxIssueSeverity ?? null) as string | null,
  };
}

function mapIssue(raw: Record<string, unknown>): NormHealthIssue {
  return {
    id: String(raw.id ?? raw.Id ?? ''),
    ruleCode: String(raw.ruleCode ?? raw.RuleCode ?? ''),
    severity: String(raw.severity ?? raw.Severity ?? ''),
    normIds: asArray<number>(raw.normIds ?? raw.NormIds).map(Number),
    groupId: (raw.groupId ?? raw.GroupId ?? null) as number | null,
    message: String(raw.message ?? raw.Message ?? ''),
    details: raw.details ?? raw.Details ?? null,
    detectedAt: String(raw.detectedAt ?? raw.DetectedAt ?? ''),
    recommendation: (raw.recommendation ?? raw.Recommendation ?? null) as string | null,
  };
}

function mapHealth(raw: Record<string, unknown>): HealthSummary {
  return {
    errorCount: Number(raw.errorCount ?? raw.ErrorCount ?? 0),
    warningCount: Number(raw.warningCount ?? raw.WarningCount ?? 0),
    infoCount: Number(raw.infoCount ?? raw.InfoCount ?? 0),
    issues: asArray<Record<string, unknown>>(raw.issues ?? raw.Issues).map(mapIssue),
  };
}

export async function fetchNormRegistryList(
  filter: NormFilter,
  signal?: AbortSignal,
): Promise<PagedNorms> {
  const { data } = await apiClient.post<Record<string, unknown>>(
    '/analytics/norm-registry/list',
    filter,
    { signal },
  );
  return {
    items: asArray<Record<string, unknown>>(data.items ?? data.Items).map(mapListItem),
    totalCount: Number(data.totalCount ?? data.TotalCount ?? 0),
    page: Number(data.page ?? data.Page ?? 1),
    pageSize: Number(data.pageSize ?? data.PageSize ?? 20),
  };
}

export async function fetchNormRegistryHealth(
  groupIds?: number[] | null,
  signal?: AbortSignal,
): Promise<HealthSummary> {
  const { data } = await apiClient.post<Record<string, unknown>>(
    '/analytics/norm-registry/health-check',
    { groupIds: groupIds ?? null },
    { signal },
  );
  return mapHealth(data);
}

export async function fetchNormRegistryDetails(
  normId: number,
  signal?: AbortSignal,
): Promise<NormDetails> {
  const { data } = await apiClient.get<Record<string, unknown>>(
    `/analytics/norm-registry/${normId}`,
    { signal },
  );
  const normRaw = (data.norm ?? data.Norm ?? {}) as Record<string, unknown>;
  const usageRaw = (data.usage ?? data.Usage ?? {}) as Record<string, unknown>;
  return {
    norm: mapListItem(normRaw),
    usage: {
      totalVersions: Number(usageRaw.totalVersions ?? usageRaw.TotalVersions ?? 0),
      planVersions: Number(usageRaw.planVersions ?? usageRaw.PlanVersions ?? 0),
      factVersions: Number(usageRaw.factVersions ?? usageRaw.FactVersions ?? 0),
      primaryVersions: Number(usageRaw.primaryVersions ?? usageRaw.PrimaryVersions ?? 0),
      firstVersionDate: (usageRaw.firstVersionDate ?? usageRaw.FirstVersionDate ?? null) as string | null,
      lastVersionDate: (usageRaw.lastVersionDate ?? usageRaw.LastVersionDate ?? null) as string | null,
      monthlyHistogram: asArray<Record<string, unknown>>(
        usageRaw.monthlyHistogram ?? usageRaw.MonthlyHistogram,
      ).map((h) => ({
        month: String(h.month ?? h.Month ?? ''),
        count: Number(h.count ?? h.Count ?? 0),
      })),
    },
    previousNormId: (data.previousNormId ?? data.PreviousNormId ?? null) as number | null,
    nextNormId: (data.nextNormId ?? data.NextNormId ?? null) as number | null,
    auditHistory: asArray<Record<string, unknown>>(data.auditHistory ?? data.AuditHistory).map((a) => ({
      id: Number(a.id ?? a.Id ?? 0),
      action: String(a.action ?? a.Action ?? ''),
      changedBy: String(a.changedBy ?? a.ChangedBy ?? ''),
      timestamp: String(a.timestamp ?? a.Timestamp ?? ''),
      oldValue: (a.oldValue ?? a.OldValue ?? null) as string | null,
      newValue: (a.newValue ?? a.NewValue ?? null) as string | null,
    })),
    issues: asArray<Record<string, unknown>>(data.issues ?? data.Issues).map(mapIssue),
    unitName: (data.unitName ?? data.UnitName ?? null) as string | null,
    coefficientsJson: (data.coefficientsJson ?? data.CoefficientsJson ?? null) as string | null,
    deviationCoefficientsJson: (data.deviationCoefficientsJson ?? data.DeviationCoefficientsJson ?? null) as
      | string
      | null,
    deviationModelType: String(data.deviationModelType ?? data.DeviationModelType ?? ''),
  };
}

export async function fetchNormRegistryTimeline(
  includeHistorical = true,
  includeFuture = true,
  signal?: AbortSignal,
): Promise<TimelineResponse> {
  const { data } = await apiClient.get<Record<string, unknown>>(
    '/analytics/norm-registry/timeline',
    { params: { includeHistorical, includeFuture }, signal },
  );
  return {
    bars: asArray<Record<string, unknown>>(data.bars ?? data.Bars).map((b) => ({
      normId: Number(b.normId ?? b.NormId ?? 0),
      groupId: Number(b.groupId ?? b.GroupId ?? 0),
      groupName: String(b.groupName ?? b.GroupName ?? ''),
      label: String(b.label ?? b.Label ?? ''),
      color: String(b.color ?? b.Color ?? '#2563eb'),
      status: String(b.status ?? b.Status ?? ''),
      effectiveFrom: String(b.effectiveFrom ?? b.EffectiveFrom ?? ''),
      effectiveTo: (b.effectiveTo ?? b.EffectiveTo ?? null) as string | null,
      hasIssue: Boolean(b.hasIssue ?? b.HasIssue ?? false),
    })),
    groups: asArray<Record<string, unknown>>(data.groups ?? data.Groups).map((g) => ({
      id: Number(g.id ?? g.Id ?? 0),
      name: String(g.name ?? g.Name ?? ''),
      color: String(g.color ?? g.Color ?? '#2563eb'),
    })),
  };
}

export async function fetchNormRegistryFilters(
  signal?: AbortSignal,
): Promise<NormRegistryFiltersMeta> {
  const { data } = await apiClient.get<Record<string, unknown>>(
    '/analytics/norm-registry/filters',
    { signal },
  );
  return {
    groups: asArray<Record<string, unknown>>(data.groups ?? data.Groups).map((g) => ({
      id: Number(g.id ?? g.Id ?? 0),
      name: String(g.name ?? g.Name ?? ''),
      code: (g.code ?? g.Code ?? null) as string | null,
      parentId: (g.parentId ?? g.ParentId ?? null) as number | null,
      unitName: (g.unitName ?? g.UnitName ?? null) as string | null,
    })),
    formulaTypes: asArray<string>(data.formulaTypes ?? data.FormulaTypes).map(String),
    statuses: asArray<string>(data.statuses ?? data.Statuses).map(String),
  };
}

export async function archiveNorm(normId: number): Promise<NormListItem> {
  const { data } = await apiClient.post<Record<string, unknown>>(
    `/analytics/norm-registry/${normId}/archive`,
  );
  return mapListItem(data);
}

export async function exportNormRegistry(
  format: 'xlsx' | 'csv',
  filter: NormFilter,
): Promise<void> {
  const params = new URLSearchParams();
  params.set('format', format);
  if (filter.groupIds?.length) filter.groupIds.forEach((id) => params.append('groupIds', String(id)));
  if (filter.statuses?.length) filter.statuses.forEach((s) => params.append('statuses', s));
  if (filter.formulaTypes?.length) filter.formulaTypes.forEach((s) => params.append('formulaTypes', s));
  if (filter.onlyWithIssues != null) params.set('onlyWithIssues', String(filter.onlyWithIssues));
  if (filter.search) params.set('search', filter.search);

  const blob = await downloadBlob(`/analytics/norm-registry/export?${params.toString()}`);
  triggerBrowserDownload(blob.blob, blob.filename);
}

export async function exportNormHealthReport(format: 'xlsx' | 'csv'): Promise<void> {
  const blob = await downloadBlob(`/analytics/norm-registry/health-report?format=${format}`);
  triggerBrowserDownload(blob.blob, blob.filename);
}
