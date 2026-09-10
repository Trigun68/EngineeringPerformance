import { useCallback, useMemo } from 'react';
import { Box } from '@mui/material';
import { DataTable } from '../../../components/DataTable/DataTable';
import { createFactFacilityColumns } from '../gridColumns';
import type { ProjectsPageFacilityRow } from '../types';

const TOTAL_ROW_WRAP_SX = {
  height: '100%',
  '& .forecast-summary-total-row': {
    backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
    fontWeight: 600,
  },
} as const;

export interface FactFacilityGridProps {
  rowData: ProjectsPageFacilityRow[];
  projectId: number;
  loading: boolean;
}

/** Грида «Итого по объектам» факта: pinned ИТОГО, DataTable. */
export function FactFacilityGrid({ rowData, projectId, loading }: FactFacilityGridProps) {
  const facilityColumns = useMemo(() => createFactFacilityColumns(), []);

  const facilityPinnedBottomRow = useMemo((): ProjectsPageFacilityRow[] => {
    if (rowData.length === 0) return [];
    const total = rowData.reduce((s, r) => s + (r.laborHours ?? 0), 0);
    const pes = rowData.reduce((s, r) => s + (r.pessimisticHours ?? 0), 0);
    const base = rowData.reduce((s, r) => s + (r.baseHours ?? 0), 0);
    const opt = rowData.reduce((s, r) => s + (r.optimisticHours ?? 0), 0);
    return [{
      facilityCode: '',
      facilityName: 'ИТОГО',
      laborHours: total,
      pessimisticHours: pes,
      baseHours: base,
      optimisticHours: opt,
      zScore: null,
      interpretation: '',
    }];
  }, [rowData]);

  const facilityGetRowClass = useCallback(
    (params: { node?: { rowPinned?: 'top' | 'bottom' | null } }) =>
      params.node?.rowPinned === 'bottom' ? 'forecast-summary-total-row' : undefined,
    [],
  );

  return (
    <Box sx={TOTAL_ROW_WRAP_SX}>
      <DataTable<ProjectsPageFacilityRow>
        rowData={rowData}
        columnDefs={facilityColumns}
        objectName="Assessment"
        gridId="fact-facility"
        projectId={projectId}
        loading={loading}
        height="100%"
        pinnedBottomRowData={facilityPinnedBottomRow}
        getRowClass={facilityGetRowClass}
        showColumnSettings
      />
    </Box>
  );
}
