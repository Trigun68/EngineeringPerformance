import { describe, expect, it } from 'vitest';
import type { CompareScenarioMetrics } from './compareColumns';
import {
  compareTabPlannedHoursDelta,
  plannedLaborHoursDelta,
  summaryTotalPlannedLaborHoursDelta,
  sumPlannedLaborHoursDelta,
} from './compareColumns';

function metrics(overrides: Partial<CompareScenarioMetrics> = {}): CompareScenarioMetrics {
  return {
    plannedLaborHoursA: 100,
    plannedLaborHoursB: 130,
    plannedLaborHoursOptimisticA: 80,
    plannedLaborHoursOptimisticB: 90,
    plannedLaborHoursPessimisticA: 120,
    plannedLaborHoursPessimisticB: 160,
    ...overrides,
  };
}

describe('plannedLaborHoursDelta', () => {
  it('subtracts A from B for each scenario', () => {
    const row = metrics();
    expect(plannedLaborHoursDelta(row, 'base')).toBe(30);
    expect(plannedLaborHoursDelta(row, 'optimistic')).toBe(10);
    expect(plannedLaborHoursDelta(row, 'pessimistic')).toBe(40);
  });

  it('treats null hours as zero', () => {
    expect(plannedLaborHoursDelta(metrics({
      plannedLaborHoursA: null,
      plannedLaborHoursB: 12,
    }), 'base')).toBe(12);
    expect(plannedLaborHoursDelta(metrics({
      plannedLaborHoursA: 8,
      plannedLaborHoursB: undefined,
    }), 'base')).toBe(-8);
  });
});

describe('summaryTotalPlannedLaborHoursDelta', () => {
  it('uses only the ИТОГО row (level -1), not subtree Итого', () => {
    const rows = [
      { ...metrics({ plannedLaborHoursA: 10, plannedLaborHoursB: 40 }), isTotal: false },
      {
        ...metrics({ plannedLaborHoursA: 10, plannedLaborHoursB: 40 }),
        isTotal: true,
        level: 0,
        name: 'Итого: корень',
      },
      {
        ...metrics({ plannedLaborHoursA: 200, plannedLaborHoursB: 250 }),
        isTotal: true,
        level: -1,
        name: 'ИТОГО',
      },
    ];
    expect(summaryTotalPlannedLaborHoursDelta(rows, 'base')).toBe(50);
  });

  it('returns null when the grand total row is missing', () => {
    expect(summaryTotalPlannedLaborHoursDelta([
      { ...metrics(), isTotal: true, level: 0 },
    ], 'base')).toBeNull();
  });
});

describe('compareTabPlannedHoursDelta', () => {
  const data = {
    summary: [
      { ...metrics({ plannedLaborHoursA: 10, plannedLaborHoursB: 15 }), isTotal: false },
      {
        ...metrics({ plannedLaborHoursA: 1000, plannedLaborHoursB: 1100 }),
        isTotal: true,
        level: -1,
      },
    ],
    facility: [
      metrics({ plannedLaborHoursA: 10, plannedLaborHoursB: 12 }),
      metrics({ plannedLaborHoursA: 20, plannedLaborHoursB: 25 }),
    ],
    detail: [
      metrics({ plannedLaborHoursA: 1, plannedLaborHoursB: 4 }),
      metrics({
        plannedLaborHoursA: 2,
        plannedLaborHoursB: 3,
        plannedLaborHoursOptimisticA: 1,
        plannedLaborHoursOptimisticB: 8,
      }),
    ],
  };

  it('takes ИТОГО on summary and sums facility/detail rows', () => {
    expect(compareTabPlannedHoursDelta('compare-total', data, 'base')).toBe(100);
    expect(compareTabPlannedHoursDelta('compare-facility', data, 'base')).toBe(7);
    expect(compareTabPlannedHoursDelta('compare-detail', data, 'base')).toBe(4);
  });

  it('uses the selected scenario hours', () => {
    expect(compareTabPlannedHoursDelta('compare-facility', data, 'optimistic')).toBe(
      sumPlannedLaborHoursDelta(data.facility, 'optimistic'),
    );
    expect(compareTabPlannedHoursDelta('compare-detail', data, 'optimistic')).toBe(17);
  });

  it('hides the chip on Детально until rows are loaded', () => {
    expect(compareTabPlannedHoursDelta('compare-detail', { ...data, detail: [] }, 'base')).toBeNull();
  });

  it('returns null without compare data', () => {
    expect(compareTabPlannedHoursDelta('compare-total', null, 'base')).toBeNull();
  });
});
