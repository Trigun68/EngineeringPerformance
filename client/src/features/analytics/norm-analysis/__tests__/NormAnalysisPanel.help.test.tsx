import { createTheme, ThemeProvider } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { NormAnalysisPanel } from '../NormAnalysisPanel';

vi.mock('../../../../hooks/usePermission', () => ({
  usePermission: () => ({ canRead: true, canEdit: false }),
}));

vi.mock('../../../../api/userPreferencesApi', () => ({
  getUserPreferences: vi.fn(async () => ({})),
  putUserPreferences: vi.fn(async () => undefined),
}));

vi.mock('../../../../api/normRegistryApi', () => ({
  exportNormHealthReport: vi.fn(),
  exportNormRegistry: vi.fn(),
  fetchNormRegistryFilters: vi.fn(async () => ({ groups: [], formulaTypes: [], statuses: [] })),
  fetchNormRegistryHealth: vi.fn(async () => ({
    errorCount: 0,
    warningCount: 0,
    infoCount: 0,
    issues: [],
  })),
  fetchNormRegistryList: vi.fn(async () => ({ items: [], totalCount: 0, page: 1, pageSize: 200 })),
  fetchNormRegistryTimeline: vi.fn(async () => ({ bars: [], groups: [] })),
}));

vi.mock('../../../../api/factNormAnalyticsApi', () => ({
  runFactNormAnalysis: vi.fn(async () => ({ comparisons: [] })),
}));

const theme = createTheme();

function renderPanel(ui: ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  return render(
    <QueryClientProvider client={client}>
      <ThemeProvider theme={theme}>{ui}</ThemeProvider>
    </QueryClientProvider>,
  );
}

describe('NormAnalysisPanel help button', () => {
  it('opens the help dialog from the info icon', async () => {
    renderPanel(<NormAnalysisPanel dateFrom="2026-01-01" dateTo="2026-08-01" />);
    await waitFor(() => {
      expect(screen.getByTestId('norm-analysis-help')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('norm-analysis-help'));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Справка: анализ норм' })).toBeInTheDocument();
    expect(screen.getByTestId('norm-analysis-help-body')).toHaveTextContent('CV');
  });
});
