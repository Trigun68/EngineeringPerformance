import { useMemo } from 'react';
import type { GridApi } from 'ag-grid-community';
import type { VersionCompareFacilityRow } from '../../../api/analyticsApi';
import { DataTable } from '../../../components/DataTable/DataTable';
import { createCompareFacilityColumns, type CompareScenario } from './compareColumns';
import { buildCompareFacilityPinnedTotal } from './compareFacilityTotals';

export interface CompareFacilityGridProps {
  rowData: VersionCompareFacilityRow[];
  projectId: number;
  loading: boolean;
  showOnlyDifferences: boolean;
  compareScenario: CompareScenario;
  onGridApiReady: (api: GridApi) => void;
}

function compareFacilityRowId(row: VersionCompareFacilityRow): string {
  return `${row.facilityCode}|${row.facilityName}`;
}

/** Грида «По объектам» сравнения: pinned ИТОГО, DataTable. */
export function CompareFacilityGrid({
  rowData,
  projectId,
  loading,
  showOnlyDifferences,
  compareScenario,
  onGridApiReady,
}: CompareFacilityGridProps) {
  const facilityColumns = useMemo(
    () => createCompareFacilityColumns(compareScenario),
    [compareScenario],
  );
  const facilityPinnedBottomRow = useMemo(
    () => buildCompareFacilityPinnedTotal(rowData),
    [rowData],
  );

  return (
    <DataTable<VersionCompareFacilityRow>
      key={`compare-facility-${showOnlyDifferences}-${rowData.length}`}
      rowData={rowData}
      columnDefs={facilityColumns}
      objectName="VersionComparison"
      gridId="compare-facility"
      projectId={projectId}
      loading={loading}
      height="100%"
      showColumnSettings
      pinnedBottomRowData={facilityPinnedBottomRow}
      getRowIdValue={compareFacilityRowId}
      onGridApiReady={onGridApiReady}
    />
  );
}
