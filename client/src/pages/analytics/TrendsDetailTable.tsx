import { useMemo } from 'react';
import { Alert, Box, Chip, Stack, Typography } from '@mui/material';
import TimelineIcon from '@mui/icons-material/Timeline';
import type { ColDef } from 'ag-grid-community';
import { DataTable } from '../../components/DataTable/DataTable';
import { interpretationCellStyle } from '../../utils/interpretationStyle';
import type { TrendDetailRow } from '../../api/analyticsApi';

interface TrendsDetailTableProps {
  versionName: string | null;
  rows: TrendDetailRow[];
  loading: boolean;
  error: string | null;
}

export function TrendsDetailTable({ versionName, rows, loading, error }: TrendsDetailTableProps) {
  const columnDefs = useMemo<ColDef<TrendDetailRow>[]>(
    () => [
      { field: 'facilityCode', headerName: 'Код объекта', minWidth: 110 },
      { field: 'facilityName', headerName: 'Объект', minWidth: 180, flex: 1 },
      { field: 'pvCode', headerName: 'Код ПВ', minWidth: 100 },
      { field: 'pvName', headerName: 'Показатель вида работ', minWidth: 200, flex: 1 },
      {
        field: 'factLaborHours',
        headerName: 'Часы факта',
        width: 120,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(0) : '—'),
      },
      {
        field: 'zScore',
        headerName: 'Z',
        width: 90,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(2) : '—'),
      },
      {
        field: 'interpretation',
        headerName: 'Интерпретация',
        minWidth: 170,
        cellStyle: (p) => interpretationCellStyle(p.value),
      },
    ],
    [],
  );

  if (!versionName) {
    return (
      <Alert severity="info" icon={<TimelineIcon fontSize="small" />}>
        Кликните по точке на графике «Факт vs Прогноз/План», чтобы увидеть детализацию по объектам и показателям
        видов работ для выбранной версии.
      </Alert>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, minHeight: 320 }}>
      <Stack direction="row" spacing={1} alignItems="center">
        <Typography sx={{ fontSize: 14, fontWeight: 600, color: '#334155' }}>
          Детализация по версии
        </Typography>
        <Chip size="small" color="primary" variant="outlined" label={versionName} />
      </Stack>
      {error ? (
        <Alert severity="error">{error}</Alert>
      ) : (
        <Box sx={{ flex: 1, minHeight: 280 }}>
          <DataTable
            rowData={rows}
            columnDefs={columnDefs}
            loading={loading}
            objectName="GroupTrend"
            gridId="analytics-trends-detail"
            getRowIdValue={(row, index) => `${row.facilityCode}-${row.pvCode}-${index}`}
          />
        </Box>
      )}
    </Box>
  );
}
