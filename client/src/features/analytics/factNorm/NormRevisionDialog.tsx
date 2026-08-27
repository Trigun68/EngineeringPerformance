import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import type {
  NormComparison,
  NormSuggestion,
  ReferenceFormula,
} from '../../../api/factNormAnalyticsApi';
import { createNormFromSuggestion } from '../../../api/factNormAnalyticsApi';
import { createIicNorm, fetchIicNormById } from '../../../api/registersApi';
import { DeviationFormulaEditor } from '../../norms/DeviationFormulaEditor';
import { NormFormulaEditor } from '../../norms/NormFormulaEditor';
import { defaultDeviationFormula, defaultNormFormula } from '../../../types/norms';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../../utils/normalizeNormFormulas';
import { getApiErrorMessage } from '../../../services/apiError';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { FactVsNormChart } from './FactVsNormChart';
import { severityLabel, severityRatio } from './severity';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

interface Props {
  open: boolean;
  row: NormComparison | null;
  /** Готовое предложение алгоритма для этой строки, если оно есть. */
  suggestion: NormSuggestion | null;
  formulas: ReferenceFormula[];
  onClose: () => void;
  onSaved: () => void;
}

/**
 * Единая точка изменения нормы прямо со страницы «Сравнение факта и норм».
 * Если у строки есть готовое предложение алгоритма — редактируем и сохраняем его
 * (formulaType-код + параметры, через специализированный эндпоинт with suggestion tracking).
 * Если предложения нет (в т.ч. для строк с вердиктом «Норма актуальна») — открываем
 * полноценный редактор формулы реестра норм и создаём новую версию нормы вручную.
 */
export function NormRevisionDialog({ open, row, suggestion, formulas, onClose, onSaved }: Props) {
  const hasSuggestion = suggestion != null;

  // --- Ветка 1: редактирование по готовому предложению (code-формулы analytics) ---
  const [effectiveFrom, setEffectiveFrom] = useState(todayIso());
  const [formulaType, setFormulaType] = useState('LINEAR');
  const [params, setParams] = useState<Record<string, number>>({});
  const [mean, setMean] = useState(0);
  const [sigma, setSigma] = useState(0);
  const [previewParams, setPreviewParams] = useState<Record<string, number>>({});

  useEffect(() => {
    if (!open || !suggestion) return;
    setEffectiveFrom(todayIso());
    setFormulaType(suggestion.proposedFormulaType);
    setParams({ ...suggestion.proposedParameters });
    setPreviewParams({ ...suggestion.proposedParameters });
    setMean(suggestion.proposedMeanPerformance);
    setSigma(suggestion.proposedStdDeviation);
  }, [open, suggestion]);

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

  // --- Ветка 2: ручное редактирование без предложения (реестр норм, числовые формулы) ---
  const {
    data: currentNorm,
    isLoading: normLoading,
  } = useQuery({
    queryKey: ['iic-norm-by-id', row?.normId],
    queryFn: () => fetchIicNormById(row!.normId),
    enabled: open && !hasSuggestion && !!row,
  });

  const [manualEffectiveFrom, setManualEffectiveFrom] = useState(todayIso());
  const [manualNormFormula, setManualNormFormula] = useState(defaultNormFormula());
  const [manualDeviationFormula, setManualDeviationFormula] = useState(defaultDeviationFormula());
  const [manualPerformance, setManualPerformance] = useState<number | null>(null);
  const [manualStdDeviation, setManualStdDeviation] = useState<number | null>(null);

  useEffect(() => {
    if (!open || hasSuggestion) return;
    setManualEffectiveFrom(todayIso());
    if (currentNorm) {
      const nf = normalizeNormFormula(currentNorm.normFormula, currentNorm.performance);
      const df = normalizeDeviationFormula(
        currentNorm.deviationFormula,
        currentNorm.performance,
        currentNorm.standardDeviation,
      );
      setManualNormFormula(nf);
      setManualDeviationFormula(df);
      setManualPerformance(nf.coefficients.a ?? currentNorm.performance ?? null);
      setManualStdDeviation(currentNorm.standardDeviation ?? null);
    }
  }, [open, hasSuggestion, currentNorm]);

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSaveSuggestion = async () => {
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

  const handleSaveManual = async () => {
    if (!row || !manualEffectiveFrom) return;
    if (
      !window.confirm(
        `Создать новую версию нормы с effectiveFrom = ${manualEffectiveFrom}? Текущая версия будет закрыта этой датой.`,
      )
    ) {
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await createIicNorm({
        workDirectionId: row.workDirectionId,
        performance: manualPerformance,
        estimate: currentNorm?.estimate ?? null,
        standardDeviation: manualStdDeviation,
        unitId: currentNorm?.resolvedUnitId ?? currentNorm?.unitId ?? null,
        effectiveFrom: manualEffectiveFrom,
        effectiveTo: null,
        normFormula: manualNormFormula,
        deviationFormula: manualDeviationFormula,
      });
      onSaved();
      onClose();
    } catch (e) {
      setError(getApiErrorMessage(e, 'Не удалось сохранить норму.'));
    } finally {
      setSaving(false);
    }
  };

  const ratio = row ? severityRatio(row) : 0;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="md"
      aria-modal
      data-testid="norm-revision-dialog"
    >
      <WorkingOverlay open={saving || (normLoading && !hasSuggestion)} />
      <DialogTitle>
        Изменение нормы{row ? `: ${row.groupName}` : ''}
      </DialogTitle>
      <DialogContent>
        {row && (
          <Alert severity={ratio >= 1.5 ? 'error' : ratio >= 0.5 ? 'warning' : 'info'} sx={{ mb: 2 }}>
            Текущее отклонение факта от нормы: {severityLabel(ratio)}. Среднее по факту{' '}
            {row.factMeanPerformance.toFixed(3)} против нормы {row.normMeanPerformance.toFixed(3)}
            {' '}(Δ {row.deltaMean >= 0 ? '+' : ''}{row.deltaMean.toFixed(3)}).
          </Alert>
        )}

        {hasSuggestion ? (
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              Алгоритм подготовил предложение по этой группе. Значения ниже предзаполнены
              предложением — при необходимости скорректируйте их перед сохранением.
            </Typography>
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
                onChange={(e) => setParams((prev) => ({ ...prev, [name]: Number(e.target.value) }))}
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
            <Divider />
            <Typography variant="subtitle2">Превью: было / будет</Typography>
            <FactVsNormChart
              title={suggestion?.groupName ?? ''}
              points={row?.chartPoints ?? []}
              currentFormula={row?.currentFormula ?? formulaType}
              currentParams={row?.currentParameters ?? null}
              proposedFormula={formulaType}
              proposedParams={previewParams}
              deviationSigma={row?.normStdDeviation}
            />
          </Stack>
        ) : (
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              Готового предложения от алгоритма нет — задайте формулу нормы и коридор отклонения
              вручную. Будет создана новая версия нормы, действующая с указанной даты.
            </Typography>
            <TextField
              label="Дата начала действия новой версии"
              type="date"
              value={manualEffectiveFrom}
              onChange={(e) => setManualEffectiveFrom(e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            <NormFormulaEditor
              key={row?.normId ?? 'manual-norm'}
              chartVisible={open && !hasSuggestion}
              value={manualNormFormula}
              deviationFormula={manualDeviationFormula}
              onChange={(nextNorm) => {
                setManualNormFormula(nextNorm);
                setManualPerformance(nextNorm.coefficients.a ?? manualPerformance);
              }}
            />
            <DeviationFormulaEditor
              value={manualDeviationFormula}
              onChange={(nextDev) => {
                setManualDeviationFormula(nextDev);
                const sigmaRatio = nextDev.coefficients.sigma ?? 0;
                const perf = manualNormFormula.coefficients.a ?? manualPerformance ?? 0;
                setManualStdDeviation(perf > 0 ? sigmaRatio * perf : manualStdDeviation);
              }}
            />
          </Stack>
        )}

        {error && (
          <Box sx={{ mt: 2 }}>
            <Alert severity="error">{error}</Alert>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Отмена</Button>
        {hasSuggestion ? (
          <Button
            variant="contained"
            disabled={saving || sigma <= 0 || !suggestion}
            onClick={() => void handleSaveSuggestion()}
            data-testid="create-norm-save"
          >
            Сохранить
          </Button>
        ) : (
          <Button
            variant="contained"
            disabled={saving || normLoading || !row}
            onClick={() => void handleSaveManual()}
            data-testid="save-norm-manual"
          >
            Сохранить новую версию
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
