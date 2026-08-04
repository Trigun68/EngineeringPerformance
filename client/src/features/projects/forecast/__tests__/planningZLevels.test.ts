import { describe, expect, it } from 'vitest';
import type { ProjectVersionPlanningParameter } from '../../../../api/planningParametersApi';
import {
  cascadeZ,
  filterVisibleRows,
  leafDirectionIds,
  projectRollup,
  weightedFactZ,
  Z_PARAMETER_LEVEL,
  weightedAvgZ,
} from '../planningZLevels';

function row(
  partial: Partial<ProjectVersionPlanningParameter> & {
    iicWorkDirectionId: number;
    parentId: number | null;
  },
): ProjectVersionPlanningParameter {
  return {
    id: partial.iicWorkDirectionId,
    iicWorkDirectionId: partial.iicWorkDirectionId,
    parentId: partial.parentId,
    sortOrder: partial.sortOrder ?? partial.iicWorkDirectionId,
    code: partial.code ?? `C${partial.iicWorkDirectionId}`,
    name: partial.name ?? `N${partial.iicWorkDirectionId}`,
    unitName: null,
    zPessimistic: partial.zPessimistic ?? 0.5,
    zBase: partial.zBase ?? 0,
    zOptimistic: partial.zOptimistic ?? -0.5,
    zActual: null,
    laborHoursWeight: partial.laborHoursWeight ?? 0,
  };
}

describe('planningZLevels', () => {
  const tree = [
    row({ iicWorkDirectionId: 1, parentId: null, zBase: 0, laborHoursWeight: 0 }),
    row({ iicWorkDirectionId: 2, parentId: 1, zBase: 0, laborHoursWeight: 0 }),
    row({ iicWorkDirectionId: 3, parentId: 2, zBase: 1, laborHoursWeight: 10 }),
    row({ iicWorkDirectionId: 4, parentId: 2, zBase: 3, laborHoursWeight: 30 }),
  ];

  it('cascadeZ from project updates all directions', () => {
    const next = cascadeZ(tree, 'project', { zPessimistic: 0.1, zBase: 0, zOptimistic: -0.1 });
    expect(next.every((r) => r.zBase === 0 && r.zPessimistic === 0.1)).toBe(true);
  });

  it('cascadeZ from level-1 node updates descendants including self', () => {
    const next = cascadeZ(tree, 1, { zPessimistic: 0.2, zBase: 2, zOptimistic: -0.2 });
    expect(next.find((r) => r.iicWorkDirectionId === 1)?.zBase).toBe(2);
    expect(next.find((r) => r.iicWorkDirectionId === 3)?.zBase).toBe(2);
    expect(next.find((r) => r.iicWorkDirectionId === 4)?.zBase).toBe(2);
  });

  it('filterVisibleRows respects Z level', () => {
    const withDepth = [
      { ...tree[0], depth: 0, hasChildren: true },
      { ...tree[1], depth: 1, hasChildren: true },
      { ...tree[2], depth: 2, hasChildren: false },
    ];
    expect(filterVisibleRows(withDepth, Z_PARAMETER_LEVEL.ByProject)).toHaveLength(0);
    expect(filterVisibleRows(withDepth, Z_PARAMETER_LEVEL.Level1).map((r) => r.depth)).toEqual([0]);
    expect(filterVisibleRows(withDepth, Z_PARAMETER_LEVEL.Level2).map((r) => r.depth)).toEqual([0, 1]);
    expect(filterVisibleRows(withDepth, Z_PARAMETER_LEVEL.EachElement)).toHaveLength(3);
  });

  it('weightedAvgZ uses labor hours weights on leaves', () => {
    // leaves 3 and 4: (1*10 + 3*30) / 40 = 2.5
    expect(weightedAvgZ(tree, (r) => r.zBase)).toBeCloseTo(2.5);
    expect(projectRollup(tree).zBase).toBeCloseTo(2.5);
  });

  it('leafDirectionIds skips parents', () => {
    expect(leafDirectionIds(tree).sort()).toEqual([3, 4]);
  });

  it('weightedAvgZ falls back to arithmetic mean when weights are zero', () => {
    const noWeights = tree.map((r) => ({ ...r, laborHoursWeight: 0 }));
    expect(weightedAvgZ(noWeights, (r) => r.zBase)).toBeCloseTo(2); // (1+3)/2
  });

  it('weightedFactZ uses fact hours and returns null without Z', () => {
    const zMap = new Map<number, number | null>([
      [3, 1],
      [4, 3],
    ]);
    const hours = new Map<number, number | null>([
      [3, 10],
      [4, 30],
    ]);
    expect(weightedFactZ(tree, zMap, hours)).toBeCloseTo(2.5);
    expect(weightedFactZ(tree, new Map())).toBeNull();
  });
});
