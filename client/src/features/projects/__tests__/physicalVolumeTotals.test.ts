import { describe, expect, it } from 'vitest';
import { formatPhysicalVolumeCell } from '../gridColumns';
import { buildPerformanceTreeRows } from '../fact/performanceTree';
import type { PerformanceRow } from '../../../api/analyticsApi';

describe('formatPhysicalVolumeCell', () => {
  it('prefers display text over numeric volume', () => {
    expect(formatPhysicalVolumeCell('150 (м²); 7 (шт.)', 999)).toBe('150 (м²); 7 (шт.)');
  });

  it('formats numeric volume when display is empty', () => {
    expect(formatPhysicalVolumeCell(null, 12.4, 0)).toMatch(/12/);
  });
});

describe('buildPerformanceTreeRows', () => {
  it('keeps total rows as top-level hierarchy entries', () => {
    const rows: PerformanceRow[] = [
      {
        workDirectionId: 1,
        workDirectionName: 'Root',
        workDirectionCode: 'R',
        parentId: null,
        level: 0,
        normUnitName: 'м²',
        factPhysicalVolume: 10,
        factLaborHours: 5,
        factProductivity: null,
        factEstimate: null,
        normProductivity: null,
        normEstimate: null,
        normStdDev: null,
        zScore: null,
        interpretation: null,
        subtreeHours: null,
        isTotal: false,
      },
      {
        workDirectionId: 1,
        workDirectionName: 'Итого: Root',
        workDirectionCode: 'R',
        parentId: null,
        level: 0,
        normUnitName: null,
        factPhysicalVolume: null,
        factLaborHours: 5,
        factProductivity: null,
        factEstimate: null,
        normProductivity: null,
        normEstimate: null,
        normStdDev: null,
        zScore: null,
        interpretation: null,
        subtreeHours: null,
        isTotal: true,
        factPhysicalVolumeDisplay: '10 (м²)',
      },
    ];

    const tree = buildPerformanceTreeRows(rows);
    expect(tree).toHaveLength(2);
    expect(tree[0].hierarchy).toEqual(['Root']);
    expect(tree[1].hierarchy).toEqual(['Итого: Root']);
    expect(tree[1].factPhysicalVolumeDisplay).toBe('10 (м²)');
  });
});
