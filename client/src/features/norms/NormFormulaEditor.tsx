import { Suspense, lazy, useEffect, useMemo, useState } from 'react';
import { Alert, CircularProgress, MenuItem, Stack, TextField } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { fetchFormulaTemplates, previewNormCurve } from '../../api/normsApi';
import type { DeviationFormulaDto, NormFormulaDto } from '../../types/norms';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../utils/normalizeNormFormulas';
// График норм открывается только в модалке редактирования — Highcharts грузим по требованию (ISS-018).
const NormBandChart = lazy(() => import('./NormBandChart').then((m) => ({ default: m.NormBandChart })));

export interface NormFormulaEditorProps {
  value: NormFormulaDto;
  deviationFormula: DeviationFormulaDto;
  onChange: (value: NormFormulaDto) => void;
  /** Для reflow графика после открытия модального окна. */
  chartVisible?: boolean;
}

export function NormFormulaEditor({
  value,
  deviationFormula,
  onChange,
  chartVisible = true,
}: NormFormulaEditorProps) {
  const {
    data: templates = [],
    isLoading: templatesLoading,
    isError: templatesError,
  } = useQuery({ queryKey: ['formula-templates'], queryFn: fetchFormulaTemplates, staleTime: 60_000 });

  const template = templates.find((t) => t.type === value.type);
  const [band, setBand] = useState<Array<{ volume: number; center: number; upper: number; lower: number }>>([]);
  const [previewError, setPreviewError] = useState<string | null>(null);

  const normalizedNorm = useMemo(
    () => normalizeNormFormula(value, value.coefficients?.a),
    [value],
  );
  const normalizedDeviation = useMemo(
    () => normalizeDeviationFormula(deviationFormula, normalizedNorm.coefficients.a),
    [deviationFormula, normalizedNorm.coefficients.a],
  );

  const deviationRatio = normalizedDeviation.coefficients.sigma ?? 0;

  const warnZero = useMemo(() => {
    const a = normalizedNorm.coefficients.a ?? 0;
    const b = normalizedNorm.coefficients.b ?? 0;
    return a - b * normalizedNorm.maxVolume < 0;
  }, [normalizedNorm]);

  useEffect(() => {
    if (!chartVisible) return;
    const timer = setTimeout(() => {
      setPreviewError(null);
      void previewNormCurve(normalizedNorm, normalizedDeviation)
        .then((r) => setBand(r.band ?? []))
        .catch((error) => {
          setBand([]);
          const status = (error as { response?: { status?: number } })?.response?.status;
          setPreviewError(status ? `Ошибка preview (${status})` : 'Не удалось построить график');
        });
    }, 300);
    return () => clearTimeout(timer);
  }, [chartVisible, deviationRatio, normalizedDeviation, normalizedNorm]);

  if (templatesLoading) {
    return <CircularProgress size={24} />;
  }

  return (
    <Stack spacing={1.5}>
      {templatesError && (
        <Alert severity="error">Не удалось загрузить шаблоны формул. Проверьте подключение к API.</Alert>
      )}
      {templates.length === 0 && !templatesError && (
        <Alert severity="warning">Список типов формул пуст.</Alert>
      )}
      {previewError && (
        <Alert severity="warning">{previewError}</Alert>
      )}

      <TextField
        select
        size="small"
        label="Тип формулы"
        value={normalizedNorm.type}
        onChange={(e) => onChange({ ...value, type: Number(e.target.value) as NormFormulaDto['type'] })}
      >
        {templates.map((t) => (
          <MenuItem key={t.type} value={t.type}>{t.displayName}</MenuItem>
        ))}
      </TextField>

      {template?.coefficients.map((c) => (
        <TextField
          key={c.name}
          size="small"
          type="number"
          label={c.label}
          value={value.coefficients[c.name] ?? ''}
          onChange={(e) =>
            onChange({
              ...value,
              coefficients: { ...value.coefficients, [c.name]: e.target.value === '' ? 0 : Number(e.target.value) },
            })
          }
        />
      ))}

      {template?.requiresBounds && (
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          <TextField
            size="small"
            type="number"
            label="V min"
            value={value.minVolume}
            onChange={(e) => onChange({ ...value, minVolume: Number(e.target.value) })}
          />
          <TextField
            size="small"
            type="number"
            label="V max"
            value={value.maxVolume}
            onChange={(e) => onChange({ ...value, maxVolume: Number(e.target.value) })}
          />
          <TextField
            select
            size="small"
            label="Экстраполяция"
            value={value.extrapolation}
            onChange={(e) => onChange({ ...value, extrapolation: Number(e.target.value) as NormFormulaDto['extrapolation'] })}
          >
            <MenuItem value={1}>Ноль за пределами диапазона</MenuItem>
            <MenuItem value={2}>Линейно от границы</MenuItem>
          </TextField>
        </Stack>
      )}

      {warnZero && value.type === 1 && (
        <Alert severity="warning">При больших объемах норма станет нулевой (a - b·V_max &lt; 0)</Alert>
      )}

      <Suspense fallback={<CircularProgress size={20} />}>
        <NormBandChart band={band} deviationRatio={deviationRatio} chartVisible={chartVisible} />
      </Suspense>
    </Stack>
  );
}
