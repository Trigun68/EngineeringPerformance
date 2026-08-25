import type { VersionCompareFacilityRow } from '../../../api/analyticsApi';

function sumField(
  rows: VersionCompareFacilityRow[],
  pick: (row: VersionCompareFacilityRow) => number | null | undefined,
): number {
  return rows.reduce((total, row) => total + (pick(row) ?? 0), 0);
}

function nullIfZero(value: number): number | null {
  return value > 0 ? value : null;
}

/** Pinned «ИТОГО» для сравнения «По объектам»: план (6 колонок) + факт A/B. */
export function buildCompareFacilityPinnedTotal(
  rows: VersionCompareFacilityRow[],
): VersionCompareFacilityRow[] {
  if (rows.length === 0) return [];

  const factA = sumField(rows, (r) => r.factLaborHoursA);
  const factB = sumField(rows, (r) => r.factLaborHoursB);
  const hoursDelta = rows.reduce((total, row) => total + (row.hoursDelta ?? 0), 0);

  return [{
    facilityCode: '',
    facilityName: 'ИТОГО',
    factPhysicalVolumeA: nullIfZero(sumField(rows, (r) => r.factPhysicalVolumeA)),
    factPhysicalVolumeB: nullIfZero(sumField(rows, (r) => r.factPhysicalVolumeB)),
    plannedLaborHoursPessimisticA: nullIfZero(sumField(rows, (r) => r.plannedLaborHoursPessimisticA)),
    plannedLaborHoursA: nullIfZero(sumField(rows, (r) => r.plannedLaborHoursA)),
    plannedLaborHoursOptimisticA: nullIfZero(sumField(rows, (r) => r.plannedLaborHoursOptimisticA)),
    plannedLaborHoursPessimisticB: nullIfZero(sumField(rows, (r) => r.plannedLaborHoursPessimisticB)),
    plannedLaborHoursB: nullIfZero(sumField(rows, (r) => r.plannedLaborHoursB)),
    plannedLaborHoursOptimisticB: nullIfZero(sumField(rows, (r) => r.plannedLaborHoursOptimisticB)),
    factLaborHoursA: nullIfZero(factA),
    factLaborHoursB: nullIfZero(factB),
    hoursDelta: factA > 0 && factB > 0 ? hoursDelta : null,
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
  }];
}
