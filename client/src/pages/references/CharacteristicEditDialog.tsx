import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import {
  createCharacteristicOption,
  deleteCharacteristicOption,
  fetchCharacteristicOptions,
  updateCharacteristicOption,
  updateDirectoryEntry,
  archiveDirectoryEntry,
  unarchiveDirectoryEntry,
  type CharacteristicOption,
  type DirectoryEntry,
} from '../../api/directoriesApi';

interface OptionPair {
  id?: number;
  key: string;
  value: string;
  sortOrder: number;
}

interface CharacteristicEditDialogProps {
  open: boolean;
  characteristic: DirectoryEntry | null;
  canEdit: boolean;
  onClose: () => void;
  onSaved: () => Promise<void>;
}

function emptyPair(sortOrder = 0): OptionPair {
  return { key: '', value: '', sortOrder };
}

function toPairs(options: CharacteristicOption[]): OptionPair[] {
  return options.map((o) => ({
    id: o.id,
    key: o.name,
    value: o.valuePercent == null ? '' : String(o.valuePercent),
    sortOrder: o.sortOrder,
  }));
}

export function CharacteristicEditDialog({
  open,
  characteristic,
  canEdit,
  onClose,
  onSaved,
}: CharacteristicEditDialogProps) {
  const [name, setName] = useState('');
  const [pairs, setPairs] = useState<OptionPair[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [isArchived, setIsArchived] = useState(false);

  const load = useCallback(async () => {
    if (!characteristic) return;
    setName(characteristic.name);
    setIsArchived(characteristic.isArchived);
    const options = await fetchCharacteristicOptions(characteristic.id);
    setPairs(options.length > 0 ? toPairs(options) : [emptyPair()]);
  }, [characteristic]);

  useEffect(() => {
    if (open && characteristic) {
      setError(null);
      void load();
    }
  }, [characteristic, load, open]);

  const handleAddPair = () => {
    setPairs((prev) => [...prev, emptyPair(prev.length)]);
  };

  const handleRemovePair = (index: number) => {
    setPairs((prev) => prev.filter((_, i) => i !== index));
  };

  const handleSave = async () => {
    if (!characteristic || !name.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      await updateDirectoryEntry('project-characteristics', characteristic.id, {
        name: name.trim(),
        sortOrder: characteristic.sortOrder,
        properties: {},
      });

      const existing = await fetchCharacteristicOptions(characteristic.id);
      const keptIds = new Set(pairs.filter((p) => p.id).map((p) => p.id!));

      for (const option of existing) {
        if (!keptIds.has(option.id)) {
          await deleteCharacteristicOption(characteristic.id, option.id);
        }
      }

      for (const [index, pair] of pairs.entries()) {
        if (!pair.key.trim()) continue;
        const body = {
          name: pair.key.trim(),
          valuePercent: pair.value ? Number(pair.value) : null,
          sortOrder: pair.sortOrder ?? index,
        };
        if (pair.id) {
          await updateCharacteristicOption(characteristic.id, pair.id, body);
        } else {
          await createCharacteristicOption(characteristic.id, body);
        }
      }

      if (isArchived !== characteristic.isArchived) {
        if (isArchived) {
          await archiveDirectoryEntry('project-characteristics', characteristic.id);
        } else {
          await unarchiveDirectoryEntry('project-characteristics', characteristic.id);
        }
      }

      onClose();
      await onSaved();
    } catch {
      setError('Не удалось сохранить характеристику.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <Box sx={{ bgcolor: '#2C3E50', px: 2, py: 1.5 }}>
        <Typography sx={{ color: '#ECF0F1', fontSize: 16, fontWeight: 600 }}>
          Редактирование характеристики
        </Typography>
      </Box>
      <DialogContent>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <TextField
          label="Название"
          value={name}
          onChange={(e) => setName(e.target.value)}
          fullWidth
          size="small"
          sx={{ mb: 2 }}
          required
        />

        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Пары значений
        </Typography>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Ключ</TableCell>
              <TableCell>Значение</TableCell>
              {canEdit && <TableCell width={56} />}
            </TableRow>
          </TableHead>
          <TableBody>
            {pairs.map((pair, index) => (
              <TableRow key={pair.id ?? `new-${index}`}>
                <TableCell>
                  <TextField
                    value={pair.key}
                    onChange={(e) =>
                      setPairs((prev) =>
                        prev.map((p, i) => (i === index ? { ...p, key: e.target.value } : p)),
                      )
                    }
                    size="small"
                    fullWidth
                    disabled={!canEdit}
                  />
                </TableCell>
                <TableCell>
                  <TextField
                    value={pair.value}
                    onChange={(e) =>
                      setPairs((prev) =>
                        prev.map((p, i) => (i === index ? { ...p, value: e.target.value } : p)),
                      )
                    }
                    size="small"
                    fullWidth
                    disabled={!canEdit}
                  />
                </TableCell>
                {canEdit && (
                  <TableCell>
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => handleRemovePair(index)}
                      aria-label="Удалить пару"
                    >
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>

        {canEdit && (
          <Button size="small" variant="outlined" sx={{ mt: 1 }} onClick={handleAddPair}>
            Добавить пару
          </Button>
        )}

        <Box sx={{ mt: 2 }}>
          <label>
            <input
              type="checkbox"
              checked={isArchived}
              onChange={(e) => setIsArchived(e.target.checked)}
              disabled={!canEdit}
            />{' '}
            Архивная запись
          </label>
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={submitting}>
          Отмена
        </Button>
        {canEdit && (
          <Button variant="contained" onClick={() => void handleSave()} disabled={submitting}>
            Сохранить
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
