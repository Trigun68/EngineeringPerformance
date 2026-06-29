import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import type { ColDef, RowClassParams } from 'ag-grid-community';
import { fetchSystemLogs, type SerilogLogEntry } from '../../api/adminApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { getApiErrorMessage } from '../../services/apiError';
import { formatDateTime } from '../../utils/formatDateTime';

const LOG_LEVELS = ['', 'Error', 'Warning', 'Information', 'Debug'] as const;

function logRowClass(params: RowClassParams<SerilogLogEntry>): string | undefined {
  const level = params.data?.level?.toLowerCase();
  if (level === 'error' || level === 'fatal') return 'log-row-error';
  if (level === 'warning') return 'log-row-warning';
  return undefined;
}

export function LogViewer() {
  const [rows, setRows] = useState<SerilogLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [level, setLevel] = useState('');
  const [search, setSearch] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await fetchSystemLogs({
        take: 1000,
        level: level || undefined,
        search: search.trim() || undefined,
      });
      setRows(result.entries);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить логи.'));
    } finally {
      setLoading(false);
    }
  }, [level, search]);

  useEffect(() => {
    void load();
  }, [load]);

  const columnDefs = useMemo<ColDef<SerilogLogEntry>[]>(
    () => [
      {
        field: 'timestampUtc',
        headerName: 'Время (UTC)',
        minWidth: 190,
        valueFormatter: (p) => formatDateTime(p.value),
      },
      { field: 'level', headerName: 'Уровень', maxWidth: 110 },
      { field: 'message', headerName: 'Сообщение', minWidth: 280, flex: 1 },
      { field: 'sourceContext', headerName: 'Источник', minWidth: 180 },
      { field: 'exception', headerName: 'Исключение', minWidth: 200 },
    ],
    [],
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, gap: 2 }}>
      <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
        <TextField
          select
          size="small"
          label="Уровень"
          value={level}
          onChange={(e) => setLevel(e.target.value)}
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">Все</MenuItem>
          {LOG_LEVELS.filter(Boolean).map((item) => (
            <MenuItem key={item} value={item}>{item}</MenuItem>
          ))}
        </TextField>
        <TextField
          size="small"
          label="Поиск"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') void load();
          }}
          sx={{ minWidth: 240 }}
        />
      </Stack>

      {error && <Alert severity="error">{error}</Alert>}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress size={28} />
        </Box>
      ) : (
        <Box
          sx={{
            flex: 1,
            minHeight: 400,
            '& .log-row-error': { backgroundColor: 'rgba(211, 47, 47, 0.12) !important' },
            '& .log-row-warning': { backgroundColor: 'rgba(237, 108, 2, 0.12) !important' },
          }}
        >
          <DataTable
            rowData={rows}
            columnDefs={columnDefs}
            objectName="Administration"
            height="100%"
            pagination
            paginationPageSize={50}
            getRowClass={logRowClass}
            getRowIdValue={(row, index) => `${row.timestampUtc}-${index}`}
            gridTestId="admin-log-viewer-grid"
          />
        </Box>
      )}
    </Box>
  );
}

export default LogViewer;
