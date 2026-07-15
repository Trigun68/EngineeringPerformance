import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  TextField,
  Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import {
  fetchPhysicalVolumeById,
  updatePhysicalVolume,
} from '../../../api/projectDataApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { usePermission } from '../../../hooks/usePermission';

export interface ProjectPhysicalVolumeQuickEditDialogProps {
  open: boolean;
  projectId: number;
  versionId: number;
  rowId: number | null;
  nomenclature: string;
  quantity: number;
  onClose: () => void;
  onSaved?: () => void;
}

export function ProjectPhysicalVolumeQuickEditDialog({
  open,
  projectId,
  versionId,
  rowId,
  nomenclature,
  quantity,
  onClose,
  onSaved,
}: ProjectPhysicalVolumeQuickEditDialogProps) {
  const { canEdit } = usePermission('ProjectPhysicalVolumes');
  const [quantityDraft, setQuantityDraft] = useState(String(quantity));
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setQuantityDraft(String(quantity));
      setError(null);
    }
  }, [open, quantity]);

  const handleSave = useCallback(async () => {
    if (!rowId || !canEdit) return;
    const parsed = Number(quantityDraft.replace(',', '.'));
    if (!Number.isFinite(parsed)) {
      setError('Укажите корректное количество.');
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const entity = await fetchPhysicalVolumeById(projectId, versionId, rowId);
      if (!entity) {
        setError('Запись не найдена.');
        return;
      }
      await updatePhysicalVolume(projectId, versionId, rowId, {
        physicalVolumeId: entity.physicalVolumeId,
        quantity: parsed,
        facilityCode: entity.facilityCode,
        facilityName: entity.facilityName,
        workName: entity.workName,
        laborHoursPerUnit: entity.laborHoursPerUnit,
      });
      onSaved?.();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить изменения.'));
    } finally {
      setSaving(false);
    }
  }, [canEdit, onClose, onSaved, projectId, quantityDraft, rowId, versionId]);

  useEffect(() => {
    if (!open || !rowId) return;
    setLoading(true);
    void fetchPhysicalVolumeById(projectId, versionId, rowId)
      .then((entity) => {
        if (entity) setQuantityDraft(String(entity.quantity));
      })
      .catch((err) => {
        setError(getApiErrorMessage(err, 'Не удалось загрузить запись.'));
      })
      .finally(() => setLoading(false));
  }, [open, projectId, rowId, versionId]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth data-testid="ppv-quick-edit-dialog">
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        Редактирование физ. объёма
        <IconButton aria-label="Закрыть" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {nomenclature || '—'}
        </Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
            <CircularProgress size={24} />
          </Box>
        ) : (
          <TextField
            label="Количество"
            type="number"
            fullWidth
            size="small"
            value={quantityDraft}
            disabled={!canEdit}
            onChange={(e) => setQuantityDraft(e.target.value)}
            inputProps={{ step: 'any' }}
          />
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Отмена</Button>
        {canEdit && (
          <Button variant="contained" disabled={saving || loading || !rowId} onClick={() => void handleSave()}>
            {saving ? 'Сохранение…' : 'Сохранить'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
