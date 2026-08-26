import type { ReactNode } from 'react';
import { Box, Paper, Stack, Typography } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import RemoveIcon from '@mui/icons-material/Remove';
import { getInterpretationColor } from '../../utils/interpretationStyle';

const numberFormat = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 0 });
const pctFormat = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 1, signDisplay: 'always' });

function formatHours(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return '—';
  return `${numberFormat.format(value)} ч`;
}

/** Классификация отклонения факта от прогноза/плана — переиспользует цветовую шкалу интерпретации Z. */
function classifyDeviation(pct: number | null): { label: string; color: string } {
  if (pct == null || !Number.isFinite(pct)) return { label: 'Нет данных', color: getInterpretationColor(null) };
  const abs = Math.abs(pct);
  if (abs <= 5) return { label: 'Отличное соответствие', color: getInterpretationColor('отличное') };
  if (abs <= 15) return { label: 'Удовлетворительное отклонение', color: getInterpretationColor('удовлетворительное') };
  return { label: 'Критическое отклонение', color: getInterpretationColor('критическое') };
}

export interface TrendsKpiData {
  lastFactValue: number | null;
  lastFactVersionName: string | null;
  lastFactTimestamp: string | null;
  matchedForecastValue: number | null;
  deltaAbs: number | null;
  deltaPct: number | null;
  cumulativeDeltaAbs: number | null;
  cumulativeDeltaPct: number | null;
  versionsCount: number;
}

function TrendIcon({ value }: { value: number | null }) {
  if (value == null || !Number.isFinite(value) || Math.abs(value) < 0.5) {
    return <RemoveIcon sx={{ fontSize: 16 }} />;
  }
  return value > 0 ? <ArrowUpwardIcon sx={{ fontSize: 16 }} /> : <ArrowDownwardIcon sx={{ fontSize: 16 }} />;
}

function KpiCard({
  title,
  value,
  valueColor,
  subtitle,
  badge,
}: {
  title: string;
  value: string;
  valueColor?: string;
  subtitle?: string;
  badge?: { label: string; color: string; icon?: ReactNode };
}) {
  return (
    <Paper
      variant="outlined"
      sx={{
        p: 2,
        flex: '1 1 200px',
        minWidth: 200,
        borderColor: '#E2E8F0',
        borderRadius: 2.5,
        display: 'flex',
        flexDirection: 'column',
        gap: 0.5,
      }}
    >
      <Typography sx={{ fontSize: 12, fontWeight: 600, color: '#94A3B8', textTransform: 'uppercase', letterSpacing: 0.3 }}>
        {title}
      </Typography>
      <Typography sx={{ fontSize: 24, fontWeight: 700, color: valueColor ?? '#1E293B', lineHeight: 1.2 }}>
        {value}
      </Typography>
      {badge && (
        <Stack direction="row" spacing={0.5} alignItems="center" sx={{ color: badge.color }}>
          {badge.icon}
          <Typography sx={{ fontSize: 13, fontWeight: 600, color: badge.color }}>{badge.label}</Typography>
        </Stack>
      )}
      {subtitle && (
        <Typography sx={{ fontSize: 12, color: '#94A3B8' }}>{subtitle}</Typography>
      )}
    </Paper>
  );
}

export function TrendsKpiCards({ kpi }: { kpi: TrendsKpiData }) {
  const deviation = classifyDeviation(kpi.deltaPct);
  const cumulativeDeviation = classifyDeviation(kpi.cumulativeDeltaPct);

  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5 }}>
      <KpiCard
        title="Факт (последняя версия)"
        value={formatHours(kpi.lastFactValue)}
        subtitle={kpi.lastFactVersionName ?? undefined}
      />
      <KpiCard
        title="Прогноз / План"
        value={formatHours(kpi.matchedForecastValue)}
        subtitle="на ту же версию"
      />
      <KpiCard
        title="Отклонение Δ"
        value={`${kpi.deltaAbs != null ? numberFormat.format(kpi.deltaAbs) : '—'} ч`}
        valueColor={deviation.color}
        badge={{
          label: kpi.deltaPct != null ? `${pctFormat.format(kpi.deltaPct)}% · ${deviation.label}` : deviation.label,
          color: deviation.color,
          icon: <TrendIcon value={kpi.deltaAbs} />,
        }}
      />
      <KpiCard
        title="Накопленное отклонение"
        value={`${kpi.cumulativeDeltaAbs != null ? numberFormat.format(kpi.cumulativeDeltaAbs) : '—'} ч`}
        valueColor={cumulativeDeviation.color}
        badge={{
          label:
            kpi.cumulativeDeltaPct != null
              ? `${pctFormat.format(kpi.cumulativeDeltaPct)}% · по всем версиям`
              : 'по всем версиям',
          color: cumulativeDeviation.color,
        }}
      />
      <KpiCard
        title="Версий в выборке"
        value={String(kpi.versionsCount)}
        subtitle="точек тренда факта"
      />
    </Box>
  );
}
