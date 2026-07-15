import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import gridFixture from '../../../../tests/fixtures/grid-data.json';
import { createForecastSummaryColumns } from '../gridColumns';
import { ProjectsPage } from '../ProjectsPage';

const fetchProjectsPageData = vi.fn();
const fetchProjectVersions = vi.fn();
const exportProjectsPagePdf = vi.fn();
const exportProjectsPageExcel = vi.fn();

vi.mock('../../../api/projectsPageApi', () => ({
  fetchProjectsPageData: (...args: unknown[]) => fetchProjectsPageData(...args),
  exportProjectsPagePdf: (...args: unknown[]) => exportProjectsPagePdf(...args),
  exportProjectsPageExcel: (...args: unknown[]) => exportProjectsPageExcel(...args),
}));

vi.mock('../../../api/projectVersionsApi', () => ({
  fetchProjectVersions: (...args: unknown[]) => fetchProjectVersions(...args),
}));

const usePermissionMock = vi.fn();
vi.mock('../../../hooks/usePermission', () => ({
  usePermission: (name: string) => usePermissionMock(name),
}));

const dictionaryState = {
  getUnitName: (id: number | null | undefined) => (id === 1 ? 'шт' : String(id ?? '')),
  getDirectionName: (id: number | null | undefined) => (id === 1 ? 'Направление А' : String(id ?? '')),
};

vi.mock('../../../store/useDictionaryStore', () => ({
  useDictionaryStore: (selector: (s: typeof dictionaryState) => unknown) => selector(dictionaryState),
}));

vi.mock('../../../components/DataTable/DataTable', () => ({
  DataTable: ({ rowData, loading }: { rowData: unknown[]; loading?: boolean }) => (
    <div
      data-testid="data-table"
      data-loading={loading ? 'true' : 'false'}
      data-rows={rowData.length}
    />
  ),
}));

function renderPage(role: 'manager' | 'observer') {
  usePermissionMock.mockImplementation((objectName: string) => {
    if (role === 'observer') {
      return {
        canRead: true,
        hasAccess: false,
        hasLevel: (level: number) => level <= 1,
        level: 1,
        canEdit: false,
        isAdmin: false,
      };
    }
    const editObjects = new Set(['Projects', 'ProjectDataUpload', 'ProjectAnalysisVersions']);
    const canEdit = editObjects.has(objectName);
    return {
      canRead: true,
      hasAccess: canEdit,
      hasLevel: (level: number) => level <= 2,
      level: canEdit ? 2 : 1,
      canEdit,
      isAdmin: false,
    };
  });

  return render(
    <MemoryRouter initialEntries={['/projects/1/grids?versionId=1']}>
      <Routes>
        <Route path="/projects/:projectId/grids" element={<ProjectsPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('ProjectsPage grids', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchProjectVersions.mockResolvedValue([
      { id: 1, versionName: 'v1', name: 'Версия 1', isArchived: false },
      { id: 2, versionName: 'v2', name: 'Версия 2', isArchived: false },
    ]);
    fetchProjectsPageData.mockResolvedValue(gridFixture);
    exportProjectsPagePdf.mockResolvedValue(undefined);
    exportProjectsPageExcel.mockResolvedValue(undefined);
  });

  it('shows skeleton while loading then renders grid', async () => {
    fetchProjectsPageData.mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve(gridFixture), 50)),
    );

    renderPage('manager');

    expect(screen.getByText(/данные проекта/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId('data-table')).toHaveAttribute('data-rows', '1');
    });
  });

  it('creates WPF-style forecast summary columns', () => {
    const cols = createForecastSummaryColumns();
    expect(cols.find((c) => c.field === 'factPhysicalVolume')).toBeDefined();
    expect(cols.find((c) => c.field === 'baseHours')).toBeDefined();
    expect(cols.find((c) => c.field === 'normUnitName')).toBeUndefined();
  });

  it('renders export toolbar buttons and calls export API', async () => {
    renderPage('manager');

    await waitFor(() => {
      expect(screen.getByTestId('data-table')).toBeInTheDocument();
    });

    const excelButton = screen.getByRole('button', { name: 'Экспорт Excel' });
    const pdfButton = screen.getByRole('button', { name: 'Экспорт PDF' });
    expect(excelButton).toBeInTheDocument();
    expect(pdfButton).toBeInTheDocument();

    // versionId выставляется после загрузки списка версий — кнопки disabled до этого
    await waitFor(() => {
      expect(excelButton).not.toBeDisabled();
    });

    fireEvent.click(excelButton);
    await waitFor(() => {
      expect(exportProjectsPageExcel).toHaveBeenCalledWith(1, 1, null);
    });

    fireEvent.click(pdfButton);
    await waitFor(() => {
      expect(exportProjectsPagePdf).toHaveBeenCalledWith(1, 1, null);
    });
  });

  it('hides privileged context menu items for observer', async () => {
    const { buildProjectsPageContextMenu } = await import('../buildContextMenu');
    const items = buildProjectsPageContextMenu(
      {
        canEditData: false,
        canCopyVersion: false,
        canImport: false,
        canExport: true,
        canDelete: false,
      },
      {
        onEdit: vi.fn(),
        onCopyVersion: vi.fn(),
        onImport: vi.fn(),
        onExportExcel: vi.fn(),
        onExportPdf: vi.fn(),
        onDelete: vi.fn(),
      },
    );

    const names = items.map((i) => i.name);
    expect(names).not.toContain('Редактировать');
    expect(names).not.toContain('Импорт Excel');
    expect(names).not.toContain('Удалить');
    expect(names).toContain('Экспорт Excel');
  });
});
