import type { TimelineBar } from '../../../api/normRegistryApi';

/** Не рисовать «Сегодня», если маркер прижат к краю оси. */
export const TODAY_EDGE_MARGIN_PCT = 3;

/** Визуальный зазор между соседними сегментами на одной дорожке. */
export const SEGMENT_GAP_PX = 4;

export interface TimelineGroupRow {
  groupId: number;
  groupName: string;
  bars: TimelineBar[];
}

export interface TimelineWindow {
  startMs: number;
  endMs: number;
}

export interface TimelineAxis {
  minMs: number;
  maxMs: number;
  spanMs: number;
  todayMs: number;
  todayLeftPct: number;
  todayVisible: boolean;
}

export interface TimelineSegmentLayout {
  bar: TimelineBar;
  leftPct: number;
  widthPct: number;
}

const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})/;

function parseIsoDateLocal(iso: string, endOfDay: boolean): number | null {
  const match = ISO_DATE.exec(iso.trim());
  if (!match) return null;
  const y = Number(match[1]);
  const m = Number(match[2]) - 1;
  const d = Number(match[3]);
  const date = endOfDay ? new Date(y, m, d, 23, 59, 59, 999) : new Date(y, m, d);
  const ms = date.getTime();
  return Number.isFinite(ms) ? ms : null;
}

/** Окно выбранного периода аналитики (локальные календарные дни). */
export function parsePeriodWindow(dateFrom?: string | null, dateTo?: string | null): TimelineWindow | null {
  if (!dateFrom?.trim() || !dateTo?.trim()) return null;
  const startMs = parseIsoDateLocal(dateFrom, false);
  const endMs = parseIsoDateLocal(dateTo, true);
  if (startMs == null || endMs == null || endMs <= startMs) return null;
  return { startMs, endMs };
}

export function barOverlapsWindow(bar: TimelineBar, window: TimelineWindow): boolean {
  const from = new Date(bar.effectiveFrom).getTime();
  if (!Number.isFinite(from) || from > window.endMs) return false;
  const to = bar.effectiveTo ? new Date(bar.effectiveTo).getTime() : Number.POSITIVE_INFINITY;
  return to >= window.startMs;
}

export function filterBarsToWindow(bars: TimelineBar[], window: TimelineWindow | null): TimelineBar[] {
  if (!window) return bars;
  return bars.filter((bar) => barOverlapsWindow(bar, window));
}

/** Одна строка на группу IIC; порядок групп — как в исходном списке баров. */
export function groupTimelineBars(bars: TimelineBar[]): TimelineGroupRow[] {
  const order: number[] = [];
  const byGroup = new Map<number, TimelineBar[]>();
  for (const bar of bars) {
    const existing = byGroup.get(bar.groupId);
    if (!existing) {
      order.push(bar.groupId);
      byGroup.set(bar.groupId, [bar]);
    } else {
      existing.push(bar);
    }
  }
  return order.map((groupId) => {
    const items = byGroup.get(groupId) ?? [];
    const sorted = [...items].sort(
      (a, b) => new Date(a.effectiveFrom).getTime() - new Date(b.effectiveFrom).getTime(),
    );
    return {
      groupId,
      groupName: sorted[0]?.groupName ?? `#${groupId}`,
      bars: sorted,
    };
  });
}

function axisFromRange(min: number, max: number, nowMs: number): TimelineAxis {
  let lo = min;
  let hi = max;
  if (!Number.isFinite(lo) || !Number.isFinite(hi) || hi <= lo) {
    lo = Number.isFinite(lo) ? lo : nowMs;
    hi = lo + 86_400_000;
  }
  const spanMs = hi - lo;
  const todayLeftPct = ((nowMs - lo) / spanMs) * 100;
  const todayVisible =
    todayLeftPct > TODAY_EDGE_MARGIN_PCT && todayLeftPct < 100 - TODAY_EDGE_MARGIN_PCT;
  return {
    minMs: lo,
    maxMs: hi,
    spanMs,
    todayMs: nowMs,
    todayLeftPct,
    todayVisible,
  };
}

/**
 * Ось: выбранный период аналитики, иначе min(EffectiveFrom) … max(EffectiveTo ?? now).
 * Не тянуть min к «сегодня» — иначе маркер оказывается в начале дорожки.
 */
export function buildTimelineAxis(
  bars: TimelineBar[],
  nowMs = Date.now(),
  window?: TimelineWindow | null,
): TimelineAxis {
  if (window) {
    return axisFromRange(window.startMs, window.endMs, nowMs);
  }
  if (bars.length === 0) {
    return {
      minMs: 0,
      maxMs: 1,
      spanMs: 1,
      todayMs: nowMs,
      todayLeftPct: 0,
      todayVisible: false,
    };
  }

  let min = Infinity;
  let max = -Infinity;
  for (const bar of bars) {
    const from = new Date(bar.effectiveFrom).getTime();
    const to = bar.effectiveTo ? new Date(bar.effectiveTo).getTime() : nowMs;
    if (Number.isFinite(from)) min = Math.min(min, from);
    if (Number.isFinite(to)) max = Math.max(max, to);
  }
  return axisFromRange(min, max, nowMs);
}

/** Сегменты, обрезанные по оси — ничего не рисуем за выбранный период. */
export function layoutSegments(bars: TimelineBar[], axis: TimelineAxis): TimelineSegmentLayout[] {
  const result: TimelineSegmentLayout[] = [];
  for (const bar of bars) {
    const rawFrom = new Date(bar.effectiveFrom).getTime();
    const rawTo = bar.effectiveTo ? new Date(bar.effectiveTo).getTime() : axis.maxMs;
    if (!Number.isFinite(rawFrom) || !Number.isFinite(rawTo)) continue;
    const fromMs = Math.max(rawFrom, axis.minMs);
    const toMs = Math.min(rawTo, axis.maxMs);
    if (toMs <= fromMs) continue;
    const leftPct = ((fromMs - axis.minMs) / axis.spanMs) * 100;
    const widthPct = Math.max(0.4, ((toMs - fromMs) / axis.spanMs) * 100);
    result.push({ bar, leftPct, widthPct });
  }
  return result;
}

export function formatTimelineDate(ms: number): string {
  return new Date(ms).toLocaleDateString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}
