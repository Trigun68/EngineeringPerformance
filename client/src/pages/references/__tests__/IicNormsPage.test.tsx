import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { IicNormsPage } from '../IicNormsPage';

vi.mock('../../../hooks/usePermission', () => ({
  usePermission: () => ({
    canRead: false,
    canEdit: false,
    hasAccess: false,
    hasLevel: () => false,
    level: 0,
    isAdmin: false,
  }),
}));

describe('IicNormsPage', () => {
  it('shows access warning when read is denied', () => {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <MemoryRouter>
          <IicNormsPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );
    expect(screen.getByText('Нет прав на просмотр регистра норм.')).toBeInTheDocument();
  });
});
