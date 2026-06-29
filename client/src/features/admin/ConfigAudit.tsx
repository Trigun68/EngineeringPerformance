import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  Stack,
  TextField,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { fetchConfigAudit, type ConfigAuditEntry } from '../../api/adminApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { getApiErrorMessage } from '../../services/apiError';
import { formatDateTime } from '../../utils/formatDateTime';

export function ConfigAudit() {
  const [rows, setRows] = useState<ConfigAuditEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [userFilter, setUserFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [search, setSearch] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchConfigAudit({
        from: dateFrom ? new Date(dateFrom).toISOString() : undefined,
        to: dateTo ? new Date(`${dateTo}T23:59:59`).toISOString() : undefined,
        search: search.trim() || undefined,
        take: 500,
      });

      const filtered = userFilter.trim()
        ? data.filter((row) =>
            row.userName.toLowerCase().includes(userFilter.trim().toLowerCase()))
        : data;

      setRows(filtered);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить аудит настроек.'));
    } finally {
      setLoading(false);
    }
  }, [dateFrom, dateTo, search, userFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  const columnDefs = useMemo<ColDef<ConfigAuditEntry>[]>(
    () => [
      {
        field: 'timestampUtc',
        headerName: 'Время',
        minWidth: 180,
        valueFormatter: (p) => formatDateTime(p.value),
      },
      { field: 'userName', headerName: 'Пользователь', minWidth: 140 },
      { field: 'category', headerName: 'Категория', maxWidth: 140 },
      { field: 'entityType', headerName: 'Сущность', maxWidth: 140 },
      { field: 'entityId', headerName: 'ID', maxWidth: 80 },
      { field: 'description', headerName: 'Описание', minWidth: 260, flex: 1 },
    ],
    [],
  );

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, gap: 2 }}>
      <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
        <TextField
          size="small"
          label="Пользователь"
          value={userFilter}
          onChange={(e) => setUserFilter(e.target.value)}
          sx={{ minWidth: 180 }}
        />
        <TextField
          size="small"
          type="date"
          label="С"
          InputLabelProps={{ shrink: true }}
          value={dateFrom}
          onChange={(e) => setDateFrom(e.target.value)}
        />
        <TextField
          size="small"
          type="date"
          label="По"
          InputLabelProps={{ shrink: true }}
          value={dateTo}
          onChange={(e) => setDateTo(e.target.value)}
        />
        <TextField
          size="small"
          label="Поиск"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ minWidth: 200 }}
        />
      </Stack>

      {error && <Alert severity="error">{error}</Alert>}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress size={28} />
        </Box>
      ) : (
        <Box sx={{ flex: 1, minHeight: 400 }}>
          <DataTable
            rowData={rows}
            columnDefs={columnDefs}
            objectName="Administration"
            height="100%"
            pagination
            paginationPageSize={50}
            gridTestId="admin-config-audit-grid"
          />
        </Box>
      )}
    </Box>
  );
}

export default ConfigAudit;
