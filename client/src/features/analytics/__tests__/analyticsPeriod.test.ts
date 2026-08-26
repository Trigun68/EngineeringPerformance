import { describe, expect, it } from 'vitest';
import { formatIsoDate, resolveAnalyticsPeriod } from '../analyticsPeriod';

describe('resolveAnalyticsPeriod', () => {
  it('1m is last 30 days including today', () => {
    const today = new Date(2026, 6, 27); // 27 Jul 2026
    expect(resolveAnalyticsPeriod('1m', today)).toEqual({
      dateFrom: '2026-06-28',
      dateTo: '2026-07-27',
    });
  });

  it('quarter is from first day of current quarter to today', () => {
    expect(resolveAnalyticsPeriod('quarter', new Date(2026, 6, 27))).toEqual({
      dateFrom: '2026-07-01',
      dateTo: '2026-07-27',
    });
    expect(resolveAnalyticsPeriod('quarter', new Date(2026, 1, 15))).toEqual({
      dateFrom: '2026-01-01',
      dateTo: '2026-02-15',
    });
  });

  it('prev-quarter is full previous calendar quarter', () => {
    // Q3 2026 → previous = Q2
    expect(resolveAnalyticsPeriod('prev-quarter', new Date(2026, 6, 27))).toEqual({
      dateFrom: '2026-04-01',
      dateTo: '2026-06-30',
    });
    // Q1 2026 → previous = Q4 2025
    expect(resolveAnalyticsPeriod('prev-quarter', new Date(2026, 1, 10))).toEqual({
      dateFrom: '2025-10-01',
      dateTo: '2025-12-31',
    });
  });

  it('year is Jan 1 of current year through today', () => {
    expect(resolveAnalyticsPeriod('year', new Date(2026, 6, 27))).toEqual({
      dateFrom: '2026-01-01',
      dateTo: '2026-07-27',
    });
  });

  it('prev-year is full previous calendar year', () => {
    expect(resolveAnalyticsPeriod('prev-year', new Date(2026, 6, 27))).toEqual({
      dateFrom: '2025-01-01',
      dateTo: '2025-12-31',
    });
  });

  it('custom returns empty range', () => {
    expect(resolveAnalyticsPeriod('custom', new Date(2026, 6, 27))).toEqual({
      dateFrom: '',
      dateTo: '',
    });
  });

  it('formatIsoDate uses local calendar day', () => {
    expect(formatIsoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });
});
