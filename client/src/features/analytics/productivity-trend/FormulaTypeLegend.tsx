import { Box, Stack, Typography } from '@mui/material';
import type { ProductivityTrendLegendItem } from '../../../api/productivityTrendApi';

interface Props {
  legend: ProductivityTrendLegendItem[];
}

export function FormulaTypeLegend({ legend }: Props) {
  if (legend.length === 0) return null;

  return (
    <Stack spacing={1} data-testid="prod-trend-legend" aria-label="Типы формул нормы">
      <Typography variant="subtitle2">Тип формулы N(V)</Typography>
      <Stack direction="row" flexWrap="wrap" gap={1.5}>
        {legend.map((item) => (
          <Stack key={item.formulaType} direction="row" spacing={0.75} alignItems="center">
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
              {item.displayName}
            </Typography>
          </Stack>
        ))}
      </Stack>
    </Stack>
  );
}
