import { describe, expect, it } from 'vitest';
import { formatGridNumber } from '../formatGridNumber';

describe('formatGridNumber', () => {
  it('formats with thousand separators for ru-RU', () => {
    expect(formatGridNumber(1234567.89, 2)).toMatch(/1[\s\u00a0]?234[\s\u00a0]?567,89/);
  });

  it('returns dash for empty values', () => {
    expect(formatGridNumber(null, 2)).toBe('—');
  });
});
