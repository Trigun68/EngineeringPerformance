import { useMemo } from 'react';
import { Box, Chip, Typography } from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { DataTable } from '../../../components/DataTable/DataTable';
import type { NormListItem } from '../../../api/normRegistryApi';
import { formatDate } from '../../../utils/formatDateTime';

interface Props {
  rows: NormListItem[];
  loading: boolean;
  onRowClick: (row: NormListItem) => void;
}

const STATUS_RU: Record<string, string> = {
  Active: 'Активна',
  Historical: 'Историческая',
  Future: 'Будущая',
};

const SEVERITY_COLOR: Record<string, 'error' | 'warning' | 'info' | 'default'> = {
  Error: 'error',
  Warning: 'warning',
  Info: 'info',
};

export function NormRegistryTable({ rows, loading, onRowClick }: Props) {
  const columnDefs = useMemo<ColDef<NormListItem>[]>(
    () => [
      { field: 'id', headerName: 'Id', width: 80 },
      {
        field: 'groupName',
        headerName: 'Группа',
        flex: 1.4,
        minWidth: 180,
        valueGetter: (p) =>
          p.data?.groupCode ? `${p.data.groupCode} — ${p.data.groupName}` : p.data?.groupName,
      },
      { field: 'formulaType', headerName: 'Формула', width: 120 },
      { field: 'formulaDisplay', headerName: 'Выражение', flex: 1, minWidth: 140 },
      {
        field: 'meanPerformance',
        headerName: 'μ',
        width: 90,
        valueFormatter: (p) => (p.value == null ? '—' : Number(p.value).toFixed(2)),
      },
      {
        field: 'stdDeviation',
        headerName: 'σ',
        width: 90,
        valueFormatter: (p) => (p.value == null ? '—' : Number(p.value).toFixed(4)),
      },
      {
        field: 'effectiveFrom',
        headerName: 'С',
        width: 110,
        valueFormatter: (p) => (p.value ? formatDate(String(p.value)) : '—'),
      },
      {
        field: 'effectiveTo',
        headerName: 'По',
        width: 110,
        valueFormatter: (p) => (p.value ? formatDate(String(p.value)) : '∞'),
      },
      {
        field: 'status',
        headerName: 'Статус',
        width: 120,
        cellRenderer: (p: { value?: string }) => STATUS_RU[p.value ?? ''] ?? p.value ?? '—',
      },
      { field: 'totalVersions', headerName: 'Версий', width: 90 },
      {
        field: 'issueCount',
        headerName: 'Проблемы',
        width: 120,
        cellRenderer: (p: { data?: NormListItem }) => {
          const n = p.data?.issueCount ?? 0;
          if (n <= 0) return '—';
          const sev = p.data?.maxIssueSeverity ?? 'Info';
          return (
            <Chip
              size="small"
              label={n}
              color={SEVERITY_COLOR[sev] ?? 'default'}
              aria-label={`Проблем: ${n}, макс. ${sev}`}
            />
          );
        },
      },
    ],
    [],
  );

  if (!loading && rows.length === 0) {
    return (
      <Box
        sx={{ p: 3, textAlign: 'center', border: '1px dashed', borderColor: 'divider', borderRadius: 1 }}
        data-testid="norm-analysis-empty"
      >
        <Typography color="text.secondary">Нет норм по текущим фильтрам</Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ height: 420 }} data-testid="norm-analysis-table">
      <DataTable<NormListItem>
        rowData={rows}
        columnDefs={columnDefs}
        loading={loading}
        getRowIdValue={(r) => String(r.id)}
        onRowClicked={onRowClick}
        objectName="IicWorkDirectionNorms"
        gridTestId="norm-analysis-grid"
      />
    </Box>
  );
}
