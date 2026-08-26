import type {
  CompareChartResult,
  VersionCompareFacilityRow,
  VersionCompareResult,
  VersionCompareRow,
} from '../../../api/analyticsApi';

function absDelta(value: number | null | undefined): number {
  return Math.abs(value ?? 0);
}

function hoursDelta(a: number | null | undefined, b: number | null | undefined): number | null {
  if (a == null || b == null) return null;
  return b - a;
}

function iicPoints(rows: VersionCompareRow[], metric: 'zScore' | 'hours', top: number) {
  const byHours = metric === 'hours';
  return rows
    .filter((r) => r.workDirectionId >= 0 && !r.isTotal)
    .map((r) => ({
      category: r.name,
      workDirectionId: r.workDirectionId,
      valueA: byHours ? (r.plannedLaborHoursA ?? null) : (r.factZScoreA ?? null),
      valueB: byHours ? (r.plannedLaborHoursB ?? null) : (r.factZScoreB ?? null),
      delta: byHours ? hoursDelta(r.plannedLaborHoursA, r.plannedLaborHoursB) : (r.factZDelta ?? null),
    }))
    .sort((a, b) => absDelta(b.delta) - absDelta(a.delta))
    .slice(0, top);
}

function facilityPoints(rows: VersionCompareFacilityRow[], metric: 'zScore' | 'hours', top: number) {
  const byHours = metric === 'hours';
  return rows
    .map((f) => ({
      category: f.facilityName,
      workDirectionId: null as number | null,
      valueA: byHours ? f.factLaborHoursA : f.zScoreA,
      valueB: byHours ? f.factLaborHoursB : f.zScoreB,
      delta: byHours ? f.hoursDelta : f.zDelta,
    }))
    .sort((a, b) => absDelta(b.delta) - absDelta(a.delta))
    .slice(0, top);
}

/** Диаграмма из уже загруженного summary/facility — без второго тяжёлого GET compare/chart. */
export function buildCompareChartFromSummary(
  compare: VersionCompareResult,
  versionAName: string,
  versionBName: string,
  level: 'iic' | 'facility' = 'iic',
  metric: 'zScore' | 'hours' = 'zScore',
  top = 20,
): CompareChartResult {
  const take = Math.min(100, Math.max(1, top));
  const points =
    level === 'facility'
      ? facilityPoints(compare.facility, metric, take)
      : iicPoints(compare.summary, metric, take);

  return {
    level,
    metric,
    versionAName,
    versionBName,
    points,
  };
}
