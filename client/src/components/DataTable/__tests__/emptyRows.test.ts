import { describe, expect, it } from 'vitest';
import { EMPTY_ROWS, emptyRows } from '../emptyRows';

describe('emptyRows', () => {
  it('returns the same frozen identity for every generic', () => {
    expect(emptyRows<{ id: number }>()).toBe(EMPTY_ROWS);
    expect(emptyRows<string>()).toBe(emptyRows<{ id: number }>());
    expect(Object.isFrozen(EMPTY_ROWS)).toBe(true);
  });
});
