import { useMemo } from 'react';
import { Alert, Box, Button, Paper, Slider, Stack, Typography } from '@mui/material';
import RestartAltOutlinedIcon from '@mui/icons-material/RestartAltOutlined';
import { pctPointsInBand } from './normAnalysisMetrics';

interface Props {
  mean: number | null;
  sigma: number | null;
  factYs: number[] | null;
  meanOffsetPct: number;
  sigmaMultiplier: number;
  onMeanOffset: (value: number) => void;
  onSigmaMultiplier: (value: number) => void;
  onReset: () => void;
}

/** Клиентский превью «что если»: сдвиг μ и множитель σ без публикации нормы. */
export function NormWhatIfSimulator({
  mean,
  sigma,
  factYs,
  meanOffsetPct,
  sigmaMultiplier,
  onMeanOffset,
  onSigmaMultiplier,
  onReset,
}: Props) {
  const sim = useMemo(() => {
    if (mean == null || sigma == null) return null;
    const nextMean = mean * (1 + meanOffsetPct / 100);
    const nextSigma = sigma * sigmaMultiplier;
    const before = factYs && factYs.length > 0 ? pctPointsInBand(factYs, mean, sigma) : null;
    const after = factYs && factYs.length > 0 ? pctPointsInBand(factYs, nextMean, nextSigma) : null;
    const baseRatio = mean === 0 ? 0 : sigma / mean;
    const newRatio = nextMean === 0 ? 0 : nextSigma / nextMean;
    return { nextMean, nextSigma, before, after, baseRatio, newRatio };
  }, [mean, sigma, factYs, meanOffsetPct, sigmaMultiplier]);

  return (
    <Paper variant="outlined" sx={{ p: 2, bgcolor: 'action.hover' }} data-testid="norm-analysis-simulator">
      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          Симулятор «что если»: правка параметров формулы
        </Typography>
        <Button
          size="small"
          startIcon={<RestartAltOutlinedIcon fontSize="small" />}
          onClick={onReset}
          disabled={meanOffsetPct === 0 && sigmaMultiplier === 1}
          data-testid="norm-analysis-simulator-reset"
        >
          Сбросить
        </Button>
      </Stack>
      {mean == null || sigma == null || !sim ? (
        <Alert severity="info" sx={{ mt: 1.5 }}>
          У выбранной нормы нет μ/σ для симуляции.
        </Alert>
      ) : (
        <>
          <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap', mt: 1.5 }}>
            <Box sx={{ minWidth: 220, flex: '1 1 220px' }}>
              <Typography variant="caption" color="text.secondary">
                Смещение μ: {meanOffsetPct > 0 ? '+' : ''}
                {meanOffsetPct}% (было {mean.toFixed(3)}, станет {sim.nextMean.toFixed(3)})
              </Typography>
              <Slider
                size="small"
                min={-20}
                max={20}
                step={1}
                value={meanOffsetPct}
                onChange={(_, v) => onMeanOffset(Array.isArray(v) ? v[0] : v)}
                aria-label="Смещение μ, %"
                data-testid="norm-analysis-sim-mean"
              />
            </Box>
            <Box sx={{ minWidth: 220, flex: '1 1 220px' }}>
              <Typography variant="caption" color="text.secondary">
                Ширина допуска (×σ): {sigmaMultiplier.toFixed(2)}× (было ±
                {(sim.baseRatio * 100).toFixed(0)}%, станет ±{(sim.newRatio * 100).toFixed(0)}%)
              </Typography>
              <Slider
                size="small"
                min={0.5}
                max={2}
                step={0.05}
                value={sigmaMultiplier}
                onChange={(_, v) => onSigmaMultiplier(Array.isArray(v) ? v[0] : v)}
                aria-label="Множитель ширины допуска"
                data-testid="norm-analysis-sim-sigma"
              />
            </Box>
          </Box>
          <Stack direction="row" spacing={3} sx={{ mt: 2 }} flexWrap="wrap" useFlexGap>
            <Box>
              <Typography variant="caption" color="text.secondary">
                Точек факта в допуске сейчас
              </Typography>
              <Typography variant="h6" sx={{ lineHeight: 1.2 }} data-testid="norm-analysis-sim-band-pct">
                {sim.before == null ? '—' : `${sim.before}%`}
              </Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">
                Точек факта в допуске после правки
              </Typography>
              <Typography
                variant="h6"
                sx={{ lineHeight: 1.2 }}
                color={
                  sim.after == null || sim.before == null
                    ? 'text.primary'
                    : sim.after > sim.before
                      ? 'success.main'
                      : sim.after < sim.before
                        ? 'error.main'
                        : 'text.primary'
                }
                data-testid="norm-analysis-sim-mean-pair"
              >
                {sim.after == null ? 'нет точек факта за период' : `${sim.after}%`}
                {sim.after != null && sim.before != null && sim.after !== sim.before ? (
                  <Typography component="span" variant="caption" sx={{ ml: 0.75 }}>
                    ({sim.after > sim.before ? '+' : ''}
                    {sim.after - sim.before} п.п.)
                  </Typography>
                ) : null}
              </Typography>
            </Box>
          </Stack>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
            Расчёт основан на исторических точках факта по этой группе IIC и не публикует новую версию
            нормы — только предварительный просчёт эффекта.
          </Typography>
        </>
      )}
    </Paper>
  );
}
