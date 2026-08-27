import { describe, expect, it } from 'vitest';
import {
  bucketPointsByMonth,
  buildWaterfallSteps,
  computePeriodKpi,
  type GroupTrendLike,
  type TrendPointLike,
} from '../trendDetailCalc';

describe('bucketPointsByMonth', () => {
  it('groups points into monthly buckets with weighted average', () => {
    const points: TrendPointLike[] = [
      { date: '2026-05-10', value: 1.0, weight: 50 },
      { date: '2026-05-20', value: -0.5, weight: 30 },
      { date: '2026-06-05', value: 2.0, weight: 20 },
    ];

    const buckets = bucketPointsByMonth(points);

    expect(buckets).toHaveLength(2);
    expect(buckets[0].date).toBe('2026-05-01');
    // (1.0*50 + (-0.5)*30) / 80 = 0.4375
    expect(buckets[0].value).toBeCloseTo(0.4375, 4);
    expect(buckets[1].date).toBe('2026-06-01');
    expect(buckets[1].value).toBeCloseTo(2.0, 4);
  });

  it('skips null values and invalid dates', () => {
    const points: TrendPointLike[] = [
      { date: '2026-05-10', value: null, weight: 50 },
      { date: 'not-a-date', value: 1.0, weight: 10 },
      { date: '2026-05-15', value: 0.5, weight: 10 },
    ];

    const buckets = bucketPointsByMonth(points);
    expect(buckets).toHaveLength(1);
    expect(buckets[0].value).toBeCloseTo(0.5, 4);
  });

  it('falls back to arithmetic mean when all weights are zero', () => {
    const points: TrendPointLike[] = [
      { date: '2026-05-10', value: 1.0, weight: 0 },
      { date: '2026-05-20', value: 3.0, weight: 0 },
    ];

    const buckets = bucketPointsByMonth(points);
    expect(buckets[0].value).toBeCloseTo(2.0, 4);
  });

  it('returns empty array for empty input', () => {
    expect(bucketPointsByMonth([])).toEqual([]);
  });

  it('sorts buckets chronologically regardless of input order', () => {
    const points: TrendPointLike[] = [
      { date: '2026-08-01', value: 3, weight: 1 },
      { date: '2026-05-01', value: 1, weight: 1 },
      { date: '2026-06-01', value: 2, weight: 1 },
    ];

    const buckets = bucketPointsByMonth(points);
    expect(buckets.map((b) => b.date)).toEqual(['2026-05-01', '2026-06-01', '2026-08-01']);
  });
});

describe('computePeriodKpi', () => {
  it('returns first/last valid bucket and their delta', () => {
    const buckets = bucketPointsByMonth([
      { date: '2026-05-01', value: -0.42, weight: 10 },
      { date: '2026-06-01', value: -0.31, weight: 10 },
      { date: '2026-08-01', value: 0.05, weight: 10 },
    ]);

    const kpi = computePeriodKpi(buckets);
    expect(kpi.initial).toBeCloseTo(-0.42, 4);
    expect(kpi.current).toBeCloseTo(0.05, 4);
    expect(kpi.delta).toBeCloseTo(0.47, 4);
  });

  it('returns nulls when no valid buckets', () => {
    expect(computePeriodKpi([])).toEqual({ current: null, initial: null, delta: null });
    expect(computePeriodKpi([{ date: '2026-05-01', value: null }])).toEqual({
      current: null,
      initial: null,
      delta: null,
    });
  });
});

describe('buildWaterfallSteps', () => {
  const groups: GroupTrendLike[] = [
    {
      groupId: 1,
      groupName: 'Металлоконструкции',
      points: [
        { date: '2026-05-01', value: 0.0, weight: 60 },
        { date: '2026-08-01', value: 0.5, weight: 60 },
      ],
    },
    {
      groupId: 2,
      groupName: 'Трубопроводы',
      points: [
        { date: '2026-05-01', value: 0.0, weight: 40 },
        { date: '2026-08-01', value: -0.25, weight: 40 },
      ],
    },
  ];

  it('weights group deltas by hour share and reconciles with overall delta', () => {
    // Δгрупп: +0.5 (доля 0.6) и −0.25 (доля 0.4) → вклады +0.3 и −0.1; общий Δ = +0.2.
    const steps = buildWaterfallSteps(groups, 0.2);

    expect(steps).not.toBeNull();
    expect(steps).toHaveLength(2);
    expect(steps![0].name).toBe('Металлоконструкции');
    expect(steps![0].value).toBeCloseTo(0.3, 4);
    expect(steps![1].name).toBe('Трубопроводы');
    expect(steps![1].value).toBeCloseTo(-0.1, 4);
    // Сумма вкладов сходится к общему Δ.
    expect(steps!.reduce((s, x) => s + x.value, 0)).toBeCloseTo(0.2, 4);
  });

  it('adds "Прочие направления" absorbing residual beyond top-3', () => {
    const many: GroupTrendLike[] = Array.from({ length: 5 }, (_, i) => ({
      groupId: i + 1,
      groupName: `Направление ${i + 1}`,
      points: [
        { date: '2026-05-01', value: 0, weight: 10 },
        { date: '2026-08-01', value: 0.1 * (i + 1), weight: 10 },
      ],
    }));
    // Вклады: 0.02/0.04/0.06/0.08/0.10; общий Δ = 0.3; топ-3 = 0.10+0.08+0.06=0.24 → прочие 0.06.
    const steps = buildWaterfallSteps(many, 0.3, 3);

    expect(steps).toHaveLength(4);
    expect(steps![3].name).toBe('Прочие направления');
    expect(steps![3].value).toBeCloseTo(0.06, 4);
    expect(steps!.reduce((s, x) => s + x.value, 0)).toBeCloseTo(0.3, 4);
  });

  it('returns null when overall delta or weights are missing', () => {
    expect(buildWaterfallSteps(groups, null)).toBeNull();
    expect(buildWaterfallSteps([], 0.2)).toBeNull();
    const zeroWeight: GroupTrendLike[] = [
      { groupId: 1, groupName: 'A', points: [{ date: '2026-05-01', value: 1, weight: 0 }] },
    ];
    expect(buildWaterfallSteps(zeroWeight, 0.2)).toBeNull();
  });
});
