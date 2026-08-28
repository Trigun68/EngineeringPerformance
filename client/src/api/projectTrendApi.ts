import apiClient, { downloadBlob, downloadBlobPost, triggerBrowserDownload } from '../services/apiClient';

export interface HoursBridgeLineDto {
  groupId: number;
  groupName: string;
  volumeEffect: number;
  productivityEffect: number;
}

export interface HoursBridgeResultDto {
  lines: HoursBridgeLineDto[];
  totalVolumeEffect: number;
  totalProductivityEffect: number;
}

export interface HoursCoverageDto {
  hoursTotal: number;
  hoursWithZ: number;
  hoursWithBridgePair: number;
  hoursEligibleForBridge: number;
  shareWithZ: number | null;
  shareWithBridgePair: number | null;
}

export interface ZeroBaselineBarDto {
  name: string;
  zScore: number;
}

export interface CumulativeHoursPointDto {
  date: string;
  cumulativeHours: number;
  label?: string | null;
}

export interface NamedSCurveDto {
  name: string;
  points: CumulativeHoursPointDto[];
}

export interface WaterfallStepDto {
  name: string;
  value: number;
}

export interface PercentileBandPointDto {
  date: string;
  p10: number;
  p90: number;
}

export interface TrendEventDto {
  timestamp: string;
  type: string;
  label: string;
  versionId?: number | null;
}

export interface MoverRowDto {
  projectId: number;
  projectCode: string;
  projectName: string;
  zScore: number | null;
  weight: number;
  contribution: number;
}

export interface ProjectTrendZPointDto {
  date: string;
  direction: string;
  versionName: string;
  zScore: number | null;
  hours: number;
}

export interface ProjectTrendDto {
  projectCode: string;
  projectName: string;
  factVersionId: number | null;
  factVersionName: string | null;
  factVersionDate: string | null;
  planVersionId: number | null;
  planVersionName: string | null;
  zHistory: ProjectTrendZPointDto[];
  hoursBridge: HoursBridgeResultDto;
  zFromZero: ZeroBaselineBarDto[];
  sCurve: CumulativeHoursPointDto[];
  deltaWaterfallStart: number | null;
  deltaWaterfall: WaterfallStepDto[] | null;
  warnings: string[];
  coverage: HoursCoverageDto;
  portfolioZHistory: ProjectTrendZPointDto[];
  zPercentileBand: PercentileBandPointDto[];
  events: TrendEventDto[];
  planSCurve: CumulativeHoursPointDto[];
  rawHoursDelta: number | null;
  planDeltaWaterfallStart: number | null;
  planDeltaWaterfall: WaterfallStepDto[] | null;
}

function num(raw: unknown): number {
  const n = Number(raw);
  return Number.isFinite(n) ? n : 0;
}

function numOrNull(raw: unknown): number | null {
  if (raw == null) return null;
  const n = Number(raw);
  return Number.isFinite(n) ? n : null;
}

function str(raw: unknown, fallback = ''): string {
  return raw == null ? fallback : String(raw);
}

export const EMPTY_COVERAGE: HoursCoverageDto = {
  hoursTotal: 0,
  hoursWithZ: 0,
  hoursWithBridgePair: 0,
  hoursEligibleForBridge: 0,
  shareWithZ: null,
  shareWithBridgePair: null,
};

export function normalizeHoursBridge(raw: unknown): HoursBridgeResultDto {
  const row = (raw ?? {}) as Record<string, unknown>;
  const linesRaw = (row.lines ?? row.Lines ?? []) as Array<Record<string, unknown>>;
  return {
    lines: linesRaw.map((l) => ({
      groupId: num(l.groupId ?? l.GroupId),
      groupName: str(l.groupName ?? l.GroupName),
      volumeEffect: num(l.volumeEffect ?? l.VolumeEffect),
      productivityEffect: num(l.productivityEffect ?? l.ProductivityEffect),
    })),
    totalVolumeEffect: num(row.totalVolumeEffect ?? row.TotalVolumeEffect),
    totalProductivityEffect: num(row.totalProductivityEffect ?? row.TotalProductivityEffect),
  };
}

export function normalizeCoverage(raw: unknown): HoursCoverageDto {
  const row = (raw ?? {}) as Record<string, unknown>;
  return {
    hoursTotal: num(row.hoursTotal ?? row.HoursTotal),
    hoursWithZ: num(row.hoursWithZ ?? row.HoursWithZ),
    hoursWithBridgePair: num(row.hoursWithBridgePair ?? row.HoursWithBridgePair),
    hoursEligibleForBridge: num(row.hoursEligibleForBridge ?? row.HoursEligibleForBridge),
    shareWithZ: numOrNull(row.shareWithZ ?? row.ShareWithZ),
    shareWithBridgePair: numOrNull(row.shareWithBridgePair ?? row.ShareWithBridgePair),
  };
}

export function normalizeZeroBars(raw: unknown): ZeroBaselineBarDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return {
      name: str(row.name ?? row.Name),
      zScore: num(row.zScore ?? row.ZScore),
    };
  });
}

export function normalizeSCurve(raw: unknown): CumulativeHoursPointDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return {
      date: str(row.date ?? row.Date),
      cumulativeHours: num(row.cumulativeHours ?? row.CumulativeHours),
      label: (row.label ?? row.Label ?? null) as string | null,
    };
  });
}

export function normalizeNamedSCurves(raw: unknown): NamedSCurveDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return {
      name: str(row.name ?? row.Name),
      points: normalizeSCurve(row.points ?? row.Points),
    };
  });
}

export function normalizeWaterfall(raw: unknown): WaterfallStepDto[] | null {
  if (!Array.isArray(raw) || raw.length === 0) return null;
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return { name: str(row.name ?? row.Name), value: num(row.value ?? row.Value) };
  });
}

export function normalizeBand(raw: unknown): PercentileBandPointDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return {
      date: str(row.date ?? row.Date),
      p10: num(row.p10 ?? row.P10),
      p90: num(row.p90 ?? row.P90),
    };
  });
}

export function normalizeEvents(raw: unknown): TrendEventDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return {
      timestamp: str(row.timestamp ?? row.Timestamp),
      type: str(row.type ?? row.Type),
      label: str(row.label ?? row.Label),
      versionId: numOrNull(row.versionId ?? row.VersionId),
    };
  });
}

export function normalizeMovers(raw: unknown): MoverRowDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return {
      projectId: num(row.projectId ?? row.ProjectId),
      projectCode: str(row.projectCode ?? row.ProjectCode),
      projectName: str(row.projectName ?? row.ProjectName),
      zScore: numOrNull(row.zScore ?? row.ZScore),
      weight: num(row.weight ?? row.Weight),
      contribution: num(row.contribution ?? row.Contribution),
    };
  });
}

function normalizeZPoint(p: Record<string, unknown>): ProjectTrendZPointDto {
  return {
    date: str(p.date ?? p.Date),
    direction: str(p.direction ?? p.Direction),
    versionName: str(p.versionName ?? p.VersionName),
    zScore: numOrNull(p.zScore ?? p.ZScore),
    hours: num(p.hours ?? p.Hours),
  };
}

export async function fetchProjectTrend(projectId: number, signal?: AbortSignal): Promise<ProjectTrendDto> {
  const { data } = await apiClient.get<Record<string, unknown>>(`/projects/${projectId}/report-trend`, {
    signal,
  });
  const history = (data.zHistory ?? data.ZHistory ?? []) as Array<Record<string, unknown>>;
  const portfolio = (data.portfolioZHistory ?? data.PortfolioZHistory ?? []) as Array<Record<string, unknown>>;
  return {
    projectCode: str(data.projectCode ?? data.ProjectCode),
    projectName: str(data.projectName ?? data.ProjectName),
    factVersionId: numOrNull(data.factVersionId ?? data.FactVersionId),
    factVersionName: (data.factVersionName ?? data.FactVersionName ?? null) as string | null,
    factVersionDate: (data.factVersionDate ?? data.FactVersionDate ?? null) as string | null,
    planVersionId: numOrNull(data.planVersionId ?? data.PlanVersionId),
    planVersionName: (data.planVersionName ?? data.PlanVersionName ?? null) as string | null,
    zHistory: history.map(normalizeZPoint),
    hoursBridge: normalizeHoursBridge(data.hoursBridge ?? data.HoursBridge),
    zFromZero: normalizeZeroBars(data.zFromZero ?? data.ZFromZero),
    sCurve: normalizeSCurve(data.sCurve ?? data.SCurve),
    deltaWaterfallStart: numOrNull(data.deltaWaterfallStart ?? data.DeltaWaterfallStart),
    deltaWaterfall: normalizeWaterfall(data.deltaWaterfall ?? data.DeltaWaterfall),
    warnings: (data.warnings ?? data.Warnings ?? []) as string[],
    coverage: normalizeCoverage(data.coverage ?? data.Coverage),
    portfolioZHistory: portfolio.map(normalizeZPoint),
    zPercentileBand: normalizeBand(data.zPercentileBand ?? data.ZPercentileBand),
    events: normalizeEvents(data.events ?? data.Events),
    planSCurve: normalizeSCurve(data.planSCurve ?? data.PlanSCurve),
    rawHoursDelta: numOrNull(data.rawHoursDelta ?? data.RawHoursDelta),
    planDeltaWaterfallStart: numOrNull(data.planDeltaWaterfallStart ?? data.PlanDeltaWaterfallStart),
    planDeltaWaterfall: normalizeWaterfall(data.planDeltaWaterfall ?? data.PlanDeltaWaterfall),
  };
}

export async function exportProjectTrendExcel(projectId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(`/projects/${projectId}/report-trend/export.xlsx`);
  triggerBrowserDownload(blob, filename);
}

export async function exportProjectTrendPdf(projectId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(`/projects/${projectId}/report-trend/export.pdf`);
  triggerBrowserDownload(blob, filename);
}

export async function exportAnalyticsTrendExcel(body: Record<string, unknown>): Promise<void> {
  const { blob, filename } = await downloadBlobPost('/analytics/trends/z-score/export.xlsx', body);
  triggerBrowserDownload(blob, filename);
}

export async function exportAnalyticsTrendPdf(body: Record<string, unknown>): Promise<void> {
  const { blob, filename } = await downloadBlobPost('/analytics/trends/z-score/export.pdf', body);
  triggerBrowserDownload(blob, filename);
}
