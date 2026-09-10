import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import {
  createLaborIicBinding,
  deleteLaborIicBinding,
  fetchLaborIicBindings,
  updateLaborIicBinding,
  type LaborIicBinding,
} from '../../api/directoriesApi';
import { HierarchicalSelect } from '../../components/dictionary/HierarchicalSelect';
import { ToolbarActionButton } from '../../components/export/ToolbarActionButton';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { getApiErrorMessage } from '../../services/apiError';
import { formatIisNorm } from '../../utils/formatIisNorm';
import { resolveIicDirectionUnitName } from '../../utils/resolveIicDirectionUnit';

interface LaborIicBindingDialogProps {
  open: boolean;
  laborIntensityId: number | null;
  title: string;
  canEdit: boolean;
  onClose: () => void;
  onChanged: () => void;
}

function toDateInput(value: string | null | undefined): string {
  if (!value) return '';
  return value.slice(0, 10);
}

export function LaborIicBindingDialog({
  open,
  laborIntensityId,
  title,
  canEdit,
  onClose,
  onChanged,
}: LaborIicBindingDialogProps) {
  const iicItems = useDictionaryStore((s) => s.lists.iicWorkDirections);
  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const [rows, setRows] = useState<LaborIicBinding[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<LaborIicBinding | null>(null);
  const [effectiveFrom, setEffectiveFrom] = useState('');
  const [directionId, setDirectionId] = useState<number | null>(null);

  const load = async (id: number) => {
    setError(null);
    try {
      setRows(await fetchLaborIicBindings(id));
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить привязки ИИС.'));
    }
  };

  useEffect(() => {
    if (!open || laborIntensityId == null) return;
    setSelectedId(null);
    void load(laborIntensityId);
  }, [open, laborIntensityId]);

  const openCreate = () => {
    setEditing(null);
    setEffectiveFrom(new Date().toISOString().slice(0, 10));
    setDirectionId(null);
    setEditorOpen(true);
  };

  const openEdit = () => {
    const row = rows.find((r) => r.id === selectedId);
    if (!row) return;
    setEditing(row);
    setEffectiveFrom(toDateInput(row.effectiveFrom));
    setDirectionId(row.iicWorkDirectionId);
    setEditorOpen(true);
  };

  const saveEditor = async () => {
    if (laborIntensityId == null || !effectiveFrom) return;
    setError(null);
    try {
      const body = { iicWorkDirectionId: directionId, effectiveFrom };
      if (editing) {
        await updateLaborIicBinding(laborIntensityId, editing.id, body);
      } else {
        await createLaborIicBinding(laborIntensityId, body);
      }
      setEditorOpen(false);
      await load(laborIntensityId);
      onChanged();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить привязку.'));
    }
  };

  const removeSelected = async () => {
    if (laborIntensityId == null || selectedId == null) return;
    setError(null);
    try {
      await deleteLaborIicBinding(laborIntensityId, selectedId);
      setSelectedId(null);
      await load(laborIntensityId);
      onChanged();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось удалить привязку.'));
    }
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
        <DialogTitle>{title || 'Привязки ИИС'}</DialogTitle>
        <DialogContent>
          {error && <Alert severity="error" sx={{ mb: 1 }}>{error}</Alert>}
          <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
            <ToolbarActionButton disabled={!canEdit} onClick={openCreate}>
              Добавить
            </ToolbarActionButton>
            <ToolbarActionButton disabled={!canEdit || selectedId == null} onClick={openEdit}>
              Редактировать
            </ToolbarActionButton>
            <ToolbarActionButton disabled={!canEdit || selectedId == null} onClick={() => void removeSelected()}>
              Удалить
            </ToolbarActionButton>
          </Stack>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Действует с</TableCell>
                <TableCell>Действует по</TableCell>
                <TableCell>Группа ИИС</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3}>
                    <Typography color="text.secondary">Записей нет</Typography>
                  </TableCell>
                </TableRow>
              )}
              {rows.map((row) => (
                <TableRow
                  key={row.id}
                  hover
                  selected={selectedId === row.id}
                  onClick={() => setSelectedId(row.id)}
                  onDoubleClick={() => {
                    setSelectedId(row.id);
                    if (canEdit) {
                      setEditing(row);
                      setEffectiveFrom(toDateInput(row.effectiveFrom));
                      setDirectionId(row.iicWorkDirectionId);
                      setEditorOpen(true);
                    }
                  }}
                  sx={{ cursor: 'pointer' }}
                >
                  <TableCell>{toDateInput(row.effectiveFrom)}</TableCell>
                  <TableCell>{toDateInput(row.effectiveTo) || '—'}</TableCell>
                  <TableCell>{row.iicGroupName || '—'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Закрыть</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editorOpen} onClose={() => setEditorOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? 'Редактирование периода' : 'Новая привязка'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Действует с"
              type="date"
              value={effectiveFrom}
              onChange={(e) => setEffectiveFrom(e.target.value)}
              InputLabelProps={{ shrink: true }}
              fullWidth
            />
            <HierarchicalSelect
              items={iicItems}
              value={directionId}
              onChange={setDirectionId}
              allowEmpty
              emptyLabel="(без направления)"
              formatLabel={(item) => formatIisNorm({
                code: item.code,
                name: item.name,
                unitName: resolveIicDirectionUnitName(item.id, iicItems, getUnitName),
              })}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditorOpen(false)}>Отмена</Button>
          <Button variant="contained" onClick={() => void saveEditor()}>Сохранить</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
