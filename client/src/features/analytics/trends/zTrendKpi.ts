import type { ZTrendGroup, ZTrendPoint } from '../../../api/zTrendApi';

export interface ZTrendKpiData {
  lastFactZ: number | null;
  lastFactDate: string | null;
  lastFactProjectCount: number | null;
  matchedPlanZ: number | null;
  planMatchKind: 'same-date' | 'nearest-prior' | 'nearest' | 'latest' | null;
  deltaZ: number | null;
  versionsCount: number;
  projectsCount: number;
}

interface FlatPoint {
  date: string;
  time: number;
  zScore: number | null;
  projectCount: number;
}

function flattenPoints(group: ZTrendGroup, seriesType: 'plan' | 'fact'): FlatPoint[] {
  return group[seriesType].segments
    .flatMap((s) => s.points)
    .map((p: ZTrendPoint) => ({
      date: p.date,
      time: new Date(p.date).getTime(),
      zScore: p.zScore,
      projectCount: p.projectCount,
    }))
    .filter((p) => Number.isFinite(p.time))
    .sort((a, b) => a.time - b.time);
}

/** Дата без времени — для сравнения «та же дата» независимо от часового пояса ответа. */
function sameDay(a: string, b: string): boolean {
  return a.slice(0, 10) === b.slice(0, 10);
}

/**
 * План-точка для сравнения с последним фактом: та же дата, иначе ближайшая ≤ даты факта,
 * иначе ближайшая по модулю (fallback при разрозненных осях плана/факта).
 */
function matchPlanPoint(
  planPoints: FlatPoint[],
  fact: FlatPoint,
): { point: FlatPoint; kind: ZTrendKpiData['planMatchKind'] } | null {
  const withZ = planPoints.filter((p) => p.zScore != null);
  if (withZ.length === 0) return null;

  const sameDate = withZ.find((p) => sameDay(p.date, fact.date));
  if (sameDate) return { point: sameDate, kind: 'same-date' };

  const prior = withZ.filter((p) => p.time <= fact.time);
  if (prior.length > 0) return { point: prior[prior.length - 1], kind: 'nearest-prior' };

  const nearest = withZ.reduce((best, p) =>
    Math.abs(p.time - fact.time) < Math.abs(best.time - fact.time) ? p : best,
  );
  return { point: nearest, kind: 'nearest' };
}

export function computeZTrendKpi(group: ZTrendGroup): ZTrendKpiData {
  const factPoints = flattenPoints(group, 'fact').filter((p) => p.zScore != null);
  const planPoints = flattenPoints(group, 'plan');

  const lastFact = factPoints.length > 0 ? factPoints[factPoints.length - 1] : null;
  // Без факт-точек показываем последний плановый Z, чтобы KPI не дегридировал в сплошные прочерки.
  const match = lastFact
    ? matchPlanPoint(planPoints, lastFact)
    : (() => {
        const withZ = planPoints.filter((p) => p.zScore != null);
        return withZ.length > 0
          ? { point: withZ[withZ.length - 1], kind: 'latest' as const }
          : null;
      })();

  const projectIds = new Set<number>();
  for (const seg of group.fact.segments) {
    for (const p of seg.points) {
      for (const c of p.contributions ?? []) {
        if (c.zScore != null) projectIds.add(c.projectId);
      }
    }
  }

  return {
    lastFactZ: lastFact?.zScore ?? null,
    lastFactDate: lastFact?.date ?? null,
    lastFactProjectCount: lastFact?.projectCount ?? null,
    matchedPlanZ: match?.point.zScore ?? null,
    planMatchKind: match?.kind ?? null,
    deltaZ:
      lastFact?.zScore != null && match?.point.zScore != null
        ? lastFact.zScore - match.point.zScore
        : null,
    versionsCount: factPoints.length,
    projectsCount: projectIds.size,
  };
}
