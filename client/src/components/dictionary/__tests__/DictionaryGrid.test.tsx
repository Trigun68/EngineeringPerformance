import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ExcelSetColumnFilter } from '../ExcelSetColumnFilter';
import { DictionaryGrid } from '../DictionaryGrid';

const { putUserPreferences, getUserPreferences } = vi.hoisted(() => ({
  putUserPreferences: vi.fn().mockResolvedValue(undefined),
  getUserPreferences: vi.fn().mockResolvedValue({}),
}));

let capturedGridProps: Record<string, unknown> = {};

vi.mock('../../../hooks/usePermission', () => ({
  usePermission: () => ({
    canRead: true,
    hasAccess: true,
    hasLevel: () => true,
    level: 2,
    canEdit: true,
    isAdmin: true,
  }),
}));

vi.mock('../../../api/userPreferencesApi', () => ({
  getUserPreferences,
  putUserPreferences,
}));

vi.mock('ag-grid-react', () => ({
  AgGridReact: (props: Record<string, unknown>) => {
    capturedGridProps = props;
    const rowData = props.rowData as unknown[];
    return (
      <div
        data-testid="dictionary-grid"
        data-rows={rowData.length}
        data-filter={String((props.defaultColDef as { filter?: { name?: string } })?.filter?.name ?? '')}
      />
    );
  },
}));

describe('DictionaryGrid', () => {
  it('uses Excel set column filter in defaultColDef', () => {
    render(
      <DictionaryGrid
        rowData={[{ id: 1, name: 'A' }]}
        columnDefs={[{ colId: 'name', headerName: 'Наименование' }]}
        objectName="PhysicalVolumes"
        gridId="ref-physical-volumes"
      />,
    );

    const defaultColDef = capturedGridProps.defaultColDef as {
      filter?: { name?: string };
      filterParams?: { buttons?: string[]; closeOnApply?: boolean; suppressSorting?: boolean };
    };

    expect(defaultColDef.filter).toBe(ExcelSetColumnFilter);
    expect(defaultColDef.filter?.name).toBe('ExcelSetColumnFilter');
    expect(defaultColDef.filterParams).toMatchObject({
      buttons: ['apply', 'reset'],
      closeOnApply: true,
      suppressSorting: true,
      excelMode: 'windows',
    });
    expect(capturedGridProps.components).toHaveProperty('ExcelSetColumnFilter');
  });

  it('renders grid wrapper with default full height', () => {
    render(
      <DictionaryGrid
        rowData={[{ id: 1 }]}
        columnDefs={[{ colId: 'name', headerName: 'Наименование' }]}
        objectName="PhysicalVolumes"
        gridId="ref-physical-volumes"
      />,
    );

    const grid = screen.getByTestId('dictionary-grid');
    expect(grid).toHaveAttribute('data-rows', '1');
  });

  it('shows tree expand/collapse controls when treeData enabled', () => {
    render(
      <DictionaryGrid
        rowData={[{ id: 1 }]}
        columnDefs={[{ colId: 'name', headerName: 'Наименование' }]}
        objectName="IicWorkDirections"
        gridId="ref-iic-work-directions"
        treeData
      />,
    );

    expect(screen.getByRole('button', { name: 'Развернуть всё' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Свернуть всё' })).toBeInTheDocument();
    expect(capturedGridProps.treeData).toBe(true);
  });
});
