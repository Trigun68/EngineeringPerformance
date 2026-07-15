import apiClient from '../services/apiClient';
import type { DeviationFormulaDto, NormFormulaDto } from '../types/norms';

export interface FormulaTemplateDto {
  type: number;
  displayName: string;
  formulaLatex: string;
  requiresBounds: boolean;
  coefficients: Array<{ name: string; label: string; required: boolean; min?: number; max?: number }>;
}

export interface NormPreviewPoint {
  volume: number;
  center: number;
  upper: number;
  lower: number;
}

export async function fetchFormulaTemplates(): Promise<FormulaTemplateDto[]> {
  const { data } = await apiClient.get<FormulaTemplateDto[]>('/formula-templates');
  return data;
}

export async function previewNormCurve(
  normFormula: NormFormulaDto,
  deviationFormula: DeviationFormulaDto,
  points = 100,
): Promise<{ curve: Array<{ volume: number; n: number }>; band: NormPreviewPoint[] }> {
  const { data } = await apiClient.post('/norms/preview', {
    normFormula: {
      ...normFormula,
      piecewisePoints: normFormula.piecewisePoints ?? null,
      polynomialCoeffs: normFormula.polynomialCoeffs ?? null,
    },
    deviationFormula,
    points,
  });
  return data;
}
