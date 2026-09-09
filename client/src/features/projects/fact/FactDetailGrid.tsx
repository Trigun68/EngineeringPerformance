import { useMemo } from 'react';
import type { GridApi } from 'ag-grid-community';
import { DataTable, type ContextMenuItem } from '../../../components/DataTable/DataTable';
import { createFactDetailColumns } from '../gridColumns';
import type { ProjectsPageDetailRow } from '../types';

export interface FactDetailGridProps {
  rowData: ProjectsPageDetailRow[];
  projectId: number;
  loading: boolean;
  contextMenuItems: ContextMenuItem<ProjectsPageDetailRow>[];
  onGridApiReady: (api: GridApi) => void;
}

function factDetailRowId(row: ProjectsPageDetailRow, index: number): string {
  return row.rowId != null ? `${row.isEquipment ? 'eq' : 'pv'}-${row.rowId}` : `fact-detail-${index}`;
}

/** Грида «Детально» факта: группировка, контекстное меню, DataTable. */
export function FactDetailGrid({
  rowData,
  projectId,
  loading,
  contextMenuItems,
  onGridApiReady,
}: FactDetailGridProps) {
  const detailColumns = useMemo(() => createFactDetailColumns(), []);

  return (
    <DataTable<ProjectsPageDetailRow>
      rowData={rowData}
      columnDefs={detailColumns}
      objectName="Assessment"
      gridId="fact-detail"
      projectId={projectId}
      loading={loading}
      height="100%"
      enableRowGrouping
      showColumnSettings
      contextMenuItems={contextMenuItems}
      onGridApiReady={onGridApiReady}
      getRowIdValue={factDetailRowId}
    />
  );
}
