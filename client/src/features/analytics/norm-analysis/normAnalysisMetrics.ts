import type { HealthSummary, NormHealthIssue, NormListItem } from '../../../api/normRegistryApi';
import { formatIisNorm } from '../../../utils/formatIisNorm';

export type NormConfidence = 'High' | 'Medium' | 'Low';
export type NormKpiFilter = 'all' | 'Error' | 'Warning' | 'cv' | 'lowConfidence' | 'ok';

const CV_OUTLIER_FACTOR = 1.5;
const LOW_USAGE_VERSIONS = 3;
const MEDIUM_USAGE_VERSIONS = 8;

/** CV = |σ / производительность| по полям активной нормы (не Estimate/μ лог-модели). */
export function coefficientOfVariation(mean: number | null, sigma: number | null): number | null {
  if (mean == null || sigma == null || mean === 0) return null;
  return Math.abs(sigma / mean);
}

export function medianNumber(values: number[]): number | null {
  if (values.length === 0) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 === 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
}

export function isCvOutlier(cv: number | null, medianCv: number | null): boolean {
  if (cv == null || medianCv == null || medianCv <= 0) return false;
  return cv >= medianCv * CV_OUTLIER_FACTOR;
}

/**
 * Уверенность в норме: мало версий / нет μ или σ / σ=0 → Low;
 * умеренное число версий или есть замечания → Medium; иначе High.
 */
export function confidenceOf(item: NormListItem): NormConfidence {
  if (
    item.meanPerformance == null ||
    item.stdDeviation == null ||
    item.stdDeviation === 0 ||
    item.totalVersions < LOW_USAGE_VERSIONS
  ) {
    return 'Low';
  }
  if (item.totalVersions < MEDIUM_USAGE_VERSIONS || (item.issueCount ?? 0) > 0) {
    return 'Medium';
  }
  return 'High';
}

export interface NormKpiSummary {
  total: number;
  errors: number;
  warnings: number;
  cvOutliers: number;
  lowConfidence: number;
  ok: number;
  medianCv: number | null;
}

export function summarizeNormKpis(items: NormListItem[]): NormKpiSummary {
  const cvs = items
    .map((n) => coefficientOfVariation(n.meanPerformance, n.stdDeviation))
    .filter((v): v is number => v != null);
  const medianCv = medianNumber(cvs);
  const errors = items.filter((n) => n.maxIssueSeverity === 'Error').length;
  const warnings = items.filter((n) => n.maxIssueSeverity === 'Warning').length;
  const cvOutliers = items.filter((n) =>
    isCvOutlier(coefficientOfVariation(n.meanPerformance, n.stdDeviation), medianCv),
  ).length;
  const ok = items.filter((n) => !n.maxIssueSeverity && (n.issueCount ?? 0) === 0).length;
  const lowConfidence = items.filter((n) => confidenceOf(n) === 'Low').length;
  return {
    total: items.length,
    errors,
    warnings,
    cvOutliers,
    lowConfidence,
    ok,
    medianCv,
  };
}

export function filterNormsByKpi(
  items: NormListItem[],
  filter: NormKpiFilter,
  medianCv: number | null,
): NormListItem[] {
  if (filter === 'all') return items;
  if (filter === 'ok') return items.filter((n) => !n.maxIssueSeverity && (n.issueCount ?? 0) === 0);
  if (filter === 'cv') {
    return items.filter((n) =>
      isCvOutlier(coefficientOfVariation(n.meanPerformance, n.stdDeviation), medianCv),
    );
  }
  if (filter === 'lowConfidence') return items.filter((n) => confidenceOf(n) === 'Low');
  return items.filter((n) => n.maxIssueSeverity === filter);
}

export function coverageIssues(health: HealthSummary | null): NormHealthIssue[] {
  if (!health) return [];
  return health.issues.filter((i) => i.ruleCode === 'NO_ACTIVE_NORM');
}

export function maxSeverityForGroup(
  issues: NormHealthIssue[],
  groupId: number,
): 'Error' | 'Warning' | 'Info' | null {
  const related = issues.filter((i) => i.groupId === groupId);
  if (related.some((i) => i.severity === 'Error')) return 'Error';
  if (related.some((i) => i.severity === 'Warning')) return 'Warning';
  if (related.some((i) => i.severity === 'Info')) return 'Info';
  return null;
}

const SEVERITY_BAR: Record<'Error' | 'Warning' | 'Info', string> = {
  Error: '#E74C3C',
  Warning: '#F39C12',
  Info: '#3498DB',
};

export function severityBarColor(
  fallback: string,
  severity: 'Error' | 'Warning' | 'Info' | null,
): string {
  return severity ? SEVERITY_BAR[severity] : fallback;
}

/** Доля точек факта внутри полосы mean ± sigma. */
export function pctPointsInBand(values: number[], mean: number, sigma: number): number | null {
  if (values.length === 0 || sigma < 0) return null;
  const lower = mean - sigma;
  const upper = mean + sigma;
  const inside = values.filter((v) => v >= lower && v <= upper).length;
  return Math.round((inside / values.length) * 100);
}

export type IssueSeverityKey = 'Error' | 'Warning' | 'Info' | 'None';

export const SEVERITY_COLOR: Record<IssueSeverityKey, string> = {
  Error: '#E74C3C',
  Warning: '#F39C12',
  Info: '#3498DB',
  None: '#27AE60',
};

export const SEVERITY_BG: Record<IssueSeverityKey, string> = {
  Error: 'rgba(231, 76, 60, 0.16)',
  Warning: 'rgba(243, 156, 18, 0.16)',
  Info: 'rgba(52, 152, 219, 0.14)',
  None: 'rgba(39, 174, 96, 0.12)',
};

export const SEVERITY_LABEL: Record<IssueSeverityKey, string> = {
  Error: 'Требует пересмотра',
  Warning: 'Есть отклонение',
  Info: 'Незначительное замечание',
  None: 'В норме',
};

export const STATUS_LABEL: Record<string, string> = {
  Active: 'Активна',
  Historical: 'Историческая',
  Future: 'Будущая',
};

export const CONFIDENCE_COLOR: Record<NormConfidence, string> = {
  High: '#27AE60',
  Medium: '#F39C12',
  Low: '#E74C3C',
};

export const CONFIDENCE_LABEL: Record<NormConfidence, string> = {
  High: 'Высокая',
  Medium: 'Средняя',
  Low: 'Низкая',
};

const SEVERITY_RANK: Record<IssueSeverityKey, number> = {
  Error: 3,
  Warning: 2,
  Info: 1,
  None: 0,
};

export function toSeverityKey(value: string | null | undefined): IssueSeverityKey {
  if (value === 'Error' || value === 'Warning' || value === 'Info') return value;
  return 'None';
}

export function cvHeatBackground(cv: number | null, medianCv: number | null): string {
  if (cv == null || medianCv == null || medianCv <= 0) return 'transparent';
  const ratio = cv / medianCv;
  if (ratio >= 2.2) return SEVERITY_BG.Error;
  if (ratio >= 1.5) return SEVERITY_BG.Warning;
  return 'transparent';
}

export function monthsSince(iso: string, now: Date = new Date()): number | null {
  const from = new Date(iso);
  if (!Number.isFinite(from.getTime())) return null;
  return Math.max(
    0,
    (now.getFullYear() - from.getFullYear()) * 12 + (now.getMonth() - from.getMonth()),
  );
}

export function isStaleActiveReview(status: string, months: number | null): boolean {
  return status === 'Active' && months != null && months >= 12;
}

export function previousNormInGroup(
  items: NormListItem[],
  current: NormListItem,
): NormListItem | null {
  const from = new Date(current.effectiveFrom).getTime();
  if (!Number.isFinite(from)) return null;
  const earlier = items.filter((n) => {
    if (n.groupId !== current.groupId || n.id === current.id) return false;
    const t = new Date(n.effectiveFrom).getTime();
    return Number.isFinite(t) && t < from;
  });
  if (earlier.length === 0) return null;
  return [...earlier].sort(
    (a, b) => new Date(b.effectiveFrom).getTime() - new Date(a.effectiveFrom).getTime(),
  )[0];
}

/** Последние μ той же группы (до 6 точек) — честный дрейф нормы, не синтетика факт/норма. */
export function driftMeansForGroup(items: NormListItem[], groupId: number): number[] {
  return items
    .filter((n) => n.groupId === groupId && n.meanPerformance != null)
    .sort((a, b) => new Date(a.effectiveFrom).getTime() - new Date(b.effectiveFrom).getTime())
    .map((n) => n.meanPerformance as number)
    .slice(-6);
}

export interface NormGroupMeta {
  id: number;
  name: string;
  code: string | null;
  parentId: number | null;
  unitName: string | null;
}

export function heatmapCategory(item: NormListItem, groups: NormGroupMeta[]): string {
  const g = groups.find((x) => x.id === item.groupId);
  if (g?.parentId != null) {
    const parent = groups.find((x) => x.id === g.parentId);
    if (parent) {
      return formatIisNorm({ code: parent.code, name: parent.name, unitName: parent.unitName });
    }
  }
  return formatIisNorm({ code: item.groupCode, name: item.groupName, unitName: g?.unitName });
}

/** Имя без ведущего кода «X — ». */
export function iicNameWithoutCode(formatted: string): string {
  const parts = formatted.split(' — ');
  return parts.length > 1 ? parts.slice(1).join(' — ') : formatted;
}

export function sortNormsForRegistry(items: NormListItem[]): NormListItem[] {
  return [...items].sort((a, b) => {
    const rank =
      SEVERITY_RANK[toSeverityKey(b.maxIssueSeverity)] - SEVERITY_RANK[toSeverityKey(a.maxIssueSeverity)];
    if (rank !== 0) return rank;
    return (a.groupName ?? '').localeCompare(b.groupName ?? '', 'ru');
  });
}

export function issuesForNorm(issues: NormHealthIssue[], row: NormListItem): NormHealthIssue[] {
  return issues.filter((i) => i.normIds.includes(row.id) || i.groupId === row.groupId);
}

export function severityForNorm(issues: NormHealthIssue[], row: NormListItem): IssueSeverityKey {
  const related = issuesForNorm(issues, row);
  if (related.some((i) => i.severity === 'Error') || row.maxIssueSeverity === 'Error') return 'Error';
  if (related.some((i) => i.severity === 'Warning') || row.maxIssueSeverity === 'Warning') return 'Warning';
  if (related.some((i) => i.severity === 'Info') || row.maxIssueSeverity === 'Info') return 'Info';
  return 'None';
}

export function coverageChipLabel(issue: NormHealthIssue, groups: NormGroupMeta[]): string {
  if (issue.groupId != null) {
    const g = groups.find((x) => x.id === issue.groupId);
    if (g) return formatIisNorm({ code: g.code, name: g.name, unitName: g.unitName });
  }
  return issue.message;
}
