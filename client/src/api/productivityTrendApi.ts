import apiClient from '../services/apiClient';

export type ProductivityVolumeMode = 'median' | 'p10' | 'p90';

export interface ProductivityTrendPoint {
  date: string;
  hoursAtVStar: number | null;
  hoursPerUnit: number | null;
  index: number | null;
  normId: number | null;
  formulaType: string;
  flag: string;
}

export interface ProductivityTrendSegment {
  normId: number | null;
  normName: string;
  formulaType: string;
  color: string;
  segmentKey: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  points: ProductivityTrendPoint[];
}

export interface ProductivityTrendGroup {
  groupId: number;
  groupName: string;
  vStar: number | null;
  vStarSource: string;
  segments: ProductivityTrendSegment[];
}

export interface ProductivityTrendLegendItem {
  formulaType: string;
  displayName: string;
  color: string;
}

export interface ProductivityTrendTransition {
  groupId: number;
  groupName: string;
  oldNormId: number;
  newNormId: number;
  effectiveFrom: string;
  oldFormulaType: string;
  newFormulaType: string;
  vStarOld: number | null;
  vStarNew: number | null;
  hoursOldP10: number | null;
  hoursNewP10: number | null;
  hoursOldMedian: number | null;
  hoursNewMedian: number | null;
  hoursOldP90: number | null;
  hoursNewP90: number | null;
  standardEffectHours: number | null;
  mixEffectHours: number | null;
  weightHours: number;
  verdict: string;
  curvesCross: boolean;
  logVolumeLtOne: boolean;
}

export interface ProductivityTrendCounterfactual {
  groupId: number;
  groupName: string;
  projectId: number;
  projectCode: string;
  volume: number;
  hoursOld: number | null;
  hoursNew: number | null;
  deltaHours: number | null;
}

export interface ProductivityTrendResponse {
  volumeMode: string;
  overall: ProductivityTrendGroup;
  byGroup: ProductivityTrendGroup[];
  legend: ProductivityTrendLegendItem[];
  transitions: ProductivityTrendTransition[];
  counterfactuals: ProductivityTrendCounterfactual[];
  warnings: string[];
}

export interface ProductivityTrendRequest {
  periodStart?: string | null;
  periodEnd?: string | null;
  volumeMode?: ProductivityVolumeMode;
  workDirectionIds?: number[] | null;
  projectIds?: number[] | null;
}

function asRecord(value: unknown): Record<string, unknown> {
  return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : {};
}

function num(raw: Record<string, unknown>, camel: string, pascal: string): number | null {
  const value = raw[camel] ?? raw[pascal];
  if (value == null) return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

function str(raw: Record<string, unknown>, camel: string, pascal: string, fallback = ''): string {
  const value = raw[camel] ?? raw[pascal];
  return value == null ? fallback : String(value);
}

function normalizePoint(raw: Record<string, unknown>): ProductivityTrendPoint {
  return {
    date: str(raw, 'date', 'Date'),
    hoursAtVStar: num(raw, 'hoursAtVStar', 'HoursAtVStar'),
    hoursPerUnit: num(raw, 'hoursPerUnit', 'HoursPerUnit'),
    index: num(raw, 'index', 'Index'),
    normId: num(raw, 'normId', 'NormId'),
    formulaType: str(raw, 'formulaType', 'FormulaType'),
    flag: str(raw, 'flag', 'Flag', 'ok'),
  };
}

function normalizeSegment(raw: Record<string, unknown>): ProductivityTrendSegment {
  const points = (raw.points ?? raw.Points ?? []) as Array<Record<string, unknown>>;
  const to = raw.effectiveTo ?? raw.EffectiveTo;
  return {
    normId: num(raw, 'normId', 'NormId'),
    normName: str(raw, 'normName', 'NormName'),
    formulaType: str(raw, 'formulaType', 'FormulaType'),
    color: str(raw, 'color', 'Color', '#888'),
    segmentKey: str(raw, 'segmentKey', 'SegmentKey'),
    effectiveFrom: str(raw, 'effectiveFrom', 'EffectiveFrom'),
    effectiveTo: to == null ? null : String(to),
    points: points.map(normalizePoint),
  };
}

function normalizeGroup(raw: Record<string, unknown>): ProductivityTrendGroup {
  const segments = (raw.segments ?? raw.Segments ?? []) as Array<Record<string, unknown>>;
  return {
    groupId: Number(raw.groupId ?? raw.GroupId ?? 0),
    groupName: str(raw, 'groupName', 'GroupName'),
    vStar: num(raw, 'vStar', 'VStar'),
    vStarSource: str(raw, 'vStarSource', 'VStarSource'),
    segments: segments.map(normalizeSegment),
  };
}

function normalizeTransition(raw: Record<string, unknown>): ProductivityTrendTransition {
  return {
    groupId: Number(raw.groupId ?? raw.GroupId ?? 0),
    groupName: str(raw, 'groupName', 'GroupName'),
    oldNormId: Number(raw.oldNormId ?? raw.OldNormId ?? 0),
    newNormId: Number(raw.newNormId ?? raw.NewNormId ?? 0),
    effectiveFrom: str(raw, 'effectiveFrom', 'EffectiveFrom'),
    oldFormulaType: str(raw, 'oldFormulaType', 'OldFormulaType'),
    newFormulaType: str(raw, 'newFormulaType', 'NewFormulaType'),
    vStarOld: num(raw, 'vStarOld', 'VStarOld'),
    vStarNew: num(raw, 'vStarNew', 'VStarNew'),
    hoursOldP10: num(raw, 'hoursOldP10', 'HoursOldP10'),
    hoursNewP10: num(raw, 'hoursNewP10', 'HoursNewP10'),
    hoursOldMedian: num(raw, 'hoursOldMedian', 'HoursOldMedian'),
    hoursNewMedian: num(raw, 'hoursNewMedian', 'HoursNewMedian'),
    hoursOldP90: num(raw, 'hoursOldP90', 'HoursOldP90'),
    hoursNewP90: num(raw, 'hoursNewP90', 'HoursNewP90'),
    standardEffectHours: num(raw, 'standardEffectHours', 'StandardEffectHours'),
    mixEffectHours: num(raw, 'mixEffectHours', 'MixEffectHours'),
    weightHours: Number(raw.weightHours ?? raw.WeightHours ?? 0),
    verdict: str(raw, 'verdict', 'Verdict'),
    curvesCross: Boolean(raw.curvesCross ?? raw.CurvesCross),
    logVolumeLtOne: Boolean(raw.logVolumeLtOne ?? raw.LogVolumeLtOne),
  };
}

export async function fetchProductivityTrend(
  request: ProductivityTrendRequest,
  signal?: AbortSignal,
): Promise<ProductivityTrendResponse> {
  const { data } = await apiClient.post<unknown>(
    '/analytics/trends/productivity',
    request,
    { signal },
  );
  const raw = asRecord(data);
  const legend = (raw.legend ?? raw.Legend ?? []) as Array<Record<string, unknown>>;
  const byGroup = (raw.byGroup ?? raw.ByGroup ?? []) as Array<Record<string, unknown>>;
  const transitions = (raw.transitions ?? raw.Transitions ?? []) as Array<Record<string, unknown>>;
  const counterfactuals = (raw.counterfactuals ?? raw.Counterfactuals ?? []) as Array<Record<string, unknown>>;
  const warnings = (raw.warnings ?? raw.Warnings ?? []) as string[];
  return {
    volumeMode: str(raw, 'volumeMode', 'VolumeMode', 'median'),
    overall: normalizeGroup(asRecord(raw.overall ?? raw.Overall)),
    byGroup: byGroup.map(normalizeGroup),
    legend: legend.map((item) => ({
      formulaType: str(item, 'formulaType', 'FormulaType'),
      displayName: str(item, 'displayName', 'DisplayName'),
      color: str(item, 'color', 'Color', '#888'),
    })),
    transitions: transitions.map(normalizeTransition),
    counterfactuals: counterfactuals.map((item) => ({
      groupId: Number(item.groupId ?? item.GroupId ?? 0),
      groupName: str(item, 'groupName', 'GroupName'),
      projectId: Number(item.projectId ?? item.ProjectId ?? 0),
      projectCode: str(item, 'projectCode', 'ProjectCode'),
      volume: Number(item.volume ?? item.Volume ?? 0),
      hoursOld: num(item, 'hoursOld', 'HoursOld'),
      hoursNew: num(item, 'hoursNew', 'HoursNew'),
      deltaHours: num(item, 'deltaHours', 'DeltaHours'),
    })),
    warnings,
  };
}
