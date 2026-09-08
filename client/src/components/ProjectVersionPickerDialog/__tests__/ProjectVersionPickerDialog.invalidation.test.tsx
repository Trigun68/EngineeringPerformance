import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { MockInstance } from 'vitest';
import type { ReactNode } from 'react';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { queryKeys } from '../../../services/queryKeys';
import {
  invalidateAfterVersionMutation,
  ProjectVersionPickerDialog,
} from '../ProjectVersionPickerDialog';

const theme = createTheme();

const fetchProjectVersions = vi.fn();
const createProjectVersion = vi.fn();
const updateProjectVersion = vi.fn();
const deleteProjectVersion = vi.fn();
const copyProjectVersion = vi.fn();
const setPrimaryProjectVersion = vi.fn();

vi.mock('../../../api/projectVersionsApi', () => ({
  fetchProjectVersions: (...args: unknown[]) => fetchProjectVersions(...args),
  createProjectVersion: (...args: unknown[]) => createProjectVersion(...args),
  updateProjectVersion: (...args: unknown[]) => updateProjectVersion(...args),
  deleteProjectVersion: (...args: unknown[]) => deleteProjectVersion(...args),
  copyProjectVersion: (...args: unknown[]) => copyProjectVersion(...args),
  setPrimaryProjectVersion: (...args: unknown[]) => setPrimaryProjectVersion(...args),
}));

vi.mock('../../../store/useDictionaryStore', () => ({
  useDictionaryStore: (sel: (s: { lists: { physicalVolumeTypes: { id: number; name: string }[] } }) => unknown) =>
    sel({ lists: { physicalVolumeTypes: [{ id: 1, name: 'КФО' }] } }),
}));

vi.mock('../../DataTable/DataTable', () => ({
  DataTable: ({
    rowData,
    onRowClicked,
  }: {
    rowData: ProjectVersion[];
    onRowClicked?: (row: ProjectVersion) => void;
  }) => (
    <div data-testid="version-table">
      {rowData.map((row) => (
        <button key={row.id} type="button" onClick={() => onRowClicked?.(row)}>
          {row.versionName}
        </button>
      ))}
    </div>
  ),
}));

vi.mock('../../WorkingOverlay/WorkingOverlay', () => ({
  WorkingOverlay: () => null,
}));

vi.mock('../../../hooks/useConfirmation', () => ({
  useConfirmation: () => ({
    confirm: vi.fn(async () => true),
    Dialog: null,
  }),
}));

const sampleVersion: ProjectVersion = {
  id: 10,
  projectId: 5,
  name: 'Основная',
  versionName: 'Основная',
  description: null,
  direction: 1,
  physicalVolumeTypeId: 1,
  sortOrder: 0,
  isArchived: false,
  createdAt: '2026-01-01T12:00:00Z',
  isPrimary: false,
  rowVersion: 'rv1',
};

function createdVersion(overrides: Partial<ProjectVersion> = {}): ProjectVersion {
  return {
    ...sampleVersion,
    id: 11,
    versionName: 'Новая',
    name: 'Новая',
    isPrimary: true,
    ...overrides,
  };
}

function renderPicker(ui: React.ReactElement, queryClient: QueryClient) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <ThemeProvider theme={theme}>
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    </ThemeProvider>
  );
  return render(ui, { wrapper });
}

function expectWorkspaceInvalidated(spy: MockInstance, projectId: number) {
  expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.projectVersions(projectId) });
  expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.fact.root(projectId) });
  expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.forecast.root(projectId) });
  expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.compare.root(projectId) });
}

describe('invalidateAfterVersionMutation', () => {
  it('invalidates versions list and fact/forecast/compare roots', async () => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const spy = vi.spyOn(client, 'invalidateQueries');
    await invalidateAfterVersionMutation(client, 5);
    expectWorkspaceInvalidated(spy, 5);
  });
});

describe('ProjectVersionPickerDialog cache invalidation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchProjectVersions.mockResolvedValue([]);
  });

  it('invalidates workspace caches after create', async () => {
    const created = createdVersion();
    createProjectVersion.mockResolvedValue(created);
    fetchProjectVersions.mockResolvedValueOnce([]).mockResolvedValue([created]);

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    renderPicker(
      <ProjectVersionPickerDialog
        open
        title="Версии"
        projectId={5}
        onSelect={vi.fn()}
        onClose={vi.fn()}
      />,
      queryClient,
    );

    await waitFor(() => expect(fetchProjectVersions).toHaveBeenCalledWith(5));
    fireEvent.click(screen.getByLabelText('Добавить версию'));
    const title = await screen.findByText('Новая версия анализа');
    const editDialog = title.closest('.MuiDialog-root') ?? title.closest('[role="dialog"]');
    expect(editDialog).toBeTruthy();
    const scope = within(editDialog as HTMLElement);
    fireEvent.change(scope.getAllByRole('textbox')[0], { target: { value: 'Новая' } });
    fireEvent.mouseDown(scope.getByRole('combobox', { name: 'Справочник физобъёмов' }));
    fireEvent.click(await screen.findByRole('option', { name: 'КФО' }));
    fireEvent.click(scope.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(createProjectVersion).toHaveBeenCalled());
    await waitFor(() => expectWorkspaceInvalidated(spy, 5));
  });

  it('invalidates after delete', async () => {
    fetchProjectVersions.mockResolvedValue([sampleVersion]);
    deleteProjectVersion.mockResolvedValue(undefined);

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const spy = vi.spyOn(queryClient, 'invalidateQueries');

    renderPicker(
      <ProjectVersionPickerDialog
        open
        title="Версии"
        projectId={5}
        onSelect={vi.fn()}
        onClose={vi.fn()}
      />,
      queryClient,
    );

    await waitFor(() => expect(fetchProjectVersions).toHaveBeenCalledWith(5));
    await waitFor(() => {
      expect(screen.getByTestId('version-delete-button')).not.toBeDisabled();
    });
    fireEvent.click(screen.getByTestId('version-delete-button'));
    fireEvent.click(await screen.findByTestId('version-delete-confirm-button'));
    await waitFor(() => expect(deleteProjectVersion).toHaveBeenCalledWith(5, 10));
    await waitFor(() => expectWorkspaceInvalidated(spy, 5));
  });
});
