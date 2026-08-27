<<<<<<< HEAD
import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import { ACCENT_FACT, ACCENT_PLAN, INTERPRETATION_COLOR, percentileFraction } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

/** Градиентная дорожка перцентиля 0..100 с маркерами план/факт — компактный визуальный аналог MarkerTrack. */
function PercentileGauge({
  planZ,
  factZ,
  hasFact,
}: {
  planZ: number | null;
  factZ: number | null;
  hasFact: boolean;
}) {
  const planPos = percentileFraction(planZ);
  const factPos = hasFact ? percentileFraction(factZ) : null;

  return (
    <Box>
      <Box
        sx={{
          position: 'relative',
          height: 10,
          borderRadius: 5,
          background: `linear-gradient(90deg, ${INTERPRETATION_COLOR.Excellent} 0%, ${INTERPRETATION_COLOR.Good} 25%, ${INTERPRETATION_COLOR.Satisfactory} 50%, ${INTERPRETATION_COLOR.NeedsAttention} 75%, ${INTERPRETATION_COLOR.Critical} 100%)`,
        }}
      >
        {planPos !== null && (
          <Box
            sx={{
              position: 'absolute',
              left: `${planPos}%`,
              top: '50%',
              transform: 'translate(-50%, -50%)',
              width: 10,
              height: 10,
              borderRadius: '50%',
              bgcolor: ACCENT_PLAN,
              border: '2px solid #FFFFFF',
              boxShadow: `0 0 0 1px ${ACCENT_PLAN}`,
              zIndex: 1,
            }}
          />
        )}
        {factPos !== null && (
          <Box
            sx={{
              position: 'absolute',
              left: `${factPos}%`,
              top: '50%',
              transform: 'translate(-50%, -50%)',
              width: 14,
              height: 14,
              borderRadius: '50%',
              bgcolor: ACCENT_FACT,
              border: '2.5px solid #FFFFFF',
              boxShadow: `0 0 0 1.5px ${ACCENT_FACT}, 0 2px 6px rgba(0,0,0,0.25)`,
              zIndex: 2,
            }}
          />
        )}
      </Box>
      <Stack direction="row" justifyContent="space-between" sx={{ mt: 0.5 }}>
        <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.muted }}>
          0
        </Typography>
        <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.muted }}>
          100
        </Typography>
      </Stack>
    </Box>
  );
}
=======
import { Box, Card, CardContent, Stack, Typography } from '@mui/material';
import { ACCENT_FACT, ACCENT_PLAN, MUTED_COLOR, ZONE_COLOR } from '../reportCalc';
>>>>>>> origin/master

const MIN = -1.75;
const MAX = 1.75;

<<<<<<< HEAD
const ZONES = [
  { from: MIN, to: -1, label: 'Отлично', color: INTERPRETATION_COLOR.Excellent },
  { from: -1, to: -0.5, label: 'Хорошо', color: INTERPRETATION_COLOR.Good },
  { from: -0.5, to: 0.5, label: 'Удовлетв.', color: INTERPRETATION_COLOR.Satisfactory },
  { from: 0.5, to: 1, label: 'Внимание', color: INTERPRETATION_COLOR.NeedsAttention },
  { from: 1, to: MAX, label: 'Критично', color: INTERPRETATION_COLOR.Critical },
=======
// Пороги зон — паритет backend PerformanceInterpretationHelper.GetInterpretation.
// Цвета — пастельная палитра мокапа ReportRedesignMockup.
const ZONES = [
  { from: MIN, to: -1, label: 'Отлично', color: ZONE_COLOR.Excellent },
  { from: -1, to: -0.5, label: 'Хорошо', color: ZONE_COLOR.Good },
  { from: -0.5, to: 0.5, label: 'Удовл.', color: ZONE_COLOR.Satisfactory },
  { from: 0.5, to: 1, label: 'Внимание', color: ZONE_COLOR.NeedsAttention },
  { from: 1, to: MAX, label: 'Критично', color: ZONE_COLOR.Critical },
>>>>>>> origin/master
];

function pct(z: number) {
  const clamped = Math.min(MAX, Math.max(MIN, z));
  return ((clamped - MIN) / (MAX - MIN)) * 100;
}

/**
 * Строка-дорожка с маркером на шкале Z. План и факт рисуются на раздельных дорожках
 * (а не общим слоем над шкалой), чтобы подписи значений не пересекались, даже когда
<<<<<<< HEAD
 * оба Z близки друг к другу.
=======
 * оба Z близки друг к другу. Факт — крупный акцентный маркер с бейджем значения.
>>>>>>> origin/master
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
<<<<<<< HEAD
            fontFamily: 'inherit',
            fontSize: emphasized ? 11.5 : 10.5,
            fontWeight: emphasized ? 800 : 700,
            color: emphasized ? color : wpColors.muted,
=======
            fontSize: emphasized ? 11.5 : 10.5,
            fontWeight: emphasized ? 800 : 700,
            color: emphasized ? color : MUTED_COLOR,
>>>>>>> origin/master
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
<<<<<<< HEAD
                color: emphasized ? '#FFFFFF' : wpColors.muted,
                px: emphasized ? 0.9 : 0,
                py: emphasized ? 0.15 : 0,
                borderRadius: '6px',
                fontFamily: 'monospace',
=======
                color: emphasized ? '#FFFFFF' : MUTED_COLOR,
                px: emphasized ? 0.9 : 0,
                py: emphasized ? 0.15 : 0,
                borderRadius: '6px',
>>>>>>> origin/master
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
<<<<<<< HEAD
  percentile: string | null;
  planPercentile?: string | null;
}

export function ZScale({ planZScore, factZScore, percentile, planPercentile }: ZScaleProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={3}>
=======
}

export function ZScale({ planZScore, factZScore }: ZScaleProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={2}>
>>>>>>> origin/master
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
<<<<<<< HEAD
              <Box sx={{ display: 'flex', height: 28, width: '100%', overflow: 'hidden', borderRadius: 1 }}>
                {ZONES.map((zone) => (
                  <Box
                    key={zone.label}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      width: `${((zone.to - zone.from) / (MAX - MIN)) * 100}%`,
                      minWidth: 0,
                      overflow: 'hidden',
                      bgcolor: zone.color,
                    }}
                  >
                    <Typography
                      noWrap
                      sx={{
                        display: { xs: 'none', sm: 'block' },
                        fontSize: 11,
                        fontWeight: 500,
                        color: '#FFFFFF',
                        px: 0.5,
                      }}
                    >
                      {zone.label}
                    </Typography>
                  </Box>
                ))}
              </Box>

              <Stack
                direction="row"
                justifyContent="space-between"
                sx={{ mt: 1, fontFamily: 'monospace', fontSize: 11, color: wpColors.muted }}
=======
              <Box sx={{ height: 1, bgcolor: '#E4E9ED', mb: 1 }} />
              <Box sx={{ display: 'flex', height: 14, borderRadius: '7px', overflow: 'hidden' }}>
                {ZONES.map((zone) => (
                  <Box key={zone.label} sx={{ flex: zone.to - zone.from, bgcolor: zone.color }} />
                ))}
              </Box>

              {/* Подписи зон под полосой — на узких экранах скрыты, дублируются легендой ниже. */}
              <Stack direction="row" sx={{ mt: 1, display: { xs: 'none', sm: 'flex' } }}>
                {ZONES.map((zone) => (
                  <Box key={zone.label} sx={{ flex: zone.to - zone.from, textAlign: 'center' }}>
                    <Typography sx={{ fontSize: 10.5, color: MUTED_COLOR, fontWeight: 600 }}>{zone.label}</Typography>
                  </Box>
                ))}
              </Stack>

              <Stack
                direction="row"
                justifyContent="space-between"
                sx={{ mt: 0.5, fontSize: 11, color: MUTED_COLOR }}
>>>>>>> origin/master
              >
                {['−1.5', '−1.0', '−0.5', '0', '+0.5', '+1.0', '+1.5'].map((tick) => (
                  <span key={tick}>{tick}</span>
                ))}
              </Stack>

<<<<<<< HEAD
              {/* На узких экранах полосы зон слишком тонкие для подписей — дублируем их обёртываемой легендой. */}
=======
>>>>>>> origin/master
              <Stack
                direction="row"
                spacing={1.5}
                useFlexGap
                flexWrap="wrap"
                sx={{ display: { xs: 'flex', sm: 'none' }, mt: 1 }}
              >
                {ZONES.map((zone) => (
                  <Stack key={zone.label} direction="row" spacing={0.5} alignItems="center">
                    <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: zone.color, flexShrink: 0 }} />
<<<<<<< HEAD
                    <Typography sx={{ fontSize: 11, color: wpColors.muted }}>{zone.label}</Typography>
=======
                    <Typography sx={{ fontSize: 11, color: MUTED_COLOR }}>{zone.label}</Typography>
>>>>>>> origin/master
                  </Stack>
                ))}
              </Stack>
            </Box>
          </Box>
<<<<<<< HEAD

          <Box sx={{ borderTop: 1, borderColor: 'divider', pt: 2 }}>
            <Stack direction="row" justifyContent="space-between" alignItems="baseline" sx={{ mb: 1 }}>
              <Typography variant="caption" color="text.secondary">
                Перцентиль (0..100, по Z в диапазоне ±1σ)
              </Typography>
              <Stack direction="row" spacing={2}>
                <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 500, color: ACCENT_PLAN }}>
                  План: {planPercentile ?? 'Н/Д'}
                </Typography>
                <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600, color: ACCENT_FACT }}>
                  Факт: {percentile ?? 'Н/Д'}
                </Typography>
              </Stack>
            </Stack>
            <PercentileGauge planZ={planZScore} factZ={factZScore} hasFact={factZScore !== null} />
          </Box>

          <Grid container spacing={2} sx={{ borderTop: 1, borderColor: 'divider', pt: 2 }}>
            <Grid item xs={6} sm={4}>
              <Typography variant="caption" color="text.secondary" display="block">
                Метод агрегации
              </Typography>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                Σ(Z × часы) / Σ(часы)
              </Typography>
            </Grid>
            <Grid item xs={6} sm={4}>
              <Typography variant="caption" color="text.secondary" display="block">
                База агрегации
              </Typography>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                Корневые направления
              </Typography>
            </Grid>
            <Grid item xs={6} sm={4}>
              <Typography variant="caption" color="text.secondary" display="block">
                Источник норм
              </Typography>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                Снимок версии (ИИЦ)
              </Typography>
            </Grid>
          </Grid>
=======
>>>>>>> origin/master
        </Stack>
      </CardContent>
    </Card>
  );
}
