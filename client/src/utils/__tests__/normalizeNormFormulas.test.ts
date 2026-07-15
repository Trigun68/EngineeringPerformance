import { describe, expect, it } from 'vitest';
import { normalizeDeviationFormula, normalizeNormFormula } from '../normalizeNormFormulas';

describe('normalizeNormFormula', () => {
  it('fills coefficient a from performance for legacy rows', () => {
    const result = normalizeNormFormula(
      { type: 1, coefficients: {}, minVolume: 0, maxVolume: 0, extrapolation: 2 },
      120,
    );
    expect(result.coefficients.a).toBe(120);
    expect(result.maxVolume).toBe(999999);
  });
});

describe('normalizeDeviationFormula', () => {
  it('derives sigma from performance and std dev', () => {
    const result = normalizeDeviationFormula(null, 100, 10);
    expect(result.coefficients.sigma).toBeCloseTo(0.1);
  });
});
