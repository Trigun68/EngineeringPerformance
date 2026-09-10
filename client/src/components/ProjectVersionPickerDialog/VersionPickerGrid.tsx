import { useMemo } from 'react';
import { Box } from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import type { ProjectVersion } from '../../api/projectVersionsApi';
import { OBJECT_NAME } from '../../constants/accessControlConstants';
import { formatDateTime } from '../../utils/formatDateTime';
import { DataTable } from '../DataTable/DataTable';
import { directionLabel } from './versionPickerModel';

export interface VersionPickerGridProps {
  rowData: ProjectVersion[];
  gridId: string;
  onRowClicked: (row: ProjectVersion) => void;
  onSelectionChanged: (rows: ProjectVersion[]) => void;
  onRowDoubleClicked?: (row: ProjectVersion) => void;
  height?: number;
}

export function VersionPickerGrid({
  rowData,
  gridId,
  onRowClicked,
  onSelectionChanged,
  onRowDoubleClicked,
  height = 320,
}: VersionPickerGridProps) {
  const columnDefs = useMemo<ColDef<ProjectVersion>[]>(
    () => [
      { field: 'versionName', headerName: 'Наименование версии', minWidth: 160, flex: 1 },
      {
        field: 'direction',
        headerName: 'Тип',
        maxWidth: 90,
        valueFormatter: (p) => directionLabel(Number(p.value)),
      },
      {
        field: 'isPrimary',
        headerName: 'Основная',
        maxWidth: 110,
        valueFormatter: (p) => (p.value ? 'Да' : ''),
      },
      { field: 'description', headerName: 'Описание', minWidth: 180, flex: 1 },
      {
        field: 'createdAt',
        headerName: 'Дата создания',
        minWidth: 150,
        valueFormatter: (p) => (p.value ? formatDateTime(p.value) : ''),
      },
    ],
    [],
  );

  return (
    <Box sx={{ height }}>
      <DataTable
        rowData={rowData}
        columnDefs={columnDefs}
        objectName={OBJECT_NAME.ProjectAnalysisVersions}
        gridId={gridId}
        rowSelectionMode="single"
        getRowIdValue={(row) => String(row.id)}
        onRowClicked={onRowClicked}
        onSelectionChanged={onSelectionChanged}
        onRowDoubleClicked={onRowDoubleClicked}
        height="100%"
      />
    </Box>
  );
}
