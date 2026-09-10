import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { WorkCostRow } from '../../../api/registersApi';
import { WorkCostsSetRateDialog } from '../WorkCostsSetRateDialog';

const saveWorkCostsBatch = vi.fn();

vi.mock('../../../api/registersApi', () => ({
  saveWorkCostsBatch: (...args: unknown[]) => saveWorkCostsBatch(...args),
}));

const sampleRows: WorkCostRow[] = [
  {
    id: 1,
    workDirectionId: 10,
    parentId: null,
    sortOrder: 1,
    workDirectionName: 'Группа А',
    ratePerHour: 100,
    effectiveFrom: '2026-01-01',
  },
];

function renderDialog(onSaved = vi.fn(), onClose = vi.fn()) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={client}>
      <WorkCostsSetRateDialog
        open
        rows={sampleRows}
        canEdit
        mode="create"
        onClose={onClose}
        onSaved={onSaved}
      />
    </QueryClientProvider>,
  );
  return { onSaved, onClose };
}

describe('WorkCostsSetRateDialog save mutation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('saves via useMutation and notifies the page once', async () => {
    saveWorkCostsBatch.mockResolvedValue({ writtenCount: 1 });
    const { onSaved, onClose } = renderDialog();

    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => {
      expect(saveWorkCostsBatch).toHaveBeenCalledTimes(1);
    });
    expect(saveWorkCostsBatch).toHaveBeenCalledWith(
      expect.objectContaining({
        items: [{ workDirectionId: 10, ratePerHour: 100 }],
      }),
    );
    await waitFor(() => {
      expect(onSaved).toHaveBeenCalledTimes(1);
      expect(onClose).toHaveBeenCalledTimes(1);
    });
  });

  it('shows getApiErrorMessage on save failure', async () => {
    saveWorkCostsBatch.mockRejectedValue({
      response: { data: { detail: 'Ставка вне диапазона.' } },
    });
    const { onSaved } = renderDialog();

    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByText('Ставка вне диапазона.')).toBeInTheDocument();
    expect(onSaved).not.toHaveBeenCalled();
  });
});
