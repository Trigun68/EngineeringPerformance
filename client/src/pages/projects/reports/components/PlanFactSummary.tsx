import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import RemoveIcon from '@mui/icons-material/Remove';
import type { ProjectReportSummary } from '../../../../api/analyticsApi';
import {
  ACCENT_FACT,
  ACCENT_PLAN,
  fmt,
  fmtSigned,
  getInterpretation,
  INTERPRETATION_COLOR,
  INTERPRETATION_RU,
  MUTED_COLOR,
} from '../reportCalc';

interface PlanFactSummaryProps {
  summary: ProjectReportSummary;
  accuracyScore: number | null;
}

/**
 * Заменяет прежние SummaryCards. Показывает блоки «План» и «Факт» по основным версиям проекта
 * (см. backend ProjectReportSummaryDto), отклонение факта от плана и точность сравнения с ИИЦ.
 */
export function PlanFactSummary({ summary, accuracyScore }: PlanFactSummaryProps) {
  const planInterpretation = getInterpretation(summary.planWeightedZScore);
  const planColor = planInterpretation ? INTERPRETATION_COLOR[planInterpretation] : MUTED_COLOR;

  const hasFact = summary.factVersionId !== null;
  const factInterpretation = getInterpretation(summary.factWeightedZScore);
  const factColor = hasFact && factInterpretation ? INTERPRETATION_COLOR[factInterpretation] : MUTED_COLOR;

  const hoursDelta =
    hasFact && summary.factHours !== null && summary.planHours !== null
      ? summary.factHours - summary.planHours
      : null;
  const zDelta =
    hasFact && summary.factWeightedZScore !== null && summary.planWeightedZScore !== null
      ? summary.factWeightedZScore - summary.planWeightedZScore
      : null;
  // Меньший Z = лучше, поэтому отрицательная дельта Z — улучшение факта относительно плана.
  const isZFlat = zDelta === null || Math.abs(zDelta) < 0.005;
  const zTrendColor = isZFlat ? MUTED_COLOR : zDelta! < 0 ? INTERPRETATION_COLOR.Excellent : INTERPRETATION_COLOR.Critical;
  const ZTrendIcon = isZFlat ? RemoveIcon : zDelta! < 0 ? ArrowDownwardIcon : ArrowUpwardIcon;

  return (
    <Grid container spacing={2}>
      <Grid item xs={12} md={5}>
        <Card variant="outlined" sx={{ height: '100%', borderLeft: 4, borderLeftColor: ACCENT_PLAN }}>
          <CardContent>
            <Stack spacing={1.5}>
              <Stack direction="row" justifyContent="space-between" alignItems="baseline">
                <Typography
                  variant="caption"
                  sx={{ color: ACCENT_PLAN, letterSpacing: 0.4, textTransform: 'uppercase', fontWeight: 700 }}
                >
                  План
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {summary.planVersionName ?? 'Нет версии плана'}
                </Typography>
              </Stack>

              <Stack direction="row" spacing={3} flexWrap="wrap">
                <Box>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Трудоёмкость плана
                  </Typography>
                  <Box sx={{ fontFamily: 'monospace', fontSize: 24, fontWeight: 600, lineHeight: 1.3 }}>
                    {fmt(summary.planHours, 0)}{' '}
                    <Typography component="span" variant="body1" sx={{ fontWeight: 400, color: 'text.secondary' }}>
                      ч
                    </Typography>
                  </Box>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary" display="block">
                    Z-план
                  </Typography>
                  <Box sx={{ fontFamily: 'monospace', fontSize: 24, fontWeight: 600, lineHeight: 1.3, color: planColor }}>
                    {fmtSigned(summary.planWeightedZScore)}
                  </Box>
                </Box>
              </Stack>

              <Typography variant="body2" sx={{ color: planColor }}>
                {planInterpretation ? INTERPRETATION_RU[planInterpretation] : 'Нет данных'}
                {summary.planPercentile ? ` · перцентиль ${summary.planPercentile}` : ''}
              </Typography>
            </Stack>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12} md={5}>
        <Card variant="outlined" sx={{ height: '100%', borderLeft: 4, borderLeftColor: hasFact ? ACCENT_FACT : MUTED_COLOR }}>
          <CardContent>
            <Stack spacing={1.5}>
              <Stack direction="row" justifyContent="space-between" alignItems="baseline">
                <Typography
                  variant="caption"
                  sx={{ color: ACCENT_FACT, letterSpacing: 0.4, textTransform: 'uppercase', fontWeight: 700 }}
                >
                  Факт
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {summary.factVersionName ?? 'Основная версия факта не назначена'}
                </Typography>
              </Stack>

              {!hasFact ? (
                <Typography variant="body2" color="text.secondary">
                  Н/Д — назначьте основную версию факта в списке версий проекта.
                </Typography>
              ) : (
                <>
                  <Stack direction="row" spacing={3} flexWrap="wrap">
                    <Box>
                      <Typography variant="caption" color="text.secondary" display="block">
                        Трудоёмкость факта
                      </Typography>
                      <Box sx={{ fontFamily: 'monospace', fontSize: 24, fontWeight: 600, lineHeight: 1.3 }}>
                        {fmt(summary.factHours, 0)}{' '}
                        <Typography component="span" variant="body1" sx={{ fontWeight: 400, color: 'text.secondary' }}>
                          ч
                        </Typography>
                      </Box>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary" display="block">
                        Z-факт
                      </Typography>
                      <Box
                        sx={{ fontFamily: 'monospace', fontSize: 24, fontWeight: 600, lineHeight: 1.3, color: factColor }}
                      >
                        {fmtSigned(summary.factWeightedZScore)}
                      </Box>
                    </Box>
                  </Stack>

                  <Stack spacing={0.5}>
                    <Typography variant="body2" sx={{ color: factColor }}>
                      {factInterpretation ? INTERPRETATION_RU[factInterpretation] : 'Нет данных'}
                      {summary.factPercentile ? ` · перцентиль ${summary.factPercentile}` : ''}
                    </Typography>
                    {zDelta !== null && hoursDelta !== null && (
                      <Stack direction="row" spacing={0.5} alignItems="center">
                        <ZTrendIcon sx={{ fontSize: 14, color: zTrendColor }} />
                        <Typography variant="caption" sx={{ color: zTrendColor }}>
                          {fmtSigned(zDelta)} Z к плану · {fmtSigned(hoursDelta, 0)} ч
                        </Typography>
                      </Stack>
                    )}
                  </Stack>
                </>
              )}
            </Stack>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12} md={2}>
        <Card variant="outlined" sx={{ height: '100%' }}>
          <CardContent>
            <Stack spacing={1}>
              <Typography
                variant="caption"
                sx={{ color: 'text.secondary', letterSpacing: 0.4, textTransform: 'uppercase', fontWeight: 600 }}
              >
                Точность сравнения
              </Typography>
              <Box sx={{ fontFamily: 'monospace', fontSize: 28, fontWeight: 600, lineHeight: 1 }}>
                {accuracyScore === null ? (
                  'Н/Д'
                ) : (
                  <>
                    {fmt(accuracyScore, 1)}
                    <Typography component="span" variant="body1" sx={{ ml: 0.5, fontWeight: 400, color: 'text.secondary' }}>
                      %
                    </Typography>
                  </>
                )}
              </Box>
              <Typography variant="body2" color="text.secondary">
                Средняя PD% по характеристикам проекта
              </Typography>
            </Stack>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
}
