import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

const mockAudit = [
  {
    id: 1,
    userId: 1,
    userName: 'Администратор',
    timestampUtc: '2026-06-08T09:00:00Z',
    category: 'Access Rights',
    entityType: 'PermissionMatrix',
    entityId: null,
    description: 'Updated permission matrix (3 objects)',
  },
];

vi.mock('../../../api/adminApi', () => ({
  fetchConfigAudit: vi.fn(() => Promise.resolve(mockAudit)),
}));

vi.mock('../../../components/DataTable/DataTable', () => ({
  DataTable: ({ rowData }: { rowData: typeof mockAudit }) => (
    <div data-testid="audit-grid">
      {rowData.map((row) => (
        <div key={row.id}>{row.userName}: {row.description}</div>
      ))}
    </div>
  ),
}));

import { ConfigAudit } from '../ConfigAudit';
import { fetchConfigAudit } from '../../../api/adminApi';

describe('ConfigAudit', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders audit entries', async () => {
    render(<ConfigAudit />);

    await waitFor(() => {
      expect(screen.getByText(/Updated permission matrix/)).toBeInTheDocument();
    });
  });

  it('filters by user client-side', async () => {
    render(<ConfigAudit />);

    await waitFor(() => expect(screen.getByTestId('audit-grid')).toBeInTheDocument());

    fireEvent.change(screen.getByLabelText('Пользователь'), { target: { value: 'Unknown' } });

    await waitFor(() => {
      expect(screen.queryByText(/Updated permission matrix/)).not.toBeInTheDocument();
    });

    expect(fetchConfigAudit).toHaveBeenCalled();
  });
});
