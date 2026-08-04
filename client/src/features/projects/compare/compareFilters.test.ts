import { describe, expect, it } from 'vitest';
import {
  COMPARE_PRESENCE_BOTH,
  filterCompareDetailRows,
  filterCompareFacilityRows,
  filterCompareSummaryRows,
  hasDetailDifference,
  hasFacilityDifference,
  hasSummaryDifference,
} from './compareFilters';
import type {
  VersionCompareDetailRow,
  VersionCompareFacilityRow,
  VersionCompareRow,
} from '../../../api/analyticsApi';

function summaryBase(overrides: Partial<VersionCompareRow> = {}): VersionCompareRow {
  return {
    workDirectionId: 1,
    parentId: null,
    name: 'Направление',
    factZScoreA: 0,
    factZScoreB: 0,
    factZDelta: 0,
    interpretationA: '',
    interpretationB: '',
    plannedLaborHoursA: 100,
    plannedLaborHoursB: 100,
    plannedLaborHoursOptimisticA: 80,
    plannedLaborHoursOptimisticB: 70,
    plannedLaborHoursPessimisticA: 120,
    plannedLaborHoursPessimisticB: 120,
    planZScoreA: 0.2,
    planZScoreB: 0.2,
    ...overrides,
  };
}

describe('compareFilters', () => {
  it('hasSummaryDifference hides when FactZ, PlanZ and base plan labor match', () => {
    expect(hasSummaryDifference(summaryBase(), 'base')).toBe(false);
  });

  it('hasSummaryDifference uses active scenario for planned labor', () => {
    const row = summaryBase();
    expect(hasSummaryDifference(row, 'base')).toBe(false);
    expect(hasSummaryDifference(row, 'optimistic')).toBe(true);
  });

  it('hasSummaryDifference shows when plan Z differs', () => {
    expect(hasSummaryDifference(summaryBase({ planZScoreB: -0.1 }), 'base')).toBe(true);
  });

  it('hasFacilityDifference hides when both fact hours are null', () => {
    const row: VersionCompareFacilityRow = {
      facilityCode: 'F1',
      facilityName: 'Obj',
      factPhysicalVolumeA: 10,
      factPhysicalVolumeB: 20,
      plannedLaborHoursA: 100,
      plannedLaborHoursB: 50,
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
      zScoreA: 1,
      zScoreB: 2,
      zDelta: 1,
      interpretationA: '',
      interpretationB: '',
    };
    expect(hasFacilityDifference(row)).toBe(false);
  });

  it('hasFacilityDifference shows when Z differs and hours present', () => {
    const row = {
      factLaborHoursA: 10,
      factLaborHoursB: 10,
      zScoreA: 0.1,
      zScoreB: 0.2,
    } as VersionCompareFacilityRow;
    expect(hasFacilityDifference(row)).toBe(true);
  });

  it('hasDetailDifference treats A+B identical fact metrics as no difference', () => {
    const row: VersionCompareDetailRow = {
      facilityCode: 'A',
      facilityName: 'Obj',
      documentSet: 'DS',
      pvCode: 'PV1',
      pvName: 'Name',
      workId: 'W1',
      workName: 'Work',
      quantityA: 10,
      quantityB: 10,
      quantityDelta: 0,
      plannedLaborHoursA: 120,
      plannedLaborHoursB: 999,
      plannedLaborHoursOptimisticA: 100,
      plannedLaborHoursOptimisticB: 1,
      plannedLaborHoursPessimisticA: 140,
      plannedLaborHoursPessimisticB: 1,
      factLaborHoursA: 100,
      factLaborHoursB: 100,
      hoursDelta: 0,
      plannedProductivityA: 1.25,
      plannedProductivityB: 9,
      plannedProductivityOptimisticA: 1.5,
      plannedProductivityOptimisticB: 9,
      plannedProductivityPessimisticA: 1.1,
      plannedProductivityPessimisticB: 9,
      factProductivityA: 1.5,
      factProductivityB: 1.5,
      productivityDelta: 0,
      zScoreA: 0.5,
      zScoreB: 9,
      zDelta: 8.5,
      iicGroupName: 'G',
      iicUnitName: 'u',
      presence: COMPARE_PRESENCE_BOTH,
      bindingType: '',
      catalogId: '',
      tag: '',
      itemName: '',
      workComment: '',
      estimationBasis: '',
      isAssessment: '',
      coefficient: null,
      normProductivity: null,
      normStdDev: null,
      isEquipment: false,
    };
    expect(hasDetailDifference(row)).toBe(false);
  });

  it('hasDetailDifference shows only-A rows', () => {
    const row = { presence: 'только A' } as VersionCompareDetailRow;
    expect(hasDetailDifference(row)).toBe(true);
  });

  it('filterCompareSummaryRows keeps ancestors', () => {
    const rows: VersionCompareRow[] = [
      summaryBase({
        workDirectionId: 1,
        parentId: null,
        name: 'Root',
        factZScoreA: 0,
        factZScoreB: 0,
        planZScoreA: 0,
        planZScoreB: 0,
        plannedLaborHoursA: 1,
        plannedLaborHoursB: 1,
      }),
      summaryBase({
        workDirectionId: 2,
        parentId: 1,
        name: 'Child',
        factZScoreA: 0,
        factZScoreB: 1,
        planZScoreA: 0,
        planZScoreB: 0,
        plannedLaborHoursA: 1,
        plannedLaborHoursB: 1,
      }),
    ];
    const filtered = filterCompareSummaryRows(rows, true);
    expect(filtered.map((r) => r.workDirectionId).sort()).toEqual([1, 2]);
  });

  it('filterCompareSummaryRows hides all identical leaves', () => {
    const rows = [summaryBase({ workDirectionId: 1 }), summaryBase({ workDirectionId: 2, name: 'B' })];
    expect(filterCompareSummaryRows(rows, true, 'base')).toHaveLength(0);
  });

  it('filterCompareFacilityRows hides rows with both null fact hours', () => {
    const rows = [
      {
        facilityCode: 'F1',
        factLaborHoursA: null,
        factLaborHoursB: null,
        zScoreA: 1,
        zScoreB: 2,
      } as VersionCompareFacilityRow,
    ];
    expect(filterCompareFacilityRows(rows, true)).toHaveLength(0);
  });

  it('filterCompareDetailRows hides identical A+B fact rows even if plan differs', () => {
    const rows: VersionCompareDetailRow[] = [
      {
        facilityCode: 'A',
        facilityName: 'Obj',
        documentSet: '',
        pvCode: 'PV',
        pvName: 'N',
        workId: '',
        workName: '',
        quantityA: 1,
        quantityB: 1,
        quantityDelta: 0,
        plannedLaborHoursA: 1,
        plannedLaborHoursB: 999,
        plannedLaborHoursOptimisticA: 1,
        plannedLaborHoursOptimisticB: 1,
        plannedLaborHoursPessimisticA: 1,
        plannedLaborHoursPessimisticB: 1,
        factLaborHoursA: 1,
        factLaborHoursB: 1,
        hoursDelta: 0,
        plannedProductivityA: 1,
        plannedProductivityB: 9,
        plannedProductivityOptimisticA: 1,
        plannedProductivityOptimisticB: 1,
        plannedProductivityPessimisticA: 1,
        plannedProductivityPessimisticB: 1,
        factProductivityA: 1,
        factProductivityB: 1,
        productivityDelta: 0,
        zScoreA: 1,
        zScoreB: 9,
        zDelta: 8,
        iicGroupName: '',
        iicUnitName: '',
        presence: COMPARE_PRESENCE_BOTH,
        bindingType: '',
        catalogId: '',
        tag: '',
        itemName: '',
        workComment: '',
        estimationBasis: '',
        isAssessment: '',
        coefficient: null,
        normProductivity: null,
        normStdDev: null,
        isEquipment: false,
      },
    ];
    expect(filterCompareDetailRows(rows, true)).toHaveLength(0);
  });
});
