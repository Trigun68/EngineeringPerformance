import { describe, expect, it } from 'vitest';
import {
  COMPARE_PRESENCE_BOTH,
  filterCompareDetailRows,
  filterCompareSummaryRows,
  hasDetailDifference,
} from './compareFilters';
import type { VersionCompareDetailRow, VersionCompareRow } from '../../../api/analyticsApi';

describe('compareFilters', () => {
  it('hasDetailDifference treats A+B identical rows as no difference', () => {
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
      factLaborHoursA: 100,
      factLaborHoursB: 100,
      hoursDelta: 0,
      factProductivityA: 1.5,
      factProductivityB: 1.5,
      productivityDelta: 0,
      iicGroupName: 'G',
      iicUnitName: 'u',
      presence: COMPARE_PRESENCE_BOTH,
    };
    expect(hasDetailDifference(row)).toBe(false);
  });

  it('hasDetailDifference shows only-A rows', () => {
    const row = { presence: 'только A' } as VersionCompareDetailRow;
    expect(hasDetailDifference(row)).toBe(true);
  });

  it('filterCompareSummaryRows keeps ancestors', () => {
    const rows: VersionCompareRow[] = [
      { workDirectionId: 1, parentId: null, name: 'Root', zScoreA: 0, zScoreB: 0, zDelta: 0, interpretationA: 'A', interpretationB: 'A' },
      { workDirectionId: 2, parentId: 1, name: 'Child', zScoreA: 0, zScoreB: 1, zDelta: 1, interpretationA: 'A', interpretationB: 'B' },
    ];
    const filtered = filterCompareSummaryRows(rows, true);
    expect(filtered.map((r: VersionCompareRow) => r.workDirectionId).sort()).toEqual([1, 2]);
  });

  it('filterCompareDetailRows hides identical A+B rows', () => {
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
        factLaborHoursA: 1,
        factLaborHoursB: 1,
        hoursDelta: 0,
        factProductivityA: 1,
        factProductivityB: 1,
        productivityDelta: 0,
        iicGroupName: '',
        iicUnitName: '',
        presence: COMPARE_PRESENCE_BOTH,
      },
    ];
    expect(filterCompareDetailRows(rows, true)).toHaveLength(0);
  });
});
