import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

vi.mock('highcharts', () => ({ default: {} }));

vi.mock('highcharts-react-official', () => ({
  default: ({ options }: { options: { title?: { text?: string } } }) => (
    <div data-testid="health-chart">{options?.title?.text}</div>
  ),
}));

const mockHealth = {
  checkedAtUtc: '2026-06-08T10:00:00Z',
  services: [
    { name: 'Database', isConfigured: true, isHealthy: true, status: 'Connected', responseTimeMs: 12.5 },
    { name: 'Redis', isConfigured: false, isHealthy: false, status: 'Not configured', responseTimeMs: null },
  ],
  responseTimeTrend24h: [{ timestampUtc: '2026-06-08T09:00:00Z', value: 20 }],
};

vi.mock('../../../api/adminApi', () => ({
  fetchSystemHealth: vi.fn(() => Promise.resolve(mockHealth)),
}));

import { HealthDashboard } from '../HealthDashboard';

describe('HealthDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders traffic lights and response chart', async () => {
    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByTestId('health-service-Database')).toBeInTheDocument();
    });

    const indicators = screen.getAllByTestId('health-indicator');
    expect(indicators.length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Connected')).toBeInTheDocument();
    expect(screen.getByTestId('health-chart')).toHaveTextContent('Среднее время отклика API');
  });
});
