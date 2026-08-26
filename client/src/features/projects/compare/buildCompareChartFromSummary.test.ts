import { describe, expect, it } from 'vitest';
import type { VersionCompareResult } from '../../../api/analyticsApi';
import { buildCompareChartFromSummary } from './buildCompareChartFromSummary';

function emptyCompare(overrides: Partial<VersionCompareResult> = {}): VersionCompareResult {
  return { summary: [], facility: [], detail: [], ...overrides };
}

describe('buildCompareChartFromSummary', () => {
  it('берёт top по |ΔZ| и пропускает итого', () => {
    const compare = emptyCompare({
      summary: [
        {
          workDirectionId: 0,
          parentId: null,
          name: 'Итого',
          factZScoreA: 1,
          factZScoreB: 2,
          factZDelta: 1,
          interpretationA: '-',
          interpretationB: '-',
          isTotal: true,
        },
        {
          workDirectionId: 10,
          parentId: null,
          name: 'A',
          factZScoreA: 0.1,
          factZScoreB: 0.2,
          factZDelta: 0.1,
          interpretationA: '-',
          interpretationB: '-',
        },
        {
          workDirectionId: 11,
          parentId: null,
          name: 'B',
          factZScoreA: -1,
          factZScoreB: 2,
          factZDelta: 3,
          interpretationA: '-',
          interpretationB: '-',
        },
      ],
    });

    const chart = buildCompareChartFromSummary(compare, 'vA', 'vB', 'iic', 'zScore', 1);

    expect(chart.versionAName).toBe('vA');
    expect(chart.points).toHaveLength(1);
    expect(chart.points[0]?.category).toBe('B');
    expect(chart.points[0]?.valueA).toBe(-1);
    expect(chart.points[0]?.delta).toBe(3);
  });

  it('для объектов и hours берёт факт. часы', () => {
    const compare = emptyCompare({
      facility: [
        {
          facilityCode: '1',
          facilityName: 'Объект',
          factPhysicalVolumeA: null,
          factPhysicalVolumeB: null,
          plannedLaborHoursA: null,
          plannedLaborHoursB: null,
          plannedLaborHoursOptimisticA: null,
          plannedLaborHoursOptimisticB: null,
          plannedLaborHoursPessimisticA: null,
          plannedLaborHoursPessimisticB: null,
          factLaborHoursA: 10,
          factLaborHoursB: 4,
          hoursDelta: -6,
          plannedProductivityA: null,
          plannedProductivityB: null,
          plannedProductivityOptimisticA: null,
          plannedProductivityOptimisticB: null,
          plannedProductivityPessimisticA: null,
          plannedProductivityPessimisticB: null,
          factProductivityA: null,
          factProductivityB: null,
          zScoreA: 1,
          zScoreB: 2,
          zDelta: 1,
          interpretationA: '',
          interpretationB: '',
        },
      ],
    });

    const chart = buildCompareChartFromSummary(compare, 'A', 'B', 'facility', 'hours', 20);

    expect(chart.level).toBe('facility');
    expect(chart.points).toHaveLength(1);
    expect(chart.points[0]?.valueA).toBe(10);
    expect(chart.points[0]?.valueB).toBe(4);
    expect(chart.points[0]?.delta).toBe(-6);
  });
});
