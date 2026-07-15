import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  Radio,
  RadioGroup,
  Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import {
  fetchImportIterations,
  rollbackImportIteration,
  type ImportDataType,
  type ImportIteration,
} from '../../../api/importApi';
import { getApiErrorMessage } from '../../../services/apiError';

export interface IterationDeleteDialogProps {
  open: boolean;
  dataType: ImportDataType;
  projectId: number;
  versionId: number;
  onClose: () => void;
  onSuccess?: () => void;
}

const DATA_TYPE_LABELS: Record<ImportDataType, string> = {
  'physical-volumes': 'физические объёмы',
  equipment: 'оборудование',
  'labor-intensities': 'трудоёмкость',
};

function formatIterationLabel(iteration: ImportIteration): string {
  const date = new Date(iteration.importedAt).toLocaleString('ru-RU');
  const filePart = iteration.sourceFileName ? ` — ${iteration.sourceFileName}` : '';
  const descPart = iteration.description ? ` (${iteration.description})` : '';
  return `${date}: ${iteration.recordCount} записей${filePart}${descPart}`;
}

export function IterationDeleteDialog({
  open,
  dataType,
  projectId,
  versionId,
  onClose,
  onSuccess,
}: IterationDeleteDialogProps) {
  const [iterations, setIterations] = useState<ImportIteration[]>([]);
  const [selectedAt, setSelectedAt] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const title = useMemo(
    () => `Удаление итерации: ${DATA_TYPE_LABELS[dataType]}`,
    [dataType],
  );

  const loadIterations = useCallback(async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const list = await fetchImportIterations(projectId, versionId, dataType);
      setIterations(list);
      setSelectedAt(list[0]?.importedAt ?? '');
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить список итераций.'));
      setIterations([]);
      setSelectedAt('');
    } finally {
      setLoading(false);
    }
  }, [dataType, projectId, versionId]);

  useEffect(() => {
    if (open) {
      void loadIterations();
    }
  }, [open, loadIterations]);

  const handleDelete = async () => {
    if (!selectedAt) return;
    if (!window.confirm('Удалить выбранную итерацию импорта? Это действие необратимо.')) return;

    setDeleting(true);
    setError(null);
    setResult(null);
    try {
      const deleted = await rollbackImportIteration(projectId, versionId, dataType, selectedAt);
      setResult(`Удалено записей: ${deleted}`);
      onSuccess?.();
      await loadIterations();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось удалить итерацию.'));
    } finally {
      setDeleting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth data-testid="iteration-delete-dialog">
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        {title}
        <IconButton aria-label="Закрыть" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Выберите итерацию импорта для отката. Будут удалены все записи, загруженные в этой итерации.
        </Typography>

        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {result && <Alert severity="success" sx={{ mb: 2 }}>{result}</Alert>}

        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
            <CircularProgress size={28} />
          </Box>
        ) : iterations.length === 0 ? (
          <Alert severity="info">Нет итераций импорта для удаления.</Alert>
        ) : (
          <RadioGroup value={selectedAt} onChange={(e) => setSelectedAt(e.target.value)}>
            {iterations.map((iteration) => (
              <FormControlLabel
                key={iteration.importedAt}
                value={iteration.importedAt}
                control={<Radio size="small" />}
                label={formatIterationLabel(iteration)}
              />
            ))}
          </RadioGroup>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Закрыть</Button>
        <Button
          variant="contained"
          color="error"
          disabled={!selectedAt || deleting || loading}
          onClick={() => void handleDelete()}
        >
          {deleting ? 'Удаление…' : 'Удалить итерацию'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
