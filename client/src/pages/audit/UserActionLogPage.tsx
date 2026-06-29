import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  FormControlLabel,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import {
  archiveUserActionLogs,
  exportUserActionLogsExcel,
  fetchUserActionLogs,
  restoreUserActionLogs,
  type UserActionLogEntry,
} from '../../api/auditApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { usePermission } from '../../hooks/usePermission';
import { useAuthStore } from '../../store/useAuthStore';
import { formatDateTime } from '../../utils/formatDateTime';

export function UserActionLogPage() {
  const { canRead } = usePermission('UserActionLog');
  const isAdmin = useAuthStore((s) => s.user?.roleName === 'Administrator');
  const [rows, setRows] = useState<UserActionLogEntry[]>([]);
  const [selected, setSelected] = useState<UserActionLogEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [showArchived, setShowArchived] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const load = useCallback(async () => {
    if (!canRead) return;
    setLoading(true);
    setError(null);
    try {
      setRows(await fetchUserActionLogs({ take: 500, includeArchived: showArchived }));
      setSelected([]);
    } catch {
      setError('Не удалось загрузить журнал действий.');
    } finally {
      setLoading(false);
    }
  }, [canRead, showArchived]);

  useEffect(() => {
    void load();
  }, [load]);

  const columnDefs = useMemo<ColDef<UserActionLogEntry>[]>(
    () => [
      {
        field: 'timestamp',
        headerName: 'Время',
        minWidth: 180,
        valueFormatter: (p) => formatDateTime(p.value),
      },
      { field: 'userName', headerName: 'Пользователь', minWidth: 140 },
      { field: 'actionType', headerName: 'Действие', maxWidth: 120 },
      { field: 'entityType', headerName: 'Сущность', maxWidth: 120 },
      { field: 'entityId', headerName: 'ID', maxWidth: 80 },
      { field: 'description', headerName: 'Описание', minWidth: 200 },
      { field: 'ipOrHost', headerName: 'Хост', maxWidth: 120 },
      {
        field: 'isArchived',
        headerName: 'Архив',
        maxWidth: 80,
        valueFormatter: (p) => (p.value ? 'Да' : 'Нет'),
      },
    ],
    [],
  );

  const handleArchiveSelected = async () => {
    if (selected.length === 0) return;
    if (!window.confirm(`Перенести в архив ${selected.length} записей?`)) return;
    setError(null);
    try {
      await archiveUserActionLogs(selected.map((row) => row.id));
      setStatus(`В архив перенесено записей: ${selected.length}.`);
      await load();
    } catch {
      setError('Не удалось архивировать выбранные записи.');
    }
  };

  const handleRestoreFile = async (file: File) => {
    setError(null);
    try {
      const result = await restoreUserActionLogs(file);
      setStatus(
        `Восстановлено: ${result.importedCount}, дубли: ${result.skippedDuplicates}, пропущено: ${result.skippedInvalid + result.skippedOrphans}.`,
      );
      await load();
    } catch {
      setError('Не удалось восстановить журнал из файла.');
    }
  };

  const handleExport = async () => {
    setError(null);
    try {
      await exportUserActionLogsExcel({ take: 500, includeArchived: showArchived });
      setStatus('Журнал выгружен в Excel.');
    } catch {
      setError('Не удалось выгрузить журнал в Excel.');
    }
  };

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр журнала.</Alert>;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', gap: 0, minHeight: 480 }}>
      <Typography variant="h5" sx={{ mb: 1, flexShrink: 0 }}>
        Журнал действий пользователей
      </Typography>

      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1, flexShrink: 0, flexWrap: 'wrap' }}>
        {isAdmin && (
          <>
            <Tooltip title="Архивировать выбранные записи">
              <span>
                <Button
                  size="small"
                  variant="outlined"
                  disabled={selected.length === 0}
                  onClick={() => void handleArchiveSelected()}
                >
                  В архив
                </Button>
              </span>
            </Tooltip>
            <Button size="small" variant="outlined" onClick={() => fileInputRef.current?.click()}>
              Восстановить
            </Button>
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv,.xlsx"
              hidden
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) void handleRestoreFile(file);
                e.target.value = '';
              }}
            />
          </>
        )}
        <Button size="small" variant="outlined" onClick={() => void handleExport()}>
          Выгрузить в Excel
        </Button>
        <Button size="small" onClick={() => void load()}>
          Обновить
        </Button>
        <FormControlLabel
          control={
            <Checkbox
              size="small"
              checked={showArchived}
              onChange={(e) => setShowArchived(e.target.checked)}
            />
          }
          label="Показать архивные"
          sx={{ ml: 1, '& .MuiFormControlLabel-label': { fontSize: 13 } }}
        />
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 1, flexShrink: 0 }}>
          {error}
        </Alert>
      )}
      {status && (
        <Alert severity="success" sx={{ mb: 1, flexShrink: 0 }} onClose={() => setStatus(null)}>
          {status}
        </Alert>
      )}

      <Box sx={{ flex: 1, minHeight: 0 }}>
        <DataTable
          rowData={rows}
          columnDefs={columnDefs}
          loading={loading}
          objectName="UserActionLog"
          gridId="audit-user-actions"
          height="100%"
          rowSelectionMode="multiple"
          onSelectionChanged={setSelected}
        />
      </Box>
    </Box>
  );
}
