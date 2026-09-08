import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { ProjectVersionPickerDialog } from '../ProjectVersionPickerDialog';

vi.mock('../../../api/projectVersionsApi', () => ({
  fetchProjectVersions: vi.fn().mockResolvedValue([]),
  copyProjectVersion: vi.fn(),
  createProjectVersion: vi.fn(),
  deleteProjectVersion: vi.fn(),
  setPrimaryProjectVersion: vi.fn(),
  updateProjectVersion: vi.fn(),
}));

vi.mock('../../../store/useDictionaryStore', () => ({
  useDictionaryStore: (selector: (s: { lists: { physicalVolumeTypes: unknown[] } }) => unknown) =>
    selector({ lists: { physicalVolumeTypes: [] } }),
}));

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

vi.mock('ag-grid-react', () => ({
  AgGridReact: () => <div data-testid="ag-grid" />,
}));

function wrap(ui: React.ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('ProjectVersionPickerDialog', () => {
  it('renders dialog testid and select button', () => {
    wrap(
      <ProjectVersionPickerDialog
        open
        title="Выбор версии (план)"
        projectId={1}
        onSelect={() => undefined}
        onClose={() => undefined}
      />,
    );
    expect(screen.getByTestId('version-picker-dialog')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Выбрать' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Отмена' })).toBeInTheDocument();
    expect(screen.getByLabelText('Добавить версию')).toBeInTheDocument();
  });
});
