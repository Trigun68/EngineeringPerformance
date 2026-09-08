import { useMemo } from 'react';
import { Box } from '@mui/material';
import type { ColDef, GridApi } from 'ag-grid-community';
import { DictionaryGrid } from '../../components/dictionary/DictionaryGrid';
import { OBJECT_NAME } from '../../constants/accessControlConstants';
import { formatDate } from '../../utils/formatDateTime';
import { formulaTypeLabel } from '../../features/norms/kimCurve';
import type { IicNorm } from '../../api/registersApi';
import type { IicNormTreeRow } from './iicNormTree';

export interface IicNormsGridProps {
  rowData: IicNormTreeRow[];
  loading: boolean;
  canEdit: boolean;
  getUnitLabel: (directionId: number) => string;
  onGridApiReady: (api: GridApi<IicNormTreeRow>) => void;
  onRowClicked: (row: IicNormTreeRow) => void;
  onEdit?: (row: IicNorm) => void;
  onDelete: (row: IicNormTreeRow) => void;
}

export function IicNormsGrid({
  rowData,
  loading,
  canEdit,
  getUnitLabel,
  onGridApiReady,
  onRowClicked,
  onEdit,
  onDelete,
}: IicNormsGridProps) {
  const columnDefs = useMemo<ColDef<IicNormTreeRow>[]>(
    () => [
      { field: 'performance', headerName: 'Производительность', minWidth: 130 },
      {
        headerName: 'Тип формулы',
        minWidth: 140,
        valueGetter: (p) => formulaTypeLabel(p.data?.normFormula?.type),
      },
      {
        colId: 'migration',
        headerName: 'Миграция',
        minWidth: 130,
        valueGetter: (p) => (p.data?.migratedAt ? 'Автоконверт.' : ''),
      },
      { field: 'estimate', headerName: 'μ', minWidth: 100 },
      { field: 'standardDeviation', headerName: 'σ', minWidth: 110 },
      {
        colId: 'unitName',
        headerName: 'Ед. изм.',
        minWidth: 100,
        valueGetter: (p) => p.data?.unitName ?? getUnitLabel(p.data?.workDirectionId ?? 0),
        filterValueGetter: (p) => {
          const id = p.data?.resolvedUnitId ?? p.data?.unitId;
          return id == null ? '' : String(id);
        },
      },
      {
        field: 'effectiveFrom',
        headerName: 'Действует с',
        minWidth: 120,
        valueFormatter: (p) => formatDate(p.value),
        filter: 'agDateColumnFilter',
        filterParams: {
          comparator: (filterDate: Date, cellValue: string) => {
            if (!cellValue) return -1;
            const cellDate = new Date(cellValue);
            if (cellDate < filterDate) return -1;
            if (cellDate > filterDate) return 1;
            return 0;
          },
        },
      },
      {
        field: 'effectiveTo',
        headerName: 'Действует по',
        minWidth: 120,
        valueFormatter: (p) => formatDate(p.value),
        filter: 'agDateColumnFilter',
        filterParams: {
          comparator: (filterDate: Date, cellValue: string) => {
            if (!cellValue) return -1;
            const cellDate = new Date(cellValue);
            if (cellDate < filterDate) return -1;
            if (cellDate > filterDate) return 1;
            return 0;
          },
        },
      },
    ],
    [getUnitLabel],
  );

  const autoGroupColumnDef = useMemo<ColDef<IicNormTreeRow>>(
    () => ({
      headerName: 'Группа',
      minWidth: 320,
      flex: 1,
      cellRendererParams: { suppressCount: true },
      filterValueGetter: (p) => (p.data?.workDirectionId != null ? String(p.data.workDirectionId) : ''),
      valueGetter: (p) => {
        const hierarchy = p.data?.hierarchy;
        if (!hierarchy || hierarchy.length === 0) return '';
        return hierarchy[hierarchy.length - 1];
      },
    }),
    [],
  );

  return (
    <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
      <DictionaryGrid<IicNormTreeRow>
        rowData={rowData}
        columnDefs={columnDefs}
        loading={loading}
        objectName={OBJECT_NAME.IicWorkDirectionNorms}
        gridId="ref-iic-norms"
        treeData
        hierarchyField="hierarchy"
        autoGroupColumnDef={autoGroupColumnDef}
        canEdit={canEdit}
        showExportButton={false}
        onGridApiReady={onGridApiReady}
        onRowClicked={onRowClicked}
        onRowDoubleClicked={canEdit ? onEdit : undefined}
        onEdit={canEdit ? onEdit : undefined}
        onDelete={onDelete}
      />
    </Box>
  );
}
