import { Box, Paper, Skeleton, Typography } from '@mui/material';
import TrendingFlatIcon from '@mui/icons-material/TrendingFlat';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import type { ReactNode } from 'react';

export type KpiTone = 'positive' | 'negative' | 'neutral';

export interface KpiItem {
  key: string;
  label: string;
  value: string;
  helperText?: string;
  tone?: KpiTone;
  icon?: ReactNode;
}

interface Props {
  items: KpiItem[];
  loading?: boolean;
  testId?: string;
}

const TONE_COLOR: Record<KpiTone, string> = {
  positive: 'success.main',
  negative: 'error.main',
  neutral: 'text.primary',
};

function toneIcon(tone: KpiTone | undefined) {
  if (tone === 'positive') return <TrendingUpIcon fontSize="small" />;
  if (tone === 'negative') return <TrendingDownIcon fontSize="small" />;
  return <TrendingFlatIcon fontSize="small" />;
}

/** Ряд KPI-карточек: текущее значение, было на начало периода, изменение, охват выборки. */
export function KpiSummaryRow({ items, loading, testId = 'kpi-summary-row' }: Props) {
  return (
    <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }} data-testid={testId}>
      {loading
        ? Array.from({ length: items.length || 4 }).map((_, i) => (
            <Skeleton
              key={i}
              variant="rounded"
              sx={{ flex: '1 1 220px', minWidth: 200 }}
              height={84}
            />
          ))
        : items.map((item) => {
            const tone = item.tone ?? 'neutral';
            const color = TONE_COLOR[tone];
            return (
              <Paper
                key={item.key}
                variant="outlined"
                sx={{ flex: '1 1 220px', minWidth: 200, p: 2 }}
                data-testid={`${testId}-${item.key}`}
              >
                <Typography variant="caption" color="text.secondary">
                  {item.label}
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 0.75, mt: 0.5 }}>
                  <Box sx={{ color, display: 'flex' }}>{item.icon ?? toneIcon(item.tone)}</Box>
                  <Typography variant="h5" component="div" sx={{ color, fontWeight: 600, lineHeight: 1.1 }}>
                    {item.value}
                  </Typography>
                </Box>
                {item.helperText ? (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                    {item.helperText}
                  </Typography>
                ) : null}
              </Paper>
            );
          })}
    </Box>
  );
}
