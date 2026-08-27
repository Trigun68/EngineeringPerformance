import { Box, Stack, Typography } from '@mui/material';
import {
  ACCENT_FACT,
  ACCENT_PLAN,
  MUTED_COLOR,
  ZONE_COLOR,
  Z_SCALE_ZONES,
  zScaleVisualPct,
} from '../reportCalc';
import { BORDER, ReportMockupCard, SectionLabel } from './reportChrome';

/**
 * Строка-дорожка с маркером на шкале Z. План и факт на раздельных дорожках,
 * чтобы подписи не пересекались. Факт — крупный акцентный маркер с бейджем.
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
  const left = z === null ? null : zScaleVisualPct(z);
  return (
    <Stack direction="row" alignItems="center" spacing={1.2} sx={{ height: emphasized ? 30 : 22 }}>
      <Box sx={{ width: 46, textAlign: 'right', flexShrink: 0 }}>
        <Typography
          sx={{
            fontSize: emphasized ? 11.5 : 10.5,
            fontWeight: emphasized ? 800 : 700,
            color: emphasized ? color : MUTED_COLOR,
          }}
        >
          {rowLabel}
        </Typography>
      </Box>
      <Box sx={{ flex: 1, position: 'relative', height: '100%' }}>
        {z !== null && left !== null && (
          <>
            <Box
              sx={{
                position: 'absolute',
                left: `${left}%`,
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
                left: `${left}%`,
                top: '50%',
                transform: emphasized ? 'translate(14px, -50%)' : 'translate(11px, -50%)',
                bgcolor: emphasized ? color : 'transparent',
                color: emphasized ? '#FFFFFF' : MUTED_COLOR,
                px: emphasized ? 0.9 : 0,
                py: emphasized ? 0.15 : 0,
                borderRadius: '6px',
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
}

export function ZScale({ planZScore, factZScore }: ZScaleProps) {
  return (
    <ReportMockupCard testId="report-v1-z-scale">
      <SectionLabel>Шкала интерпретации Z-оценки</SectionLabel>
      <Typography sx={{ fontSize: 11, color: MUTED_COLOR, mt: 0.5, mb: 1.5 }}>
        Меньший Z — выше производительность; справа лучше.
      </Typography>

      <Box>
        <Stack spacing={0.5} sx={{ mb: 1 }}>
          <MarkerTrack z={planZScore} rowLabel="План" color={ACCENT_PLAN} />
          <MarkerTrack z={factZScore} rowLabel="Факт" color={ACCENT_FACT} emphasized />
        </Stack>

        <Box sx={{ pl: '58px' }}>
          <Box sx={{ height: 1, bgcolor: BORDER, mb: 1 }} />
          <Box sx={{ display: 'flex', height: 14, borderRadius: '7px', overflow: 'hidden' }}>
            {Z_SCALE_ZONES.map((zone) => (
              <Box
                key={zone.label}
                data-testid={`report-z-zone-${zone.interpretation}`}
                sx={{ flex: zone.to - zone.from, bgcolor: ZONE_COLOR[zone.interpretation] }}
              />
            ))}
          </Box>

          <Stack direction="row" sx={{ mt: 1, display: { xs: 'none', sm: 'flex' } }}>
            {Z_SCALE_ZONES.map((zone) => (
              <Box key={zone.label} sx={{ flex: zone.to - zone.from, textAlign: 'center' }}>
                <Typography sx={{ fontSize: 10.5, color: MUTED_COLOR, fontWeight: 600 }}>{zone.label}</Typography>
              </Box>
            ))}
          </Stack>

          <Stack
            direction="row"
            justifyContent="space-between"
            sx={{ mt: 0.5, fontSize: 11, color: MUTED_COLOR }}
          >
            {['+3', '0', '−3'].map((tick) => (
              <span key={tick} data-testid="report-v1-z-tick">
                {tick}
              </span>
            ))}
          </Stack>

          <Stack
            direction="row"
            spacing={1.5}
            useFlexGap
            flexWrap="wrap"
            sx={{ display: { xs: 'flex', sm: 'none' }, mt: 1 }}
          >
            {Z_SCALE_ZONES.map((zone) => (
              <Stack key={zone.label} direction="row" spacing={0.5} alignItems="center">
                <Box
                  sx={{
                    width: 8,
                    height: 8,
                    borderRadius: '50%',
                    bgcolor: ZONE_COLOR[zone.interpretation],
                    flexShrink: 0,
                  }}
                />
                <Typography sx={{ fontSize: 11, color: MUTED_COLOR }}>{zone.label}</Typography>
              </Stack>
            ))}
          </Stack>
        </Box>
      </Box>
    </ReportMockupCard>
  );
}
