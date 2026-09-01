import type { DeviationFormulaDto, FormulaType, NormFormulaDto } from '../../types/norms';

export const KIM_CURVE_TYPE = 12 as const satisfies FormulaType;
export const LOGNORMAL_DEVIATION = 5;

export const FORMULA_TYPE_LABELS: Record<number, string> = {
  1: 'Линейная',
  2: 'Логарифмическая',
  3: 'Сдвинутая логарифмическая',
  4: 'Уайт (степенная)',
  5: 'Степенная',
  6: 'Экспоненциальное затухание',
  7: 'Кусочно-линейная',
  8: 'Полиномиальная',
  9: 'Постоянная',
  10: 'Гиперболическая',
  11: 'Экспоненциальная',
  12: 'Логнормальное распределение',
};

export function formulaTypeLabel(type: number | undefined | null): string {
  if (type == null || Number.isNaN(type)) return '—';
  return FORMULA_TYPE_LABELS[type] ?? String(type);
}

export function isKimCurveType(type: number | undefined | null): boolean {
  return type === KIM_CURVE_TYPE;
}

export function muFromProductivity(p: number): number {
  return p > 0 ? Math.log(p) : 0;
}

export function withKimDerived(formula: NormFormulaDto, sigmaFallback = 0): NormFormulaDto {
  const a = formula.coefficients.a ?? 0;
  const sigma = formula.coefficients.sigma ?? sigmaFallback;
  return {
    ...formula,
    type: KIM_CURVE_TYPE,
    coefficients: {
      ...formula.coefficients,
      a,
      mu: muFromProductivity(a),
      sigma,
    },
  };
}

export function kimDeviation(sigma: number): DeviationFormulaDto {
  return { modelType: LOGNORMAL_DEVIATION, coefficients: { sigma } };
}

export function applyNormFormulaFields(
  nextNorm: NormFormulaDto,
  currentDeviation: DeviationFormulaDto,
): {
  normFormula: NormFormulaDto;
  deviationFormula: DeviationFormulaDto;
  performance: number | null;
  estimate: number | null;
  standardDeviation: number | null;
} {
  if (isKimCurveType(nextNorm.type)) {
    const synced = withKimDerived(nextNorm, currentDeviation.coefficients.sigma ?? 0);
    const sigma = synced.coefficients.sigma ?? 0;
    return {
      normFormula: synced,
      deviationFormula: kimDeviation(sigma),
      performance: synced.coefficients.a ?? null,
      estimate: synced.coefficients.mu ?? null,
      standardDeviation: sigma,
    };
  }

  return {
    normFormula: nextNorm,
    deviationFormula: currentDeviation.modelType === LOGNORMAL_DEVIATION
      ? { modelType: 1, coefficients: { sigma: currentDeviation.coefficients.sigma ?? 0 } }
      : currentDeviation,
    performance: nextNorm.coefficients.a ?? null,
    estimate: null,
    standardDeviation:
      currentDeviation.coefficients.sigma != null && (nextNorm.coefficients.a ?? 0) > 0
        ? currentDeviation.coefficients.sigma * (nextNorm.coefficients.a ?? 0)
        : null,
  };
}
