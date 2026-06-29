import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

vi.mock('highcharts', () => ({ default: {} }));

vi.mock('highcharts-react-official', () => ({
  default: ({ options }: { options: { title?: { text?: string } } }) => (
    <div data-testid="highcharts-chart">{options?.title?.text ?? 'chart'}</div>
  ),
}));

vi.mock('../../../api/monitoringApi', () => ({
  fetchSystemMetrics: vi.fn(() =>
    Promise.resolve({
      red: {
        totalRequests: 42,
        averageDurationMs: 18.5,
        p95DurationMs: 41.2,
        serverErrorCount: 1,
        windowMinutes: 15,
      },
      requestRateSeries: [{ timestampUtc: '2026-06-08T10:00:00.000Z', value: 5 }],
      errorDistribution: [{ errorType: 'Http500', count: 1 }],
      responseTimeTrend24h: [{ timestampUtc: '2026-06-08T09:00:00.000Z', value: 20 }],
      database: { isConfigured: true, isConnected: true, status: 'Connected' },
      redis: { isConfigured: false, isConnected: false, status: 'Not configured' },
      collectedAtUtc: '2026-06-08T10:15:00.000Z',
    })),
}));

import { MonitoringDashboard } from '../MonitoringDashboard';

describe('MonitoringDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders RED summary and Highcharts placeholders', async () => {
    render(<MonitoringDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Системный мониторинг (RED)')).toBeInTheDocument();
    });

    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('18.5 мс')).toBeInTheDocument();
    expect(screen.getByText('41.2 мс')).toBeInTheDocument();
    expect(screen.getAllByTestId('highcharts-chart')).toHaveLength(3);
    expect(screen.getByText('Нагрузка API (запросы/мин)')).toBeInTheDocument();
    expect(screen.getByText('Распределение ошибок')).toBeInTheDocument();
    expect(screen.getByText('Тренд времени отклика (24 ч)')).toBeInTheDocument();
  });
});
