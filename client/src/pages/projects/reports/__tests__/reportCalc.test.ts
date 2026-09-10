import { describe, expect, it } from 'vitest';
import {
  ACCENT_FACT,
  ACCENT_PLAN,
  DIRECTION_FACT,
  DIRECTION_PLAN,
  buildInsights,
  displayPhysicalVolume,
  formatFactSourceLabel,
  formatPlanSourceLabel,
  formatTrendPointLabel,
  formatTrendScenarioTooltipHtml,
  formatTrendScenarioTooltipLines,
  fmt,
  fmtSigned,
  formatReportVolume,
  historyToTrendPoints,
  hoursDeltaPct,
  INTERPRETATION_BG,
  INTERPRETATION_COLOR,
  INTERPRETATION_SHORT_RU,
  keyDisciplines,
  resolveSubtreePhysicalVolume,
  selectVersionTrendPoints,
  ZONE_COLOR,
  zScaleVisualPct,
  type Interpretation,
  type PerformanceTreeRow,
  type VersionTrendPoint,
} from '../reportCalc';

describe('displayPhysicalVolume / insights at IIC first level', () => {
  const root = (partial: Partial<PerformanceTreeRow>): PerformanceTreeRow => ({
    workDirectionId: 1,
    workDirectionName: 'Автоматизация',
    workDirectionCode: 'IIC-5',
    parentId: null,
    level: 0,
    normUnitName: 'ед.',
    factPhysicalVolume: null,
    factLaborHours: 100,
    factProductivity: 10,
    factEstimate: null,
    normProductivity: 8,
    normEstimate: null,
    normStdDev: 1,
    zScore: 1.2,
    interpretation: null,
    subtreeHours: 100,
    subtreePhysicalVolume: 10,
    children: [],
    ...partial,
  });

  it('uses subtree volume on roots and own volume on leaves', () => {
    const r = root({});
    const leaf: PerformanceTreeRow = {
      ...r,
      workDirectionId: 52,
      parentId: 1,
      level: 1,
      factPhysicalVolume: 10,
      factLaborHours: null,
      factProductivity: null,
      zScore: null,
      subtreeHours: null,
      subtreePhysicalVolume: 10,
      children: [],
    };
    expect(displayPhysicalVolume(r)).toBe(10);
    expect(displayPhysicalVolume(leaf)).toBe(10);
    expect(resolveSubtreePhysicalVolume({ ...r, subtreePhysicalVolume: null })).toBe(10);
  });

  it('buildInsights ranks first-level rows and computes overrun from subtree volume', () => {
    const insights = buildInsights(
      [root({})],
      {
        totalFactLaborHours: 100,
        weightedZScore: 1.2,
        weightedZScorePercentile: '',
        weightedZScoreInterpretation: null,
        unallocatedLaborHours: 0,
        physicalVolumeTotalsByUnit: [],
      },
      90,
    );
    expect(insights.some((i) => i.kind === 'risk')).toBe(true);
    expect(insights[0].detail).toContain('ч');
  });

  it('keyDisciplines keeps rows with own fact hours', () => {
    expect(
      keyDisciplines([
        root({}),
        root({ workDirectionId: 2, factLaborHours: 0, subtreeHours: 0 }),
      ]),
    ).toHaveLength(1);
  });

  it('keyDisciplines keeps a claimed child with own hours', () => {
    const child = root({
      workDirectionId: 11,
      parentId: 1,
      factLaborHours: 40,
      subtreeHours: 40,
    });
    const disciplines = keyDisciplines([
      root({ factLaborHours: 0, subtreeHours: 140 }),
      child,
    ]);
    expect(disciplines.map((r) => r.workDirectionId)).toEqual([11]);
  });
});

describe('fmt / fmtSigned (ru-RU, 1 vs 2 decimals)', () => {
  it('formats volume and hours with one decimal and grouping', () => {
    const hours = fmt(12345.67, 1).replace(/\u00A0|\u202F/g, ' ');
    expect(hours).toBe('12 345,7');
    expect(fmt(null, 1)).toBe('—');
  });

  it('formatReportVolume dashes empty and zero volumes', () => {
    expect(formatReportVolume(null)).toBe('—');
    expect(formatReportVolume(0, 'ед.')).toBe('—');
    expect(formatReportVolume(10, 'ед.')).toBe(`${fmt(10, 1)} ед.`);
  });

  it('formats productivity and Z with two decimals', () => {
    expect(fmt(12.3456, 2)).toBe('12,35');
    expect(fmtSigned(-0.86, 2)).toBe('-0,86');
    expect(fmtSigned(1.2, 2)).toBe('+1,20');
  });
});

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

  it('uses full mockup chip labels', () => {
    expect(INTERPRETATION_SHORT_RU.Satisfactory).toBe('Удовлетворительно');
    expect(INTERPRETATION_SHORT_RU.NeedsAttention).toBe('Требует внимания');
  });
});

describe('zScaleVisualPct (left = worse, right = better)', () => {
  it('places zero at the center', () => {
    expect(zScaleVisualPct(0)).toBe(50);
  });

  it('places negative Z (better) to the right of center', () => {
    expect(zScaleVisualPct(-1.5)).toBeGreaterThan(50);
    expect(zScaleVisualPct(-3)).toBe(100);
  });

  it('places positive Z (worse) to the left of center', () => {
    expect(zScaleVisualPct(1.5)).toBeLessThan(50);
    expect(zScaleVisualPct(3)).toBe(0);
  });
});

describe('formatPlanSourceLabel / formatFactSourceLabel', () => {
  it('formats primary plan as «из версии … (основная)»', () => {
    expect(formatPlanSourceLabel('Факт v5', true)).toBe('из версии Факт v5 (основная)');
    expect(formatPlanSourceLabel(null, true)).toBe('Нет версии плана');
    expect(formatPlanSourceLabel('План v1', false)).toBe('из версии План v1');
  });

  it('formats primary fact as «… (основная)»', () => {
    expect(formatFactSourceLabel('Факт v5', true)).toBe('Факт v5 (основная)');
    expect(formatFactSourceLabel(null, true)).toBe('');
    expect(formatFactSourceLabel('Факт v2', false)).toBe('Факт v2');
  });
});

describe('selectVersionTrendPoints', () => {
  const point = (partial: Partial<VersionTrendPoint> & Pick<VersionTrendPoint, 'versionId' | 'direction'>): VersionTrendPoint => ({
    versionName: `v${partial.versionId}`,
    date: '1 янв',
    sortDate: partial.sortDate ?? partial.versionId,
    weightedZScore: -0.2,
    totalHours: 100,
    optimisticZScore: -0.4,
    optimisticHours: 90,
    baseZScore: -0.2,
    baseHours: 100,
    pessimisticZScore: 0.1,
    pessimisticHours: 110,
    ...partial,
  });

  it('keeps all plan points with Z and only the primary fact point', () => {
    const selected = selectVersionTrendPoints(
      [
        point({ versionId: 1, direction: DIRECTION_PLAN, sortDate: 10, weightedZScore: 0.4 }),
        point({ versionId: 2, direction: DIRECTION_PLAN, sortDate: 20, weightedZScore: -0.1 }),
        point({ versionId: 3, direction: DIRECTION_FACT, sortDate: 15, weightedZScore: 0.3 }),
        point({ versionId: 4, direction: DIRECTION_FACT, sortDate: 30, weightedZScore: -0.34 }),
      ],
      4,
    );
    expect(selected.map((p) => p.versionId)).toEqual([1, 2, 4]);
  });

  it('drops plan points without Z and fact points that are not primary', () => {
    const selected = selectVersionTrendPoints(
      [
        point({ versionId: 1, direction: DIRECTION_PLAN, weightedZScore: null }),
        point({ versionId: 9, direction: DIRECTION_FACT, weightedZScore: 1 }),
      ],
      4,
    );
    expect(selected).toEqual([]);
  });

  it('maps history dates onto trend points', () => {
    const mapped = historyToTrendPoints([
      {
        versionId: 1,
        versionName: 'План v1',
        createdAt: '2025-02-10T00:00:00Z',
        direction: DIRECTION_PLAN,
        weightedZScore: 0.42,
        totalLaborHours: 1000,
        optimisticZScore: 0.1,
        optimisticLaborHours: 800,
        baseZScore: 0.42,
        baseLaborHours: 1000,
        pessimisticZScore: 0.8,
        pessimisticLaborHours: 1200,
      },
    ]);
    expect(mapped[0].versionId).toBe(1);
    expect(mapped[0].sortDate).toBeGreaterThan(0);
    expect(mapped[0].date).toBeTruthy();
    expect(mapped[0].totalHours).toBe(1000);
    expect(mapped[0].optimisticZScore).toBe(0.1);
    expect(mapped[0].baseHours).toBe(1000);
    expect(mapped[0].pessimisticHours).toBe(1200);
  });
});

describe('trend point labels and scenario tooltip', () => {
  it('joins Z and hours in the point label', () => {
    expect(formatTrendPointLabel(-0.86, 1881170)).toBe(`${fmtSigned(-0.86)} · ${fmt(1881170, 0)} ч`);
    expect(formatTrendPointLabel(null, null)).toBe('— · — ч');
  });

  it('lists three planning scenarios in the tooltip', () => {
    const lines = formatTrendScenarioTooltipLines({
      versionName: '27.07.26',
      optimisticZScore: -1.06,
      optimisticHours: 1700000,
      baseZScore: -0.86,
      baseHours: 1881170,
      pessimisticZScore: -0.66,
      pessimisticHours: 2000000,
    });
    expect(lines[0]).toBe('27.07.26');
    expect(lines[1]).toContain('Оптимистичный');
    expect(lines[2]).toContain('Базовый');
    expect(lines[3]).toContain('Пессимистичный');
    expect(formatTrendScenarioTooltipHtml({
      versionId: 1,
      versionName: '27.07.26',
      direction: DIRECTION_PLAN,
      date: '27 июл',
      sortDate: 1,
      weightedZScore: -0.86,
      totalHours: 1881170,
      optimisticZScore: -1.06,
      optimisticHours: 1700000,
      baseZScore: -0.86,
      baseHours: 1881170,
      pessimisticZScore: -0.66,
      pessimisticHours: 2000000,
    })).toContain('<br/>');
  });
});
