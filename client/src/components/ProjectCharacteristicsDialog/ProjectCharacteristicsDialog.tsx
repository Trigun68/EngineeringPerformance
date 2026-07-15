import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import {
  fetchProjectCharacteristics,
  saveProjectCharacteristics,
  type ProjectCharacteristicSelection,
} from '../../api/projectCharacteristicsApi';
import { getApiErrorMessage } from '../../services/apiError';

interface ProjectCharacteristicsDialogProps {
  open: boolean;
  projectId: number | null;
  onClose: () => void;
  onSaved?: () => void;
}

export function ProjectCharacteristicsDialog({
  open,
  projectId,
  onClose,
  onSaved,
}: ProjectCharacteristicsDialogProps) {
  const [rows, setRows] = useState<ProjectCharacteristicSelection[]>([]);
  const [draft, setDraft] = useState<Record<number, number | null>>({});
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchProjectCharacteristics(projectId);
      setRows(data);
      setDraft(
        Object.fromEntries(
          data.map((item) => [item.characteristicId, item.selectedOptionId ?? null]),
        ),
      );
    } catch {
      setError('Не удалось загрузить характеристики проекта.');
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    if (open) {
      void load();
    }
  }, [open, load]);

  const handleSave = async () => {
    if (!projectId) return;
    setSaving(true);
    setError(null);
    try {
      const selections = Object.entries(draft)
        .filter(([, optionId]) => optionId != null)
        .map(([characteristicId, optionId]) => ({
          characteristicId: Number(characteristicId),
          optionId: optionId as number,
        }));
      await saveProjectCharacteristics(projectId, selections);
      onSaved?.();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить характеристики проекта.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Характеристики проекта</DialogTitle>
      <DialogContent>
        <Box sx={{ py: 1 }}>
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          {loading ? (
            <Typography variant="body2" color="text.secondary">
              Загрузка характеристик...
            </Typography>
          ) : rows.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Для проекта не настроены характеристики.
            </Typography>
          ) : (
            <Stack spacing={1.5}>
              {rows.map((row) => (
                <Stack
                  key={row.characteristicId}
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1.5}
                  alignItems={{ xs: 'flex-start', sm: 'center' }}
                >
                  <Typography sx={{ width: { xs: '100%', sm: 260 } }}>
                    {row.characteristicName}
                  </Typography>
                  <TextField
                    select
                    size="small"
                    fullWidth
                    label="Значение"
                    value={draft[row.characteristicId] ?? ''}
                    onChange={(e) => {
                      const value = e.target.value;
                      setDraft((prev) => ({
                        ...prev,
                        [row.characteristicId]: value === '' ? null : Number(value),
                      }));
                    }}
                  >
                    <MenuItem value="">Не выбрано</MenuItem>
                    {row.options.map((option) => (
                      <MenuItem key={option.id} value={option.id}>
                        {option.name}
                        {option.valuePercent != null ? ` (${option.valuePercent}%)` : ''}
                      </MenuItem>
                    ))}
                  </TextField>
                </Stack>
              ))}
            </Stack>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Закрыть</Button>
        <Button
          variant="contained"
          onClick={() => void handleSave()}
          disabled={!projectId || loading || saving}
        >
          Сохранить
        </Button>
      </DialogActions>
    </Dialog>
  );
}
