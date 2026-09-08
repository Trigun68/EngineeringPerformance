import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { PlanningParametersDialog } from '../PlanningParametersDialog';

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

vi.mock('../../../api/planningParametersApi', () => ({
  fetchPlanningParameters: vi.fn().mockResolvedValue([]),
  savePlanningParameters: vi.fn(),
}));

vi.mock('../../../api/projectVersionsApi', () => ({
  fetchProjectVersions: vi.fn().mockResolvedValue([]),
  updateProjectVersion: vi.fn(),
}));

vi.mock('../../../api/analyticsApi', () => ({
  fetchPerformance: vi.fn().mockResolvedValue([]),
}));

describe('PlanningParametersDialog', () => {
  it('renders dialog testid and title', () => {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <PlanningParametersDialog
          open
          projectId={1}
          versionId={2}
          onClose={() => undefined}
        />
      </QueryClientProvider>,
    );
    expect(screen.getByTestId('planning-parameters-dialog')).toBeInTheDocument();
    expect(screen.getByText('Параметры планирования')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Закрыть' }).length).toBeGreaterThan(0);
  });
});
