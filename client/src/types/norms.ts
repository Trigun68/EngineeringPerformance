export type FormulaType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
export type ExtrapolationMode = 1 | 2;
export type DeviationModelType = 1 | 2 | 3 | 4 | 5;
export type PlanningScenario = 1 | 2 | 3;

export type ZParameterLevel = 1 | 2 | 3 | 4;

export interface PiecewisePointDto {
  volume: number;
  norm: number;
}

export interface NormFormulaDto {
  type: FormulaType;
  coefficients: Record<string, number>;
  minVolume: number;
  maxVolume: number;
  extrapolation: ExtrapolationMode;
  piecewisePoints?: PiecewisePointDto[] | null;
  polynomialCoeffs?: number[] | null;
}

export interface DeviationFormulaDto {
  modelType: DeviationModelType;
  coefficients: Record<string, number>;
}

export const defaultNormFormula = (performance = 0): NormFormulaDto => ({
  type: 1,
  coefficients: { a: performance, b: 0 },
  minVolume: 0,
  maxVolume: 999999,
  extrapolation: 2,
});

export const defaultDeviationFormula = (sigma = 0): DeviationFormulaDto => ({
  modelType: 1,
  coefficients: { sigma },
});

export const PLANNING_SCENARIO_LABELS: Record<PlanningScenario, string> = {
  1: 'По каждому значению',
  2: 'По объёму группы IIC проекта',
  3: 'По объёму группы IIC объектов',
};
