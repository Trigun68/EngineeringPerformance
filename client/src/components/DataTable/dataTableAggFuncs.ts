import type { IAggFuncParams } from 'ag-grid-community';

/** Сумма числовых значений; null/undefined трактуются как 0 (для подитогов сценариев). */
export function sumNullableAggFunc(params: IAggFuncParams): number {
  let sum = 0;
  for (const value of params.values) {
    if (value == null || value === '') continue;
    const n = Number(value);
    if (Number.isFinite(n)) sum += n;
  }
  return sum;
}

export const dataTableAggFuncs = {
  sumNullable: sumNullableAggFunc,
} as const;
