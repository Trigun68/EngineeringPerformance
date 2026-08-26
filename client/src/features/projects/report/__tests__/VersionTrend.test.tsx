import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { VersionTrend } from '../VersionTrend';
import type { PerformanceReportHistoryPoint } from '../../../../api/performanceReportApi';

function point(
  partial: Partial<PerformanceReportHistoryPoint> & Pick<PerformanceReportHistoryPoint, 'versionId' | 'direction' | 'createdAt'>,
): PerformanceReportHistoryPoint {
  return {
    versionName: `v${partial.versionId}`,
    weightedZScore: -0.4,
    totalLaborHours: 100,
    normSnapshotKey: '1:1',
    ...partial,
  };
}

describe('VersionTrend', () => {
  it('shows empty state when no Z', () => {
    render(
      <VersionTrend
        points={[
          point({
            versionId: 1,
            direction: 1,
            createdAt: '2026-01-01T00:00:00Z',
            weightedZScore: null,
          }),
        ]}
      />,
    );
    expect(screen.getByTestId('report-version-trend-empty')).toBeInTheDocument();
  });

  it('uses plan/fact series colors and tints snapshot change', () => {
    render(
      <VersionTrend
        points={[
          point({
            versionId: 1,
            direction: 1,
            createdAt: '2026-01-01T00:00:00Z',
            normSnapshotKey: '1:1',
          }),
          point({
            versionId: 2,
            direction: 1,
            createdAt: '2026-06-01T00:00:00Z',
            normSnapshotKey: '1:9',
            weightedZScore: -0.2,
          }),
          point({
            versionId: 3,
            direction: 2,
            createdAt: '2026-03-01T00:00:00Z',
            weightedZScore: 0.3,
            totalLaborHours: 40,
          }),
        ]}
      />,
    );
    const svg = screen.getByTestId('report-version-trend');
    const plan = svg.querySelectorAll('circle[data-series="plan"]');
    const fact = svg.querySelectorAll('circle[data-series="fact"]');
    expect(plan).toHaveLength(2);
    expect(fact).toHaveLength(1);
    expect(plan[0].getAttribute('fill')).toBe('var(--ep-report-primary)');
    expect(fact[0].getAttribute('fill')).toBe('var(--ep-report-perf-critical)');
    expect(plan[1].getAttribute('data-snapshot-changed')).toBe('true');
    const x1 = Number(plan[0].getAttribute('cx'));
    const x2 = Number(plan[1].getAttribute('cx'));
    expect(x2).toBeGreaterThan(x1);
  });
});
