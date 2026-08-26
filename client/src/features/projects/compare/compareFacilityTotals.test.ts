import { describe, expect, it } from 'vitest';
import type { VersionCompareFacilityRow } from '../../../api/analyticsApi';
import { buildCompareFacilityPinnedTotal } from './compareFacilityTotals';

function facilityRow(
  overrides: Partial<VersionCompareFacilityRow> = {},
): VersionCompareFacilityRow {
  return {
    facilityCode: 'F1',
    facilityName: 'Объект',
    factPhysicalVolumeA: null,
    factPhysicalVolumeB: null,
    plannedLaborHoursA: null,
    plannedLaborHoursB: null,
    plannedLaborHoursOptimisticA: null,
    plannedLaborHoursOptimisticB: null,
    plannedLaborHoursPessimisticA: null,
    plannedLaborHoursPessimisticB: null,
    factLaborHoursA: null,
    factLaborHoursB: null,
    hoursDelta: null,
    plannedProductivityA: null,
    plannedProductivityB: null,
    plannedProductivityOptimisticA: null,
    plannedProductivityOptimisticB: null,
    plannedProductivityPessimisticA: null,
    plannedProductivityPessimisticB: null,
    factProductivityA: null,
    factProductivityB: null,
    zScoreA: null,
    zScoreB: null,
    zDelta: null,
    interpretationA: '',
    interpretationB: '',
    ...overrides,
  };
}

describe('buildCompareFacilityPinnedTotal', () => {
  it('returns empty when there are no facility rows', () => {
    expect(buildCompareFacilityPinnedTotal([])).toEqual([]);
  });

  it('sums plan hours and fact hours into an ИТОГО row', () => {
    const rows = [
      facilityRow({
        facilityCode: 'A',
        plannedLaborHoursPessimisticA: 12,
        plannedLaborHoursA: 10,
        plannedLaborHoursOptimisticA: 8,
        plannedLaborHoursPessimisticB: 22,
        plannedLaborHoursB: 20,
        plannedLaborHoursOptimisticB: 18,
        factLaborHoursA: 5,
        factLaborHoursB: 7,
        hoursDelta: 2,
      }),
      facilityRow({
        facilityCode: 'B',
        plannedLaborHoursPessimisticA: 3,
        plannedLaborHoursA: 4,
        plannedLaborHoursOptimisticA: 1,
        plannedLaborHoursPessimisticB: 6,
        plannedLaborHoursB: 5,
        plannedLaborHoursOptimisticB: 2,
        factLaborHoursA: 1,
        factLaborHoursB: 3,
        hoursDelta: 2,
      }),
    ];

    const [total] = buildCompareFacilityPinnedTotal(rows);
    expect(total.facilityName).toBe('ИТОГО');
    expect(total.plannedLaborHoursPessimisticA).toBe(15);
    expect(total.plannedLaborHoursA).toBe(14);
    expect(total.plannedLaborHoursOptimisticA).toBe(9);
    expect(total.plannedLaborHoursPessimisticB).toBe(28);
    expect(total.plannedLaborHoursB).toBe(25);
    expect(total.plannedLaborHoursOptimisticB).toBe(20);
    expect(total.factLaborHoursA).toBe(6);
    expect(total.factLaborHoursB).toBe(10);
    expect(total.hoursDelta).toBe(4);
    expect(total.zScoreA).toBeNull();
  });
});
