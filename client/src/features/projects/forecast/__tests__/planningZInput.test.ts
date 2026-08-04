import { describe, expect, it } from 'vitest';
import { formatZDisplay, parseZInput } from '../PlanningParametersDialog';

describe('parseZInput', () => {
  it('parses comma and dot decimals', () => {
    expect(parseZInput('0,5')).toBe(0.5);
    expect(parseZInput('0.5')).toBe(0.5);
    expect(parseZInput('-1,25')).toBe(-1.25);
    expect(parseZInput(' 1,00 ')).toBe(1);
  });

  it('returns null for incomplete input', () => {
    expect(parseZInput('')).toBeNull();
    expect(parseZInput('-')).toBeNull();
    expect(parseZInput(',')).toBeNull();
    expect(parseZInput('.')).toBeNull();
    expect(parseZInput('-,')).toBeNull();
  });

  it('accepts trailing separator as integer so far', () => {
    expect(parseZInput('1,')).toBe(1);
    expect(parseZInput('1.')).toBe(1);
  });
});

describe('formatZDisplay', () => {
  it('formats with comma decimal separator', () => {
    expect(formatZDisplay(0.5)).toBe('0,50');
    expect(formatZDisplay(0)).toBe('0,00');
    expect(formatZDisplay(-0.5)).toBe('-0,50');
  });
});
