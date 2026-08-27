import { Box, Card, CardContent, Stack, Typography } from '@mui/material';
import type { PerformanceTreeRow } from '../reportCalc';
import { fmt, fmtSigned, interpretationColor } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

const LIMIT = 1.75;

interface DirectionBarsProps {
  rows: PerformanceTreeRow[];
  title: string;
  description: string;
}

/** Диверджентная диаграмма: отклонение Z от нормы (0) влево — лучше, вправо — хуже. */
export function DirectionBars({ rows, title, description }: DirectionBarsProps) {
  const ranked = rows.filter((r) => r.zScore !== null).sort((a, b) => a.zScore! - b.zScore!);

  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        <Stack spacing={0.5} sx={{ mb: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            {title}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        </Stack>

        <Stack spacing={1.25} sx={{ flex: 1 }}>
          {ranked.length === 0 && (
            <Typography variant="body2" color="text.secondary">
              Нет данных для отображения.
            </Typography>
          )}
          {ranked.map((row) => {
            const z = row.zScore!;
            const width = (Math.min(Math.abs(z), LIMIT) / LIMIT) * 50;
            const color = interpretationColor(z);
            return (
              <Stack key={row.workDirectionId} direction="row" spacing={1.5} alignItems="center">
                <Box sx={{ width: { xs: 140, md: 190 }, flexShrink: 0 }}>
                  <Typography
                    variant="body2"
                    noWrap
                    title={row.workDirectionName}
                    sx={{ fontWeight: 500 }}
                  >
                    {row.workDirectionName}
                  </Typography>
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.heading }}>
                    {fmt(row.subtreeHours, 1)} ч
                  </Typography>
                </Box>

                <Box sx={{ position: 'relative', height: 24, flex: 1, borderRadius: 0.5, bgcolor: wpColors.pageBg }}>
                  <Box
                    sx={{
                      position: 'absolute',
                      top: 0,
                      bottom: 0,
                      left: '50%',
                      width: '1px',
                      bgcolor: 'divider',
                    }}
                  />
                  <Box
                    sx={{
                      position: 'absolute',
                      top: 3,
                      bottom: 3,
                      borderRadius: 0.5,
                      bgcolor: color,
                      width: `${width}%`,
                      left: z < 0 ? `${50 - width}%` : '50%',
                    }}
                  />
                </Box>

                <Typography
                  variant="body2"
                  sx={{ width: 56, flexShrink: 0, textAlign: 'right', fontFamily: 'monospace', fontWeight: 600, color }}
                >
                  {fmtSigned(z)}
                </Typography>
              </Stack>
            );
          })}
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ mt: 'auto', pt: 1.5, borderTop: 1, borderColor: 'divider' }}
          >
            Ширина полосы — величина отклонения от нормы IIC; влево от осевой линии — опережение нормы, вправо —
            перерасход трудозатрат.
          </Typography>
        </Stack>
      </CardContent>
    </Card>
  );
}
