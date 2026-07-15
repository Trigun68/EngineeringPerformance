import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material';
import type { ColDef, GridApi } from 'ag-grid-community';
import { useQuery } from '@tanstack/react-query';
import {
  createIicNorm,
  deleteIicNorm,
  exportIicNormsExcelFromGrid,
  fetchIicNormHistory,
  fetchIicNorms,
  type IicNorm,
  type IicNormHistoryItem,
  type IicNormWriteRequest,
} from '../../api/registersApi';
import { DictionaryGrid } from '../../components/dictionary/DictionaryGrid';
import { HierarchicalSelect } from '../../components/dictionary/HierarchicalSelect';
import { usePermission } from '../../hooks/usePermission';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import type { DictionaryItem } from '../../types/dictionary';
import { formatIisNorm } from '../../utils/formatIisNorm';
import { formatDate } from '../../utils/formatDateTime';
import { collectGridExportData } from '../../utils/directoryExport';
import {
  resolveIicDirectionUnitId,
  resolveIicDirectionUnitName,
} from '../../utils/resolveIicDirectionUnit';
import { DirectoryEditDialog, DirectoryFormRow } from './DirectoryEditDialog';
import { DirectoryToolbar } from './DirectoryToolbar';
import { IicNormHistoryDialog } from './IicNormHistoryDialog';
import { DeviationFormulaEditor } from '../../features/norms/DeviationFormulaEditor';
import { NormFormulaEditor } from '../../features/norms/NormFormulaEditor';
import { defaultDeviationFormula, defaultNormFormula } from '../../types/norms';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../utils/normalizeNormFormulas';

const OBJECT_NAME = 'IicWorkDirectionNorms';

interface IicNormTreeRow extends IicNorm {
  hierarchy: string[];
}

const emptyForm = (): IicNormWriteRequest => ({
  workDirectionId: 0,
  performance: null,
  estimate: null,
  standardDeviation: null,
  unitId: null,
  effectiveFrom: new Date().toISOString().slice(0, 10),
  effectiveTo: null,
  normFormula: defaultNormFormula(),
  deviationFormula: defaultDeviationFormula(),
});

function normToHistoryItem(norm: IicNorm): IicNormHistoryItem {
  const normFormula = normalizeNormFormula(norm.normFormula, norm.performance);
  const deviationFormula = normalizeDeviationFormula(
    norm.deviationFormula,
    norm.performance,
    norm.standardDeviation,
  );
  return {
    id: norm.id,
    effectiveFrom: norm.effectiveFrom.slice(0, 10),
    effectiveTo: norm.effectiveTo?.slice(0, 10) ?? null,
    performance: normFormula.coefficients.a ?? norm.performance ?? null,
    estimate: norm.estimate ?? null,
    standardDeviation: norm.standardDeviation ?? null,
    unitId: norm.resolvedUnitId ?? norm.unitId ?? null,
    normFormula,
    deviationFormula,
    migratedAt: norm.migratedAt ?? null,
  };
}

function buildDirectionPath(
  directionId: number,
  directions: DictionaryItem[],
  getUnitLabel: (directionId: number) => string,
): string[] {
  const byId = new Map(directions.map((d) => [d.id, d]));
  const segments: string[] = [];
  let current = byId.get(directionId);
  while (current) {
    segments.unshift(
      formatIisNorm({
        code: current.code,
        name: current.name,
        unitName: getUnitLabel(current.id),
      }),
    );
    current = current.parentId != null ? byId.get(current.parentId) : undefined;
  }
  return segments;
}

function toTreeRows(
  norms: IicNorm[],
  directions: DictionaryItem[],
  getUnitLabel: (directionId: number) => string,
): IicNormTreeRow[] {
  return norms.map((norm) => ({
    ...norm,
    hierarchy: buildDirectionPath(norm.workDirectionId, directions, getUnitLabel),
  }));
}

export function IicNormsPage() {
  const { canRead, canEdit } = usePermission(OBJECT_NAME);
  const directions = useDictionaryStore((s) => s.getList('iicWorkDirections'));
  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const fetchAll = useDictionaryStore((s) => s.fetchAll);

  const getUnitLabel = useCallback(
    (directionId: number) => resolveIicDirectionUnitName(directionId, directions, getUnitName),
    [directions, getUnitName],
  );

  const [actionError, setActionError] = useState<string | null>(null);
  const [selected, setSelected] = useState<IicNorm | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<IicNorm | null>(null);
  const [form, setForm] = useState<IicNormWriteRequest>(emptyForm);
  const [historyRows, setHistoryRows] = useState<IicNormHistoryItem[]>([]);
  const [selectedHistory, setSelectedHistory] = useState<IicNormHistoryItem | null>(null);
  const [historyDialogOpen, setHistoryDialogOpen] = useState(false);
  const [historyDialogInitial, setHistoryDialogInitial] = useState<IicNormHistoryItem | null>(null);
  const [asOfDate, setAsOfDate] = useState(new Date().toISOString().slice(0, 10));
  const gridApiRef = useRef<GridApi<IicNormTreeRow> | null>(null);

  const {
    data: rows = [],
    isLoading: loading,
    isError,
    refetch,
  } = useQuery<IicNorm[]>({
    queryKey: ['registers', 'iic-norms', asOfDate],
    queryFn: () => fetchIicNorms(asOfDate),
    enabled: canRead,
    staleTime: 60_000,
  });

  const treeRows = useMemo(
    () => toTreeRows(rows, directions, getUnitLabel),
    [directions, getUnitLabel, rows],
  );

  const resolvedUnitLabel = useMemo(() => {
    if (editing?.unitName) return editing.unitName;
    if (form.workDirectionId > 0) return getUnitLabel(form.workDirectionId);
    return '';
  }, [editing?.unitName, form.workDirectionId, getUnitLabel]);

  const directionLabel = useMemo(() => {
    const direction = directions.find((d) => d.id === form.workDirectionId);
    return direction
      ? formatIisNorm({ code: direction.code, name: direction.name, unitName: getUnitLabel(direction.id) })
      : '';
  }, [directions, form.workDirectionId, getUnitLabel]);

  const load = useCallback(async () => {
    await refetch();
  }, [refetch]);
  const error = actionError ?? (isError ? 'Не удалось загрузить регистр норм.' : null);

  const loadHistory = useCallback(async (workDirectionId: number) => {
    const history = await fetchIicNormHistory(workDirectionId);
    setHistoryRows(history.map(normToHistoryItem));
    setSelectedHistory(null);
  }, []);

  useEffect(() => {
    void fetchAll();
  }, [fetchAll]);

  useEffect(() => {
    if (modalOpen && editing && form.workDirectionId > 0) {
      void loadHistory(form.workDirectionId);
    } else if (!editing) {
      setHistoryRows([]);
      setSelectedHistory(null);
    }
  }, [editing, form.workDirectionId, loadHistory, modalOpen]);

  const columnDefs = useMemo<ColDef<IicNormTreeRow>[]>(
    () => [
      { field: 'performance', headerName: 'Производительность', minWidth: 130 },
      {
        headerName: 'Тип формулы',
        minWidth: 120,
        valueGetter: (p) => (p.data?.normFormula?.type === 1 ? 'Линейная' : p.data?.normFormula?.type ?? '—'),
      },
      {
        colId: 'migration',
        headerName: 'Миграция',
        minWidth: 130,
        valueGetter: (p) => (p.data?.migratedAt ? 'Автоконверт.' : ''),
      },
      { field: 'estimate', headerName: 'Оценка', minWidth: 100 },
      { field: 'standardDeviation', headerName: 'Станд. отклонение', minWidth: 140 },
      {
        headerName: 'Ед. изм.',
        minWidth: 100,
        valueGetter: (p) => p.data?.unitName ?? getUnitLabel(p.data?.workDirectionId ?? 0),
      },
      {
        field: 'effectiveFrom',
        headerName: 'Действует с',
        minWidth: 120,
        valueFormatter: (p) => formatDate(p.value),
        filter: 'agDateColumnFilter',
        filterParams: {
          comparator: (filterDate: Date, cellValue: string) => {
            if (!cellValue) return -1;
            const cellDate = new Date(cellValue);
            if (cellDate < filterDate) return -1;
            if (cellDate > filterDate) return 1;
            return 0;
          },
        },
      },
      {
        field: 'effectiveTo',
        headerName: 'Действует по',
        minWidth: 120,
        valueFormatter: (p) => formatDate(p.value),
        filter: 'agDateColumnFilter',
        filterParams: {
          comparator: (filterDate: Date, cellValue: string) => {
            if (!cellValue) return -1;
            const cellDate = new Date(cellValue);
            if (cellDate < filterDate) return -1;
            if (cellDate > filterDate) return 1;
            return 0;
          },
        },
      },
    ],
    [getUnitLabel],
  );

  const autoGroupColumnDef = useMemo<ColDef<IicNormTreeRow>>(
    () => ({
      headerName: 'Группа',
      minWidth: 320,
      field: 'hierarchy',
      cellRendererParams: { suppressCount: true },
      valueFormatter: (p) => {
        const hierarchy = p.value as string[] | undefined;
        if (!hierarchy || hierarchy.length === 0) return '';
        return hierarchy[hierarchy.length - 1];
      },
    }),
    [],
  );

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm());
    setHistoryRows([]);
    setModalOpen(true);
  };

  const openEdit = (row: IicNorm) => {
    setEditing(row);
    setForm({
      workDirectionId: row.workDirectionId,
      performance: row.performance ?? null,
      estimate: row.estimate ?? null,
      standardDeviation: row.standardDeviation ?? null,
      unitId: row.resolvedUnitId ?? row.unitId ?? null,
      effectiveFrom: row.effectiveFrom.slice(0, 10),
      effectiveTo: row.effectiveTo?.slice(0, 10) ?? null,
      normFormula: normalizeNormFormula(row.normFormula, row.performance),
      deviationFormula: normalizeDeviationFormula(row.deviationFormula, row.performance, row.standardDeviation),
    });
    setModalOpen(true);
  };

  const openAddHistory = () => {
    setHistoryDialogInitial(null);
    setHistoryDialogOpen(true);
  };

  const openEditHistory = () => {
    if (!selectedHistory) return;
    setHistoryDialogInitial(selectedHistory);
    setHistoryDialogOpen(true);
  };

  const handleDeleteHistory = async () => {
    if (!selectedHistory?.id || !window.confirm('Удалить запись истории?')) return;
    setActionError(null);
    try {
      await deleteIicNorm(selectedHistory.id);
      await loadHistory(form.workDirectionId);
      await load();
    } catch {
      setActionError('Не удалось удалить запись истории.');
    }
  };

  const handleHistorySaved = async () => {
    await loadHistory(form.workDirectionId);
    await load();
  };

  const handleSubmit = async () => {
    if (!form.workDirectionId) return;
    setActionError(null);
    try {
      if (editing) {
        setModalOpen(false);
        return;
      }
      const unitId = resolveIicDirectionUnitId(form.workDirectionId, directions);
      await createIicNorm({ ...form, unitId });
      setModalOpen(false);
      await load();
    } catch {
      setActionError('Не удалось сохранить норму.');
      throw new Error('save failed');
    }
  };

  const handleDelete = async () => {
    if (!selected || !window.confirm('Удалить норму?')) return;
    setActionError(null);
    try {
      await deleteIicNorm(selected.id);
      await load();
    } catch {
      setActionError('Не удалось удалить норму.');
    }
  };

  const handleGridApiReady = useCallback((api: GridApi<IicNormTreeRow>) => {
    gridApiRef.current = api;
  }, []);

  const handleExport = useCallback(async () => {
    const api = gridApiRef.current;
    if (!api) return;
    const { headers, rows: exportRows } = collectGridExportData(api);
    await exportIicNormsExcelFromGrid('Нормы физобъёмов (IIC)', headers, exportRows);
  }, []);

  if (!canRead) return <Alert severity="warning">Нет прав на просмотр регистра норм.</Alert>;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <Box sx={{ flexShrink: 0 }}>
        <DirectoryToolbar
        canEdit={canEdit}
        hasSelection={selected !== null}
        showArchiveActions={false}
        showArchivedCheckbox={false}
        showArchived={false}
        onShowArchivedChange={() => undefined}
        onAdd={openCreate}
        onEdit={() => selected && openEdit(selected)}
        onDelete={() => void handleDelete()}
        onArchive={() => undefined}
        onUnarchive={() => undefined}
        onRefresh={() => void load()}
        onExport={() => void handleExport()}
        />
      </Box>

      <Box sx={{ mb: 1.5, maxWidth: 220, flexShrink: 0 }}>
        <TextField
          label="Дата"
          type="date"
          size="small"
          fullWidth
          value={asOfDate}
          onChange={(e) => setAsOfDate(e.target.value)}
          InputLabelProps={{ shrink: true }}
        />
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2, flexShrink: 0 }}>
          {error}
        </Alert>
      )}

      <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        <DictionaryGrid<IicNormTreeRow>
          rowData={treeRows}
          columnDefs={columnDefs}
          loading={loading}
          objectName={OBJECT_NAME}
          gridId="ref-iic-norms"
          treeData
          hierarchyField="hierarchy"
          autoGroupColumnDef={autoGroupColumnDef}
          canEdit={canEdit}
          showExportButton={false}
          onGridApiReady={handleGridApiReady}
          onRowClicked={setSelected}
          onRowDoubleClicked={canEdit ? openEdit : undefined}
          onEdit={canEdit ? openEdit : undefined}
          onDelete={(row) => {
            setSelected(row);
            void handleDelete();
          }}
        />
      </Box>

      <DirectoryEditDialog
        open={modalOpen}
        title={editing ? 'Редактирование нормы' : 'Новая норма'}
        onClose={() => setModalOpen(false)}
        onSubmit={handleSubmit}
        maxWidth="md"
      >
        <DirectoryFormRow label="Группа">
          {editing ? (
            <TextField value={directionLabel} fullWidth size="small" InputProps={{ readOnly: true }} />
          ) : (
            <HierarchicalSelect
              items={directions}
              value={form.workDirectionId || null}
              onChange={(value) =>
                setForm({
                  ...form,
                  workDirectionId: value ?? 0,
                  unitId: value ? resolveIicDirectionUnitId(value, directions) : null,
                })
              }
              formatLabel={(item) =>
                formatIisNorm({ code: item.code, name: item.name, unitName: getUnitLabel(item.id) })
              }
              allowEmpty={false}
              required
            />
          )}
        </DirectoryFormRow>
        <DirectoryFormRow label="Ед. изм.">
          <TextField
            value={resolvedUnitLabel || '—'}
            fullWidth
            size="small"
            InputProps={{ readOnly: true }}
          />
        </DirectoryFormRow>

        {!editing && (
          <>
            <DirectoryFormRow label="Параметрическая норма N(V)">
              <NormFormulaEditor
                value={form.normFormula ?? defaultNormFormula(form.performance ?? 0)}
                deviationFormula={form.deviationFormula ?? defaultDeviationFormula()}
                onChange={(normFormula) => setForm({ ...form, normFormula, performance: normFormula.coefficients.a ?? form.performance })}
              />
            </DirectoryFormRow>
            <DirectoryFormRow label="Отклонение от нормы, %">
              <DeviationFormulaEditor
                value={form.deviationFormula ?? defaultDeviationFormula()}
                onChange={(deviationFormula) => {
                  const sigma = deviationFormula.coefficients.sigma ?? 0;
                  const perf = form.normFormula?.coefficients.a ?? form.performance ?? 0;
                  setForm({
                    ...form,
                    deviationFormula,
                    standardDeviation: perf > 0 ? sigma * perf : form.standardDeviation,
                  });
                }}
              />
            </DirectoryFormRow>
            <DirectoryFormRow label="Действует с">
              <TextField
                type="date"
                value={form.effectiveFrom}
                onChange={(e) => setForm({ ...form, effectiveFrom: e.target.value })}
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
                required
              />
            </DirectoryFormRow>
            <DirectoryFormRow label="Действует по">
              <TextField
                type="date"
                value={form.effectiveTo ?? ''}
                onChange={(e) => setForm({ ...form, effectiveTo: e.target.value || null })}
                fullWidth
                size="small"
                InputLabelProps={{ shrink: true }}
              />
            </DirectoryFormRow>
          </>
        )}

        {editing && form.workDirectionId > 0 && (
          <DirectoryFormRow label="Исторические записи">
            <Box sx={{ width: '100%' }}>
              <Stack direction="row" spacing={1} sx={{ mb: 1 }} flexWrap="wrap">
                {canEdit && (
                  <>
                    <Button size="small" variant="outlined" onClick={openAddHistory}>
                      Добавить строку
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={openEditHistory}
                      disabled={!selectedHistory}
                    >
                      Редактировать
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      color="error"
                      onClick={() => void handleDeleteHistory()}
                      disabled={!selectedHistory}
                    >
                      Удалить
                    </Button>
                  </>
                )}
              </Stack>

              <Stack
                direction="row"
                spacing={1}
                sx={{
                  mb: 0.5,
                  px: 1,
                  py: 0.5,
                  backgroundColor: 'grey.100',
                  borderRadius: 1,
                  fontWeight: 600,
                  fontSize: '0.75rem',
                  color: 'text.secondary',
                }}
              >
                <Box sx={{ width: 140 }}>Действует с</Box>
                <Box sx={{ width: 140 }}>Действует по</Box>
                <Box sx={{ width: 110 }}>Производительность</Box>
                <Box sx={{ width: 90 }}>Тип формулы</Box>
                <Box sx={{ width: 80 }}>Оценка</Box>
                <Box sx={{ width: 110 }}>Станд. отклонение</Box>
              </Stack>

              {historyRows.map((row) => {
                const isSelected = selectedHistory?.id === row.id;
                return (
                  <Stack
                    key={row.id ?? row.effectiveFrom}
                    direction="row"
                    spacing={1}
                    onClick={() => setSelectedHistory(row)}
                    sx={{
                      mb: 0.5,
                      px: 1,
                      py: 0.75,
                      cursor: 'pointer',
                      borderRadius: 1,
                      bgcolor: isSelected ? 'action.selected' : undefined,
                      '&:hover': { bgcolor: isSelected ? 'action.selected' : 'action.hover' },
                    }}
                  >
                    <Typography variant="body2" sx={{ width: 140 }}>
                      {formatDate(row.effectiveFrom)}
                    </Typography>
                    <Typography variant="body2" sx={{ width: 140 }}>
                      {row.effectiveTo ? formatDate(row.effectiveTo) : '—'}
                    </Typography>
                    <Typography variant="body2" sx={{ width: 110 }}>
                      {row.performance ?? '—'}
                    </Typography>
                    <Typography variant="body2" sx={{ width: 90 }}>
                      {row.normFormula?.type === 1 ? 'Линейная' : row.normFormula?.type ?? '—'}
                    </Typography>
                    <Typography variant="body2" sx={{ width: 80 }}>
                      {row.estimate ?? '—'}
                    </Typography>
                    <Typography variant="body2" sx={{ width: 110 }}>
                      {row.standardDeviation ?? '—'}
                    </Typography>
                  </Stack>
                );
              })}
            </Box>
          </DirectoryFormRow>
        )}
      </DirectoryEditDialog>

      <IicNormHistoryDialog
        open={historyDialogOpen}
        title={historyDialogInitial ? 'Редактирование записи' : 'Новая запись истории'}
        groupLabel={directionLabel}
        unitLabel={resolvedUnitLabel}
        workDirectionId={form.workDirectionId}
        initial={historyDialogInitial}
        onClose={() => setHistoryDialogOpen(false)}
        onSaved={() => void handleHistorySaved()}
      />
    </Box>
  );
}
