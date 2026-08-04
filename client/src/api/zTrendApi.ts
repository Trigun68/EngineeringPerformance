import apiClient from '../services/apiClient';

export type ZTrendFlag = 'ok' | 'sigma_zero' | 'no_norm' | 'clipped';
export type ZTrendWeightMode = 'basisHours' | 'uniform';

export interface ZTrendPoint {
  date: string;
  zScore: number | null;
  weight: number;
  projectCount: number;
  normId: number | null;
  flag: ZTrendFlag | string;
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

export interface ZTrendResponse {
  overall: ZTrendGroup;
  byGroup: ZTrendGroup[];
  legend: ZTrendLegendItem[];
  warnings: string[];
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

export async function fetchZTrendFilters(signal?: AbortSignal): Promise<ZTrendFilters> {
  const { data } = await apiClient.get<ZTrendFilters>('/analytics/trends/z-score/filters', { signal });
  return data;
}

export async function fetchZTrends(
  request: ZTrendRequest,
  signal?: AbortSignal,
): Promise<ZTrendResponse> {
  const { data } = await apiClient.post<ZTrendResponse>('/analytics/trends/z-score', request, { signal });
  return data;
}
