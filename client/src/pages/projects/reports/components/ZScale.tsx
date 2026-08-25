import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import { fmtSigned, interpretationColor, INTERPRETATION_COLOR } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

const MIN = -1.75;
const MAX = 1.75;

const ZONES = [
  { from: MIN, to: -1, label: 'Отлично', color: INTERPRETATION_COLOR.Excellent },
  { from: -1, to: -0.5, label: 'Хорошо', color: INTERPRETATION_COLOR.Good },
  { from: -0.5, to: 0.5, label: 'Удовлетв.', color: INTERPRETATION_COLOR.Satisfactory },
  { from: 0.5, to: 1, label: 'Внимание', color: INTERPRETATION_COLOR.NeedsAttention },
  { from: 1, to: MAX, label: 'Критично', color: INTERPRETATION_COLOR.Critical },
];

function pct(z: number) {
  const clamped = Math.min(MAX, Math.max(MIN, z));
  return ((clamped - MIN) / (MAX - MIN)) * 100;
}

interface ZScaleProps {
  zScore: number | null;
  percentile: string;
}

export function ZScale({ zScore, percentile }: ZScaleProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={3}>
          <Stack spacing={0.5}>
            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
              Шкала интерпретации Z-оценки
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Z рассчитывается в лог-пространстве: z = (ln(факт. производительность) − ln(норма)) / σ. Меньшая
              Z означает меньше часов на единицу объёма, то есть более высокую производительность.
            </Typography>
          </Stack>

          <Box sx={{ pt: 3 }}>
            <Box sx={{ position: 'relative' }}>
              <Box sx={{ display: 'flex', height: 32, width: '100%', overflow: 'hidden', borderRadius: 1 }}>
                {ZONES.map((zone) => (
                  <Box
                    key={zone.label}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      width: `${((zone.to - zone.from) / (MAX - MIN)) * 100}%`,
                      bgcolor: zone.color,
                    }}
                  >
                    <Typography sx={{ fontSize: 11, fontWeight: 500, color: '#FFFFFF', px: 0.5 }}>
                      {zone.label}
                    </Typography>
                  </Box>
                ))}
              </Box>

              {zScore !== null && (
                <Box
                  sx={{
                    position: 'absolute',
                    top: -28,
                    left: `${pct(zScore)}%`,
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    transform: 'translateX(-50%)',
                  }}
                >
                  <Box
                    sx={{
                      borderRadius: 0.5,
                      px: 0.75,
                      py: 0.25,
                      fontFamily: 'monospace',
                      fontSize: 12,
                      fontWeight: 600,
                      color: '#FFFFFF',
                      bgcolor: interpretationColor(zScore),
                    }}
                  >
                    {fmtSigned(zScore)}
                  </Box>
                  <Box sx={{ width: 2, height: 36, bgcolor: interpretationColor(zScore) }} />
                </Box>
              )}

              <Stack
                direction="row"
                justifyContent="space-between"
                sx={{ mt: 1, fontFamily: 'monospace', fontSize: 11, color: wpColors.muted }}
              >
                {['−1.5', '−1.0', '−0.5', '0', '+0.5', '+1.0', '+1.5'].map((tick) => (
                  <span key={tick}>{tick}</span>
                ))}
              </Stack>
            </Box>
          </Box>

          <Grid container spacing={2} sx={{ borderTop: 1, borderColor: 'divider', pt: 2 }}>
            <Grid item xs={6} sm={3}>
              <Typography variant="caption" color="text.secondary" display="block">
                Перцентиль
              </Typography>
              <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 500 }}>
                {percentile}
              </Typography>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Typography variant="caption" color="text.secondary" display="block">
                Метод агрегации
              </Typography>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                Σ(Z × часы) / Σ(часы)
              </Typography>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Typography variant="caption" color="text.secondary" display="block">
                База агрегации
              </Typography>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                Корневые направления
              </Typography>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Typography variant="caption" color="text.secondary" display="block">
                Источник норм
              </Typography>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                Снимок версии (ИИЦ)
              </Typography>
            </Grid>
          </Grid>
        </Stack>
      </CardContent>
    </Card>
  );
}
