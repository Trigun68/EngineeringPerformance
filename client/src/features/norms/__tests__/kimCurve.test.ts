import { describe, expect, it } from 'vitest';
import { defaultDeviationFormula, defaultNormFormula } from '../../../types/norms';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../../utils/normalizeNormFormulas';
import {
  applyNormFormulaFields,
  formulaTypeLabel,
  isKimCurveType,
  kimDeviation,
  KIM_CURVE_TYPE,
  LOGNORMAL_DEVIATION,
  muFromProductivity,
  withKimDerived,
} from '../kimCurve';

describe('kimCurve helpers', () => {
  it('maps type 12 and derives μ = ln(P)', () => {
    expect(isKimCurveType(KIM_CURVE_TYPE)).toBe(true);
    expect(isKimCurveType(1)).toBe(false);
    expect(formulaTypeLabel(12)).toBe('Логнормальное распределение');
    expect(muFromProductivity(10)).toBeCloseTo(Math.log(10), 10);
    expect(muFromProductivity(0)).toBe(0);

    const synced = withKimDerived({
      type: KIM_CURVE_TYPE,
      coefficients: { a: 10, sigma: 0.2 },
      minVolume: 0,
      maxVolume: 999999,
      extrapolation: 2,
    });
    expect(synced.coefficients.mu).toBeCloseTo(Math.log(10), 10);
    expect(synced.coefficients.sigma).toBe(0.2);
    expect(kimDeviation(0.2).modelType).toBe(LOGNORMAL_DEVIATION);
  });

  it('applyNormFormulaFields stores log-σ for Kim and relative σ·P for Linear', () => {
    const kim = applyNormFormulaFields(
      {
        type: 12,
        coefficients: { a: 10, sigma: 0.2 },
        minVolume: 0,
        maxVolume: 999999,
        extrapolation: 2,
      },
      defaultDeviationFormula(0.5),
    );
    expect(kim.performance).toBe(10);
    expect(kim.estimate).toBeCloseTo(Math.log(10), 10);
    expect(kim.standardDeviation).toBe(0.2);
    expect(kim.deviationFormula.modelType).toBe(5);

    const linear = applyNormFormulaFields(defaultNormFormula(100), { modelType: 1, coefficients: { sigma: 0.1 } });
    expect(linear.performance).toBe(100);
    expect(linear.estimate).toBeNull();
    expect(linear.standardDeviation).toBeCloseTo(10, 10);
    expect(linear.deviationFormula.modelType).toBe(1);
  });

  it('normalizeDeviationFormula keeps lognormal modelType 5', () => {
    const kimNorm = normalizeNormFormula({
      type: 12,
      coefficients: { a: 10, sigma: 0.2 },
      minVolume: 0,
      maxVolume: 0,
      extrapolation: 2,
    });
    expect(kimNorm.coefficients.mu).toBeCloseTo(Math.log(10), 10);

    const lognormal = normalizeDeviationFormula({ modelType: 5, coefficients: { sigma: 0.2 } }, 10, 99);
    expect(lognormal.modelType).toBe(5);
    expect(lognormal.coefficients.sigma).toBe(0.2);
  });
});
