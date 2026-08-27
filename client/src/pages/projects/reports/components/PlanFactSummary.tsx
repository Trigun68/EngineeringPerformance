<<<<<<< HEAD
import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import RemoveIcon from '@mui/icons-material/Remove';
=======
import { Box, Chip, Stack, Typography } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
>>>>>>> origin/master
import type { ProjectReportSummary } from '../../../../api/analyticsApi';
import {
  ACCENT_FACT,
  ACCENT_PLAN,
  fmt,
  fmtSigned,
  getInterpretation,
<<<<<<< HEAD
  INTERPRETATION_COLOR,
  INTERPRETATION_RU,
  MUTED_COLOR,
} from '../reportCalc';

=======
  hoursDeltaPct,
  INTERPRETATION_BG,
  INTERPRETATION_COLOR,
  INTERPRETATION_SHORT_RU,
  MUTED_COLOR,
  type Interpretation,
} from '../reportCalc';

// Дизайн-константы мокапа ReportRedesignMockup (ветка v0)
const SECTION_BG = '#F4F6F8';
const CARD_BG = '#FFFFFF';
const PRIMARY_TEXT = '#1B2A38';
const BORDER = '#E4E9ED';

function SectionLabel({ children }: { children: string }) {
  return (
    <Typography
      sx={{ fontSize: 11, fontWeight: 700, color: MUTED_COLOR, letterSpacing: 0.6, textTransform: 'uppercase' }}
    >
      {children}
    </Typography>
  );
}

/** Чип отклонения факта от плана в процентах: перерасход (>0) — красный, экономия — зелёный. */
function DeltaChip({ deltaPct }: { deltaPct: number }) {
  const isOver = deltaPct > 0;
  const color = isOver ? INTERPRETATION_COLOR.Critical : INTERPRETATION_COLOR.Excellent;
  const bg = isOver ? INTERPRETATION_BG.Critical : INTERPRETATION_BG.Excellent;
  return (
    <Chip
      size="small"
      icon={isOver ? <ArrowUpwardIcon sx={{ fontSize: 14 }} /> : <ArrowDownwardIcon sx={{ fontSize: 14 }} />}
      label={`${isOver ? '+' : ''}${deltaPct.toFixed(1)}%`}
      sx={{
        fontWeight: 700,
        fontSize: 12,
        color,
        bgcolor: bg,
        '& .MuiChip-icon': { color },
        height: 24,
      }}
    />
  );
}

function PlanFactBlock({
  kind,
  sourceLabel,
  hours,
  z,
  interpretation,
}: {
  kind: 'plan' | 'fact';
  sourceLabel: string;
  hours: number | null;
  z: number | null;
  interpretation: Interpretation | null;
}) {
  const accent = kind === 'plan' ? ACCENT_PLAN : ACCENT_FACT;
  const zColor = interpretation ? INTERPRETATION_COLOR[interpretation] : MUTED_COLOR;
  return (
    <Box
      sx={{
        flex: 1,
        bgcolor: CARD_BG,
        borderRadius: '12px',
        border: `1px solid ${BORDER}`,
        p: 2.5,
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <Box sx={{ position: 'absolute', top: 0, left: 0, width: 4, height: '100%', bgcolor: accent }} />
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1.5 }}>
        <Stack direction="row" alignItems="center" spacing={1}>
          <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: accent }} />
          <Typography sx={{ fontSize: 13, fontWeight: 700, color: PRIMARY_TEXT }}>
            {kind === 'plan' ? 'План' : 'Факт'}
          </Typography>
        </Stack>
        <Typography sx={{ fontSize: 11, color: MUTED_COLOR }}>{sourceLabel}</Typography>
      </Stack>
      <Stack direction="row" spacing={4}>
        <Box>
          <Typography sx={{ fontSize: 11, color: MUTED_COLOR, mb: 0.3 }}>
            {kind === 'plan' ? 'Трудоёмкость плана' : 'Трудоёмкость факта'}
          </Typography>
          <Typography sx={{ fontSize: 26, fontWeight: 800, color: PRIMARY_TEXT, lineHeight: 1.1 }}>
            {hours === null ? '—' : `${fmt(hours, 0)} ч`}
          </Typography>
        </Box>
        <Box>
          <Typography sx={{ fontSize: 11, color: MUTED_COLOR, mb: 0.3 }}>
            {kind === 'plan' ? 'Z-план' : 'Z-факт'}
          </Typography>
          <Typography sx={{ fontSize: 26, fontWeight: 800, color: zColor, lineHeight: 1.1 }}>
            {fmtSigned(z)}
          </Typography>
        </Box>
      </Stack>
      {interpretation && (
        <Chip
          size="small"
          label={INTERPRETATION_SHORT_RU[interpretation]}
          sx={{
            mt: 1.5,
            fontWeight: 700,
            fontSize: 11.5,
            color: INTERPRETATION_COLOR[interpretation],
            bgcolor: INTERPRETATION_BG[interpretation],
            height: 24,
          }}
        />
      )}
    </Box>
  );
}

>>>>>>> origin/master
interface PlanFactSummaryProps {
  summary: ProjectReportSummary;
  accuracyScore: number | null;
}

/**
<<<<<<< HEAD
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
=======
 * Сводка «План / Факт» по основным версиям проекта (backend ProjectReportSummaryDto).
 * Дизайн — мокап ReportRedesignMockup: две карточки с акцентными полосами, чипы интерпретации,
 * дельта-чип отклонения факта от плана и мета-строка (перцентили, метод агрегации, точность).
 */
export function PlanFactSummary({ summary, accuracyScore }: PlanFactSummaryProps) {
  const hasFact = summary.factVersionId !== null;
  const planInterpretation = getInterpretation(summary.planWeightedZScore);
  const factInterpretation = hasFact ? getInterpretation(summary.factWeightedZScore) : null;
  const deltaPct = hasFact ? hoursDeltaPct(summary.planHours, summary.factHours) : null;

  return (
    <Box sx={{ bgcolor: SECTION_BG, borderRadius: '16px', p: 3 }}>
      <SectionLabel>Сводка по проекту</SectionLabel>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mt: 1.5, mb: 2 }}>
        <PlanFactBlock
          kind="plan"
          sourceLabel={summary.planVersionName ?? 'Нет версии плана'}
          hours={summary.planHours}
          z={summary.planWeightedZScore}
          interpretation={planInterpretation}
        />
        {hasFact ? (
          <PlanFactBlock
            kind="fact"
            sourceLabel={summary.factVersionName ?? ''}
            hours={summary.factHours}
            z={summary.factWeightedZScore}
            interpretation={factInterpretation}
          />
        ) : (
          <Box
            sx={{
              flex: 1,
              bgcolor: CARD_BG,
              borderRadius: '12px',
              border: `1px solid ${BORDER}`,
              p: 2.5,
              position: 'relative',
              overflow: 'hidden',
            }}
          >
            <Box sx={{ position: 'absolute', top: 0, left: 0, width: 4, height: '100%', bgcolor: MUTED_COLOR }} />
            <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 1.5 }}>
              <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: MUTED_COLOR }} />
              <Typography sx={{ fontSize: 13, fontWeight: 700, color: PRIMARY_TEXT }}>Факт</Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary">
              Н/Д — назначьте основную версию факта в списке версий проекта.
            </Typography>
          </Box>
        )}
      </Stack>
      <Stack direction="row" spacing={2} alignItems="center" sx={{ px: 0.5 }} flexWrap="wrap" useFlexGap>
        {deltaPct !== null && (
          <>
            <Typography sx={{ fontSize: 12, color: MUTED_COLOR }}>Отклонение факта от плана:</Typography>
            <DeltaChip deltaPct={deltaPct} />
          </>
        )}
        <Box sx={{ flex: 1 }} />
        <Typography sx={{ fontSize: 12, color: MUTED_COLOR }}>
          Перцентиль: <b style={{ color: PRIMARY_TEXT }}>план {summary.planPercentile ?? 'Н/Д'}</b>
          {' · '}
          <b style={{ color: PRIMARY_TEXT }}>факт {summary.factPercentile ?? 'Н/Д'}</b>
          {' · '}Метод агрегации: <b style={{ color: PRIMARY_TEXT }}>взвешенный по IIC</b>
          {accuracyScore !== null && (
            <>
              {' · '}Точность сравнения: <b style={{ color: PRIMARY_TEXT }}>{fmt(accuracyScore, 1)}%</b>
            </>
          )}
        </Typography>
      </Stack>
    </Box>
>>>>>>> origin/master
  );
}
