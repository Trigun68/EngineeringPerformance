import { useMemo } from 'react';
import type { GridApi } from 'ag-grid-community';
import { DataTable, type ContextMenuItem } from '../../../components/DataTable/DataTable';
import { createForecastDetailColumns } from '../gridColumns';
import type { ProjectsPageDetailRow } from '../types';

export interface ForecastDetailGridProps {
  rowData: ProjectsPageDetailRow[];
  projectId: number;
  loading: boolean;
  contextMenuItems: ContextMenuItem<ProjectsPageDetailRow>[];
  onGridApiReady: (api: GridApi) => void;
}

function forecastDetailRowId(row: ProjectsPageDetailRow, index: number): string {
  return row.rowId != null ? `${row.isEquipment ? 'eq' : 'pv'}-${row.rowId}` : `detail-${index}`;
}

/** Грида «Детально» прогноза: группировка, контекстное меню, DataTable. */
export function ForecastDetailGrid({
  rowData,
  projectId,
  loading,
  contextMenuItems,
  onGridApiReady,
}: ForecastDetailGridProps) {
  const detailColumns = useMemo(() => createForecastDetailColumns(), []);

  return (
    <DataTable<ProjectsPageDetailRow>
      rowData={rowData}
      columnDefs={detailColumns}
      objectName="Planning"
      gridId="forecast_details_columns"
      projectId={projectId}
      loading={loading}
      height="100%"
      enableRowGrouping
      showColumnSettings
      contextMenuItems={contextMenuItems}
      onGridApiReady={onGridApiReady}
      getRowIdValue={forecastDetailRowId}
    />
  );
}
