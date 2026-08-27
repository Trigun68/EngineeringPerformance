import { describe, expect, it } from 'vitest';
import type { NormComparison, Verdict } from '../../../../api/factNormAnalyticsApi';
import {
  deltaHeatBackground,
  severityLabel,
  severityLevel,
  severityRatio,
} from '../severity';

function row(partial: Partial<NormComparison> & { verdict?: Verdict }): NormComparison {
  return {
    normId: 1,
    workDirectionId: 10,
    groupName: 'Группа',
    currentFormula: 'LINEAR',
    proposedFormula: 'LINEAR',
    deltaMean: 0,
    deltaSigma: 0,
    currentR2: 0.5,
    proposedR2: 0.5,
    deltaR2: 0,
    verdict: 'NormIsActual',
    requiresAttention: false,
    insufficientData: false,
    sigmaZero: false,
    suggestionId: null,
    chartPoints: [],
    proposedParameters: null,
    currentParameters: null,
    warning: null,
    normMeanPerformance: 1,
    factMeanPerformance: 1,
    factStdDeviation: 0.1,
    normStdDeviation: 0.2,
    ...partial,
  };
}

describe('severityRatio', () => {
  it('is |Δμ| / σ_нормы', () => {
    expect(severityRatio(row({ deltaMean: 0.4, normStdDeviation: 0.2 }))).toBeCloseTo(2, 6);
  });

  it('falls back to fact σ when norm σ is zero', () => {
    expect(severityRatio(row({ deltaMean: 0.3, normStdDeviation: 0, factStdDeviation: 0.1 }))).toBeCloseTo(3, 6);
  });

  it('uses verdict when both σ are missing', () => {
    expect(severityRatio(row({ verdict: 'ChangeFormulaType', normStdDeviation: 0, factStdDeviation: 0 }))).toBe(2);
    expect(severityRatio(row({ verdict: 'AdjustParameters', normStdDeviation: 0, factStdDeviation: 0 }))).toBe(1);
    expect(severityRatio(row({ verdict: 'NormIsActual', normStdDeviation: 0, factStdDeviation: 0 }))).toBe(0);
  });
});

describe('severityLevel / label', () => {
  it('maps ratio bands', () => {
    expect(severityLevel(0.2)).toBe('low');
    expect(severityLevel(0.5)).toBe('medium');
    expect(severityLevel(1.5)).toBe('high');
    expect(severityLabel(0.2)).toBe('В допуске');
    expect(severityLabel(1.6)).toBe('Требует пересмотра');
  });
});

describe('deltaHeatBackground', () => {
  it('returns transparent for missing values', () => {
    expect(deltaHeatBackground(null, 1)).toBe('transparent');
    expect(deltaHeatBackground(0.2, 0)).toBe('transparent');
  });

  it('returns a tinted background for a large delta', () => {
    expect(deltaHeatBackground(2, 1)).toMatch(/rgba/);
  });
});
