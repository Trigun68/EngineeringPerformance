import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  FormControl,
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
import type { ColDef } from 'ag-grid-community';
import {
  copyProjectVersion,
  createProjectVersion,
  deleteProjectVersion,
  fetchProjectVersions,
  updateProjectVersion,
  type ProjectVersion,
  type ProjectVersionWriteRequest,
} from '../../api/projectVersionsApi';
import { DataTable } from '../DataTable/DataTable';
import { getApiErrorMessage } from '../../services/apiError';
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
}

const emptyForm = (): VersionFormState => ({
  versionName: '',
  description: '',
  physicalVolumeTypeId: '',
});

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
  const [copyName, setCopyName] = useState('');
  const [copyDescription, setCopyDescription] = useState('');
  // Нельзя .filter() внутри селектора Zustand — каждый вызов новый массив → React #185.
  // Нельзя .filter() внутри селектора Zustand — каждый вызов новый массив → React #185.
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);
  const catalogOptions = useMemo(
    () => physicalVolumeTypes.filter((t) => t.name === 'КФО' || t.name === 'БЭР'),
    [physicalVolumeTypes],
  );
  const [submitting, setSubmitting] = useState(false);

  const defaultDirection = directionFilter === 'any' ? ANALYSIS_PLAN : directionFilter;

  const loadVersions = useCallback(async () => {
    try {
      const list = await fetchProjectVersions(projectId);
      setVersions(list);
      onVersionsChanged?.();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить версии.'));
    }
  }, [onVersionsChanged, projectId]);

  useEffect(() => {
    if (versionsProp) {
      setVersions(versionsProp);
    } else if (open) {
      void loadVersions();
    }
  }, [loadVersions, open, versionsProp]);

  const filteredVersions = useMemo(() => {
    if (directionFilter === 'any') return versions;
    return versions.filter((v) => v.direction === directionFilter);
  }, [directionFilter, versions]);

  useEffect(() => {
    if (!open) return;
    const initial =
      filteredVersions.find((v) => v.id === selectedVersionId) ?? filteredVersions[0] ?? null;
    setSelected(initial);
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
    setForm(emptyForm());
    setEditOpen(true);
  };

  const openEdit = () => {
    if (!selected) return;
    setEditMode('edit');
    setForm({
      versionName: selected.versionName,
      description: selected.description ?? '',
      physicalVolumeTypeId: selected.physicalVolumeTypeId?.toString() ?? '',
    });
    setEditOpen(true);
  };

  const handleSaveVersion = async () => {
    if (!form.versionName.trim() || !form.physicalVolumeTypeId) return;
    setSubmitting(true);
    setError(null);
    try {
      const body: ProjectVersionWriteRequest = {
        versionName: form.versionName.trim(),
        description: form.description.trim() || null,
        direction: editMode === 'edit' && selected ? selected.direction : defaultDirection,
        physicalVolumeTypeId: form.physicalVolumeTypeId
          ? Number(form.physicalVolumeTypeId)
          : null,
      };
      if (editMode === 'edit' && selected) {
        const updated = await updateProjectVersion(projectId, selected.id, body);
        setSelected(updated);
      } else {
        const created = await createProjectVersion(projectId, body);
        setSelected(created);
      }
      setEditOpen(false);
      await loadVersions();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить версию.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!selected) return;
    if (!window.confirm(`Удалить версию «${selected.versionName}»?`)) return;
    setError(null);
    try {
      await deleteProjectVersion(projectId, selected.id);
      setSelected(null);
      await loadVersions();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось удалить версию.'));
    }
  };

  const openCopy = () => {
    if (!selected) return;
    setCopyName(`${selected.versionName} (копия)`);
    setCopyDescription(selected.description ?? '');
    setCopyOpen(true);
  };

  const handleCopy = async () => {
    if (!selected || !copyName.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      const copy = await copyProjectVersion(projectId, selected.id, {
        newVersionName: copyName.trim(),
        description: copyDescription.trim() || null,
      });
      setCopyOpen(false);
      setSelected(copy);
      await loadVersions();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось скопировать версию.'));
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
              <IconButton size="small" onClick={() => void handleDelete()} disabled={!selected} aria-label="Удалить версию">
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Копировать версию">
              <IconButton size="small" onClick={openCopy} disabled={!selected} aria-label="Копировать версию">
                <ContentCopyIcon fontSize="small" />
              </IconButton>
            </Tooltip>
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
                onRowClicked={(row) => setSelected(row)}
                onRowDoubleClicked={(row) => {
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
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditOpen(false)} disabled={submitting}>
            Отмена
          </Button>
          <Button variant="contained" onClick={() => void handleSaveVersion()} disabled={submitting || !form.physicalVolumeTypeId}>
            Сохранить
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={copyOpen} onClose={() => setCopyOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Копировать версию</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {selected && (
              <Alert severity="info" variant="outlined">
                Источник: {selected.versionName}
              </Alert>
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
          <Button variant="contained" onClick={() => void handleCopy()} disabled={submitting}>
            Копировать
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

export { buildVersionLabel, ANALYSIS_PLAN, ANALYSIS_FACT };
