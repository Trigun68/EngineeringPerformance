import type { DynamicsPoint } from './PeriodDynamicsChart';
import type { WaterfallStep } from './FactorWaterfallChart';

/**
 * Расчёты детализированных тренд-вкладок (мокап TrendDetailMockup на реальных данных).
 * Бакетирование по месяцам — взвешенное среднее точек версий, вес = часы версии.
 */

export interface TrendPointLike {
  /** ISO-дата точки (дата создания версии). */
  date: string;
  /** Значение метрики (Z-оценка или индекс производительности); null — нет данных. */
  value: number | null;
  /** Вес точки (часы версии). */
  weight: number;
}

export interface TrendVersionRow {
  projectCode: string;
  projectName: string;
  versionName: string;
  date: string;
  value: number | null;
}

export interface PeriodKpi {
  current: number | null;
  initial: number | null;
  delta: number | null;
}

/** Ключ месяца вида «2026-08» → ISO первого числа месяца. */
function monthKey(isoDate: string): string | null {
  const d = new Date(isoDate);
  if (!Number.isFinite(d.getTime())) return null;
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  return `${y}-${m}`;
}

/**
 * Схлопывает точки версий в помесячные бакеты: взвешенное среднее значения по весам.
 * Точки без значения не участвуют; бакет без валидных точек получает value=null.
 * Порядок — хронологический.
 */
export function bucketPointsByMonth(points: TrendPointLike[]): DynamicsPoint[] {
  const buckets = new Map<
    string,
    { weightedSum: number; weightSum: number; valueSum: number; count: number }
  >();
  for (const p of points) {
    const key = monthKey(p.date);
    if (!key || p.value === null) continue;
    const w = p.weight > 0 ? p.weight : 0;
    const bucket = buckets.get(key) ?? { weightedSum: 0, weightSum: 0, valueSum: 0, count: 0 };
    bucket.weightedSum += p.value * w;
    bucket.weightSum += w;
    bucket.valueSum += p.value;
    bucket.count += 1;
    buckets.set(key, bucket);
  }

  return [...buckets.entries()]
    .sort(([a], [b]) => (a < b ? -1 : 1))
    .map(([key, b]) => ({
      date: `${key}-01`,
      // Нулевой суммарный вес (все часы 0) — арифметическое среднее по точкам бакета.
      value: b.weightSum > 0 ? b.weightedSum / b.weightSum : b.valueSum / b.count,
    }));
}

/** KPI периода по помесячным бакетам: текущее/стартовое значение и изменение. */
export function computePeriodKpi(buckets: DynamicsPoint[]): PeriodKpi {
  const valid = buckets.filter((b) => b.value !== null);
  if (valid.length === 0) return { current: null, initial: null, delta: null };
  const initial = valid[0].value;
  const current = valid[valid.length - 1].value;
  return {
    current,
    initial,
    delta: current !== null && initial !== null ? current - initial : null,
  };
}

export interface GroupTrendLike {
  groupId: number;
  groupName: string;
  points: TrendPointLike[];
}

/**
 * Вклад направлений в изменение общего показателя за период (для водопада).
 * contribution_g = Δгруппы × доля веса группы; «Прочие направления» поглощают
 * остаток до фактического общего Δ (включая дрейф весов между периодами).
 * Возвращает топ-3 по модулю + «Прочие», либо null если данных недостаточно.
 */
export function buildWaterfallSteps(
  groups: GroupTrendLike[],
  overallDelta: number | null,
  topCount = 3,
): WaterfallStep[] | null {
  if (overallDelta === null || groups.length === 0) return null;

  const totalWeight = groups.reduce(
    (sum, g) => sum + g.points.reduce((s, p) => s + (p.weight > 0 ? p.weight : 0), 0),
    0,
  );
  if (totalWeight <= 0) return null;

  const contributions: { name: string; value: number }[] = [];
  for (const g of groups) {
    const kpi = computePeriodKpi(bucketPointsByMonth(g.points));
    if (kpi.delta === null) continue;
    const weight = g.points.reduce((s, p) => s + (p.weight > 0 ? p.weight : 0), 0);
    contributions.push({
      name: g.groupName,
      value: kpi.delta * (weight / totalWeight),
    });
  }
  if (contributions.length === 0) return null;

  contributions.sort((a, b) => Math.abs(b.value) - Math.abs(a.value));
  const top = contributions.slice(0, topCount);
  const topSum = top.reduce((s, c) => s + c.value, 0);
  const rest = overallDelta - topSum;

  const steps: WaterfallStep[] = top.map((c) => ({ name: c.name, value: c.value }));
  if (contributions.length > topCount || Math.abs(rest) > 1e-9) {
    steps.push({ name: 'Прочие направления', value: rest });
  }
  return steps;
}
