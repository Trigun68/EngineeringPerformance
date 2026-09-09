import { useMemo } from 'react';
import type { GridApi } from 'ag-grid-community';
import type { VersionCompareDetailRow } from '../../../api/analyticsApi';
import { DataTable } from '../../../components/DataTable/DataTable';
import { createCompareDetailColumns, type CompareScenario } from './compareColumns';

export interface CompareDetailGridProps {
  rowData: VersionCompareDetailRow[];
  /** Длина исходных (не deferred) строк — как в ключе DataTable до выноса. */
  keyRowCount: number;
  projectId: number;
  loading: boolean;
  showOnlyDifferences: boolean;
  compareScenario: CompareScenario;
  onGridApiReady: (api: GridApi) => void;
}

function compareDetailRowId(row: VersionCompareDetailRow): string {
  return `${row.facilityCode}|${row.pvCode}|${row.workId}`;
}

/** Грида «Детально» сравнения: группировка, DataTable. */
export function CompareDetailGrid({
  rowData,
  keyRowCount,
  projectId,
  loading,
  showOnlyDifferences,
  compareScenario,
  onGridApiReady,
}: CompareDetailGridProps) {
  const detailColumns = useMemo(() => createCompareDetailColumns(compareScenario), [compareScenario]);

  return (
    <DataTable<VersionCompareDetailRow>
      key={`compare-detail-${showOnlyDifferences}-${keyRowCount}`}
      rowData={rowData}
      columnDefs={detailColumns}
      objectName="VersionComparison"
      gridId="compare-detail"
      projectId={projectId}
      loading={loading}
      height="100%"
      enableRowGrouping
      showColumnSettings
      getRowIdValue={compareDetailRowId}
      onGridApiReady={onGridApiReady}
    />
  );
}
