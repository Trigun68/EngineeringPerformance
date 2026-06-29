import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

const mockLogs = {
  entries: [
    {
      timestampUtc: '2026-06-08T10:00:00Z',
      level: 'Error',
      message: 'Database timeout',
      exception: 'SqlException',
      sourceContext: 'HealthCheckService',
    },
    {
      timestampUtc: '2026-06-08T10:01:00Z',
      level: 'Information',
      message: 'Request completed',
      exception: null,
      sourceContext: 'RequestMetricsMiddleware',
    },
  ],
  totalCount: 2,
};

vi.mock('../../../api/adminApi', () => ({
  fetchSystemLogs: vi.fn(() => Promise.resolve(mockLogs)),
}));

vi.mock('../../../components/DataTable/DataTable', () => ({
  DataTable: ({ rowData, getRowClass }: {
    rowData: typeof mockLogs.entries;
    getRowClass?: (params: { data?: (typeof mockLogs.entries)[0] }) => string | undefined;
  }) => (
    <div data-testid="log-grid">
      {rowData.map((row, index) => (
        <div
          key={row.timestampUtc}
          data-testid={`log-row-${index}`}
          className={getRowClass?.({ data: row }) ?? ''}
        >
          {row.level}: {row.message}
        </div>
      ))}
    </div>
  ),
}));

import { LogViewer } from '../LogViewer';
import { fetchSystemLogs } from '../../../api/adminApi';

describe('LogViewer', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders logs and applies error row class', async () => {
    render(<LogViewer />);

    await waitFor(() => {
      expect(screen.getByText(/Database timeout/)).toBeInTheDocument();
    });

    expect(screen.getByTestId('log-row-0')).toHaveClass('log-row-error');
    expect(fetchSystemLogs).toHaveBeenCalled();
  });

  it('requests logs with default parameters', async () => {
    render(<LogViewer />);

    await waitFor(() => {
      expect(fetchSystemLogs).toHaveBeenCalledWith(
        expect.objectContaining({ take: 1000 }),
      );
    });
  });
});
