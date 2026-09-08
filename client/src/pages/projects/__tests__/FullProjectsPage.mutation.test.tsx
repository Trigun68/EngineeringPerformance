import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Project } from '../../../types/project';
import { queryKeys } from '../../../services/queryKeys';
import { FullProjectsPage } from '../FullProjectsPage';

const theme = createTheme();

const createProject = vi.fn();
const updateProject = vi.fn();
const fetchProjects = vi.fn();

vi.mock('../../../api/projectsApi', () => ({
  createProject: (...args: unknown[]) => createProject(...args),
  updateProject: (...args: unknown[]) => updateProject(...args),
  fetchProjects: (...args: unknown[]) => fetchProjects(...args),
}));

vi.mock('../../../api/userPreferencesApi', () => ({
  getUserPreferences: vi.fn(async () => ({})),
  putUserPreferences: vi.fn(async () => undefined),
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

vi.mock('../../../features/projects/shared/useProjectWorkspace', () => ({
  useProjectWorkspace: () => ({
    data: null,
    loading: false,
    error: null,
    visibleWorkspaceTabs: [],
  }),
}));

vi.mock('../../../features/projects/forecast/ForecastPanel', () => ({ ForecastPanel: () => null }));
vi.mock('../../../features/projects/fact/FactPanel', () => ({ FactPanel: () => null }));
vi.mock('../../../features/projects/compare/ComparePanel', () => ({ ComparePanel: () => null }));
vi.mock('../../../features/projects/report/ReportPanel', () => ({ ReportPanel: () => null }));
vi.mock('../../../components/ParticipantsModal/ParticipantsModal', () => ({
  ParticipantsModal: () => null,
}));
vi.mock('../../../components/ProjectCharacteristicsDialog/ProjectCharacteristicsDialog', () => ({
  ProjectCharacteristicsDialog: () => null,
}));

vi.mock('../../../components/ProjectSelectionDialog/ProjectSelectionDialog', () => ({
  ProjectSelectionDialog: ({
    open,
    onCreateNew,
  }: {
    open: boolean;
    onCreateNew?: () => void;
  }) => (open ? (
    <button type="button" onClick={() => onCreateNew?.()}>
      Создать проект
    </button>
  ) : null),
}));

vi.mock('../../../features/projects/shared/WorkspaceChromeToggleRow', () => ({
  WorkspaceChromeToggleRow: () => null,
}));

vi.mock('../../../components/WorkingOverlay/PanelBusyBar', () => ({
  PanelBusyBar: () => null,
}));

const created: Project = {
  id: 99,
  code: 'P-99',
  name: 'Новый',
  planVersionCount: 0,
  factVersionCount: 0,
};

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const spy = vi.spyOn(client, 'invalidateQueries');
  const wrapper = ({ children }: { children: ReactNode }) => (
    <ThemeProvider theme={theme}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/projects']}>
          <Routes>
            <Route path="/projects" element={children} />
            <Route path="/projects/:projectId" element={children} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </ThemeProvider>
  );
  render(<FullProjectsPage />, { wrapper });
  return { spy };
}

async function openCreateModal() {
  fireEvent.click(await screen.findByRole('button', { name: 'Выбрать проект' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Создать проект' }));
  const title = await screen.findByText('Создание проекта');
  const dialog = title.closest('.MuiDialog-root') ?? title.closest('[role="dialog"]');
  const scope = dialog ? within(dialog as HTMLElement) : screen;
  const fields = scope.getAllByRole('textbox');
  fireEvent.change(fields[0], { target: { value: 'P-99' } });
  fireEvent.change(fields[1], { target: { value: 'Новый' } });
  return scope;
}

describe('FullProjectsPage create/update mutations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchProjects.mockResolvedValue([]);
  });

  it('invalidates projects.all prefix after successful create', async () => {
    createProject.mockResolvedValue(created);
    const { spy } = renderPage();
    await waitFor(() => expect(fetchProjects).toHaveBeenCalled());
    const scope = await openCreateModal();
    fireEvent.click(scope.getByRole('button', { name: 'Сохранить' }));
    await waitFor(() => expect(createProject).toHaveBeenCalled());
    await waitFor(() => {
      expect(spy).toHaveBeenCalledWith({ queryKey: queryKeys.projects.all() });
    });
    expect(queryKeys.projects.all(true)[0]).toBe('projects');
    expect(queryKeys.projects.all(true)[1]).toBe('all');
    expect(queryKeys.projects.all()[0]).toBe(queryKeys.projects.all(false)[0]);
  });

  it('shows getApiErrorMessage when create fails', async () => {
    createProject.mockRejectedValue({
      response: { data: { detail: 'Код проекта уже занят.' } },
    });
    renderPage();
    await waitFor(() => expect(fetchProjects).toHaveBeenCalled());
    const scope = await openCreateModal();
    fireEvent.click(scope.getByRole('button', { name: 'Сохранить' }));
    expect(await screen.findByTestId('project-save-error')).toHaveTextContent('Код проекта уже занят.');
    expect(createProject).toHaveBeenCalled();
  });
});
