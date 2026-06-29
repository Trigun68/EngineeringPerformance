import { useState } from 'react';
import {
  Alert,
  Box,
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
  createCharacteristicOption,
  deleteCharacteristicOption,
  updateCharacteristicOption,
  type CharacteristicOption,
} from '../../api/directoriesApi';

interface CharacteristicOptionsPanelProps {
  characteristicId: number;
  options: CharacteristicOption[];
  canEdit: boolean;
  onChanged: () => Promise<void>;
}

interface OptionDraft {
  name: string;
  valuePercent: string;
  sortOrder: string;
}

function toDraft(option: CharacteristicOption): OptionDraft {
  return {
    name: option.name,
    valuePercent: option.valuePercent == null ? '' : String(option.valuePercent),
    sortOrder: String(option.sortOrder),
  };
}

function emptyDraft(): OptionDraft {
  return { name: '', valuePercent: '', sortOrder: '0' };
}

export function CharacteristicOptionsPanel({
  characteristicId,
  options,
  canEdit,
  onChanged,
}: CharacteristicOptionsPanelProps) {
  const [newOption, setNewOption] = useState<OptionDraft>(emptyDraft());
  const [modalOpen, setModalOpen] = useState(false);
  const [editingOption, setEditingOption] = useState<CharacteristicOption | null>(null);
  const [draft, setDraft] = useState<OptionDraft>(emptyDraft());
  const [error, setError] = useState<string | null>(null);

  const openCreate = () => {
    setEditingOption(null);
    setDraft(emptyDraft());
    setModalOpen(true);
  };

  const openEdit = (option: CharacteristicOption) => {
    setEditingOption(option);
    setDraft(toDraft(option));
    setModalOpen(true);
  };

  const handleAdd = async () => {
    if (!newOption.name.trim()) return;
    setError(null);
    try {
      await createCharacteristicOption(characteristicId, {
        name: newOption.name.trim(),
        valuePercent: newOption.valuePercent ? Number(newOption.valuePercent) : null,
        sortOrder: Number(newOption.sortOrder) || 0,
      });
      setNewOption(emptyDraft());
      await onChanged();
    } catch {
      setError('Не удалось добавить опцию.');
    }
  };

  const handleSaveModal = async () => {
    if (!draft.name.trim()) return;
    setError(null);
    try {
      if (editingOption) {
        await updateCharacteristicOption(characteristicId, editingOption.id, {
          name: draft.name.trim(),
          valuePercent: draft.valuePercent ? Number(draft.valuePercent) : null,
          sortOrder: Number(draft.sortOrder) || 0,
        });
      }
      setModalOpen(false);
      await onChanged();
    } catch {
      setError('Не удалось сохранить опцию.');
    }
  };

  const handleDelete = async (optionId: number) => {
    if (!window.confirm('Удалить опцию?')) return;
    setError(null);
    try {
      await deleteCharacteristicOption(characteristicId, optionId);
      await onChanged();
    } catch {
      setError('Не удалось удалить опцию.');
    }
  };

  return (
    <Box sx={{ mt: 3 }}>
      <Typography variant="subtitle1" sx={{ mb: 1 }}>
        Варианты характеристики #{characteristicId}
      </Typography>
      {error && <Alert severity="error" sx={{ mb: 1 }}>{error}</Alert>}

      {canEdit && (
        <Stack direction="row" spacing={1} sx={{ mb: 2 }} alignItems="center">
          <TextField
            size="small"
            label="Описание варианта"
            value={newOption.name}
            onChange={(e) => setNewOption({ ...newOption, name: e.target.value })}
            sx={{ flex: 2 }}
          />
          <TextField
            size="small"
            label="PD, %"
            type="number"
            value={newOption.valuePercent}
            onChange={(e) => setNewOption({ ...newOption, valuePercent: e.target.value })}
            sx={{ width: 110 }}
          />
          <TextField
            size="small"
            label="Порядок"
            type="number"
            value={newOption.sortOrder}
            onChange={(e) => setNewOption({ ...newOption, sortOrder: e.target.value })}
            sx={{ width: 100 }}
          />
          <Button variant="contained" onClick={() => void handleAdd()}>
            Добавить
          </Button>
          <Button variant="outlined" onClick={openCreate}>
            В модальном окне
          </Button>
        </Stack>
      )}

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Описание варианта</TableCell>
            <TableCell width={100}>PD, %</TableCell>
            <TableCell width={90}>Порядок</TableCell>
            {canEdit && <TableCell width={160}>Действия</TableCell>}
          </TableRow>
        </TableHead>
        <TableBody>
          {options.map((option) => (
            <TableRow
              key={option.id}
              hover
              sx={{ cursor: canEdit ? 'pointer' : 'default' }}
              onDoubleClick={() => canEdit && openEdit(option)}
            >
              <TableCell>{option.name}</TableCell>
              <TableCell>{option.valuePercent ?? ''}</TableCell>
              <TableCell>{option.sortOrder}</TableCell>
              {canEdit && (
                <TableCell>
                  <Stack direction="row" spacing={1}>
                    <Button size="small" variant="outlined" onClick={() => openEdit(option)}>
                      Изменить
                    </Button>
                    <Button size="small" color="error" onClick={() => void handleDelete(option.id)}>
                      Удалить
                    </Button>
                  </Stack>
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Dialog open={modalOpen} onClose={() => setModalOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{editingOption ? 'Редактирование варианта' : 'Новый вариант'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Описание варианта"
              value={draft.name}
              onChange={(e) => setDraft({ ...draft, name: e.target.value })}
              fullWidth
              size="small"
              autoFocus
            />
            <TextField
              label="PD, %"
              type="number"
              value={draft.valuePercent}
              onChange={(e) => setDraft({ ...draft, valuePercent: e.target.value })}
              fullWidth
              size="small"
            />
            <TextField
              label="Порядок"
              type="number"
              value={draft.sortOrder}
              onChange={(e) => setDraft({ ...draft, sortOrder: e.target.value })}
              fullWidth
              size="small"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setModalOpen(false)}>Отмена</Button>
          <Button variant="contained" onClick={() => void handleSaveModal()} disabled={!editingOption}>
            Сохранить
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
