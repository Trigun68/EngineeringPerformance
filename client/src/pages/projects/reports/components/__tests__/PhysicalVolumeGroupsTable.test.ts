import { describe, expect, it } from 'vitest';
import type { PerformanceTreeRow } from '../../reportCalc';
import { mapPhysicalVolumeGroupsRow } from '../PhysicalVolumeGroupsTable';

function row(partial: Partial<PerformanceTreeRow> = {}): PerformanceTreeRow {
  return {
    workDirectionId: 1,
    workDirectionName: 'Автоматизация',
    workDirectionCode: 'IIC-5',
    parentId: null,
    level: 0,
    normUnitName: 'ед.',
    factPhysicalVolume: null,
    factLaborHours: 100,
    factProductivity: 10,
    factEstimate: null,
    normProductivity: 8,
    normEstimate: null,
    normStdDev: 1,
    zScore: 1.2,
    interpretation: null,
    subtreeHours: 100,
    subtreePhysicalVolume: 50,
    physicalVolume: 10,
    assessmentPhysicalVolume: 15,
    children: [],
    ...partial,
  };
}

describe('mapPhysicalVolumeGroupsRow', () => {
  it('uses assessmentPhysicalVolume, not subtree reconstruction', () => {
    const mapped = mapPhysicalVolumeGroupsRow(row());
    expect(mapped.physicalVolume).toBe(15);
  });

  it('dashes empty grouped volume', () => {
    expect(mapPhysicalVolumeGroupsRow(row({ assessmentPhysicalVolume: null })).physicalVolume).toBeNull();
    expect(mapPhysicalVolumeGroupsRow(row({ assessmentPhysicalVolume: 0 })).physicalVolume).toBeNull();
  });
});
