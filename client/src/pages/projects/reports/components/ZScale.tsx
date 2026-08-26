import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import { ACCENT_FACT, ACCENT_PLAN, INTERPRETATION_COLOR } from '../reportCalc';
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

/**
 * Строка-дорожка с маркером на шкале Z. План и факт рисуются на раздельных дорожках
 * (а не общим слоем над шкалой), чтобы подписи значений не пересекались, даже когда
 * оба Z близки друг к другу.
 */
function MarkerTrack({
  z,
  rowLabel,
  color,
  emphasized,
}: {
  z: number | null;
  rowLabel: string;
  color: string;
  emphasized?: boolean;
}) {
  const dim = emphasized ? 20 : 13;
  return (
    <Stack direction="row" alignItems="center" spacing={1.2} sx={{ height: emphasized ? 30 : 22 }}>
      <Box sx={{ width: 46, textAlign: 'right', flexShrink: 0 }}>
        <Typography
          sx={{
            fontFamily: 'inherit',
            fontSize: emphasized ? 11.5 : 10.5,
            fontWeight: emphasized ? 800 : 700,
            color: emphasized ? color : wpColors.muted,
          }}
        >
          {rowLabel}
        </Typography>
      </Box>
      <Box sx={{ flex: 1, position: 'relative', height: '100%' }}>
        {z !== null && (
          <>
            <Box
              sx={{
                position: 'absolute',
                left: `${pct(z)}%`,
                top: '50%',
                transform: 'translate(-50%, -50%)',
                width: dim,
                height: dim,
                borderRadius: '50%',
                bgcolor: color,
                border: emphasized ? '3px solid #FFFFFF' : '2px solid #FFFFFF',
                boxShadow: emphasized
                  ? `0 0 0 2.5px ${color}, 0 3px 8px rgba(0,0,0,0.28)`
                  : `0 0 0 1.5px ${color}`,
                zIndex: emphasized ? 2 : 1,
              }}
            />
            <Box
              sx={{
                position: 'absolute',
                left: `${pct(z)}%`,
                top: '50%',
                transform: emphasized ? 'translate(14px, -50%)' : 'translate(11px, -50%)',
                bgcolor: emphasized ? color : 'transparent',
                color: emphasized ? '#FFFFFF' : wpColors.muted,
                px: emphasized ? 0.9 : 0,
                py: emphasized ? 0.15 : 0,
                borderRadius: '6px',
                fontFamily: 'monospace',
                fontSize: emphasized ? 11 : 10,
                fontWeight: emphasized ? 800 : 600,
                whiteSpace: 'nowrap',
              }}
            >
              {z > 0 ? '+' : ''}
              {z.toFixed(2)}
            </Box>
          </>
        )}
      </Box>
    </Stack>
  );
}

interface ZScaleProps {
  planZScore: number | null;
  factZScore: number | null;
  percentile: string | null;
}

export function ZScale({ planZScore, factZScore, percentile }: ZScaleProps) {
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

          <Box>
            <Stack spacing={0.5} sx={{ mb: 1 }}>
              <MarkerTrack z={planZScore} rowLabel="План" color={ACCENT_PLAN} />
              <MarkerTrack z={factZScore} rowLabel="Факт" color={ACCENT_FACT} emphasized />
            </Stack>

            <Box sx={{ pl: '58px' }}>
              <Box sx={{ display: 'flex', height: 28, width: '100%', overflow: 'hidden', borderRadius: 1 }}>
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
                Перцентиль (факт)
              </Typography>
              <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 500 }}>
                {percentile ?? 'Н/Д'}
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
