import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { ProductivityTrendPanel } from '../ProductivityTrendPanel';

vi.mock('../../../api/productivityTrendApi', () => ({
  fetchProductivityTrend: vi.fn().mockResolvedValue({
    overall: { segments: [], groupName: 'all' },
    byGroup: [],
    transitions: [],
    warnings: [],
    legend: [],
  }),
}));

vi.mock('../../../api/productivityIndexTrendApi', () => ({
  fetchProductivityIndexTrend: vi.fn().mockResolvedValue({
    overall: { plan: [], fact: [] },
    byGroup: [],
    warnings: [],
    detail: null,
  }),
}));

vi.mock('../../../api/zTrendApi', () => ({
  fetchZTrends: vi.fn().mockResolvedValue(null),
}));

vi.mock('highcharts', () => ({ default: {} }));
vi.mock('highcharts-react-official', () => ({
  default: () => <div data-testid="highcharts-chart" />,
}));
vi.mock('highcharts/highcharts-more', () => ({ default: vi.fn() }));
vi.mock('../ProductivityTrendChart', () => ({
  ProductivityTrendChart: () => <div data-testid="highcharts-chart" />,
}));
vi.mock('../ProductivityTrendFilters', () => ({
  ProductivityTrendFilters: () => null,
}));
vi.mock('../ProductivityTrendTransitionsGrid', () => ({
  ProductivityTrendTransitionsGrid: () => null,
}));

describe('ProductivityTrendPanel', () => {
  it('renders panel testid', () => {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <ProductivityTrendPanel dateFrom="2026-01-01" dateTo="2026-12-31" volumeMode="median" />
      </QueryClientProvider>,
    );
    expect(screen.getByTestId('prod-trend-panel')).toBeInTheDocument();
  });
});
