import { describe, expect, it } from 'vitest';
import type { ProjectVersionDirection } from '../../../../api/analyticsApi';
import {
  factFoForCard,
  orderDirectionsForCards,
  ownLabor,
  ownVolumes,
  subtreeIdsForCard,
  sumFactCardHours,
} from '../factAnalysisTree';

function dir(
  partial: Partial<ProjectVersionDirection> & Pick<ProjectVersionDirection, 'id'>,
): ProjectVersionDirection {
  return {
    parentId: null,
    level: 0,
    code: String(partial.id),
    name: `N${partial.id}`,
    unitName: 'м³',
    factPhysicalVolume: null,
    factLaborHours: null,
    factProductivity: null,
    factEstimate: null,
    normProductivity: null,
    normEstimate: null,
    normStdDev: null,
    zScore: null,
    interpretation: null,
    physicalVolumes: [],
    equipmentVolumes: [],
    directionLaborIntensities: [],
    ...partial,
  };
}

describe('factAnalysisTree', () => {
  it('orders parent before descendants instead of grouping by level', () => {
    const rootA = dir({ id: 1, level: 0, parentId: null, code: 'A' });
    const childA = dir({ id: 11, level: 1, parentId: 1, code: 'A1' });
    const rootB = dir({ id: 2, level: 0, parentId: null, code: 'B' });
    expect(orderDirectionsForCards([rootA, rootB, childA]).map((d) => d.id)).toEqual([1, 11, 2]);
  });

  it('sums stored hours of listed ids without inventing parent own hours', () => {
    const parent = dir({
      id: 10,
      optimisticHours: 0,
      baseHours: 0,
      pessimisticHours: 0,
      factLaborHours: null,
    });
    const leaf = dir({
      id: 11,
      parentId: 10,
      level: 1,
      optimisticHours: 40,
      baseHours: 50,
      pessimisticHours: 60,
      factLaborHours: 12,
    });
    const cards = [parent, leaf];
    expect(sumFactCardHours(cards, [10])).toEqual({
      optimisticHours: 0,
      baseHours: 0,
      pessimisticHours: 0,
      factLaborHours: 0,
    });
    const allIds = subtreeIdsForCard(cards, 10);
    expect(sumFactCardHours(cards, allIds)).toEqual({
      optimisticHours: 40,
      baseHours: 50,
      pessimisticHours: 60,
      factLaborHours: 12,
    });
  });

  it('returns only own volumes and labor of the requested node', () => {
    const parent = dir({ id: 10, factPhysicalVolume: null });
    const leaf = dir({
      id: 11,
      parentId: 10,
      level: 1,
      physicalVolumes: [
        { code: 'K1', name: 'Позиция', unitName: 'м³', quantity: 4, coefficient: 2, total: 8 },
      ],
      directionLaborIntensities: [
        { documentationCode: 'DOC-1', plannedHours: 25, actualHours: 20, brand: 'АР' },
      ],
    });
    expect(ownVolumes(parent, 'physicalVolumes')).toEqual([]);
    expect(factFoForCard(parent, ownVolumes(parent, 'physicalVolumes'))).toBeNull();
    expect(ownVolumes(leaf, 'physicalVolumes')).toHaveLength(1);
    expect(factFoForCard(leaf, ownVolumes(leaf, 'physicalVolumes'))).toBe(8);
    expect(ownLabor(leaf)).toEqual([
      { documentationCode: 'DOC-1', plannedHours: 25, actualHours: 20, brand: 'АР' },
    ]);
    expect(ownLabor(parent)).toEqual([]);
  });

  it('uses own physicalVolume and does not fall back to remapped factPhysicalVolume', () => {
    const remappedOnly = dir({ id: 10, factPhysicalVolume: 99, physicalVolume: null });
    expect(factFoForCard(remappedOnly, [])).toBeNull();
    const own = dir({ id: 11, physicalVolume: 5, factPhysicalVolume: 99 });
    expect(factFoForCard(own, [])).toBe(5);
  });
});
