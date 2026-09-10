import { useCallback, useMemo } from 'react';
import type { ColDef, GridApi } from 'ag-grid-community';
import { Box } from '@mui/material';
import { DataTable } from '../../../components/DataTable/DataTable';
import { createCompareSummaryColumns, type CompareScenario } from './compareColumns';
import { type CompareTreeRow } from './compareTree';

const TOTAL_ROW_WRAP_SX = {
  height: '100%',
  '& .forecast-summary-total-row': {
    backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
    fontWeight: 600,
  },
} as const;

export interface CompareSummaryGridProps {
  rowData: CompareTreeRow[];
  projectId: number;
  loading: boolean;
  showOnlyDifferences: boolean;
  compareScenario: CompareScenario;
  onGridApiReady: (api: GridApi) => void;
}

function compareSummaryRowId(row: CompareTreeRow, index: number): string {
  return `cmp-${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level ?? 0}-${index}`;
}

/** Грида «Итого» сравнения: дерево направлений, DataTable. */
export function CompareSummaryGrid({
  rowData,
  projectId,
  loading,
  showOnlyDifferences,
  compareScenario,
  onGridApiReady,
}: CompareSummaryGridProps) {
  const summaryAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Направление',
      minWidth: 260,
      flex: 1,
      field: 'name' as const,
      cellRendererParams: { suppressCount: true },
    }),
    [],
  );

  const summaryColumns = useMemo(
    () => createCompareSummaryColumns(compareScenario) as unknown as ColDef<CompareTreeRow>[],
    [compareScenario],
  );

  const summaryGetRowClass = useCallback(
    (params: { data?: CompareTreeRow }) =>
      params.data?.isTotal ? 'forecast-summary-total-row' : undefined,
    [],
  );

  return (
    <Box sx={TOTAL_ROW_WRAP_SX}>
      <DataTable<CompareTreeRow>
        key={`compare-summary-${showOnlyDifferences}-${rowData.length}`}
        rowData={rowData}
        columnDefs={summaryColumns}
        objectName="VersionComparison"
        gridId="compare-summary"
        projectId={projectId}
        loading={loading}
        height="100%"
        treeData
        getDataPath={(row) => row.hierarchy}
        autoGroupColumnDef={summaryAutoGroupColumnDef}
        showColumnSettings
        getRowIdValue={compareSummaryRowId}
        getRowClass={summaryGetRowClass}
        onGridApiReady={onGridApiReady}
      />
    </Box>
  );
}
