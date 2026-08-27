import apiClient from '../services/apiClient';

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

export interface ZeroBaselineBarDto {
  name: string;
  zScore: number;
}

export interface CumulativeHoursPointDto {
  date: string;
  cumulativeHours: number;
  label?: string | null;
}

export interface WaterfallStepDto {
  name: string;
  value: number;
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

function normalizeWaterfall(raw: unknown): WaterfallStepDto[] | null {
  if (!Array.isArray(raw) || raw.length === 0) return null;
  return raw.map((item) => {
    const row = item as Record<string, unknown>;
    return { name: str(row.name ?? row.Name), value: num(row.value ?? row.Value) };
  });
}

export async function fetchProjectTrend(projectId: number, signal?: AbortSignal): Promise<ProjectTrendDto> {
  const { data } = await apiClient.get<Record<string, unknown>>(`/projects/${projectId}/report-trend`, {
    signal,
  });
  const history = (data.zHistory ?? data.ZHistory ?? []) as Array<Record<string, unknown>>;
  return {
    projectCode: str(data.projectCode ?? data.ProjectCode),
    projectName: str(data.projectName ?? data.ProjectName),
    factVersionId: numOrNull(data.factVersionId ?? data.FactVersionId),
    factVersionName: (data.factVersionName ?? data.FactVersionName ?? null) as string | null,
    factVersionDate: (data.factVersionDate ?? data.FactVersionDate ?? null) as string | null,
    planVersionId: numOrNull(data.planVersionId ?? data.PlanVersionId),
    planVersionName: (data.planVersionName ?? data.PlanVersionName ?? null) as string | null,
    zHistory: history.map((p) => ({
      date: str(p.date ?? p.Date),
      direction: str(p.direction ?? p.Direction),
      versionName: str(p.versionName ?? p.VersionName),
      zScore: numOrNull(p.zScore ?? p.ZScore),
      hours: num(p.hours ?? p.Hours),
    })),
    hoursBridge: normalizeHoursBridge(data.hoursBridge ?? data.HoursBridge),
    zFromZero: normalizeZeroBars(data.zFromZero ?? data.ZFromZero),
    sCurve: normalizeSCurve(data.sCurve ?? data.SCurve),
    deltaWaterfallStart: numOrNull(data.deltaWaterfallStart ?? data.DeltaWaterfallStart),
    deltaWaterfall: normalizeWaterfall(data.deltaWaterfall ?? data.DeltaWaterfall),
    warnings: (data.warnings ?? data.Warnings ?? []) as string[],
  };
}
