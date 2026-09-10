import { useCallback, useMemo, useRef, useState } from 'react';
import { Alert, Box, Stack, TextField, Typography } from '@mui/material';
import type { ColDef, GridApi } from 'ag-grid-community';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  deleteWorkCostPeriod,
  exportWorkCostsExcelFromGrid,
  fetchWorkCostPeriod,
  fetchWorkCostPeriods,
  fetchWorkCosts,
  type WorkCostRow,
} from '../../api/registersApi';
import { DictionaryGrid } from '../../components/dictionary/DictionaryGrid';
import { EMPTY_ROWS } from '../../components/DataTable/emptyRows';
import { ExportActionButton } from '../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../components/export/ToolbarActionButton';
import { ACTION_LABELS } from '../../constants/actionLabels';
import { useConfirmation } from '../../hooks/useConfirmation';
import { usePermission } from '../../hooks/usePermission';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';
import { getApiErrorMessage } from '../../services/apiError';
import { queryKeys } from '../../services/queryKeys';
import { collectGridExportData } from '../../utils/directoryExport';
import { formatDate } from '../../utils/formatDateTime';
import { WorkCostsPeriodsPanel, periodDateKey } from './WorkCostsPeriodsPanel';
import { WorkCostsSetRateDialog } from './WorkCostsSetRateDialog';

const OBJECT_NAME = 'EngineeringWorkCosts';

interface WorkCostTreeRow extends WorkCostRow {
  hierarchy: string[];
}

function buildDirectionPath(row: WorkCostRow, byId: Map<number, WorkCostRow>): string[] {
  const segments: string[] = [];
  let current: WorkCostRow | undefined = row;
  const seen = new Set<number>();
  while (current && !seen.has(current.workDirectionId)) {
    seen.add(current.workDirectionId);
    segments.unshift(current.workDirectionName);
    current = current.parentId != null ? byId.get(current.parentId) : undefined;
  }
  return segments;
}

function toTreeRows(rows: WorkCostRow[]): WorkCostTreeRow[] {
  const byId = new Map(rows.map((r) => [r.workDirectionId, r]));
  return rows.map((row) => ({
    ...row,
    id: row.workDirectionId,
    hierarchy: buildDirectionPath(row, byId),
  }));
}

function formatRateCell(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return '—';
  return value.toLocaleString('ru-RU', { maximumFractionDigits: 4 });
}

export function WorkCostsPage() {
  const { canRead, canEdit } = usePermission(OBJECT_NAME);
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();
  const queryClient = useQueryClient();
  const [asOfDate, setAsOfDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [selectedFrom, setSelectedFrom] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [actionError, setActionError] = useState<string | null>(null);
  const gridApiRef = useRef<GridApi<WorkCostTreeRow> | null>(null);

  const {
    data: asOfRows = EMPTY_ROWS,
    isLoading: asOfLoading,
    isError: asOfError,
    error: asOfLoadError,
  } = useQuery<WorkCostRow[]>({
    queryKey: queryKeys.registers.workCosts.asOf(asOfDate),
    queryFn: () => fetchWorkCosts(asOfDate),
    enabled: canRead,
    staleTime: 60_000,
  });

  const {
    data: periods = [],
    isLoading: periodsLoading,
    isError: periodsError,
    error: periodsLoadError,
  } = useQuery({
    queryKey: queryKeys.registers.workCosts.periods,
    queryFn: fetchWorkCostPeriods,
    enabled: canRead,
    staleTime: 30_000,
  });

  const {
    data: periodRows = EMPTY_ROWS,
    isLoading: periodLoading,
    isError: periodError,
    error: periodLoadError,
  } = useQuery<WorkCostRow[]>({
    queryKey: queryKeys.registers.workCosts.period(selectedFrom ?? undefined),
    queryFn: () => fetchWorkCostPeriod(selectedFrom!),
    enabled: canRead && selectedFrom != null,
    staleTime: 30_000,
  });

  const invalidate = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: queryKeys.registers.workCosts.root });
  }, [queryClient]);

  const { mutate: deletePeriod, isPending: isDeleting } = useMutation({
    mutationFn: (effectiveFrom: string) => deleteWorkCostPeriod(effectiveFrom),
    onSuccess: () => {
      setSelectedFrom(null);
      setActionError(null);
      void invalidate();
    },
    onError: (err) => {
      setActionError(getApiErrorMessage(err, 'Не удалось удалить период.'));
    },
  });

  const rows = selectedFrom ? periodRows : asOfRows;
  const loading = periodsLoading || (selectedFrom ? periodLoading : asOfLoading) || isDeleting;
  const treeRows = useMemo(() => toTreeRows(rows), [rows]);
  const error =
    actionError
    ?? (asOfError ? getApiErrorMessage(asOfLoadError, 'Не удалось загрузить стоимость работ.') : null)
    ?? (periodsError ? getApiErrorMessage(periodsLoadError, 'Не удалось загрузить стоимость работ.') : null)
    ?? (periodError ? getApiErrorMessage(periodLoadError, 'Не удалось загрузить стоимость работ.') : null);

  const openCreate = useCallback(() => {
    setModalMode('create');
    setModalOpen(true);
  }, []);

  const openEdit = useCallback(
    (effectiveFrom: string) => {
      setSelectedFrom(effectiveFrom);
      setModalMode('edit');
      setModalOpen(true);
    },
    [],
  );

  const handleSelectPeriod = useCallback((effectiveFrom: string) => {
    setActionError(null);
    setSelectedFrom(effectiveFrom);
  }, []);

  const handleAsOfChange = useCallback((value: string) => {
    setAsOfDate(value);
    setSelectedFrom(null);
  }, []);

  const handleDelete = useCallback(async () => {
    if (!canEdit || !selectedFrom || isDeleting) return;
    const period = periods.find((p) => periodDateKey(p.effectiveFrom) === selectedFrom);
    const count = period?.directionCount ?? rows.filter((r) => r.ratePerHour != null).length;
    const ok = await confirm({
      title: 'Удалить период?',
      description: `Будут удалены все ставки на дату ${formatDate(selectedFrom)} (${count} групп ИИС). Предыдущий открытый период будет продлён.`,
      confirmText: 'Удалить',
      cancelText: 'Отмена',
    });
    if (!ok) return;
    setActionError(null);
    deletePeriod(selectedFrom);
  }, [canEdit, confirm, deletePeriod, isDeleting, periods, rows, selectedFrom]);

  const columnDefs = useMemo<ColDef<WorkCostTreeRow>[]>(
    () => [
      {
        field: 'ratePerHour',
        headerName: 'Стоимость, ₽/ч',
        minWidth: 140,
        valueFormatter: (p) => formatRateCell(p.value as number | null | undefined),
      },
      {
        field: 'effectiveFrom',
        headerName: 'Действует с',
        minWidth: 130,
        valueFormatter: (p) => (p.value ? formatDate(p.value as string) : '—'),
      },
    ],
    [],
  );

  const autoGroupColumnDef = useMemo<ColDef<WorkCostTreeRow>>(
    () => ({
      headerName: 'Группа',
      minWidth: 320,
      flex: 1,
      cellRendererParams: { suppressCount: true },
      filterValueGetter: (p) => (p.data?.workDirectionId != null ? String(p.data.workDirectionId) : ''),
      valueGetter: (p) => {
        const hierarchy = p.data?.hierarchy;
        if (!hierarchy || hierarchy.length === 0) return '';
        return hierarchy[hierarchy.length - 1];
      },
    }),
    [],
  );

  const handleGridApiReady = useCallback((api: GridApi<WorkCostTreeRow>) => {
    gridApiRef.current = api;
  }, []);

  const handleExport = useCallback(async () => {
    const api = gridApiRef.current;
    if (!api) return;
    try {
      setActionError(null);
      const { headers, rows: exportRows } = collectGridExportData(api);
      await exportWorkCostsExcelFromGrid('Стоимость работ', headers, exportRows);
    } catch {
      setActionError('Не удалось выгрузить Excel.');
    }
  }, []);

  const dialogRows = modalMode === 'edit' ? periodRows : rows;

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр стоимости работ.</Alert>;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <WorkingOverlay open={loading} delayMs={1000} />
      <Stack
        direction="row"
        alignItems="center"
        spacing={1}
        sx={{ mb: 1, flexWrap: 'wrap', gap: 1, flexShrink: 0 }}
      >
        {canEdit && (
          <ToolbarActionButton onClick={openCreate}>Задать стоимость</ToolbarActionButton>
        )}
        {canEdit && (
          <ToolbarActionButton onClick={() => selectedFrom && openEdit(selectedFrom)} disabled={!selectedFrom}>
            Изменить
          </ToolbarActionButton>
        )}
        {canEdit && (
          <ToolbarActionButton onClick={() => void handleDelete()} disabled={!selectedFrom || isDeleting}>
            Удалить
          </ToolbarActionButton>
        )}
        <ToolbarActionButton onClick={() => void invalidate()}>{ACTION_LABELS.refresh}</ToolbarActionButton>
        <TextField
          label="Дата"
          type="date"
          size="small"
          value={asOfDate}
          onChange={(e) => handleAsOfChange(e.target.value)}
          InputLabelProps={{ shrink: true }}
          sx={{ width: 200 }}
        />
        <Typography variant="body2" color="text.secondary" sx={{ ml: 1 }}>
          {selectedFrom ? `Период с ${formatDate(selectedFrom)}` : `Срез на ${formatDate(asOfDate)}`}
        </Typography>
        <Box sx={{ flex: 1, display: 'flex', justifyContent: 'flex-end' }}>
          <ExportActionButton kind="excel" onClick={() => void handleExport()} />
        </Box>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 2, flexShrink: 0 }}>
          {error}
        </Alert>
      )}

      <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', display: 'flex', gap: 1 }}>
        <WorkCostsPeriodsPanel
          periods={periods}
          selectedFrom={selectedFrom}
          onSelect={handleSelectPeriod}
          onEdit={canEdit ? openEdit : () => undefined}
        />
        <Box sx={{ flex: 1, minWidth: 0, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
          <DictionaryGrid<WorkCostTreeRow>
            rowData={treeRows}
            columnDefs={columnDefs}
            loading={loading}
            objectName={OBJECT_NAME}
            gridId="ref-work-costs"
            treeData
            hierarchyField="hierarchy"
            autoGroupColumnDef={autoGroupColumnDef}
            canEdit={false}
            showExportButton={false}
            onGridApiReady={handleGridApiReady}
            additionalContextMenuItems={
              canEdit
                ? [
                    { name: 'Задать стоимость', action: openCreate },
                    ...(selectedFrom
                      ? [
                          { name: 'Изменить', action: () => openEdit(selectedFrom) },
                          { name: 'Удалить', action: () => void handleDelete() },
                        ]
                      : []),
                  ]
                : []
            }
            onRowDoubleClicked={
              canEdit
                ? (row) => {
                    const from = selectedFrom ?? (row.effectiveFrom ? periodDateKey(row.effectiveFrom) : null);
                    if (from) openEdit(from);
                  }
                : undefined
            }
          />
        </Box>
      </Box>

      <WorkCostsSetRateDialog
        open={modalOpen}
        rows={dialogRows}
        canEdit={canEdit}
        mode={modalMode}
        initialEffectiveFrom={modalMode === 'edit' ? (selectedFrom ?? undefined) : undefined}
        onClose={() => setModalOpen(false)}
        onSaved={() => {
          setActionError(null);
          void invalidate();
        }}
      />
      {ConfirmDialog}
    </Box>
  );
}
