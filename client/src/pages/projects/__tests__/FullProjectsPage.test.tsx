import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { FullProjectsPage } from '../FullProjectsPage';

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

vi.mock('../../../features/projects/shared/useProjectWorkspace', () => ({
  useProjectWorkspace: () => ({
    data: null,
    loading: false,
    error: null,
    visibleWorkspaceTabs: [],
  }),
}));

vi.mock('../../../api/userPreferencesApi', () => ({
  getUserPreferences: vi.fn(async () => ({})),
  putUserPreferences: vi.fn(async () => undefined),
}));

describe('FullProjectsPage', () => {
  it('shows access warning when read is denied', () => {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/projects/1']}>
          <Routes>
            <Route path="/projects/:projectId" element={<FullProjectsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );
    expect(screen.getByText('Нет прав на просмотр проектов.')).toBeInTheDocument();
  });
});
