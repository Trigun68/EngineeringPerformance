import { describe, expect, it } from 'vitest';
import {
  cascadeRate,
  collectCascadeTargetIds,
  filterVisibleRows,
  WORK_COST_LEVEL,
  parseRateInput,
} from '../workCostLevels';

const tree = [
  { id: 1, parentId: null, ratePerHour: 0 },
  { id: 2, parentId: 1, ratePerHour: 0 },
  { id: 3, parentId: 2, ratePerHour: 0 },
  { id: 4, parentId: 2, ratePerHour: 0 },
  { id: 5, parentId: null, ratePerHour: 0 },
];

describe('workCostLevels', () => {
  it('organization cascade covers every node', () => {
    const next = cascadeRate(tree, 'organization', 1200);
    expect(next.every((r) => r.ratePerHour === 1200)).toBe(true);
    expect(collectCascadeTargetIds(tree, 'organization').sort()).toEqual([1, 2, 3, 4, 5]);
  });

  it('level-1 cascade updates the subtree', () => {
    const next = cascadeRate(tree, 1, 50);
    expect(next.find((r) => r.id === 3)?.ratePerHour).toBe(50);
    expect(next.find((r) => r.id === 5)?.ratePerHour).toBe(0);
  });

  it('filterVisibleRows matches depth rules', () => {
    const withDepth = [
      { id: 1, depth: 0 },
      { id: 2, depth: 1 },
      { id: 3, depth: 2 },
    ];
    expect(filterVisibleRows(withDepth, WORK_COST_LEVEL.Organization)).toHaveLength(0);
    expect(filterVisibleRows(withDepth, WORK_COST_LEVEL.Level1).map((r) => r.depth)).toEqual([0]);
    expect(filterVisibleRows(withDepth, WORK_COST_LEVEL.Level2).map((r) => r.depth)).toEqual([0, 1]);
    expect(filterVisibleRows(withDepth, WORK_COST_LEVEL.EachElement)).toHaveLength(3);
  });

  it('parseRateInput accepts comma decimals', () => {
    expect(parseRateInput('1 234,5')).toBe(1234.5);
    expect(parseRateInput('')).toBeNull();
    expect(parseRateInput('-')).toBeNull();
  });
});
