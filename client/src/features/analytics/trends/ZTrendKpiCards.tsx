import type { ReactNode } from 'react';
import { Box, Paper, Stack, Typography } from '@mui/material';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import RemoveIcon from '@mui/icons-material/Remove';
import { getInterpretationColor } from '../../../utils/interpretationStyle';
import type { ZTrendKpiData } from './zTrendKpi';

const zFormat = new Intl.NumberFormat('ru-RU', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});
const zSignedFormat = new Intl.NumberFormat('ru-RU', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  signDisplay: 'always',
});

// Пороги зеркалят PerformanceInterpretationHelper (backend): меньший Z = выше производительность.
function zToInterpretation(z: number | null): string | null {
  if (z == null || !Number.isFinite(z)) return null;
  if (z < -1) return 'отличное';
  if (z < -0.5) return 'хорошее';
  if (z <= 0.5) return 'удовлетворительное';
  if (z <= 1) return 'требует внимания';
  return 'критическое';
}

function formatZ(value: number | null): string {
  if (value == null || !Number.isFinite(value)) return '—';
  return zFormat.format(value);
}

function formatDate(iso: string | null): string | undefined {
  if (!iso) return undefined;
  const d = new Date(iso);
  if (!Number.isFinite(d.getTime())) return undefined;
  return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

/** Δ = факт − план. Меньший Z лучше: отрицательная дельта — факт лучше плана. */
function classifyDelta(delta: number | null): { label: string; color: string } {
  if (delta == null || !Number.isFinite(delta)) {
    return { label: 'Нет данных', color: getInterpretationColor(null) };
  }
  if (Math.abs(delta) <= 0.05) {
    return { label: 'Соответствует плану', color: getInterpretationColor('удовлетворительное') };
  }
  return delta < 0
    ? { label: 'Факт лучше плана', color: getInterpretationColor('отличное') }
    : { label: 'Факт хуже плана', color: getInterpretationColor('критическое') };
}

function DeltaIcon({ value }: { value: number | null }) {
  if (value == null || !Number.isFinite(value) || Math.abs(value) <= 0.05) {
    return <RemoveIcon sx={{ fontSize: 16 }} />;
  }
  return value < 0 ? (
    <TrendingDownIcon sx={{ fontSize: 16 }} />
  ) : (
    <TrendingUpIcon sx={{ fontSize: 16 }} />
  );
}

function KpiCard({
  title,
  value,
  valueColor,
  subtitle,
  badge,
  testId,
}: {
  title: string;
  value: string;
  valueColor?: string;
  subtitle?: string;
  badge?: { label: string; color: string; icon?: ReactNode };
  testId?: string;
}) {
  return (
    <Paper
      variant="outlined"
      data-testid={testId}
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
      <Typography
        sx={{ fontSize: 12, fontWeight: 600, color: '#94A3B8', textTransform: 'uppercase', letterSpacing: 0.3 }}
      >
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
      {subtitle && <Typography sx={{ fontSize: 12, color: '#94A3B8' }}>{subtitle}</Typography>}
    </Paper>
  );
}

const PLAN_MATCH_SUBTITLE: Record<NonNullable<ZTrendKpiData['planMatchKind']>, string> = {
  'same-date': 'на ту же дату',
  'nearest-prior': 'ближайшая ≤ даты факта',
  nearest: 'ближайшая по дате',
  latest: 'последняя плановая версия',
};

export function ZTrendKpiCards({ kpi }: { kpi: ZTrendKpiData }) {
  const delta = classifyDelta(kpi.deltaZ);
  const factSubtitleParts = [formatDate(kpi.lastFactDate)];
  if (kpi.lastFactProjectCount != null && kpi.lastFactProjectCount > 0) {
    factSubtitleParts.push(`${kpi.lastFactProjectCount} проект.`);
  }

  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5 }} data-testid="ztrend-kpi-cards">
      <KpiCard
        testId="ztrend-kpi-fact"
        title="Факт Z (последняя версия)"
        value={formatZ(kpi.lastFactZ)}
        valueColor={
          kpi.lastFactZ != null
            ? getInterpretationColor(zToInterpretation(kpi.lastFactZ))
            : undefined
        }
        subtitle={factSubtitleParts.filter(Boolean).join(' · ') || undefined}
      />
      <KpiCard
        testId="ztrend-kpi-plan"
        title="План Z"
        value={formatZ(kpi.matchedPlanZ)}
        valueColor={
          kpi.matchedPlanZ != null
            ? getInterpretationColor(zToInterpretation(kpi.matchedPlanZ))
            : undefined
        }
        subtitle={kpi.planMatchKind ? PLAN_MATCH_SUBTITLE[kpi.planMatchKind] : undefined}
      />
      <KpiCard
        testId="ztrend-kpi-delta"
        title="Отклонение Δ"
        value={kpi.deltaZ != null ? zSignedFormat.format(kpi.deltaZ) : '—'}
        valueColor={delta.color}
        badge={{
          label: delta.label,
          color: delta.color,
          icon: <DeltaIcon value={kpi.deltaZ} />,
        }}
      />
      <KpiCard
        testId="ztrend-kpi-versions"
        title="Версий в выборке"
        value={String(kpi.versionsCount)}
        subtitle="точек тренда факта"
      />
      <KpiCard
        testId="ztrend-kpi-projects"
        title="Проектов"
        value={String(kpi.projectsCount)}
        subtitle="с рассчитанным Z факта"
      />
    </Box>
  );
}
