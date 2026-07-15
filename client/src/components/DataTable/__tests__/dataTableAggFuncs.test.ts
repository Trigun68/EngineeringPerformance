import { describe, expect, it } from 'vitest';
import type { IAggFuncParams } from 'ag-grid-community';
import { sumNullableAggFunc } from '../dataTableAggFuncs';

function aggParams(values: unknown[]): IAggFuncParams {
  return { values } as IAggFuncParams;
}

describe('sumNullableAggFunc', () => {
  it('sums numeric values treating null as zero', () => {
    const result = sumNullableAggFunc(aggParams([10, null, 5, undefined, '', 3.5]));
    expect(result).toBe(18.5);
  });

  it('returns zero for empty values', () => {
    const result = sumNullableAggFunc(aggParams([]));
    expect(result).toBe(0);
  });
});
