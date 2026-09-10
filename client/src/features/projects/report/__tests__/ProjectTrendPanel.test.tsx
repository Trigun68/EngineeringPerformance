import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement, ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectTrendDto } from '../../../../api/projectTrendApi';
import { EMPTY_COVERAGE } from '../../../../api/projectTrendApi';
import { ProjectTrendPanel } from '../ProjectTrendPanel';

const fetchProjectTrend = vi.fn();

vi.mock('../../../../api/projectTrendApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../../api/projectTrendApi')>();
  return {
    ...actual,
    fetchProjectTrend: (...args: unknown[]) => fetchProjectTrend(...args),
  };
});

vi.mock('../../../analytics/shared/PeriodDynamicsChart', () => ({
  PeriodDynamicsChart: () => <div data-testid="z-history-chart" />,
}));
vi.mock('../../../analytics/shared/HoursBridgeChart', () => ({
  HoursBridgeChart: () => <div />,
}));
vi.mock('../../../analytics/shared/HoursCoverageChips', () => ({
  HoursCoverageChips: () => <div />,
}));
vi.mock('../../../analytics/shared/SCurveChart', () => ({
  SCurveChart: () => <div />,
}));
vi.mock('../../../analytics/shared/ZeroBaselineChart', () => ({
  ZeroBaselineChart: () => <div />,
}));
vi.mock('../../../analytics/shared/FactorWaterfallChart', () => ({
  FactorWaterfallChart: () => <div />,
}));
vi.mock('../../../analytics/shared/AnalyticsExtraAccordion', () => ({
  AnalyticsExtraAccordion: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));

const dto: ProjectTrendDto = {
  projectCode: '25-310',
  projectName: 'Реконструкция',
  factVersionId: 2,
  factVersionName: 'Факт',
  factVersionDate: '2026-06-01T00:00:00Z',
  planVersionId: 1,
  planVersionName: 'План',
  zHistory: [],
  hoursBridge: { lines: [], totalVolumeEffect: 0, totalProductivityEffect: 0 },
  zFromZero: [],
  sCurve: [],
  deltaWaterfallStart: null,
  deltaWaterfall: null,
  warnings: [],
  coverage: EMPTY_COVERAGE,
  portfolioZHistory: [],
  zPercentileBand: [],
  events: [],
  planSCurve: [],
  rawHoursDelta: null,
  planDeltaWaterfallStart: null,
  planDeltaWaterfall: null,
};

function renderTrend(ui: ReactElement) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(ui, {
    wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    ),
  });
}

describe('ProjectTrendPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('loads via useQuery and renders project chip', async () => {
    fetchProjectTrend.mockResolvedValue(dto);
    renderTrend(<ProjectTrendPanel projectId={7} />);
    expect(screen.getByTestId('project-trend-loading')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId('project-trend-panel')).toBeInTheDocument();
    });
    expect(screen.getByTestId('project-trend-project-chip')).toHaveTextContent('25-310 — Реконструкция');
    expect(fetchProjectTrend).toHaveBeenCalledWith(7, expect.any(AbortSignal));
  });

  it('shows getApiErrorMessage on failure', async () => {
    fetchProjectTrend.mockRejectedValue({
      response: { data: { detail: 'Тренд недоступен.' } },
    });
    renderTrend(<ProjectTrendPanel projectId={7} />);
    expect(await screen.findByTestId('project-trend-error')).toHaveTextContent('Тренд недоступен.');
  });
});
