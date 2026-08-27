import apiClient from '../services/apiClient';
import type { TrendDetailAggregation } from '../features/analytics/shared/trendDetailCalc';
import { normalizeTrendDetailAggregation } from '../features/analytics/shared/trendDetailCalc';

/**
 * Тренд индекса производительности (% к норме) по основным версиям проектов.
 * Backend: POST /api/analytics/trends/productivity-index
 * Индекс = Σ(объём × норматив) / Σ(факт. часы) × 100 (PerformanceCalculationEngine).
 */

export interface ProductivityIndexPoint {
  date: string;
  versionId: number;
  projectId: number;
  projectCode: string;
  projectName: string;
  versionName: string;
  /** Индекс, % к норме; null — нет строк с объёмом+нормой+часами. */
  index: number | null;
  /** Вес точки — фактические часы версии. */
  weightHours: number;
  flag: string;
}

export interface ProductivityIndexGroup {
  groupId: number;
  groupName: string;
  plan: ProductivityIndexPoint[];
  fact: ProductivityIndexPoint[];
}

export interface ProductivityIndexTrendResponse {
  overall: ProductivityIndexGroup;
  byGroup: ProductivityIndexGroup[];
  warnings: string[];
  detail: TrendDetailAggregation | null;
}

export interface ProductivityIndexTrendRequest {
  periodStart?: string | null;
  periodEnd?: string | null;
  projectIds?: number[] | null;
}

function normalizePoint(raw: Record<string, unknown>): ProductivityIndexPoint {
  return {
    date: String(raw.date ?? raw.Date ?? ''),
    versionId: Number(raw.versionId ?? raw.VersionId ?? 0),
    projectId: Number(raw.projectId ?? raw.ProjectId ?? 0),
    projectCode: String(raw.projectCode ?? raw.ProjectCode ?? ''),
    projectName: String(raw.projectName ?? raw.ProjectName ?? ''),
    versionName: String(raw.versionName ?? raw.VersionName ?? ''),
    index: raw.index == null && raw.Index == null ? null : Number(raw.index ?? raw.Index),
    weightHours: Number(raw.weightHours ?? raw.WeightHours ?? 0),
    flag: String(raw.flag ?? raw.Flag ?? 'ok'),
  };
}

function normalizeGroup(raw: Record<string, unknown>): ProductivityIndexGroup {
  const plan = (raw.plan ?? raw.Plan ?? []) as Array<Record<string, unknown>>;
  const fact = (raw.fact ?? raw.Fact ?? []) as Array<Record<string, unknown>>;
  return {
    groupId: Number(raw.groupId ?? raw.GroupId ?? 0),
    groupName: String(raw.groupName ?? raw.GroupName ?? ''),
    plan: plan.map(normalizePoint),
    fact: fact.map(normalizePoint),
  };
}

export async function fetchProductivityIndexTrend(
  request: ProductivityIndexTrendRequest,
  signal?: AbortSignal,
): Promise<ProductivityIndexTrendResponse> {
  const { data } = await apiClient.post<Record<string, unknown>>(
    '/analytics/trends/productivity-index',
    request,
    { signal },
  );
  const byGroup = (data.byGroup ?? data.ByGroup ?? []) as Array<Record<string, unknown>>;
  return {
    overall: normalizeGroup((data.overall ?? data.Overall ?? {}) as Record<string, unknown>),
    byGroup: byGroup.map(normalizeGroup),
    warnings: (data.warnings ?? data.Warnings ?? []) as string[],
    detail: normalizeTrendDetailAggregation(
      (data.detail ?? data.Detail ?? null) as Record<string, unknown> | null,
    ),
  };
}
