import { describe, expect, it } from 'vitest';
import type { TimelineBar } from '../../../../api/normRegistryApi';
import {
  TODAY_EDGE_MARGIN_PCT,
  buildTimelineAxis,
  filterBarsToWindow,
  groupTimelineBars,
  layoutSegments,
  parsePeriodWindow,
} from '../normTimelineLayout';

function bar(partial: Partial<TimelineBar> & Pick<TimelineBar, 'normId' | 'groupId'>): TimelineBar {
  return {
    groupName: `group-${partial.groupId}`,
    label: `#${partial.normId} Linear`,
    color: '#2563eb',
    status: 'Active',
    effectiveFrom: '2024-01-01T00:00:00.000Z',
    effectiveTo: '2025-01-01T00:00:00.000Z',
    hasIssue: false,
    ...partial,
  };
}

const NOW = Date.parse('2026-08-27T12:00:00.000Z');

describe('groupTimelineBars', () => {
  it('merges two norms of the same group into one row with two segments', () => {
    const rows = groupTimelineBars([
      bar({
        normId: 2,
        groupId: 10,
        groupName: '01 — Альфа (чел-ч)',
        effectiveFrom: '2025-01-01T00:00:00.000Z',
        effectiveTo: null,
      }),
      bar({
        normId: 1,
        groupId: 10,
        groupName: '01 — Альфа (чел-ч)',
        effectiveFrom: '2024-01-01T00:00:00.000Z',
        effectiveTo: '2025-01-01T00:00:00.000Z',
      }),
    ]);
    expect(rows).toHaveLength(1);
    expect(rows[0].groupId).toBe(10);
    expect(rows[0].groupName).toBe('01 — Альфа (чел-ч)');
    expect(rows[0].bars.map((b) => b.normId)).toEqual([1, 2]);
  });

  it('keeps different groups as separate rows in source order', () => {
    const rows = groupTimelineBars([
      bar({ normId: 1, groupId: 20, groupName: 'Beta' }),
      bar({ normId: 2, groupId: 10, groupName: 'Alpha' }),
      bar({ normId: 3, groupId: 20, groupName: 'Beta' }),
    ]);
    expect(rows.map((r) => r.groupId)).toEqual([20, 10]);
    expect(rows[0].bars.map((b) => b.normId)).toEqual([1, 3]);
    expect(rows[1].bars.map((b) => b.normId)).toEqual([2]);
  });
});

describe('buildTimelineAxis', () => {
  it('hides today marker when today is at the range min', () => {
    const axis = buildTimelineAxis(
      [
        bar({
          normId: 1,
          groupId: 1,
          effectiveFrom: '2026-08-27T12:00:00.000Z',
          effectiveTo: '2027-01-01T00:00:00.000Z',
        }),
      ],
      NOW,
    );
    expect(axis.todayLeftPct).toBeLessThanOrEqual(TODAY_EDGE_MARGIN_PCT);
    expect(axis.todayVisible).toBe(false);
  });

  it('hides today marker when today is at the range max', () => {
    const axis = buildTimelineAxis(
      [
        bar({
          normId: 1,
          groupId: 1,
          effectiveFrom: '2020-01-01T00:00:00.000Z',
          effectiveTo: null,
        }),
      ],
      NOW,
    );
    expect(axis.todayLeftPct).toBeGreaterThanOrEqual(100 - TODAY_EDGE_MARGIN_PCT);
    expect(axis.todayVisible).toBe(false);
  });

  it('shows today marker when today sits inside the range', () => {
    const axis = buildTimelineAxis(
      [
        bar({
          normId: 1,
          groupId: 1,
          effectiveFrom: '2020-01-01T00:00:00.000Z',
          effectiveTo: '2020-06-01T00:00:00.000Z',
        }),
        bar({
          normId: 2,
          groupId: 1,
          effectiveFrom: '2028-01-01T00:00:00.000Z',
          effectiveTo: '2029-01-01T00:00:00.000Z',
          status: 'Future',
        }),
      ],
      NOW,
    );
    expect(axis.todayLeftPct).toBeGreaterThan(TODAY_EDGE_MARGIN_PCT);
    expect(axis.todayLeftPct).toBeLessThan(100 - TODAY_EDGE_MARGIN_PCT);
    expect(axis.todayVisible).toBe(true);
  });

  it('does not pull axis min back to today when all periods are in the future', () => {
    const from = Date.parse('2027-01-01T00:00:00.000Z');
    const axis = buildTimelineAxis(
      [
        bar({
          normId: 1,
          groupId: 1,
          effectiveFrom: '2027-01-01T00:00:00.000Z',
          effectiveTo: '2028-01-01T00:00:00.000Z',
          status: 'Future',
        }),
      ],
      NOW,
    );
    expect(axis.minMs).toBe(from);
    expect(axis.todayLeftPct).toBeLessThan(0);
    expect(axis.todayVisible).toBe(false);
  });
});

describe('layoutSegments', () => {
  it('places sequential periods on the same axis without overlapping percents', () => {
    const bars = [
      bar({
        normId: 1,
        groupId: 1,
        effectiveFrom: '2024-01-01T00:00:00.000Z',
        effectiveTo: '2025-01-01T00:00:00.000Z',
      }),
      bar({
        normId: 2,
        groupId: 1,
        effectiveFrom: '2025-01-01T00:00:00.000Z',
        effectiveTo: '2026-01-01T00:00:00.000Z',
      }),
    ];
    const axis = buildTimelineAxis(bars, NOW);
    const segs = layoutSegments(bars, axis);
    expect(segs).toHaveLength(2);
    expect(segs[0].leftPct).toBeLessThan(segs[1].leftPct);
    expect(segs[0].leftPct + segs[0].widthPct).toBeCloseTo(segs[1].leftPct, 5);
  });
});

describe('parsePeriodWindow and clipping', () => {
  const year2026 = parsePeriodWindow('2026-01-01', '2026-12-31');

  it('parses local calendar bounds for the selected year', () => {
    expect(year2026).not.toBeNull();
    expect(year2026!.startMs).toBe(new Date(2026, 0, 1).getTime());
    expect(year2026!.endMs).toBe(new Date(2026, 11, 31, 23, 59, 59, 999).getTime());
  });

  it('axis follows the selected period, not bar extents into 2040', () => {
    const bars = [
      bar({
        normId: 1,
        groupId: 1,
        effectiveFrom: '2024-01-01T00:00:00.000Z',
        effectiveTo: '2040-06-26T00:00:00.000Z',
      }),
    ];
    const axis = buildTimelineAxis(bars, NOW, year2026);
    expect(axis.minMs).toBe(year2026!.startMs);
    expect(axis.maxMs).toBe(year2026!.endMs);
  });

  it('keeps overlapping norms and drops those entirely outside the year', () => {
    const bars = [
      bar({
        normId: 1,
        groupId: 1,
        effectiveFrom: '2024-01-01T00:00:00.000Z',
        effectiveTo: '2026-06-01T00:00:00.000Z',
      }),
      bar({
        normId: 2,
        groupId: 1,
        effectiveFrom: '2026-06-01T00:00:00.000Z',
        effectiveTo: null,
      }),
      bar({
        normId: 3,
        groupId: 2,
        effectiveFrom: '2028-01-01T00:00:00.000Z',
        effectiveTo: '2029-01-01T00:00:00.000Z',
        status: 'Future',
      }),
    ];
    const visible = filterBarsToWindow(bars, year2026);
    expect(visible.map((b) => b.normId)).toEqual([1, 2]);
    const axis = buildTimelineAxis(visible, NOW, year2026);
    const segs = layoutSegments(visible, axis);
    expect(segs).toHaveLength(2);
    expect(segs[0].leftPct).toBeGreaterThanOrEqual(0);
    expect(segs[0].leftPct + segs[0].widthPct).toBeLessThanOrEqual(100.01);
    expect(segs[1].leftPct + segs[1].widthPct).toBeLessThanOrEqual(100.01);
    expect(segs[1].leftPct + segs[1].widthPct).toBeCloseTo(100, 0);
  });

  it('hides today marker when the selected period ends on today', () => {
    const window = parsePeriodWindow('2026-01-01', '2026-08-27');
    const axis = buildTimelineAxis([], NOW, window);
    expect(axis.todayLeftPct).toBeGreaterThanOrEqual(100 - TODAY_EDGE_MARGIN_PCT);
    expect(axis.todayVisible).toBe(false);
  });
});
