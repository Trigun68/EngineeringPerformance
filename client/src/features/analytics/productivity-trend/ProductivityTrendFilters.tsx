import { Alert, Box, LinearProgress, Paper, Skeleton, Typography } from '@mui/material';
import type { ProductivityIndexTrendResponse } from '../../../api/productivityIndexTrendApi';
import type { ZTrendResponse } from '../../../api/zTrendApi';
import { HoursBridgeChart } from '../shared/HoursBridgeChart';
import { HoursCoverageChips } from '../shared/HoursCoverageChips';
import { SCurveChart } from '../shared/SCurveChart';
import { TrendDetailView, type TrendDetailMetricConfig } from '../shared/TrendDetailView';
import type { FactPlanMode } from '../shared/FactPlanToggle';
import type { GroupTrendLike, TrendPointLike, TrendVersionRow } from '../shared/trendDetailCalc';

export function formatPeriodChip(dateFrom: string, dateTo: string): string {
  const fmt = (iso: string) => {
    const d = new Date(iso);
    return Number.isFinite(d.getTime())
      ? d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
      : '';
  };
  const from = dateFrom ? fmt(dateFrom) : '';
  const to = dateTo ? fmt(dateTo) : '';
  if (from && to) return `Период: ${from} – ${to}`;
  if (from) return `Период: с ${from}`;
  if (to) return `Период: по ${to}`;
  return 'Весь период';
}

export interface ProductivityTrendFiltersProps {
  dateFrom: string;
  dateTo: string;
  mode: FactPlanMode;
  onModeChange: (mode: FactPlanMode) => void;
  metric: TrendDetailMetricConfig;
  indexLoading: boolean;
  indexResult: ProductivityIndexTrendResponse | null;
  indexError: string | null;
  hasIndexPoints: boolean;
  indexPoints: { plan: TrendPointLike[]; fact: TrendPointLike[] };
  indexVersions: { plan: TrendVersionRow[]; fact: TrendVersionRow[] };
  indexWaterfallGroups: GroupTrendLike[];
  hours: ZTrendResponse | null;
}

export function ProductivityTrendFilters({
  dateFrom,
  dateTo,
  mode,
  onModeChange,
  metric,
  indexLoading,
  indexResult,
  indexError,
  hasIndexPoints,
  indexPoints,
  indexVersions,
  indexWaterfallGroups,
  hours,
}: ProductivityTrendFiltersProps) {
  return (
    <>
      {indexLoading && !indexResult && (
        <Box data-testid="prod-index-loading" sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5 }}>
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton
                key={i}
                variant="rounded"
                height={96}
                sx={{ flex: '1 1 200px', minWidth: 200, borderRadius: 2.5 }}
              />
            ))}
          </Box>
          <Skeleton variant="rounded" height={360} sx={{ borderRadius: 2.5 }} />
        </Box>
      )}

      {indexLoading && indexResult ? (
        <LinearProgress sx={{ borderRadius: 1 }} data-testid="prod-index-refresh-progress" />
      ) : null}

      {indexError && (
        <Alert severity="error" data-testid="prod-index-error">
          {indexError}
        </Alert>
      )}

      {indexResult && indexResult.warnings.length > 0 && (
        <Alert severity="info" data-testid="prod-index-warnings" sx={{ py: 0.5 }}>
          {indexResult.warnings.slice(0, 3).join(' ')}
        </Alert>
      )}

      {indexResult && !hasIndexPoints && !indexLoading && (
        <Alert severity="info" data-testid="prod-index-empty">
          Нет основных версий (isPrimary) с рассчитанным индексом производительности в выбранном
          периоде.
        </Alert>
      )}

      {indexResult && hasIndexPoints && (
        <TrendDetailView
          mode={mode}
          onModeChange={onModeChange}
          config={metric}
          periodLabel={formatPeriodChip(dateFrom, dateTo)}
          planPoints={indexPoints.plan}
          factPoints={indexPoints.fact}
          planVersions={indexVersions.plan}
          factVersions={indexVersions.fact}
          waterfallGroups={indexWaterfallGroups}
          aggregation={indexResult.detail}
          testId="prod-trend-detail"
        />
      )}

      {hours ? (
        <Box data-testid="prod-trend-hours-chapter">
          <Typography variant="h6" component="h2" sx={{ mb: 1 }}>
            Часы основных версий
          </Typography>
          <HoursCoverageChips coverage={hours.coverage} />
          <Paper variant="outlined" sx={{ p: 2, mt: 1 }}>
            {hours.disciplineBridge.lines.length > 0 ? (
              <HoursBridgeChart lines={hours.disciplineBridge.lines} testId="prod-trend-hours-bridge" />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет пары план/факт с объёмом и часами для моста.
              </Typography>
            )}
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Сырой Δ часов:{' '}
              {hours.rawHoursDelta == null
                ? '—'
                : `${hours.rawHoursDelta > 0 ? '+' : ''}${hours.rawHoursDelta.toLocaleString('ru-RU', { maximumFractionDigits: 1 })} ч`}
            </Typography>
          </Paper>
          {(hours.portfolioSCurve.length > 0 || hours.planSCurve.length > 0) && (
            <Paper variant="outlined" sx={{ p: 2, mt: 2 }}>
              <SCurveChart
                points={hours.portfolioSCurve}
                planPoints={hours.planSCurve}
                testId="prod-trend-scurve"
              />
            </Paper>
          )}
        </Box>
      ) : null}
    </>
  );
}
