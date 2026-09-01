import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Tooltip,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import DriveFileMoveIcon from '@mui/icons-material/DriveFileMove';
import StarIcon from '@mui/icons-material/Star';
import StarBorderIcon from '@mui/icons-material/StarBorder';
import type { ColDef } from 'ag-grid-community';
import {
  copyProjectVersion,
  createProjectVersion,
  deleteProjectVersion,
  fetchProjectVersions,
  setPrimaryProjectVersion,
  updateProjectVersion,
  type ProjectVersion,
  type ProjectVersionWriteRequest,
} from '../../api/projectVersionsApi';
import { DataTable } from '../DataTable/DataTable';
import { WorkingOverlay } from '../WorkingOverlay/WorkingOverlay';
import { getApiErrorMessage } from '../../services/apiError';
import { useConfirmation } from '../../hooks/useConfirmation';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { formatDateTime } from '../../utils/formatDateTime';

const ANALYSIS_PLAN = 1;
const ANALYSIS_FACT = 2;

export type VersionPickerDirection = typeof ANALYSIS_PLAN | typeof ANALYSIS_FACT | 'any';

function directionLabel(direction: number): string {
  if (direction === ANALYSIS_PLAN) return 'План';
  if (direction === ANALYSIS_FACT) return 'Факт';
  return 'Прогноз';
}

function buildVersionLabel(version: ProjectVersion): string {
  const type = directionLabel(version.direction);
  const desc = version.description?.trim();
  return desc ? `${version.versionName} — ${desc} (${type})` : `${version.versionName} (${type})`;
}

export interface ProjectVersionPickerDialogProps {
  open: boolean;
  title: string;
  projectId: number;
  versions?: ProjectVersion[];
  directionFilter?: VersionPickerDirection;
  selectedVersionId?: number | null;
  onSelect: (version: ProjectVersion) => void;
  onClose: () => void;
  onVersionsChanged?: () => void;
}

interface VersionFormState {
  versionName: string;
  description: string;
  physicalVolumeTypeId: string;
  asOfDate: string;
  isPrimary: boolean;
}

const emptyForm = (): VersionFormState => ({
  versionName: '',
  description: '',
  physicalVolumeTypeId: '',
  asOfDate: new Date().toISOString().slice(0, 10),
  isPrimary: false,
});

function toDateInputValue(iso?: string | null): string {
  if (!iso) return new Date().toISOString().slice(0, 10);
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return new Date().toISOString().slice(0, 10);
  return d.toISOString().slice(0, 10);
}

export function ProjectVersionPickerDialog({
  open,
  title,
  projectId,
  versions: versionsProp,
  directionFilter = 'any',
  selectedVersionId,
  onSelect,
  onClose,
  onVersionsChanged,
}: ProjectVersionPickerDialogProps) {
  const [versions, setVersions] = useState<ProjectVersion[]>(versionsProp ?? []);
  const [selected, setSelected] = useState<ProjectVersion | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [editOpen, setEditOpen] = useState(false);
  const [editMode, setEditMode] = useState<'create' | 'edit'>('create');
  const [form, setForm] = useState<VersionFormState>(emptyForm());
  const [copyOpen, setCopyOpen] = useState(false);
  /** same — копия выделенной; from-plan — Plan→Fact (ФО + оборудование). */
  const [copyMode, setCopyMode] = useState<'same' | 'from-plan'>('same');
  const [copyName, setCopyName] = useState('');
  const [copyDescription, setCopyDescription] = useState('');
  const [planSource, setPlanSource] = useState<ProjectVersion | null>(null);
  // Нельзя .filter() внутри селектора Zustand — каждый вызов новый массив → React #185.
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);
  const catalogOptions = useMemo(
    () => physicalVolumeTypes.filter((t) => t.name === 'КФО' || t.name === 'БЕР' || t.name === 'БЭР'),
    [physicalVolumeTypes],
  );
  const [submitting, setSubmitting] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();
  /** Id, выбранный пользователем кликом; не сбрасываем при перезагрузке списка. */
  const userPickedIdRef = useRef<number | null>(null);
  const wasOpenRef = useRef(false);
  /** Версия, зафиксированная при открытии copy/delete/edit. */
  const [actionTarget, setActionTarget] = useState<ProjectVersion | null>(null);

  const defaultDirection = directionFilter === 'any' ? ANALYSIS_PLAN : directionFilter;

  const pickVersion = useCallback((version: ProjectVersion | null, fromUser = false) => {
    if (fromUser) {
      userPickedIdRef.current = version?.id ?? null;
    }
    setSelected(version);
  }, []);

  const loadVersions = useCallback(async () => {
    try {
      const list = await fetchProjectVersions(projectId);
      setVersions(list);
      onVersionsChanged?.();
      return list;
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить версии.'));
      return [] as ProjectVersion[];
    }
  }, [onVersionsChanged, projectId]);

  // При открытии всегда грузим с API — иначе после удаления/копирования
  // устаревший versionsProp из родителя «возвращал» удалённую версию.
  useEffect(() => {
    if (!open) return;
    void loadVersions();
  }, [loadVersions, open]);

  const filteredVersions = useMemo(() => {
    if (directionFilter === 'any') return versions;
    return versions.filter((v) => v.direction === directionFilter);
  }, [directionFilter, versions]);

  const planVersions = useMemo(
    () => versions.filter((v) => v.direction === ANALYSIS_PLAN),
    [versions],
  );

  const allowCopyFromPlan = directionFilter === ANALYSIS_FACT;

  // При каждом открытии диалога сбрасываем пользовательский выбор,
  // чтобы стартовать с версии родителя (selectedVersionId).
  useEffect(() => {
    if (open && !wasOpenRef.current) {
      userPickedIdRef.current = null;
    }
    wasOpenRef.current = open;
  }, [open]);

  // Синхронизация выбора со списком: сохраняем клик пользователя,
  // не откатываем на первую строку при каждом refresh списка.
  useEffect(() => {
    if (!open) return;
    setSelected((prev) => {
      const preferredId = userPickedIdRef.current ?? selectedVersionId ?? null;
      if (preferredId != null) {
        const match = filteredVersions.find((v) => v.id === preferredId);
        if (match) return match;
      }
      if (prev) {
        const still = filteredVersions.find((v) => v.id === prev.id);
        if (still) return still;
      }
      return filteredVersions[0] ?? null;
    });
  }, [filteredVersions, open, selectedVersionId]);

  const columnDefs = useMemo<ColDef<ProjectVersion>[]>(
    () => [
      { field: 'versionName', headerName: 'Наименование версии', minWidth: 160, flex: 1 },
      {
        field: 'direction',
        headerName: 'Тип',
        maxWidth: 90,
        valueFormatter: (p) => directionLabel(Number(p.value)),
      },
      {
        field: 'isPrimary',
        headerName: 'Основная',
        maxWidth: 110,
        valueFormatter: (p) => (p.value ? 'Да' : ''),
      },
      { field: 'description', headerName: 'Описание', minWidth: 180, flex: 1 },
      {
        field: 'createdAt',
        headerName: 'Дата создания',
        minWidth: 150,
        valueFormatter: (p) => (p.value ? formatDateTime(p.value) : ''),
      },
    ],
    [],
  );

  const openCreate = () => {
    setEditMode('create');
    const peers = filteredVersions;
    setForm({
      ...emptyForm(),
      // Первая версия направления — основная по умолчанию (сервер всё равно выставит).
      isPrimary: peers.length === 0,
    });
    setEditOpen(true);
  };

  const openEdit = () => {
    if (!selected) return;
    setActionTarget(selected);
    setEditMode('edit');
    setForm({
      versionName: selected.versionName,
      description: selected.description ?? '',
      physicalVolumeTypeId: selected.physicalVolumeTypeId?.toString() ?? '',
      asOfDate: toDateInputValue(selected.createdAt),
      isPrimary: selected.isPrimary === true,
    });
    setEditOpen(true);
  };

  const handleSaveVersion = async () => {
    if (!form.versionName.trim() || !form.physicalVolumeTypeId || !form.asOfDate) return;
    const editTarget = actionTarget ?? selected;

    if (editMode === 'create' && form.isPrimary) {
      const existingPrimary = filteredVersions.find((v) => v.isPrimary);
      if (existingPrimary) {
        const ok = await confirm({
          title: 'Сделать основной?',
          description: 'Вы действительно хотите сделать эту версию основной?',
        });
        if (!ok) return;
      }
    }

    setSubmitting(true);
    setError(null);
    try {
      const body: ProjectVersionWriteRequest = {
        versionName: form.versionName.trim(),
        description: form.description.trim() || null,
        direction: editMode === 'edit' && editTarget ? editTarget.direction : defaultDirection,
        physicalVolumeTypeId: form.physicalVolumeTypeId
          ? Number(form.physicalVolumeTypeId)
          : null,
        asOfDate: `${form.asOfDate}T12:00:00Z`,
      };
      if (editMode === 'edit' && editTarget) {
        const updated = await updateProjectVersion(projectId, editTarget.id, {
          ...body,
          rowVersion: editTarget.rowVersion ?? null,
        });
        pickVersion(updated, true);
      } else {
        if (form.isPrimary) body.isPrimary = true;
        const created = await createProjectVersion(projectId, body);
        pickVersion(created, true);
      }
      setEditOpen(false);
      setActionTarget(null);
      await loadVersions();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить версию.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleSetPrimary = async () => {
    if (!selected || selected.isPrimary) return;
    setSubmitting(true);
    setError(null);
    try {
      const updated = await setPrimaryProjectVersion(projectId, selected.id);
      pickVersion(updated, true);
      await loadVersions();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось назначить основную версию.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    const target = actionTarget ?? selected;
    if (!target) return;
    setDeleteConfirmOpen(false);
    setSubmitting(true);
    setError(null);
    try {
      await deleteProjectVersion(projectId, target.id);
      userPickedIdRef.current = null;
      setActionTarget(null);
      pickVersion(null);
      const list = await loadVersions();
      const stillVisible = list.filter((v) =>
        directionFilter === 'any' ? true : v.direction === directionFilter,
      );
      pickVersion(stillVisible[0] ?? null);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось удалить версию.'));
    } finally {
      setSubmitting(false);
    }
  };

  const openCopy = () => {
    if (!selected) return;
    setCopyMode('same');
    setActionTarget(selected);
    setPlanSource(null);
    setCopyName(`${selected.versionName} (копия)`);
    setCopyDescription(selected.description ?? '');
    setCopyOpen(true);
  };

  const openCopyFromPlan = () => {
    const source = planVersions.find((v) => v.isPrimary) ?? planVersions[0] ?? null;
    setCopyMode('from-plan');
    setActionTarget(null);
    setPlanSource(source);
    setCopyName(source ? `${source.versionName} (факт)` : '');
    setCopyDescription(source?.description ?? '');
    setCopyOpen(true);
  };

  const handleCopy = async () => {
    const target = copyMode === 'from-plan' ? planSource : (actionTarget ?? selected);
    if (!target || !copyName.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      const copy = await copyProjectVersion(projectId, target.id, {
        newVersionName: copyName.trim(),
        description: copyDescription.trim() || null,
        ...(copyMode === 'from-plan' ? { targetDirection: ANALYSIS_FACT } : {}),
      });
      setCopyOpen(false);
      setActionTarget(null);
      setPlanSource(null);
      pickVersion(copy, true);
      await loadVersions();
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          copyMode === 'from-plan'
            ? 'Не удалось скопировать версию из плана.'
            : 'Не удалось скопировать версию.',
        ),
      );
    } finally {
      setSubmitting(false);
    }
  };

  const handleConfirm = () => {
    if (selected) {
      onSelect(selected);
      onClose();
    }
  };

  return (
    <>
      <WorkingOverlay open={submitting} />

      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth data-testid="version-picker-dialog">
        <DialogTitle sx={{ bgcolor: '#2C3E50', color: '#ECF0F1' }}>{title}</DialogTitle>
        <DialogContent dividers sx={{ p: 2 }}>
          <Stack direction="row" spacing={0.5} sx={{ mb: 1 }}>
            <Tooltip title="Добавить версию">
              <IconButton size="small" onClick={openCreate} aria-label="Добавить версию">
                <AddIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Редактировать версию">
              <IconButton size="small" onClick={openEdit} disabled={!selected} aria-label="Редактировать версию">
                <EditIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Удалить версию">
              <IconButton
                size="small"
                onClick={() => {
                  if (!selected) return;
                  setActionTarget(selected);
                  setDeleteConfirmOpen(true);
                }}
                disabled={!selected || submitting}
                aria-label="Удалить версию"
                data-testid="version-delete-button"
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Копировать версию">
              <IconButton
                size="small"
                onClick={openCopy}
                disabled={!selected || submitting}
                aria-label="Копировать версию"
                data-testid="version-copy-button"
              >
                <ContentCopyIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Сделать основной">
              <span>
                <IconButton
                  size="small"
                  onClick={() => void handleSetPrimary()}
                  disabled={!selected || selected.isPrimary === true || submitting}
                  aria-label="Сделать основной"
                  data-testid="version-set-primary-button"
                >
                  {selected?.isPrimary ? (
                    <StarIcon fontSize="small" color="warning" />
                  ) : (
                    <StarBorderIcon fontSize="small" />
                  )}
                </IconButton>
              </span>
            </Tooltip>
            {allowCopyFromPlan && (
              <Tooltip title="Копировать из плана (ФО и оборудование; трудоёмкость — отдельно)">
                <span>
                  <IconButton
                    size="small"
                    onClick={openCopyFromPlan}
                    disabled={submitting || planVersions.length === 0}
                    aria-label="Копировать из плана"
                    data-testid="version-copy-from-plan-button"
                  >
                    <DriveFileMoveIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
            )}
          </Stack>

          {error && (
            <Alert severity="error" sx={{ mb: 1 }} onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          {filteredVersions.length === 0 ? (
            <Alert severity="info">Нет версий для выбора. Создайте версию анализа.</Alert>
          ) : (
            <Box sx={{ height: 320 }}>
              <DataTable
                rowData={filteredVersions}
                columnDefs={columnDefs}
                objectName="ProjectAnalysisVersions"
                gridId="version-picker"
                rowSelectionMode="single"
                getRowIdValue={(row) => String(row.id)}
                onRowClicked={(row) => pickVersion(row, true)}
                onSelectionChanged={(rows) => {
                  const row = rows[0];
                  if (row) pickVersion(row, true);
                }}
                onRowDoubleClicked={(row) => {
                  pickVersion(row, true);
                  onSelect(row);
                  onClose();
                }}
                height="100%"
              />
            </Box>
          )}
          {selected && (
            <Stack sx={{ mt: 1 }}>
              <Alert severity="success" variant="outlined">
                Выбрано: {buildVersionLabel(selected)}
              </Alert>
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Отмена</Button>
          <Button variant="contained" onClick={handleConfirm} disabled={!selected}>
            Выбрать
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editOpen} onClose={() => setEditOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editMode === 'create' ? 'Новая версия анализа' : 'Редактирование версии'}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Наименование версии"
              value={form.versionName}
              onChange={(e) => setForm({ ...form, versionName: e.target.value })}
              required
              fullWidth
            />
            <TextField
              label="Описание"
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              fullWidth
              multiline
              minRows={2}
            />
            <FormControl fullWidth required>
              <InputLabel id="pv-catalog-type-label">Справочник физобъёмов</InputLabel>
              <Select
                labelId="pv-catalog-type-label"
                label="Справочник физобъёмов"
                value={form.physicalVolumeTypeId}
                onChange={(e) => setForm({ ...form, physicalVolumeTypeId: e.target.value })}
              >
                {catalogOptions.map((t) => (
                  <MenuItem key={t.id} value={String(t.id)}>
                    {t.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Дата фиксации норм"
              type="date"
              value={form.asOfDate}
              onChange={(e) => setForm({ ...form, asOfDate: e.target.value })}
              required
              fullWidth
              InputLabelProps={{ shrink: true }}
              helperText="Снимок норм IIC на эту дату. Смена БЕР↔КФО после импорта ФО недоступна."
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.isPrimary}
                  disabled={editMode === 'edit'}
                  onChange={(e) => setForm({ ...form, isPrimary: e.target.checked })}
                  data-testid="version-primary-checkbox"
                />
              }
              label={
                editMode === 'edit'
                  ? 'Основная (меняется только в списке версий)'
                  : 'Основная'
              }
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditOpen(false)} disabled={submitting}>
            Отмена
          </Button>
          <Button
            variant="contained"
            onClick={() => void handleSaveVersion()}
            disabled={submitting || !form.physicalVolumeTypeId || !form.asOfDate}
          >
            Сохранить
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={copyOpen} onClose={() => setCopyOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {copyMode === 'from-plan' ? 'Копировать версию из плана' : 'Копировать версию'}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {copyMode === 'from-plan' ? (
              <>
                <Alert severity="info" variant="outlined">
                  Будут скопированы физические объёмы и оборудование. Трудоёмкость добавьте
                  импортом после создания версии факта.
                </Alert>
                {planVersions.length === 0 ? (
                  <Alert severity="warning">Нет версий плана для копирования.</Alert>
                ) : (
                  <Box sx={{ height: 220 }}>
                    <DataTable
                      rowData={planVersions}
                      columnDefs={columnDefs}
                      objectName="ProjectAnalysisVersions"
                      gridId="version-picker-plan-source"
                      rowSelectionMode="single"
                      getRowIdValue={(row) => String(row.id)}
                      onRowClicked={(row) => {
                        setPlanSource(row);
                        setCopyName(`${row.versionName} (факт)`);
                        setCopyDescription(row.description ?? '');
                      }}
                      onSelectionChanged={(rows) => {
                        const row = rows[0];
                        if (!row) return;
                        setPlanSource(row);
                        setCopyName(`${row.versionName} (факт)`);
                        setCopyDescription(row.description ?? '');
                      }}
                      height="100%"
                    />
                  </Box>
                )}
                {planSource && (
                  <Alert severity="success" variant="outlined">
                    Источник (план): {planSource.versionName}
                  </Alert>
                )}
              </>
            ) : (
              actionTarget && (
                <Alert severity="info" variant="outlined">
                  Источник: {actionTarget.versionName}
                </Alert>
              )
            )}
            <TextField
              label="Имя новой версии"
              value={copyName}
              onChange={(e) => setCopyName(e.target.value)}
              required
              fullWidth
            />
            <TextField
              label="Описание"
              value={copyDescription}
              onChange={(e) => setCopyDescription(e.target.value)}
              fullWidth
              multiline
              minRows={2}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCopyOpen(false)} disabled={submitting}>
            Отмена
          </Button>
          <Button
            variant="contained"
            onClick={() => void handleCopy()}
            disabled={
              submitting
              || !copyName.trim()
              || (copyMode === 'from-plan' ? !planSource : !(actionTarget ?? selected))
            }
            data-testid="version-copy-confirm-button"
          >
            {submitting ? 'Работаю…' : 'Копировать'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={deleteConfirmOpen}
        onClose={() => !submitting && setDeleteConfirmOpen(false)}
        maxWidth="xs"
        fullWidth
        data-testid="version-delete-confirm"
      >
        <DialogTitle>Удалить версию?</DialogTitle>
        <DialogContent dividers>
          <Alert severity="warning">
            Версия «{actionTarget?.versionName ?? selected?.versionName}» будет скрыта из списка (архивирование).
            Связанные данные сохраняются в БД, но версия перестанет отображаться в выборе.
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmOpen(false)} disabled={submitting}>
            Отмена
          </Button>
          <Button
            color="error"
            variant="contained"
            onClick={() => void handleDelete()}
            disabled={submitting}
            data-testid="version-delete-confirm-button"
          >
            Удалить
          </Button>
        </DialogActions>
      </Dialog>
      {ConfirmDialog}
    </>
  );
}

export { buildVersionLabel, ANALYSIS_PLAN, ANALYSIS_FACT };
