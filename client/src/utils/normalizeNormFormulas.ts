import type { DeviationFormulaDto, NormFormulaDto } from '../types/norms';
import { defaultDeviationFormula, defaultNormFormula } from '../types/norms';

/** Приводит legacy/мигрированные DTO к полному виду для preview и редактора. */
export function normalizeNormFormula(
  formula: NormFormulaDto | null | undefined,
  performance?: number | null,
): NormFormulaDto {
  const base = formula ?? defaultNormFormula(performance ?? 0);
  const a = base.coefficients?.a ?? performance ?? 0;
  const b = base.coefficients?.b ?? 0;
  const maxVolume = base.maxVolume > 0 ? base.maxVolume : 999999;

  const normalized: NormFormulaDto = {
    ...base,
    type: (base.type && base.type >= 1 ? base.type : 1) as NormFormulaDto['type'],
    minVolume: base.minVolume ?? 0,
    maxVolume,
    extrapolation: (base.extrapolation && base.extrapolation >= 1 ? base.extrapolation : 2) as NormFormulaDto['extrapolation'],
    coefficients: { ...base.coefficients, a, b },
    piecewisePoints: base.piecewisePoints ?? null,
    polynomialCoeffs: base.polynomialCoeffs ?? null,
  };

  // type 12 = Кривая Кима; без импорта features/, чтобы utils не зависел от UI-модуля.
  if (normalized.type === 12) {
    const sigma = normalized.coefficients.sigma ?? 0;
    return {
      ...normalized,
      coefficients: {
        ...normalized.coefficients,
        a,
        mu: a > 0 ? Math.log(a) : 0,
        sigma,
      },
    };
  }

  return normalized;
}

export function normalizeDeviationFormula(
  formula: DeviationFormulaDto | null | undefined,
  performance?: number | null,
  standardDeviation?: number | null,
): DeviationFormulaDto {
  const sigmaFromLegacy =
    performance && standardDeviation && performance > 0 ? standardDeviation / performance : 0;

  if (formula && formula.modelType === 5) {
    const sigma = formula.coefficients?.sigma ?? standardDeviation ?? 0;
    return { modelType: 5, coefficients: { sigma } };
  }

  if (formula && formula.modelType >= 1 && formula.modelType <= 4) {
    const sigma = formula.coefficients?.sigma ?? sigmaFromLegacy;
    return { modelType: 1, coefficients: { sigma } };
  }

  return defaultDeviationFormula(sigmaFromLegacy);
}
