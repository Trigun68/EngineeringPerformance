import { Stack, Typography } from '@mui/material';

interface Props {
  performance: number | null | undefined;
  mu: number | null | undefined;
  sigma: number | null | undefined;
}

function fmt(value: number | null | undefined): string {
  if (value == null || Number.isNaN(value)) return '—';
  return value.toLocaleString('ru-RU', { maximumFractionDigits: 4 });
}

/** Три показателя нормы: производительность, μ, σ. */
export function NormTripleMetrics({ performance, mu, sigma }: Props) {
  return (
    <Stack spacing={0.75} data-testid="norm-card-metrics">
      <Typography data-testid="norm-card-performance">
        <strong>Производительность:</strong> {fmt(performance)}
      </Typography>
      <Typography data-testid="norm-card-mu">
        <strong>μ:</strong> {fmt(mu)}
      </Typography>
      <Typography data-testid="norm-card-sigma">
        <strong>σ:</strong> {fmt(sigma)}
      </Typography>
    </Stack>
  );
}
