import { useCallback, useMemo } from 'react';
import type { ColDef } from 'ag-grid-community';
import { Box } from '@mui/material';
import { DataTable } from '../../../components/DataTable/DataTable';
import { createFactPerformanceColumns } from '../gridColumns';
import { type PerformanceTreeRow } from './performanceTree';

const TOTAL_ROW_WRAP_SX = {
  height: '100%',
  '& .forecast-summary-total-row': {
    backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
    fontWeight: 600,
  },
} as const;

export interface FactPerformanceGridProps {
  rowData: PerformanceTreeRow[];
  projectId: number;
  loading: boolean;
}

function performanceRowId(row: PerformanceTreeRow, index: number): string {
  return `perf-${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level}-${index}`;
}

/** Грида «Итого» факта: дерево производительности, DataTable. */
export function FactPerformanceGrid({ rowData, projectId, loading }: FactPerformanceGridProps) {
  const performanceColumns = useMemo(
    () => createFactPerformanceColumns() as unknown as ColDef<PerformanceTreeRow>[],
    [],
  );

  const performanceAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Наименование',
      minWidth: 260,
      flex: 1,
      field: 'workDirectionName' as const,
      cellRendererParams: { suppressCount: true },
    }),
    [],
  );

  const performanceGetRowClass = useCallback(
    (params: { data?: PerformanceTreeRow }) =>
      params.data?.isTotal ? 'forecast-summary-total-row' : undefined,
    [],
  );

  return (
    <Box sx={TOTAL_ROW_WRAP_SX}>
      <DataTable<PerformanceTreeRow>
        rowData={rowData}
        columnDefs={performanceColumns}
        objectName="Assessment"
        gridId="fact-performance"
        projectId={projectId}
        loading={loading}
        height="100%"
        treeData
        getDataPath={(row) => row.hierarchy}
        autoGroupColumnDef={performanceAutoGroupColumnDef}
        getRowIdValue={performanceRowId}
        getRowClass={performanceGetRowClass}
        showColumnSettings
      />
    </Box>
  );
}
