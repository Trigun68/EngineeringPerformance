import { createTheme, ThemeProvider } from '@mui/material';
import type React from 'react';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

const { updatePermissionMatrix, putUserPreference, getUserPreferences } = vi.hoisted(() => ({
  updatePermissionMatrix: vi.fn().mockResolvedValue(undefined),
  putUserPreference: vi.fn().mockResolvedValue(undefined),
  getUserPreferences: vi.fn().mockResolvedValue({}),
}));

import { PermissionMatrixGrid } from '../PermissionMatrixGrid';
import { INDENT_PX_PER_LEVEL, PROJECTS_GROUP_ID } from '../../../constants/permissionObjectHierarchy';

vi.mock('../../../api/permissionsMatrixApi', () => ({
  updatePermissionMatrix,
}));

vi.mock('../../../api/userPreferencesApi', () => ({
  putUserPreference,
  getUserPreferences,
}));

const mockMatrix = {
  objects: ['Projects', 'Assessment', 'References', 'Users', 'UserActionLog'],
  objectHierarchy: [],
  roles: [
    { id: 1, name: 'SuperAdmin' },
    { id: 2, name: 'Admin' },
    { id: 3, name: 'Engineer' },
    { id: 4, name: 'Gip' },
  ],
  matrix: {
    Projects: { '1': 'Edit', '2': 'Edit', '3': 'Read' },
    Assessment: { '1': 'Edit', '2': 'Read', '3': 'Read' },
    References: { '1': 'Edit', '2': 'Read', '3': 'None' },
    Users: { '1': 'Edit', '2': 'Edit', '3': 'Read' },
    UserActionLog: { '1': 'Edit', '2': 'Read', '3': 'Read' },
  },
};

const fetchMatrix = vi.fn();

vi.mock('../../../store/useAuthStore', () => ({
  useAuthStore: Object.assign(
    (selector: (state: { user: { id: number; roleName: string }; refreshUser: () => Promise<void> }) => unknown) =>
      selector({ user: { id: 1, roleName: 'SuperAdmin' }, refreshUser: vi.fn().mockResolvedValue(undefined) }),
    {
      getState: () => ({
        refreshUser: vi.fn().mockResolvedValue(undefined),
      }),
    },
  ),
}));

vi.mock('../../../store/usePermissionMatrixStore', () => ({
  usePermissionMatrixStore: (selector: (state: Record<string, unknown>) => unknown) =>
    selector({
      matrix: mockMatrix,
      loading: false,
      error: null,
      fetchMatrix,
    }),
}));

vi.mock('../../../constants/accessControlConstants', () => ({
  toRussianObjectName: (name: string) => name,
}));

vi.mock('ag-grid-react', () => ({
  AgGridReact: ({
    rowData,
    columnDefs,
    onGridReady,
  }: {
    rowData: Array<{
      id: string;
      objectName: string;
      isHeader: boolean;
      level: number;
      levels: Record<string, string>;
    }>;
    columnDefs: Array<{
      headerName?: string;
      colId?: string;
      cellRenderer?: (p: { data: unknown }) => unknown;
    }>;
    onGridReady?: (event: { api: { setFilterModel: () => void } }) => void;
  }) => {
    if (onGridReady) {
      onGridReady({ api: { setFilterModel: vi.fn() } });
    }
    const objectCol = columnDefs.find((c) => c.colId === 'objectName');
    const adminCol = columnDefs.find((c) => c.colId === 'role_1');
    const managerCol = columnDefs.find((c) => c.colId === 'role_2');
    return (
      <div data-testid="permission-matrix">
        <div data-testid="object-column-header">{objectCol?.headerName}</div>
        <div data-testid="role-headers">
          {columnDefs.filter((c) => c.colId?.startsWith('role_')).map((c) => c.headerName).join(',')}
        </div>
        {rowData.map((row) => (
          <div key={row.id} data-testid={`row-${row.objectName}`}>
            {objectCol?.cellRenderer?.({ data: row }) as React.ReactNode}
            {!row.isHeader && (
              <>
                {adminCol?.cellRenderer?.({ data: row }) as React.ReactNode}
                {managerCol?.cellRenderer?.({ data: row }) as React.ReactNode}
              </>
            )}
          </div>
        ))}
      </div>
    );
  },
}));

const theme = createTheme();

describe('PermissionMatrixGrid', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders object column and role headers', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    expect(await screen.findByTestId('permission-matrix')).toBeInTheDocument();
    expect(screen.getByTestId('object-column-header')).toHaveTextContent('Объект / Роль');
    expect(screen.getByTestId('role-headers')).toHaveTextContent('Суперадминистратор,Администратор,Инженер,Главный инженер проекта');
  });

  it('applies indent for child rows by level', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    await screen.findByTestId('permission-matrix');
    const assessmentCell = screen.getByTestId(`object-cell-Assessment`);
    expect(assessmentCell).toHaveAttribute('data-indent-level', '1');
    expect(assessmentCell).toHaveStyle({ paddingLeft: `${INDENT_PX_PER_LEVEL}px` });
  });

  it('locks administrator cells to editing label', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    await screen.findByTestId('permission-matrix');
    expect(screen.getByTestId('admin-locked-Assessment-1')).toHaveTextContent('Редактирование');
    const row = screen.getByTestId(`row-${PROJECTS_GROUP_ID}`);
    expect(within(row).queryByRole('combobox')).not.toBeInTheDocument();
  });

  it('shows dropdown only for manager role', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    const row = await screen.findByTestId('row-Assessment');
    expect(within(row).getAllByRole('combobox')).toHaveLength(1);
    expect(screen.getByTestId('role-select-Assessment-2')).toBeInTheDocument();
  });

  it('shows all three access levels in manager dropdown', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    const row = await screen.findByTestId('row-Projects');
    const combobox = within(row).getByRole('combobox');
    fireEvent.mouseDown(combobox);

    expect(await screen.findByRole('option', { name: 'Нет доступа' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Чтение' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Редактирование' })).toBeInTheDocument();
  });

  it('calls PUT API with None when no access selected', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    const row = await screen.findByTestId('row-Assessment');
    const combobox = within(row).getByRole('combobox');
    fireEvent.mouseDown(combobox);
    const option = await screen.findByRole('option', { name: 'Нет доступа' });
    fireEvent.click(option);

    await waitFor(() =>
      expect(updatePermissionMatrix).toHaveBeenCalledWith({
        Assessment: { '2': 'None' },
      }),
    );
  });

  it('calls PUT API when manager access level changes', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    const row = await screen.findByTestId('row-Projects');
    const combobox = within(row).getByRole('combobox');
    fireEvent.mouseDown(combobox);
    const option = await screen.findByRole('option', { name: 'Чтение' });
    fireEvent.click(option);

    await waitFor(() =>
      expect(updatePermissionMatrix).toHaveBeenCalledWith({
        Projects: { '2': 'Read' },
      }),
    );
  });

  it('renders Settings and UserActionLog rows', async () => {
    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    await screen.findByTestId('permission-matrix');
    expect(screen.getByTestId('object-cell-SettingsPage')).toBeInTheDocument();
    expect(screen.getByTestId('object-cell-Users')).toBeInTheDocument();
    expect(screen.getByTestId('object-cell-UserActionLog')).toBeInTheDocument();
    expect(screen.getByTestId('role-select-UserActionLog-2')).toBeInTheDocument();
  });

  it('loads saved filter preferences on mount', async () => {
    getUserPreferences.mockResolvedValue({
      'grid.settings-access-rights.filter': JSON.stringify({ roleName: { filterType: 'text' } }),
    });

    render(
      <ThemeProvider theme={theme}>
        <PermissionMatrixGrid />
      </ThemeProvider>,
    );

    await waitFor(() => expect(getUserPreferences).toHaveBeenCalled());
  });
});
