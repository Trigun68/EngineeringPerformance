import { describe, expect, it } from 'vitest';
import {
  ACCENT_FACT,
  ACCENT_PLAN,
  hoursDeltaPct,
  INTERPRETATION_BG,
  INTERPRETATION_COLOR,
  ZONE_COLOR,
  type Interpretation,
} from '../reportCalc';

describe('hoursDeltaPct', () => {
  it('computes signed percent delta of fact vs plan hours', () => {
    expect(hoursDeltaPct(18240, 16980)).toBeCloseTo(-6.9, 1);
    expect(hoursDeltaPct(100, 110)).toBeCloseTo(10, 5);
  });

  it('returns null when data is missing or plan is zero', () => {
    expect(hoursDeltaPct(null, 100)).toBeNull();
    expect(hoursDeltaPct(100, null)).toBeNull();
    expect(hoursDeltaPct(0, 100)).toBeNull();
  });
});

describe('report palette (ReportRedesignMockup)', () => {
  const keys: Interpretation[] = ['Excellent', 'Good', 'Satisfactory', 'NeedsAttention', 'Critical'];

  it('defines color, chip background and zone color for every interpretation', () => {
    for (const key of keys) {
      expect(INTERPRETATION_COLOR[key]).toMatch(/^#[0-9A-F]{6}$/i);
      expect(INTERPRETATION_BG[key]).toMatch(/^#[0-9A-F]{6}$/i);
      expect(ZONE_COLOR[key]).toMatch(/^#[0-9A-F]{6}$/i);
    }
  });

  it('uses mockup accent colors: purple plan, blue fact', () => {
    expect(ACCENT_PLAN).toBe('#8E6FD8');
    expect(ACCENT_FACT).toBe('#2F6FE0');
  });
});
