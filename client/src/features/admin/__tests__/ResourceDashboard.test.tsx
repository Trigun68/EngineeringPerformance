import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import type { ResourceMetrics } from '../../../api/monitoringApi';

vi.mock('highcharts', () => ({ default: {} }));

vi.mock('highcharts-react-official', () => ({
  default: ({ options }: { options: { title?: { text?: string } } }) => (
    <div data-testid="resource-chart">{options?.title?.text}</div>
  ),
}));

const mockMetrics: ResourceMetrics = {
  collectedAtUtc: '2026-09-01T10:00:00Z',
  windowMinutes: 60,
  bucketSeconds: 10,
  historyAvailableFromUtc: '2026-09-01T09:00:00Z',
  verdicts: ['RAM: SQL 80% (1600 МиБ), бэкенд 15% (300 МиБ), Redis 5% (100 МиБ).'],
  snapshot: {
    activeUsers: 3,
    activeSessions: 5,
    kestrelConnections: 4,
    currentHttpRequests: 1,
    sqlConnections: { interactive: 6, analytics: 1, other: 0, total: 7 },
    redisClients: 2,
    analyticsGateBusy: 0,
    analyticsGateLimit: 2,
    backend: { available: true, status: 'Process', cpuCores: 0.4, rssMib: 300 },
    redis: { available: true, status: 'Connected', cpuCores: 0.05, rssMib: 100 },
    sql: { available: true, status: 'Connected', cpuCores: 0.8, rssMib: 1600 },
    cpuCoresPerUser: 0.1333,
    rssMibPerUser: 100,
    runtime: {
      gcHeapMib: 80,
      allocRateBytesPerSecond: 2_000_000,
      gen2Collections: 4,
      threadPoolBusy: 2,
      threadPoolQueue: 0,
      sqlRunnable: 0,
      redisHitRatio: 0.9,
      redisOpsPerSec: 10,
      redisEvictedKeys: 0,
      redisFragmentation: 1.1,
      sqlBatchRequestsPerSec: 20,
      sqlBufferCacheHitRatio: 0.99,
    },
  },
  series: {
    backendCpuCores: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 0.4 }],
    redisCpuCores: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 0.05 }],
    sqlCpuCores: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 0.8 }],
    backendRssMib: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 300 }],
    redisRssMib: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 100 }],
    sqlRssMib: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 1600 }],
    activeUsers: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 3 }],
    kestrelConnections: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 4 }],
    sqlInteractive: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 6 }],
    sqlAnalytics: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 1 }],
    redisClients: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 2 }],
    threadPoolQueue: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 0 }],
    sqlRunnable: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 0 }],
    redisHitRatio: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 0.9 }],
    allocRateMibPerSec: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 2 }],
    gen2Collections: [{ timestampUtc: '2026-09-01T09:59:00Z', value: 4 }],
    sqlMemoryLimitMib: 2048,
  },
};

vi.mock('../../../api/monitoringApi', () => ({
  fetchResourceMetrics: vi.fn(() => Promise.resolve(mockMetrics)),
}));

import { fetchResourceMetrics } from '../../../api/monitoringApi';
import { ResourceDashboard } from '../ResourceDashboard';

describe('ResourceDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders KPIs, verdict and stacked chart titles', async () => {
    render(<ResourceDashboard />);

    await waitFor(() => {
      expect(screen.getByTestId('resource-dashboard')).toBeInTheDocument();
    });

    expect(screen.getByText('Ресурсы сервера')).toBeInTheDocument();
    expect(screen.getByTestId('resource-kpi-users')).toHaveTextContent('3');
    expect(screen.getByTestId('resource-verdict')).toHaveTextContent('RAM:');
    expect(screen.getByText('CPU стека (доли ядра)')).toBeInTheDocument();
    expect(screen.getByText('Оперативная память (МиБ)')).toBeInTheDocument();
    expect(fetchResourceMetrics).toHaveBeenCalledWith(60);
  });

  it('requests 24h after changing the period select', async () => {
    render(<ResourceDashboard />);

    await waitFor(() => {
      expect(screen.getByTestId('resource-dashboard')).toBeInTheDocument();
    });

    fireEvent.mouseDown(screen.getByLabelText('Отчёт за'));
    fireEvent.click(screen.getByTestId('resource-period-1440'));

    await waitFor(() => {
      expect(fetchResourceMetrics).toHaveBeenCalledWith(1440);
    });
  });
});
