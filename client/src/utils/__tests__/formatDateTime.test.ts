import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTime } from '../formatDateTime';

describe('formatDate', () => {
  it('returns empty string for nullish', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate(undefined)).toBe('');
    expect(formatDate('')).toBe('');
  });

  it('keeps calendar day for date-only ISO strings', () => {
    expect(formatDate('2024-01-15')).toBe('2024-01-15');
  });

  it('keeps calendar day for UTC midnight ISO without TZ shift', () => {
    expect(formatDate('2024-01-15T00:00:00Z')).toBe('2024-01-15');
    expect(formatDate('2024-01-15T00:00:00.000Z')).toBe('2024-01-15');
  });

  it('formats Date objects as local YYYY-MM-DD', () => {
    const d = new Date(2024, 0, 15, 12, 0, 0);
    expect(formatDate(d)).toBe('2024-01-15');
  });
});

describe('formatDateTime', () => {
  it('formats local datetime as YYYY-MM-DD HH:mm:ss', () => {
    const d = new Date(2024, 0, 15, 8, 5, 9);
    expect(formatDateTime(d)).toBe('2024-01-15 08:05:09');
  });
});
