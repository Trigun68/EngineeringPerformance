import { describe, expect, it } from 'vitest';
import { computeZTrendKpi } from '../zTrendKpi';
import type { ZTrendGroup, ZTrendPoint, ZTrendSegment } from '../../../../api/zTrendApi';

function point(partial: Partial<ZTrendPoint> & { date: string }): ZTrendPoint {
  return {
    zScore: null,
    weight: 0,
    projectCount: 0,
    normId: null,
    flag: 'ok',
    contributions: null,
    ...partial,
  };
}

function segment(points: ZTrendPoint[]): ZTrendSegment {
  return {
    normId: 1,
    normName: 'Норма',
    normGroupId: null,
    normGroupName: '',
    color: '#888',
    segmentKey: 'seg-1',
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    points,
  };
}

function group(plan: ZTrendSegment[], fact: ZTrendSegment[]): ZTrendGroup {
  return {
    groupId: 0,
    groupName: 'Общий тренд',
    plan: { seriesType: 'Plan', segments: plan },
    fact: { seriesType: 'Fact', segments: fact },
  };
}

describe('computeZTrendKpi', () => {
  it('returns nulls and zero counts for empty group', () => {
    const kpi = computeZTrendKpi(group([], []));
    expect(kpi).toEqual({
      lastFactZ: null,
      lastFactDate: null,
      lastFactProjectCount: null,
      matchedPlanZ: null,
      planMatchKind: null,
      deltaZ: null,
      versionsCount: 0,
      projectsCount: 0,
    });
  });

  it('matches plan point on the same date and computes delta as fact − plan', () => {
    const g = group(
      [segment([point({ date: '2026-07-01', zScore: 0.4 }), point({ date: '2026-08-01', zScore: 0.2 })])],
      [segment([point({ date: '2026-08-01', zScore: -0.3, projectCount: 2 })])],
    );
    const kpi = computeZTrendKpi(g);
    expect(kpi.lastFactZ).toBe(-0.3);
    expect(kpi.matchedPlanZ).toBe(0.2);
    expect(kpi.planMatchKind).toBe('same-date');
    expect(kpi.deltaZ).toBeCloseTo(-0.5);
    expect(kpi.versionsCount).toBe(1);
  });

  it('falls back to nearest prior plan point when no same-date match', () => {
    const g = group(
      [segment([point({ date: '2026-06-01', zScore: 0.9 }), point({ date: '2026-07-10', zScore: 0.5 })])],
      [segment([point({ date: '2026-07-20', zScore: 0.1 })])],
    );
    const kpi = computeZTrendKpi(g);
    expect(kpi.matchedPlanZ).toBe(0.5);
    expect(kpi.planMatchKind).toBe('nearest-prior');
    expect(kpi.deltaZ).toBeCloseTo(-0.4);
  });

  it('falls back to nearest plan point when all plan points are after the fact', () => {
    const g = group(
      [segment([point({ date: '2026-09-01', zScore: 0.7 })])],
      [segment([point({ date: '2026-08-01', zScore: 0.1 })])],
    );
    const kpi = computeZTrendKpi(g);
    expect(kpi.matchedPlanZ).toBe(0.7);
    expect(kpi.planMatchKind).toBe('nearest');
    expect(kpi.deltaZ).toBeCloseTo(-0.6);
  });

  it('ignores fact points without Z when picking the last fact', () => {
    const g = group(
      [segment([point({ date: '2026-08-01', zScore: 0.2 })])],
      [
        segment([
          point({ date: '2026-08-01', zScore: -0.2 }),
          point({ date: '2026-08-15', zScore: null, flag: 'no_norm' }),
        ]),
      ],
    );
    const kpi = computeZTrendKpi(g);
    expect(kpi.lastFactZ).toBe(-0.2);
    expect(kpi.lastFactDate).toBe('2026-08-01');
    expect(kpi.versionsCount).toBe(1);
  });

  it('counts unique projects from fact contributions with Z', () => {
    const g = group(
      [],
      [
        segment([
          point({
            date: '2026-08-01',
            zScore: 0.1,
            contributions: [
              { projectId: 1, projectCode: 'A', projectName: 'A', versionId: 10, versionName: 'v1', zScore: 0.1, weight: 5 },
              { projectId: 2, projectCode: 'B', projectName: 'B', versionId: 20, versionName: 'v1', zScore: 0.2, weight: 3 },
              { projectId: 2, projectCode: 'B', projectName: 'B', versionId: 21, versionName: 'v2', zScore: null, weight: 1 },
            ],
          }),
        ]),
      ],
    );
    const kpi = computeZTrendKpi(g);
    expect(kpi.projectsCount).toBe(2);
  });

  it('falls back to latest plan Z when no fact points exist', () => {
    const g = group(
      [segment([point({ date: '2026-07-21', zScore: -0.13 }), point({ date: '2026-07-27', zScore: -0.14 })])],
      [],
    );
    const kpi = computeZTrendKpi(g);
    expect(kpi.lastFactZ).toBeNull();
    expect(kpi.matchedPlanZ).toBe(-0.14);
    expect(kpi.planMatchKind).toBe('latest');
    expect(kpi.deltaZ).toBeNull();
    expect(kpi.versionsCount).toBe(0);
  });

  it('handles points across multiple segments (norm changes)', () => {
    const g = group(
      [],
      [
        segment([point({ date: '2026-06-01', zScore: 0.8 })]),
        { ...segment([point({ date: '2026-08-01', zScore: -0.4 })]), normId: 2, segmentKey: 'seg-2' },
      ],
    );
    const kpi = computeZTrendKpi(g);
    expect(kpi.lastFactZ).toBe(-0.4);
    expect(kpi.versionsCount).toBe(2);
  });
});
