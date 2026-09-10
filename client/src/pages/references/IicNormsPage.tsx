import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Box } from '@mui/material';
import type { GridApi } from 'ag-grid-community';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import {
  exportIicNormsExcelFromGrid,
  fetchIicNormHistory,
  fetchIicNorms,
  type IicNorm,
  type IicNormHistoryItem,
  type IicNormWriteRequest,
} from '../../api/registersApi';
import { EMPTY_ROWS } from '../../components/DataTable/emptyRows';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';
import { OBJECT_NAME } from '../../constants/accessControlConstants';
import { usePermission } from '../../hooks/usePermission';
import { getApiErrorMessage } from '../../services/apiError';
import { queryKeys } from '../../services/queryKeys';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { formatIisNorm } from '../../utils/formatIisNorm';
import { collectGridExportData } from '../../utils/directoryExport';
import { resolveIicDirectionUnitName } from '../../utils/resolveIicDirectionUnit';
import { IicNormHistoryDialog } from './IicNormHistoryDialog';
import { IicNormEditDialog } from './IicNormEditDialog';
import { IicNormsGrid } from './IicNormsGrid';
import { IicNormsToolbar } from './IicNormsToolbar';
import { normToHistoryItem, toTreeRows, type IicNormTreeRow } from './iicNormTree';
import { emptyIicNormForm, formFromNorm, useIicNormMutations } from './useIicNormMutations';
import { useIicNormDeepLink } from './useIicNormDeepLink';

export function IicNormsPage() {
  const { canRead, canEdit } = usePermission(OBJECT_NAME.IicWorkDirectionNorms);
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const directions = useDictionaryStore((s) => s.getList('iicWorkDirections'));
  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const getUnitLabel = useCallback(
    (directionId: number) => resolveIicDirectionUnitName(directionId, directions, getUnitName),
    [directions, getUnitName],
  );
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionInfo, setActionInfo] = useState<string | null>(null);
  const [selected, setSelected] = useState<IicNorm | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<IicNorm | null>(null);
  const [form, setForm] = useState<IicNormWriteRequest>(emptyIicNormForm);
  const [selectedHistory, setSelectedHistory] = useState<IicNormHistoryItem | null>(null);
  const [historyDialogOpen, setHistoryDialogOpen] = useState(false);
  const [historyDialogInitial, setHistoryDialogInitial] = useState<IicNormHistoryItem | null>(null);
  const [asOfDate, setAsOfDate] = useState(new Date().toISOString().slice(0, 10));
  const gridApiRef = useRef<GridApi<IicNormTreeRow> | null>(null);
  const mutations = useIicNormMutations();
  const {
    data: rows = EMPTY_ROWS,
    isLoading: loading,
    isError,
    error: loadError,
    refetch,
  } = useQuery<IicNorm[]>({
    queryKey: queryKeys.registers.iicNorms(asOfDate),
    queryFn: () => fetchIicNorms(asOfDate),
    enabled: canRead,
    staleTime: 60_000,
  });

  const historyDirectionId =
    modalOpen && editing != null && form.workDirectionId > 0 ? form.workDirectionId : null;

  const historyQuery = useQuery({
    queryKey: queryKeys.registers.iicNormsHistory(historyDirectionId),
    queryFn: () => {
      if (historyDirectionId == null) return Promise.resolve([] as IicNorm[]);
      return fetchIicNormHistory(historyDirectionId);
    },
    enabled: historyDirectionId != null,
    staleTime: 30_000,
  });

  const historyRows = useMemo(
    () => (historyQuery.data ?? []).map(normToHistoryItem),
    [historyQuery.data],
  );
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

  const error =
    actionError
    ?? (isError ? getApiErrorMessage(loadError, 'Не удалось загрузить регистр норм.') : null)
    ?? (historyDirectionId != null && historyQuery.isError
      ? getApiErrorMessage(historyQuery.error, 'Не удалось загрузить историю норм.')
      : null);

  useEffect(() => {
    if (!editing) setSelectedHistory(null);
  }, [editing]);

  const openCreate = () => {
    setEditing(null);
    setForm(emptyIicNormForm());
    setSelectedHistory(null);
    setModalOpen(true);
  };

  const openEdit = (row: IicNorm) => {
    setEditing(row);
    setSelectedHistory(null);
    setForm(formFromNorm(row));
    setModalOpen(true);
  };

  useIicNormDeepLink({
    canRead,
    loading,
    rows,
    queryClient,
    searchParams,
    setSearchParams,
    openEdit,
    setSelected,
    setHistoryDialogInitial,
    setSelectedHistory,
    setHistoryDialogOpen,
  });

  const openAddHistory = () => {
    setHistoryDialogInitial(null);
    setHistoryDialogOpen(true);
  };

  const openEditHistory = (row?: IicNormHistoryItem | null) => {
    const target = row ?? selectedHistory;
    if (!target) return;
    setSelectedHistory(target);
    setHistoryDialogInitial(target);
    setHistoryDialogOpen(true);
  };

  const handleDeleteHistory = async () => {
    try {
      const ok = await mutations.deleteHistory(selectedHistory);
      if (ok) {
        setActionError(null);
        setSelectedHistory(null);
      }
    } catch (err) {
      setActionError(getApiErrorMessage(err, 'Не удалось удалить запись истории.'));
    }
  };

  const handleHistorySaved = async (saved: IicNorm) => {
    setActionInfo(`Пересчитано версий: ${saved.recalculatedVersionCount ?? 0}`);
    setActionError(null);
    await mutations.invalidateIicNormQueries();
  };

  const handleSubmit = async () => {
    if (!form.workDirectionId) return;
    setActionError(null);
    if (editing) {
      setModalOpen(false);
      return;
    }
    try {
      await mutations.createNorm(form, directions);
      setModalOpen(false);
    } catch (err) {
      setActionError(getApiErrorMessage(err, 'Не удалось сохранить норму.'));
      throw err;
    }
  };

  const handleDelete = async () => {
    if (!selected) return;
    setActionError(null);
    try {
      await mutations.deleteNorm(selected.id);
    } catch (err) {
      setActionError(getApiErrorMessage(err, 'Не удалось удалить норму.'));
    }
  };

  const handleExport = useCallback(async () => {
    const api = gridApiRef.current;
    if (!api) return;
    const { headers, rows: exportRows } = collectGridExportData(api);
    await exportIicNormsExcelFromGrid('Нормы физобъёмов (IIC)', headers, exportRows);
  }, []);

  if (!canRead) return <Alert severity="warning">Нет прав на просмотр регистра норм.</Alert>;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <WorkingOverlay open={loading || mutations.mutating} delayMs={mutations.mutating ? 0 : 1000} />
      <IicNormsToolbar
        canEdit={canEdit}
        hasSelection={selected !== null}
        asOfDate={asOfDate}
        onAsOfDateChange={setAsOfDate}
        onAdd={openCreate}
        onEdit={() => selected && openEdit(selected)}
        onDelete={() => void handleDelete()}
        onRefresh={() => void refetch()}
        onExport={() => void handleExport()}
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2, flexShrink: 0 }}>
          {error}
        </Alert>
      )}
      {actionInfo && (
        <Alert severity="success" sx={{ mb: 2, flexShrink: 0 }} onClose={() => setActionInfo(null)}>
          {actionInfo}
        </Alert>
      )}

      <IicNormsGrid
        rowData={treeRows}
        loading={loading}
        canEdit={canEdit}
        getUnitLabel={getUnitLabel}
        onGridApiReady={(api) => {
          gridApiRef.current = api;
        }}
        onRowClicked={setSelected}
        onEdit={canEdit ? openEdit : undefined}
        onDelete={(row) => {
          setSelected(row);
          void handleDelete();
        }}
      />

      <IicNormEditDialog
        open={modalOpen}
        editing={Boolean(editing)}
        canEdit={canEdit}
        form={form}
        onFormChange={setForm}
        directions={directions}
        getUnitLabel={getUnitLabel}
        directionLabel={directionLabel}
        resolvedUnitLabel={resolvedUnitLabel}
        historyRows={historyRows}
        selectedHistory={selectedHistory}
        onSelectHistory={setSelectedHistory}
        onAddHistory={openAddHistory}
        onEditHistory={openEditHistory}
        onDeleteHistory={() => void handleDeleteHistory()}
        onClose={() => setModalOpen(false)}
        onSubmit={handleSubmit}
      />

      <IicNormHistoryDialog
        open={historyDialogOpen}
        title={historyDialogInitial ? 'Редактирование записи' : 'Новая запись истории'}
        groupLabel={directionLabel}
        unitLabel={resolvedUnitLabel}
        workDirectionId={form.workDirectionId}
        initial={historyDialogInitial}
        onClose={() => setHistoryDialogOpen(false)}
        onSaved={(saved) => void handleHistorySaved(saved)}
      />
      {mutations.ConfirmDialog}
    </Box>
  );
}
