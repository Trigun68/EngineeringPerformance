import { describe, expect, it } from 'vitest';
import {
  collectSubtreeDirectionIds,
  collectSubtreeVolumes,
  sumSubtreeHours,
  sumVolumeTotals,
  volumesForDirection,
} from '../forecastReportSubtree';

const parent = {
  workDirectionId: 10,
  parentId: null,
  baseHours: 0,
  optimisticHours: 0,
  pessimisticHours: 0,
};
const leaf = {
  workDirectionId: 11,
  parentId: 10,
  baseHours: 50,
  optimisticHours: 40,
  pessimisticHours: 60,
};

describe('forecastReportSubtree', () => {
  it('collects parent and descendants', () => {
    expect(collectSubtreeDirectionIds([parent, leaf], 10)).toEqual([10, 11]);
    expect(collectSubtreeDirectionIds([parent, leaf], 11)).toEqual([11]);
  });

  it('sums stored hours including a parent with own zeros', () => {
    const ids = collectSubtreeDirectionIds([parent, leaf], 10);
    expect(sumSubtreeHours([parent, leaf], ids)).toEqual({
      baseHours: 50,
      optimisticHours: 40,
      pessimisticHours: 60,
    });
  });

  it('concatenates assessment volumes of the subtree', () => {
    const rows = collectSubtreeVolumes(
      {
        '11': [{ code: 'K1', name: 'Позиция', unitName: 'м3', quantity: 4, coefficient: 2, total: 8 }],
      },
      [10, 11],
    );
    expect(rows).toHaveLength(1);
    expect(sumVolumeTotals(rows)).toBe(8);
  });

  it('returns only the volumes of the requested direction', () => {
    const byDirection = {
      '10': [{ code: 'P1', name: 'Родитель', unitName: 'м3', quantity: 1, coefficient: 1, total: 1 }],
      '11': [{ code: 'K1', name: 'Позиция', unitName: 'м3', quantity: 4, coefficient: 2, total: 8 }],
    };
    expect(volumesForDirection(byDirection, 10)).toEqual(byDirection['10']);
    expect(volumesForDirection(byDirection, 11)).toEqual(byDirection['11']);
    expect(volumesForDirection(byDirection, 99)).toEqual([]);
  });
});
