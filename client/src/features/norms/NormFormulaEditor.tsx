import { Suspense, lazy, useEffect, useMemo, useState } from 'react';
import { Alert, CircularProgress, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { fetchFormulaTemplates, previewNormCurve } from '../../api/normsApi';
import type { DeviationFormulaDto, FormulaType, NormFormulaDto } from '../../types/norms';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../utils/normalizeNormFormulas';
import { isKimCurveType, kimDeviation, muFromProductivity, withKimDerived } from './kimCurve';
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
  const kim = isKimCurveType(value.type);

  const normalizedNorm = useMemo(
    () => (kim ? withKimDerived(value, value.coefficients.sigma ?? deviationFormula.coefficients.sigma ?? 0) : normalizeNormFormula(value, value.coefficients?.a)),
    [deviationFormula.coefficients.sigma, kim, value],
  );
  const normalizedDeviation = useMemo(
    () => (kim
      ? kimDeviation(normalizedNorm.coefficients.sigma ?? 0)
      : normalizeDeviationFormula(deviationFormula, normalizedNorm.coefficients.a)),
    [deviationFormula, kim, normalizedNorm.coefficients.a, normalizedNorm.coefficients.sigma],
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

  const setCoefficient = (name: string, raw: string) => {
    const numeric = raw === '' ? 0 : Number(raw);
    if (kim) {
      const nextA = name === 'a' ? numeric : (value.coefficients.a ?? 0);
      const nextSigma = name === 'sigma' ? numeric : (value.coefficients.sigma ?? 0);
      onChange(withKimDerived({ ...value, coefficients: { ...value.coefficients, a: nextA, sigma: nextSigma } }, nextSigma));
      return;
    }
    onChange({
      ...value,
      coefficients: { ...value.coefficients, [name]: numeric },
    });
  };

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
        onChange={(e) => {
          const type = Number(e.target.value) as FormulaType;
          if (isKimCurveType(type)) {
            onChange(withKimDerived({ ...value, type }, value.coefficients.sigma ?? deviationFormula.coefficients.sigma ?? 0));
            return;
          }
          onChange({ ...value, type });
        }}
      >
        {templates.map((t) => (
          <MenuItem key={t.type} value={t.type}>{t.displayName}</MenuItem>
        ))}
      </TextField>

      {kim && (
        <Typography variant="caption" color="text.secondary">
          P(z) = e^(μ + z·σ), μ = ln(P). Часы плана: P(z)·V. Z = (ln(P факт) − μ) / σ.
        </Typography>
      )}

      {template?.coefficients.map((c) => (
        <TextField
          key={c.name}
          size="small"
          type="number"
          label={c.label}
          value={kim && c.name === 'mu' ? (normalizedNorm.coefficients.mu ?? muFromProductivity(value.coefficients.a ?? 0)) : (value.coefficients[c.name] ?? '')}
          InputProps={{ readOnly: c.name === 'mu' }}
          inputProps={{
            min: c.min ?? undefined,
            max: c.max ?? undefined,
            step: c.name === 'sigma' || c.name === 'mu' ? 0.0001 : undefined,
          }}
          onChange={(e) => {
            if (c.name === 'mu') return;
            setCoefficient(c.name, e.target.value);
          }}
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
