import { describe, expect, it } from 'vitest';
import type { NormListItem } from '../../../../api/normRegistryApi';
import {
  coefficientOfVariation,
  confidenceOf,
  filterNormsByKpi,
  isCvOutlier,
  medianNumber,
  cvHeatBackground,
  driftMeansForGroup,
  monthsSince,
  pctPointsInBand,
  previousNormInGroup,
  summarizeNormKpis,
} from '../normAnalysisMetrics';

function item(partial: Partial<NormListItem>): NormListItem {
  return {
    id: 1,
    groupId: 10,
    groupName: 'Группа',
    groupCode: 'A',
    formulaType: 'Linear',
    formulaDisplay: 'N=a',
    parameters: {},
    meanPerformance: 1,
    stdDeviation: 0.1,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    status: 'Active',
    totalVersions: 10,
    issueCount: 0,
    maxIssueSeverity: null,
    ...partial,
  };
}

describe('coefficientOfVariation / outliers', () => {
  it('is σ / μ', () => {
    expect(coefficientOfVariation(2, 0.5)).toBeCloseTo(0.25, 6);
    expect(coefficientOfVariation(0, 0.5)).toBeNull();
    expect(coefficientOfVariation(null, 0.5)).toBeNull();
  });

  it('flags CV ≥ 1.5 × median', () => {
    expect(medianNumber([0.1, 0.2, 0.3])).toBeCloseTo(0.2, 6);
    // 0.2 * 1.5 в IEEE даёт чуть больше 0.3 — проверяем явное превышение и явное непревышение.
    expect(isCvOutlier(0.31, 0.2)).toBe(true);
    expect(isCvOutlier(0.29, 0.2)).toBe(false);
  });
});

describe('confidenceOf', () => {
  it('is Low when σ is zero or few versions', () => {
    expect(confidenceOf(item({ stdDeviation: 0 }))).toBe('Low');
    expect(confidenceOf(item({ totalVersions: 2 }))).toBe('Low');
  });

  it('is Medium with issues or moderate usage', () => {
    expect(confidenceOf(item({ totalVersions: 5, issueCount: 0 }))).toBe('Medium');
    expect(confidenceOf(item({ totalVersions: 12, issueCount: 1 }))).toBe('Medium');
  });

  it('is High for well-used clean norms', () => {
    expect(confidenceOf(item({ totalVersions: 12, issueCount: 0 }))).toBe('High');
  });
});

describe('summarizeNormKpis / filter', () => {
  it('counts errors, CV outliers and ok rows', () => {
    const items = [
      item({ id: 1, maxIssueSeverity: 'Error', issueCount: 1, stdDeviation: 0.1, meanPerformance: 1 }),
      item({ id: 2, maxIssueSeverity: 'Warning', issueCount: 1, stdDeviation: 0.1, meanPerformance: 1 }),
      item({ id: 3, stdDeviation: 0.8, meanPerformance: 1, totalVersions: 12 }),
      item({ id: 4, totalVersions: 12 }),
    ];
    const summary = summarizeNormKpis(items);
    expect(summary.total).toBe(4);
    expect(summary.errors).toBe(1);
    expect(summary.warnings).toBe(1);
    expect(summary.ok).toBe(2);
    expect(summary.cvOutliers).toBeGreaterThanOrEqual(1);
    expect(filterNormsByKpi(items, 'Error', summary.medianCv)).toHaveLength(1);
  });
});

describe('pctPointsInBand', () => {
  it('returns share of points within mean ± σ', () => {
    expect(pctPointsInBand([1, 1.05, 2], 1, 0.1)).toBe(67);
    expect(pctPointsInBand([], 1, 0.1)).toBeNull();
  });
});

describe('registry presentation helpers', () => {
  it('heats CV cells relative to median', () => {
    expect(cvHeatBackground(0.2, 0.2)).toBe('transparent');
    expect(cvHeatBackground(0.31, 0.2)).not.toBe('transparent');
  });

  it('counts months since effectiveFrom', () => {
    expect(monthsSince('2025-01-15', new Date('2026-03-15'))).toBe(14);
  });

  it('picks previous revision of the same group', () => {
    const current = item({ id: 2, groupId: 10, effectiveFrom: '2026-01-01', meanPerformance: 1.2 });
    const prev = item({ id: 1, groupId: 10, effectiveFrom: '2025-01-01', meanPerformance: 1.0 });
    const other = item({ id: 3, groupId: 11, effectiveFrom: '2025-06-01', meanPerformance: 9 });
    expect(previousNormInGroup([prev, current, other], current)?.id).toBe(1);
  });

  it('builds drift series from group means in date order', () => {
    const a = item({ id: 1, groupId: 10, effectiveFrom: '2025-01-01', meanPerformance: 1.0 });
    const b = item({ id: 2, groupId: 10, effectiveFrom: '2025-06-01', meanPerformance: 1.1 });
    const c = item({ id: 3, groupId: 10, effectiveFrom: '2026-01-01', meanPerformance: 1.2 });
    expect(driftMeansForGroup([c, a, b], 10)).toEqual([1.0, 1.1, 1.2]);
  });
});
