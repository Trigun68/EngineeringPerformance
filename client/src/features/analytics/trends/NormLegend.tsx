import { Box, Stack, Typography } from '@mui/material';
import type { ZTrendLegendItem } from '../../../api/zTrendApi';

function formatPeriod(from: string, to: string | null): string {
  const f = new Date(from);
  const fromLabel = Number.isNaN(f.getTime())
    ? from
    : f.toLocaleDateString('ru-RU');
  if (!to) return `${fromLabel} – н.в.`;
  const t = new Date(to);
  const toLabel = Number.isNaN(t.getTime()) ? to : t.toLocaleDateString('ru-RU');
  return `${fromLabel} – ${toLabel}`;
}

interface Props {
  legend: ZTrendLegendItem[];
}

export function NormLegend({ legend }: Props) {
  if (legend.length === 0) return null;

  return (
    <Stack
      direction="row"
      flexWrap="wrap"
      gap={1.5}
      data-testid="ztrend-legend"
      aria-label="Легенда норм"
    >
      {legend.map((item) => (
        <Stack key={item.segmentKey} direction="row" spacing={0.75} alignItems="center">
          <Box
            sx={{
              width: 14,
              height: 14,
              borderRadius: 0.5,
              bgcolor: item.color,
              border: '1px solid rgba(0,0,0,0.2)',
            }}
          />
          <Typography variant="caption" component="span">
            {item.normName}
            {item.groupName ? ` (${item.groupName})` : ''}:{' '}
            {formatPeriod(item.effectiveFrom, item.effectiveTo)}
          </Typography>
        </Stack>
      ))}
    </Stack>
  );
}
