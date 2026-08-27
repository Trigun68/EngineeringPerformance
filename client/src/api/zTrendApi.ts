import apiClient from '../services/apiClient';
import type { TrendDetailAggregation } from '../features/analytics/shared/trendDetailCalc';
import { normalizeTrendDetailAggregation } from '../features/analytics/shared/trendDetailCalc';
import {
  normalizeHoursBridge,
  normalizeSCurve,
  type HoursBridgeResultDto,
  type CumulativeHoursPointDto,
} from './projectTrendApi';

export type ZTrendFlag = 'ok' | 'sigma_zero' | 'no_norm' | 'clipped';
export type ZTrendWeightMode = 'basisHours';

export interface ZTrendContribution {
  projectId: number;
  projectCode: string;
  projectName: string;
  versionId: number;
  versionName: string;
  zScore: number | null;
  weight: number;
}

export interface ZTrendPoint {
  date: string;
  zScore: number | null;
  weight: number;
  projectCount: number;
  normId: number | null;
  flag: ZTrendFlag | string;
  contributions?: ZTrendContribution[] | null;
}

export interface ZTrendSegment {
  normId: number | null;
  normName: string;
  normGroupId: number | null;
  normGroupName: string;
  color: string;
  segmentKey: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  points: ZTrendPoint[];
}

export interface ZTrendSeries {
  seriesType: 'Plan' | 'Fact' | string;
  segments: ZTrendSegment[];
}

export interface ZTrendGroup {
  groupId: number;
  groupName: string;
  plan: ZTrendSeries;
  fact: ZTrendSeries;
}

export interface ZTrendLegendItem {
  normId: number | null;
  normName: string;
  groupId: number | null;
  groupName: string;
  color: string;
  segmentKey: string;
  effectiveFrom: string;
  effectiveTo: string | null;
}

export interface GroupZeroBaseline {
  groupId: number;
  groupName: string;
  bars: { name: string; zScore: number }[];
}

export interface ZTrendResponse {
  overall: ZTrendGroup;
  byGroup: ZTrendGroup[];
  legend: ZTrendLegendItem[];
  warnings: string[];
  detail: TrendDetailAggregation | null;
  disciplineBridge: HoursBridgeResultDto;
  groupZeroBars: GroupZeroBaseline[];
  portfolioSCurve: CumulativeHoursPointDto[];
}

export interface ZTrendRequest {
  periodStart?: string | null;
  periodEnd?: string | null;
  workDirectionIds?: number[] | null;
  projectIds?: number[] | null;
  weightMode?: ZTrendWeightMode;
}

export interface ZTrendFilters {
  projects: { id: number; code: string; name: string }[];
  groups: { id: number; code: string; name: string }[];
  weightModes: string[];
}

function normalizeContribution(raw: Record<string, unknown>): ZTrendContribution {
  return {
    projectId: Number(raw.projectId ?? raw.ProjectId ?? 0),
    projectCode: String(raw.projectCode ?? raw.ProjectCode ?? ''),
    projectName: String(raw.projectName ?? raw.ProjectName ?? ''),
    versionId: Number(raw.versionId ?? raw.VersionId ?? 0),
    versionName: String(raw.versionName ?? raw.VersionName ?? ''),
    zScore:
      raw.zScore == null && raw.ZScore == null ? null : Number(raw.zScore ?? raw.ZScore),
    weight: Number(raw.weight ?? raw.Weight ?? 0),
  };
}

function normalizePoint(raw: Record<string, unknown>): ZTrendPoint {
  const contribs = (raw.contributions ?? raw.Contributions) as
    | Array<Record<string, unknown>>
    | null
    | undefined;
  return {
    date: String(raw.date ?? raw.Date ?? ''),
    zScore:
      raw.zScore == null && raw.ZScore == null ? null : Number(raw.zScore ?? raw.ZScore),
    weight: Number(raw.weight ?? raw.Weight ?? 0),
    projectCount: Number(raw.projectCount ?? raw.ProjectCount ?? 0),
    normId:
      raw.normId == null && raw.NormId == null ? null : Number(raw.normId ?? raw.NormId),
    flag: String(raw.flag ?? raw.Flag ?? 'ok'),
    contributions: contribs?.map(normalizeContribution) ?? null,
  };
}

function normalizeSegment(raw: Record<string, unknown>): ZTrendSegment {
  const points = (raw.points ?? raw.Points ?? []) as Array<Record<string, unknown>>;
  return {
    normId:
      raw.normId == null && raw.NormId == null ? null : Number(raw.normId ?? raw.NormId),
    normName: String(raw.normName ?? raw.NormName ?? ''),
    normGroupId:
      raw.normGroupId == null && raw.NormGroupId == null
        ? null
        : Number(raw.normGroupId ?? raw.NormGroupId),
    normGroupName: String(raw.normGroupName ?? raw.NormGroupName ?? ''),
    color: String(raw.color ?? raw.Color ?? '#888'),
    segmentKey: String(raw.segmentKey ?? raw.SegmentKey ?? ''),
    effectiveFrom: String(raw.effectiveFrom ?? raw.EffectiveFrom ?? ''),
    effectiveTo: (raw.effectiveTo ?? raw.EffectiveTo ?? null) as string | null,
    points: points.map(normalizePoint),
  };
}

function normalizeSeries(raw: Record<string, unknown>): ZTrendSeries {
  const segments = (raw.segments ?? raw.Segments ?? []) as Array<Record<string, unknown>>;
  return {
    seriesType: String(raw.seriesType ?? raw.SeriesType ?? ''),
    segments: segments.map(normalizeSegment),
  };
}

function normalizeGroup(raw: Record<string, unknown>): ZTrendGroup {
  return {
    groupId: Number(raw.groupId ?? raw.GroupId ?? 0),
    groupName: String(raw.groupName ?? raw.GroupName ?? ''),
    plan: normalizeSeries((raw.plan ?? raw.Plan ?? {}) as Record<string, unknown>),
    fact: normalizeSeries((raw.fact ?? raw.Fact ?? {}) as Record<string, unknown>),
  };
}

export async function fetchZTrendFilters(signal?: AbortSignal): Promise<ZTrendFilters> {
  const { data } = await apiClient.get<Record<string, unknown>>(
    '/analytics/trends/z-score/filters',
    { signal },
  );
  const projects = (data.projects ?? data.Projects ?? []) as Array<Record<string, unknown>>;
  const groups = (data.groups ?? data.Groups ?? []) as Array<Record<string, unknown>>;
  return {
    projects: projects.map((p) => ({
      id: Number(p.id ?? p.Id),
      code: String(p.code ?? p.Code ?? ''),
      name: String(p.name ?? p.Name ?? ''),
    })),
    groups: groups.map((g) => ({
      id: Number(g.id ?? g.Id),
      code: String(g.code ?? g.Code ?? ''),
      name: String(g.name ?? g.Name ?? ''),
    })),
    weightModes: (data.weightModes ?? data.WeightModes ?? ['basisHours']) as string[],
  };
}

export async function fetchZTrends(
  request: ZTrendRequest,
  signal?: AbortSignal,
): Promise<ZTrendResponse> {
  const { data } = await apiClient.post<Record<string, unknown>>(
    '/analytics/trends/z-score',
    request,
    { signal },
  );
  const legend = (data.legend ?? data.Legend ?? []) as Array<Record<string, unknown>>;
  const byGroup = (data.byGroup ?? data.ByGroup ?? []) as Array<Record<string, unknown>>;
  return {
    overall: normalizeGroup((data.overall ?? data.Overall ?? {}) as Record<string, unknown>),
    byGroup: byGroup.map(normalizeGroup),
    legend: legend.map((l) => ({
      normId: l.normId == null && l.NormId == null ? null : Number(l.normId ?? l.NormId),
      normName: String(l.normName ?? l.NormName ?? ''),
      groupId: l.groupId == null && l.GroupId == null ? null : Number(l.groupId ?? l.GroupId),
      groupName: String(l.groupName ?? l.GroupName ?? ''),
      color: String(l.color ?? l.Color ?? '#888'),
      segmentKey: String(l.segmentKey ?? l.SegmentKey ?? ''),
      effectiveFrom: String(l.effectiveFrom ?? l.EffectiveFrom ?? ''),
      effectiveTo: (l.effectiveTo ?? l.EffectiveTo ?? null) as string | null,
    })),
    warnings: (data.warnings ?? data.Warnings ?? []) as string[],
    detail: normalizeTrendDetailAggregation(
      (data.detail ?? data.Detail ?? null) as Record<string, unknown> | null,
    ),
    disciplineBridge: normalizeHoursBridge(data.disciplineBridge ?? data.DisciplineBridge),
    groupZeroBars: ((data.groupZeroBars ?? data.GroupZeroBars ?? []) as Array<Record<string, unknown>>).map(
      (g) => ({
        groupId: Number(g.groupId ?? g.GroupId ?? 0),
        groupName: String(g.groupName ?? g.GroupName ?? ''),
        bars: ((g.bars ?? g.Bars ?? []) as Array<Record<string, unknown>>).map((b) => ({
          name: String(b.name ?? b.Name ?? ''),
          zScore: Number(b.zScore ?? b.ZScore ?? 0),
        })),
      }),
    ),
    portfolioSCurve: normalizeSCurve(data.portfolioSCurve ?? data.PortfolioSCurve),
  };
}
