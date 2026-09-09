import { useCallback, useMemo } from 'react';
import { Box } from '@mui/material';
import { DataTable } from '../../../components/DataTable/DataTable';
import { createForecastSummaryColumns } from '../gridColumns';
import type { ProjectsPageForecastSummaryRow } from '../types';

const TOTAL_ROW_WRAP_SX = {
  height: '100%',
  '& .forecast-summary-total-row': {
    backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
    fontWeight: 600,
  },
} as const;

export interface ForecastSummaryGridProps {
  rowData: ProjectsPageForecastSummaryRow[];
  projectId: number;
  loading: boolean;
}

function summaryRowId(row: ProjectsPageForecastSummaryRow, index: number): string {
  return `${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level}-${index}`;
}

/** Грида «Итого» прогноза: колонки, дерево, DataTable. */
export function ForecastSummaryGrid({ rowData, projectId, loading }: ForecastSummaryGridProps) {
  const summaryColumns = useMemo(() => createForecastSummaryColumns(), []);

  const summaryAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Направление',
      minWidth: 260,
      flex: 1,
      field: 'displayName' as const,
      cellRendererParams: { suppressCount: true },
    }),
    [],
  );

  const summaryGetRowClass = useCallback(
    (params: { data?: ProjectsPageForecastSummaryRow }) =>
      params.data?.isTotal ? 'forecast-summary-total-row' : undefined,
    [],
  );

  return (
    <Box sx={TOTAL_ROW_WRAP_SX}>
      <DataTable<ProjectsPageForecastSummaryRow>
        rowData={rowData}
        columnDefs={summaryColumns}
        objectName="Planning"
        gridId="forecast-summary"
        projectId={projectId}
        loading={loading}
        height="100%"
        treeData
        getDataPath={(row) => row.hierarchy ?? [row.displayName]}
        autoGroupColumnDef={summaryAutoGroupColumnDef}
        getRowClass={summaryGetRowClass}
        getRowIdValue={summaryRowId}
      />
    </Box>
  );
}
