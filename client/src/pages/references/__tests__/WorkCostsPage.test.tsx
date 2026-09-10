import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { WorkCostsPage } from '../WorkCostsPage';

const deleteWorkCostPeriod = vi.fn();
const fetchWorkCosts = vi.fn();
const fetchWorkCostPeriods = vi.fn();
const fetchWorkCostPeriod = vi.fn();

vi.mock('../../../hooks/usePermission', () => ({
  usePermission: () => ({
    canRead: true,
    canEdit: true,
    hasAccess: true,
    hasLevel: () => true,
    level: 2,
    isAdmin: true,
  }),
}));

vi.mock('../../../components/dictionary/DictionaryGrid', () => ({
  DictionaryGrid: () => <div data-testid="work-costs-grid" />,
}));

vi.mock('../../../api/registersApi', () => ({
  fetchWorkCosts: (...args: unknown[]) => fetchWorkCosts(...args),
  fetchWorkCostPeriods: (...args: unknown[]) => fetchWorkCostPeriods(...args),
  fetchWorkCostPeriod: (...args: unknown[]) => fetchWorkCostPeriod(...args),
  deleteWorkCostPeriod: (...args: unknown[]) => deleteWorkCostPeriod(...args),
  exportWorkCostsExcelFromGrid: vi.fn(),
  saveWorkCostsBatch: vi.fn(),
}));

const periodFrom = '2026-01-01';

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <WorkCostsPage />
    </QueryClientProvider>,
  );
}

async function selectPeriodAndDelete() {
    await waitFor(() => {
      expect(fetchWorkCostPeriods).toHaveBeenCalled();
    });
    fireEvent.click(await screen.findByTestId(`work-cost-period-${periodFrom}`));
  fireEvent.click(screen.getByRole('button', { name: 'Удалить' }));
  const confirm = await screen.findByTestId('confirm-dialog');
  fireEvent.click(within(confirm).getByRole('button', { name: 'Удалить' }));
}

describe('WorkCostsPage delete mutation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchWorkCosts.mockResolvedValue([]);
    fetchWorkCostPeriod.mockResolvedValue([]);
    fetchWorkCostPeriods.mockResolvedValue([
      {
        effectiveFrom: periodFrom,
        effectiveTo: null,
        effectiveToVaries: false,
        directionCount: 3,
        uniformRate: 100,
      },
    ]);
  });

  it('deletes the selected period and invalidates work-costs queries', async () => {
    deleteWorkCostPeriod.mockResolvedValue({ deletedCount: 3 });
    renderPage();

    await selectPeriodAndDelete();

    await waitFor(() => {
      expect(deleteWorkCostPeriod).toHaveBeenCalledWith(periodFrom);
    });
    await waitFor(() => {
      expect(screen.queryByText(`Период с ${periodFrom}`)).not.toBeInTheDocument();
    });
  });

  it('shows getApiErrorMessage when delete fails', async () => {
    deleteWorkCostPeriod.mockRejectedValue({
      response: { data: { detail: 'Период нельзя удалить.' } },
    });
    renderPage();

    await selectPeriodAndDelete();

    expect(await screen.findByText('Период нельзя удалить.')).toBeInTheDocument();
    expect(screen.getByText(`Период с ${periodFrom}`)).toBeInTheDocument();
  });
});
