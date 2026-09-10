import type { IicNorm, IicNormHistoryItem } from '../../api/registersApi';
import { formatIisNorm } from '../../utils/formatIisNorm';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../utils/normalizeNormFormulas';
import type { DictionaryItem } from '../../types/dictionary';

export interface IicNormTreeRow extends IicNorm {
  hierarchy: string[];
}

export function normToHistoryItem(norm: IicNorm): IicNormHistoryItem {
  const normFormula = normalizeNormFormula(norm.normFormula, norm.performance);
  const deviationFormula = normalizeDeviationFormula(
    norm.deviationFormula,
    norm.performance,
    norm.standardDeviation,
  );
  return {
    id: norm.id,
    effectiveFrom: norm.effectiveFrom.slice(0, 10),
    effectiveTo: norm.effectiveTo?.slice(0, 10) ?? null,
    performance: normFormula.coefficients.a ?? norm.performance ?? null,
    estimate: norm.estimate ?? null,
    standardDeviation: norm.standardDeviation ?? null,
    unitId: norm.resolvedUnitId ?? norm.unitId ?? null,
    normFormula,
    deviationFormula,
    migratedAt: norm.migratedAt ?? null,
  };
}

export function buildDirectionPath(
  directionId: number,
  directions: DictionaryItem[],
  getUnitLabel: (directionId: number) => string,
): string[] {
  const byId = new Map(directions.map((d) => [d.id, d]));
  const segments: string[] = [];
  let current = byId.get(directionId);
  while (current) {
    segments.unshift(
      formatIisNorm({
        code: current.code,
        name: current.name,
        unitName: getUnitLabel(current.id),
      }),
    );
    current = current.parentId != null ? byId.get(current.parentId) : undefined;
  }
  return segments;
}

export function toTreeRows(
  norms: IicNorm[],
  directions: DictionaryItem[],
  getUnitLabel: (directionId: number) => string,
): IicNormTreeRow[] {
  return norms.map((norm) => ({
    ...norm,
    hierarchy: buildDirectionPath(norm.workDirectionId, directions, getUnitLabel),
  }));
}
