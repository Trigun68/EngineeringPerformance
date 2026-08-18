import { useMemo } from 'react';
import { Box, Chip, Tooltip, Typography } from '@mui/material';
import type { ColDef, ICellRendererParams } from 'ag-grid-community';
import { DataTable } from '../../../components/DataTable/DataTable';
import type { NormHealthIssue, NormListItem } from '../../../api/normRegistryApi';
import { formatDate } from '../../../utils/formatDateTime';
import { formatIisNorm } from '../../../utils/formatIisNorm';

interface Props {
  rows: NormListItem[];
  loading: boolean;
  issues: NormHealthIssue[];
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

function IssuesChip({
  row,
  issues,
}: {
  row: NormListItem;
  issues: NormHealthIssue[];
}) {
  const n = row.issueCount ?? 0;
  if (n <= 0) return <span>—</span>;
  const sev = row.maxIssueSeverity ?? 'Info';
  const related = issues.filter((i) => i.normIds.includes(row.id));
  const tip =
    related.length > 0
      ? related
          .map(
            (i) =>
              `${i.severity}: ${i.message}${i.recommendation ? `\n→ ${i.recommendation}` : ''}`,
          )
          .join('\n\n')
      : `Проблем: ${n} (откройте карточку нормы для деталей)`;

  return (
    <Tooltip title={<Box sx={{ whiteSpace: 'pre-wrap', maxWidth: 360 }}>{tip}</Box>}>
      <Chip
        size="small"
        label={n}
        color={SEVERITY_COLOR[sev] ?? 'default'}
        aria-label={`Проблем: ${n}, макс. ${sev}`}
        data-testid={`norm-issue-chip-${row.id}`}
      />
    </Tooltip>
  );
}

export function NormRegistryTable({ rows, loading, issues, onRowClick }: Props) {
  const columnDefs = useMemo<ColDef<NormListItem>[]>(
    () => [
      { field: 'id', headerName: 'Id', width: 80 },
      {
        field: 'groupName',
        headerName: 'Группа IIC',
        flex: 1.4,
        minWidth: 180,
        valueGetter: (p) =>
          formatIisNorm({
            code: p.data?.groupCode,
            name: p.data?.groupName ?? '',
          }),
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
        cellRenderer: (p: ICellRendererParams<NormListItem>) =>
          p.data ? <IssuesChip row={p.data} issues={issues} /> : null,
      },
    ],
    [issues],
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
    <Box data-testid="norm-analysis-table">
      <DataTable<NormListItem>
        rowData={rows}
        columnDefs={columnDefs}
        loading={loading}
        height={Math.max(320, Math.min(640, 48 + rows.length * 40))}
        getRowIdValue={(r) => String(r.id)}
        onRowClicked={onRowClick}
        objectName="IicWorkDirectionNorms"
        gridTestId="norm-analysis-grid"
      />
    </Box>
  );
}
