import { useCallback, useEffect, useMemo, useState } from 'react';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import { Alert, Box, Button, IconButton, Stack, TextField, Typography } from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { useQuery } from '@tanstack/react-query';
import {
  createIicNorm,
  deleteIicNorm,
  exportIicNormsExcel,
  fetchIicNormHistory,
  fetchIicNorms,
  updateIicNorm,
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
import { DirectoryEditDialog, DirectoryFormRow } from './DirectoryEditDialog';
import { DirectoryToolbar } from './DirectoryToolbar';

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
});

function normToHistoryItem(norm: IicNorm): IicNormHistoryItem {
  return {
    id: norm.id,
    effectiveFrom: norm.effectiveFrom.slice(0, 10),
    effectiveTo: norm.effectiveTo?.slice(0, 10) ?? null,
    performance: norm.performance ?? null,
    estimate: norm.estimate ?? null,
    standardDeviation: norm.standardDeviation ?? null,
    unitId: norm.unitId ?? null,
  };
}

function emptyHistoryItem(): IicNormHistoryItem {
  return {
    effectiveFrom: new Date().toISOString().slice(0, 10),
    effectiveTo: null,
    performance: 0,
    estimate: null,
    standardDeviation: null,
    unitId: null,
  };
}

function buildDirectionPath(
  directionId: number,
  directions: DictionaryItem[],
  getUnitName: (id: number | null | undefined) => string,
): string[] {
  const byId = new Map(directions.map((d) => [d.id, d]));
  const segments: string[] = [];
  let current = byId.get(directionId);
  while (current) {
    segments.unshift(
      formatIisNorm({
        code: current.code,
        name: current.name,
        unitName: getUnitName(current.id),
      }),
    );
    current = current.parentId != null ? byId.get(current.parentId) : undefined;
  }
  return segments;
}

function toTreeRows(
  norms: IicNorm[],
  directions: DictionaryItem[],
  getUnitName: (id: number | null | undefined) => string,
): IicNormTreeRow[] {
  // Нормы отображаются на том же уровне, что и группы физобъёмов:
  // одна строка на направление, без искусственного дочернего leaf-узла.
  return norms.map((norm) => ({
    ...norm,
    hierarchy: buildDirectionPath(norm.workDirectionId, directions, getUnitName),
  }));
}

export function IicNormsPage() {
  const { canRead, canEdit } = usePermission(OBJECT_NAME);
  const directions = useDictionaryStore((s) => s.getList('iicWorkDirections'));
  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const fetchAll = useDictionaryStore((s) => s.fetchAll);

  const [actionError, setActionError] = useState<string | null>(null);
  const [selected, setSelected] = useState<IicNorm | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<IicNorm | null>(null);
  const [form, setForm] = useState<IicNormWriteRequest>(emptyForm);
  const [historyDraft, setHistoryDraft] = useState<IicNormHistoryItem[]>([]);
  const [asOfDate, setAsOfDate] = useState(new Date().toISOString().slice(0, 10));
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

  const treeRows = useMemo(() => {
    return toTreeRows(rows, directions, getUnitName);
  }, [directions, getUnitName, rows]);

  const directionLabel = useMemo(() => {
    const direction = directions.find((d) => d.id === form.workDirectionId);
    return direction
      ? formatIisNorm({ code: direction.code, name: direction.name, unitName: getUnitName(direction.id) })
      : '';
  }, [directions, form.workDirectionId, getUnitName]);

  const load = useCallback(async () => {
    await refetch();
  }, [refetch]);
  const error = actionError ?? (isError ? 'Не удалось загрузить регистр норм.' : null);

  const loadHistory = useCallback(async (workDirectionId: number) => {
    const history = await fetchIicNormHistory(workDirectionId);
    setHistoryDraft(history.map(normToHistoryItem));
  }, []);

  useEffect(() => {
    void fetchAll();
  }, [fetchAll]);

  useEffect(() => {
    if (modalOpen && form.workDirectionId > 0) {
      void loadHistory(form.workDirectionId);
    } else {
      setHistoryDraft([]);
    }
  }, [form.workDirectionId, loadHistory, modalOpen]);

  const columnDefs = useMemo<ColDef<IicNormTreeRow>[]>(
    () => [
      { field: 'performance', headerName: 'Производительность', minWidth: 130 },
      { field: 'estimate', headerName: 'Оценка', minWidth: 100 },
      { field: 'standardDeviation', headerName: 'Станд. отклонение', minWidth: 140 },
      {
        headerName: 'Ед. изм.',
        minWidth: 100,
        valueGetter: (p) => {
          const unitId = p.data?.unitId;
          if (!unitId) return '';
          return getUnitName(unitId);
        },
      },
      {
        field: 'effectiveFrom',
        headerName: 'Действует с',
        minWidth: 120,
        valueFormatter: (p) => formatDate(p.value),
        // Also filter properly as date
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
    [getUnitName],
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
        // Показываем только последний элемент иерархии (лист или группа)
        return hierarchy[hierarchy.length - 1];
      },
    }),
    [],
  );

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm());
    setHistoryDraft([]);
    setModalOpen(true);
  };

  const openEdit = (row: IicNorm) => {
    setEditing(row);
    setForm({
      workDirectionId: row.workDirectionId,
      performance: row.performance ?? null,
      estimate: row.estimate ?? null,
      standardDeviation: row.standardDeviation ?? null,
      unitId: row.unitId ?? null,
      effectiveFrom: row.effectiveFrom.slice(0, 10),
      effectiveTo: row.effectiveTo?.slice(0, 10) ?? null,
    });
    setModalOpen(true);
  };

  const handleAddHistoryRow = () => {
    setHistoryDraft((prev) => [...prev, emptyHistoryItem()]);
  };

  const handleRemoveHistoryRow = (index: number) => {
    setHistoryDraft((prev) => prev.filter((_, i) => i !== index));
  };

  const updateHistoryRow = (index: number, patch: Partial<IicNormHistoryItem>) => {
    setHistoryDraft((prev) => prev.map((row, i) => (i === index ? { ...row, ...patch } : row)));
  };

  const handleSubmit = async () => {
    if (!form.workDirectionId) return;
    setActionError(null);
    try {
      const payload: IicNormWriteRequest = {
        ...form,
        history: historyDraft,
      };
      if (editing) {
        await updateIicNorm(editing.id, payload);
      } else {
        const created = await createIicNorm(form);
        if (historyDraft.length > 0) {
          await updateIicNorm(created.id, payload);
        }
      }
      setModalOpen(false);
      await load();
    } catch {
      setActionError('Не удалось сохранить норму.');
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

  if (!canRead) return <Alert severity="warning">Нет прав на просмотр регистра норм.</Alert>;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0, flex: 1, overflow: 'hidden' }}>
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
        onExport={() => void exportIicNormsExcel()}
      />

      <Box sx={{ mb: 1.5, maxWidth: 220 }}>
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

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

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
              onChange={(value) => setForm({ ...form, workDirectionId: value ?? 0 })}
              formatLabel={(item) =>
                formatIisNorm({ code: item.code, name: item.name, unitName: getUnitName(item.id) })
              }
              allowEmpty={false}
              required
            />
          )}
        </DirectoryFormRow>
        <DirectoryFormRow label="Ед. изм.">
          <TextField
            value={getUnitName(form.unitId) || '—'}
            fullWidth
            size="small"
            InputProps={{ readOnly: true }}
          />
        </DirectoryFormRow>
        <DirectoryFormRow label="Производительность">
          <TextField
            type="number"
            value={form.performance ?? ''}
            onChange={(e) => setForm({ ...form, performance: e.target.value ? Number(e.target.value) : null })}
            fullWidth
            size="small"
          />
        </DirectoryFormRow>
        <DirectoryFormRow label="Оценка">
          <TextField
            type="number"
            value={form.estimate ?? ''}
            onChange={(e) => setForm({ ...form, estimate: e.target.value ? Number(e.target.value) : null })}
            fullWidth
            size="small"
          />
        </DirectoryFormRow>
        <DirectoryFormRow label="Станд. отклонение">
          <TextField
            type="number"
            value={form.standardDeviation ?? ''}
            onChange={(e) =>
              setForm({ ...form, standardDeviation: e.target.value ? Number(e.target.value) : null })
            }
            fullWidth
            size="small"
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

        {form.workDirectionId > 0 && (
          <DirectoryFormRow label="Исторические записи">
            <Box sx={{ width: '100%' }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
                <Typography variant="body2" color="text.secondary">
                  Даты действия и значения нормы
                </Typography>
                {canEdit && (
                  <Button size="small" variant="outlined" onClick={handleAddHistoryRow}>
                    Добавить строку
                  </Button>
                )}
              </Stack>

              {/* Заголовки колонок */}
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
                alignItems="center"
              >
                <Box sx={{ width: 140 }}>Действует с</Box>
                <Box sx={{ width: 140 }}>Действует по</Box>
                <Box sx={{ width: 110 }}>Производительность</Box>
                <Box sx={{ width: 80 }}>Оценка</Box>
                <Box sx={{ width: 110 }}>Станд. отклонение</Box>
                <Box sx={{ width: 40 }}></Box>
              </Stack>

              {historyDraft.map((row, index) => (
                <Stack
                  key={row.id ?? `new-${index}`}
                  direction="row"
                  spacing={1}
                  sx={{ mb: 1, px: 1, py: 0.5, alignItems: 'center' }}
                  alignItems="center"
                >
                  <TextField
                    type="date"
                    size="small"
                    value={row.effectiveFrom}
                    onChange={(e) => updateHistoryRow(index, { effectiveFrom: e.target.value })}
                    InputLabelProps={{ shrink: true }}
                    sx={{ width: 140 }}
                    disabled={!canEdit}
                  />
                  <TextField
                    type="date"
                    size="small"
                    value={row.effectiveTo ?? ''}
                    onChange={(e) => updateHistoryRow(index, { effectiveTo: e.target.value || null })}
                    InputLabelProps={{ shrink: true }}
                    sx={{ width: 140 }}
                    disabled={!canEdit}
                  />
                  <TextField
                    type="number"
                    size="small"
                    value={row.performance ?? ''}
                    onChange={(e) =>
                      updateHistoryRow(index, {
                        performance: e.target.value === '' ? null : Number(e.target.value),
                      })
                    }
                    sx={{ width: 110 }}
                    disabled={!canEdit}
                  />
                  <TextField
                    type="number"
                    size="small"
                    value={row.estimate ?? ''}
                    onChange={(e) =>
                      updateHistoryRow(index, {
                        estimate: e.target.value === '' ? null : Number(e.target.value),
                      })
                    }
                    sx={{ width: 80 }}
                    disabled={!canEdit}
                  />
                  <TextField
                    type="number"
                    size="small"
                    value={row.standardDeviation ?? ''}
                    onChange={(e) =>
                      updateHistoryRow(index, {
                        standardDeviation: e.target.value === '' ? null : Number(e.target.value),
                      })
                    }
                    sx={{ width: 110 }}
                    disabled={!canEdit}
                  />
                  {canEdit && (
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => handleRemoveHistoryRow(index)}
                      aria-label="Удалить строку"
                      sx={{ width: 40 }}
                    >
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  )}
                </Stack>
              ))}
            </Box>
          </DirectoryFormRow>
        )}
      </DirectoryEditDialog>
    </Box>
  );
}
