import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  Grid,
  TextField,
  Typography,
} from '@mui/material';
import {
  createIicNorm,
  updateIicNorm,
  type IicNorm,
  type IicNormHistoryItem,
  type IicNormWriteRequest,
} from '../../api/registersApi';
import { DeviationFormulaEditor } from '../../features/norms/DeviationFormulaEditor';
import { NormFormulaEditor } from '../../features/norms/NormFormulaEditor';
import { defaultDeviationFormula, defaultNormFormula } from '../../types/norms';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../utils/normalizeNormFormulas';
import { getApiErrorMessage } from '../../services/apiError';
import { DirectoryFormRow } from './DirectoryEditDialog';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';

interface IicNormHistoryDialogProps {
  open: boolean;
  title: string;
  groupLabel: string;
  unitLabel: string;
  workDirectionId: number;
  initial?: IicNormHistoryItem | null;
  onClose: () => void;
  onSaved: (saved: IicNorm) => void;
}

function toWriteRequest(workDirectionId: number, row: IicNormHistoryItem): IicNormWriteRequest {
  const normFormula = normalizeNormFormula(row.normFormula, row.performance);
  const deviationFormula = normalizeDeviationFormula(row.deviationFormula, row.performance, row.standardDeviation);
  const performance = normFormula.coefficients.a ?? row.performance ?? null;
  const unitId = row.unitId != null && row.unitId > 0 ? row.unitId : null;
  return {
    workDirectionId,
    performance,
    estimate: row.estimate ?? null,
    standardDeviation: row.standardDeviation ?? null,
    unitId,
    effectiveFrom: row.effectiveFrom,
    effectiveTo: row.effectiveTo ?? null,
    normFormula,
    deviationFormula,
  };
}

function emptyHistoryItem(): IicNormHistoryItem {
  return {
    effectiveFrom: new Date().toISOString().slice(0, 10),
    effectiveTo: null,
    performance: null,
    estimate: null,
    standardDeviation: null,
    unitId: null,
    normFormula: defaultNormFormula(),
    deviationFormula: defaultDeviationFormula(),
  };
}

function hydrateHistoryItem(initial: IicNormHistoryItem): IicNormHistoryItem {
  const normFormula = normalizeNormFormula(initial.normFormula, initial.performance);
  const deviationFormula = normalizeDeviationFormula(
    initial.deviationFormula,
    initial.performance,
    initial.standardDeviation,
  );
  return {
    ...initial,
    normFormula,
    deviationFormula,
    performance: normFormula.coefficients.a ?? initial.performance ?? null,
    standardDeviation:
      initial.standardDeviation
      ?? (normFormula.coefficients.a && deviationFormula.coefficients.sigma
        ? normFormula.coefficients.a * deviationFormula.coefficients.sigma
        : null),
  };
}

export function IicNormHistoryDialog({
  open,
  title,
  groupLabel,
  unitLabel,
  workDirectionId,
  initial,
  onClose,
  onSaved,
}: IicNormHistoryDialogProps) {
  const [form, setForm] = useState<IicNormHistoryItem>(emptyHistoryItem);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const normFormula = form.normFormula ?? defaultNormFormula(form.performance ?? 0);
  const deviationFormula = form.deviationFormula ?? defaultDeviationFormula();

  useEffect(() => {
    if (!open) return;
    setError(null);
    setForm(initial ? hydrateHistoryItem(initial) : emptyHistoryItem());
  }, [initial, open]);

  const handleSave = async () => {
    if (!form.effectiveFrom) return;
    setError(null);
    setSubmitting(true);
    try {
      const payload = toWriteRequest(workDirectionId, form);
      const saved = initial?.id
        ? await updateIicNorm(initial.id, payload)
        : await createIicNorm(payload);
      onSaved(saved);
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить запись истории.'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md" data-testid="iic-norm-history-dialog">
      <WorkingOverlay open={submitting} />
      <Box sx={{ bgcolor: '#2C3E50', px: 2, py: 1.5 }}>
        <Typography sx={{ color: '#ECF0F1', fontSize: 16, fontWeight: 600 }}>{title}</Typography>
      </Box>
      <DialogContent sx={{ pt: 2 }}>
        <Grid container spacing={1} columnSpacing={2}>
          <DirectoryFormRow label="Группа">
            <TextField value={groupLabel} fullWidth size="small" InputProps={{ readOnly: true }} />
          </DirectoryFormRow>
          <DirectoryFormRow label="Ед. изм.">
            <TextField value={unitLabel || '—'} fullWidth size="small" InputProps={{ readOnly: true }} />
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
          <DirectoryFormRow label="Параметрическая норма N(V)">
            <NormFormulaEditor
              key={initial?.id ?? 'new-history-norm'}
              chartVisible={open}
              value={normFormula}
              deviationFormula={deviationFormula}
              onChange={(nextNorm) =>
                setForm({
                  ...form,
                  normFormula: nextNorm,
                  performance: nextNorm.coefficients.a ?? form.performance,
                })
              }
            />
          </DirectoryFormRow>
          <DirectoryFormRow label="Отклонение от нормы, %">
            <DeviationFormulaEditor
              value={deviationFormula}
              onChange={(nextDev) => {
                const sigma = nextDev.coefficients.sigma ?? 0;
                const perf = normFormula.coefficients.a ?? form.performance ?? 0;
                setForm({
                  ...form,
                  deviationFormula: nextDev,
                  standardDeviation: perf > 0 ? sigma * perf : form.standardDeviation,
                });
              }}
            />
          </DirectoryFormRow>
        </Grid>
        {error && (
          <Alert severity="error" sx={{ mt: 1 }}>
            {error}
          </Alert>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={submitting}>
          Отмена
        </Button>
        <Button variant="contained" onClick={() => void handleSave()} disabled={submitting}>
          {submitting ? 'Сохранение...' : 'Сохранить'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
