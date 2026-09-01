import { Box, Chip, Stack, Typography } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import type { ProjectReportSummary } from '../../../../api/analyticsApi';
import {
  ACCENT_FACT,
  ACCENT_PLAN,
  fmt,
  fmtSigned,
  formatFactSourceLabel,
  formatPlanSourceLabel,
  getInterpretation,
  hoursDeltaPct,
  INTERPRETATION_BG,
  INTERPRETATION_COLOR,
  INTERPRETATION_SHORT_RU,
  MUTED_COLOR,
  type Interpretation,
} from '../reportCalc';
import { BORDER, CARD_BG, PRIMARY_TEXT, SECTION_BG, SectionLabel } from './reportChrome';

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
        flex: '1 1 280px',
        minWidth: 0,
        bgcolor: CARD_BG,
        borderRadius: '12px',
        border: `1px solid ${BORDER}`,
        p: 2.5,
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <Box sx={{ position: 'absolute', top: 0, left: 0, width: 4, height: '100%', bgcolor: accent }} />
      <Stack spacing={0.4} sx={{ mb: 1.5 }}>
        <Stack direction="row" alignItems="center" spacing={1}>
          <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: accent, flexShrink: 0 }} />
          <Typography sx={{ fontSize: 13, fontWeight: 700, color: PRIMARY_TEXT }}>
            {kind === 'plan' ? 'План' : 'Факт'}
          </Typography>
        </Stack>
        <Typography sx={{ fontSize: 11, color: MUTED_COLOR, pl: 2.25 }}>{sourceLabel}</Typography>
      </Stack>
      <Stack direction="row" spacing={4} flexWrap="wrap" useFlexGap>
        <Box>
          <Typography sx={{ fontSize: 11, color: MUTED_COLOR, mb: 0.3 }}>Трудоёмкость</Typography>
          <Typography sx={{ fontSize: 26, fontWeight: 800, color: PRIMARY_TEXT, lineHeight: 1.1 }}>
            {hours === null ? '—' : `${fmt(hours, 1)} ч`}
          </Typography>
        </Box>
        <Box>
          <Typography sx={{ fontSize: 11, color: MUTED_COLOR, mb: 0.3 }}>
            {kind === 'plan' ? 'Заложенный Z' : 'Оценка Z'}
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

interface PlanFactSummaryProps {
  summary: ProjectReportSummary;
  accuracyScore: number | null;
}

/**
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
    <Box data-testid="report-summary-section" sx={{ bgcolor: SECTION_BG, borderRadius: '16px', p: 3 }}>
      <SectionLabel>Сводка по проекту</SectionLabel>
      <Stack direction="row" spacing={2} sx={{ mt: 1.5, mb: 2 }} flexWrap="wrap" useFlexGap>
        <PlanFactBlock
          kind="plan"
          sourceLabel={formatPlanSourceLabel(summary.planVersionName, summary.planIsPrimary)}
          hours={summary.planHours}
          z={summary.planWeightedZScore}
          interpretation={planInterpretation}
        />
        {hasFact ? (
          <PlanFactBlock
            kind="fact"
            sourceLabel={formatFactSourceLabel(summary.factVersionName, summary.factIsPrimary)}
            hours={summary.factHours}
            z={summary.factWeightedZScore}
            interpretation={factInterpretation}
          />
        ) : (
          <Box
            sx={{
              flex: '1 1 280px',
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
  );
}
