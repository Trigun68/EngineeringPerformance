import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import type {
  NormSuggestion,
  ReferenceFormula,
} from '../../../api/factNormAnalyticsApi';
import { createNormFromSuggestion } from '../../../api/factNormAnalyticsApi';
import { FactVsNormChart } from './FactVsNormChart';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

interface Props {
  open: boolean;
  suggestion: NormSuggestion | null;
  formulas: ReferenceFormula[];
  chartPoints: { x: number; y: number }[];
  onClose: () => void;
  onSaved: () => void;
}

export function CreateNormFromSuggestionDialog({
  open,
  suggestion,
  formulas,
  chartPoints,
  onClose,
  onSaved,
}: Props) {
  const [effectiveFrom, setEffectiveFrom] = useState(todayIso());
  const [formulaType, setFormulaType] = useState('LINEAR');
  const [params, setParams] = useState<Record<string, number>>({});
  const [mean, setMean] = useState(0);
  const [sigma, setSigma] = useState(0);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [previewParams, setPreviewParams] = useState<Record<string, number>>({});

  useEffect(() => {
    if (!open || !suggestion) return;
    setEffectiveFrom(todayIso());
    setFormulaType(suggestion.proposedFormulaType);
    setParams({ ...suggestion.proposedParameters });
    setPreviewParams({ ...suggestion.proposedParameters });
    setMean(suggestion.proposedMeanPerformance);
    setSigma(suggestion.proposedStdDeviation);
    setError(null);
  }, [open, suggestion]);

  // Debounce live preview 300ms
  useEffect(() => {
    const t = window.setTimeout(() => setPreviewParams({ ...params }), 300);
    return () => window.clearTimeout(t);
  }, [params]);

  const selectedFormula = useMemo(
    () => formulas.find((f) => f.code === formulaType) ?? null,
    [formulas, formulaType],
  );

  const handleFormulaChange = (code: string) => {
    setFormulaType(code);
    const f = formulas.find((x) => x.code === code);
    if (!f) return;
    const defaults: Record<string, number> = {};
    for (const name of f.paramNames) defaults[name] = name === 'a' ? 1 : 0;
    setParams(defaults);
  };

  const handleSave = async () => {
    if (!suggestion) return;
    if (
      !window.confirm(
        `Создать норму с effectiveFrom = ${effectiveFrom}? Предыдущая норма будет закрыта этой датой.`,
      )
    ) {
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await createNormFromSuggestion({
        suggestionId: suggestion.id,
        effectiveFrom,
        formulaType,
        parameters: params,
        meanPerformance: mean,
        stdDeviation: sigma,
      });
      onSaved();
      onClose();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Ошибка сохранения');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="md"
      aria-modal
      data-testid="create-norm-dialog"
    >
      <DialogTitle>Создание нормы</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            label="Дата начала действия"
            type="date"
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
            InputLabelProps={{ shrink: true }}
            data-testid="create-norm-effective-from"
          />
          <FormControl fullWidth size="small">
            <InputLabel>Тип формулы</InputLabel>
            <Select
              label="Тип формулы"
              value={formulaType}
              onChange={(e) => handleFormulaChange(String(e.target.value))}
              data-testid="create-norm-formula-type"
            >
              {formulas.map((f) => (
                <MenuItem key={f.code} value={f.code}>
                  {f.displayName} ({f.formula})
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {selectedFormula?.paramNames.map((name) => (
            <TextField
              key={name}
              label={`Параметр ${name}`}
              type="number"
              size="small"
              value={params[name] ?? 0}
              onChange={(e) =>
                setParams((prev) => ({ ...prev, [name]: Number(e.target.value) }))
              }
            />
          ))}
          <TextField
            label="Средняя производительность"
            type="number"
            size="small"
            value={mean}
            onChange={(e) => setMean(Number(e.target.value))}
          />
          <TextField
            label="σ"
            type="number"
            size="small"
            value={sigma}
            onChange={(e) => setSigma(Number(e.target.value))}
            error={sigma <= 0}
            helperText={sigma <= 0 ? 'σ должна быть > 0' : undefined}
          />
          <Typography variant="subtitle2">Превью кривой</Typography>
          <FactVsNormChart
            title={suggestion?.groupName ?? ''}
            points={chartPoints}
            currentFormula={formulaType}
            currentParams={null}
            proposedFormula={formulaType}
            proposedParams={previewParams}
          />
          {error && (
            <Typography color="error" variant="body2">
              {error}
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Отмена</Button>
        <Button
          variant="contained"
          disabled={saving || sigma <= 0 || !suggestion}
          onClick={() => void handleSave()}
          data-testid="create-norm-save"
        >
          Сохранить
        </Button>
      </DialogActions>
    </Dialog>
  );
}
