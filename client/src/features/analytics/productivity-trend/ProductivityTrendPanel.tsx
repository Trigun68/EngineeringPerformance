import { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  Grid,
  LinearProgress,
  Paper,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import HelpOutlineOutlinedIcon from '@mui/icons-material/HelpOutlineOutlined';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import { FACT_ACCENT_COLOR, PLAN_ACCENT_COLOR } from '../../../constants/planFactColors';
import type { FactPlanMode } from '../shared/FactPlanToggle';
import { AnalyticsExtraAccordion } from '../shared/AnalyticsExtraAccordion';
import type { TrendDetailMetricConfig } from '../shared/TrendDetailView';
import { FormulaTypeLegend } from './FormulaTypeLegend';
import { ProductivityTrendChart } from './ProductivityTrendChart';
import { ProductivityTrendFilters } from './ProductivityTrendFilters';
import { ProductivityTrendTransitionsGrid } from './ProductivityTrendTransitionsGrid';
import { formatNum, sortByAttention } from './productivityTrendLabels';
import { useProductivityTrendData } from './useProductivityTrendData';
import type { ProductivityVolumeMode } from '../../../api/productivityTrendApi';

interface Props {
  dateFrom: string;
  dateTo: string;
  volumeMode: ProductivityVolumeMode;
}

const PRODUCTIVITY_INDEX_METRIC: TrendDetailMetricConfig = {
  yAxisTitle: 'Индекс производительности, % к норме',
  unit: ' %',
  digits: 1,
  zeroLine: false,
  goodDirection: 'positive',
  factColor: FACT_ACCENT_COLOR,
  planColor: PLAN_ACCENT_COLOR,
};

export function ProductivityTrendPanel({ dateFrom, dateTo, volumeMode }: Props) {
  const [mode, setMode] = useState<FactPlanMode>('fact');
  const data = useProductivityTrendData(dateFrom, dateTo, volumeMode);
  const {
    result, loading, error, indexResult, indexLoading, indexError, hours,
    indexPoints, indexVersions, indexWaterfallGroups, hasIndexPoints,
  } = data;

  const hasSeries = Boolean(
    result && (result.overall.segments.some((s) => s.points.length > 0) || result.byGroup.length > 0),
  );
  const sortedTransitions = useMemo(
    () => sortByAttention(result?.transitions ?? []),
    [result],
  );
  const summary = useMemo(() => {
    const transitions = result?.transitions ?? [];
    const total = transitions.length;
    const tightening = transitions.filter((t) => t.verdict === 'tightening').length;
    const loosening = transitions.filter((t) => t.verdict === 'loosening').length;
    return { total, tightening, loosening, other: total - tightening - loosening };
  }, [result]);

  const kpiCards = [
    {
      key: 'total',
      label: 'Всего смен формулы',
      value: summary.total,
      icon: <AssessmentOutlinedIcon fontSize="small" />,
      color: 'text.secondary' as const,
    },
    {
      key: 'tightening',
      label: 'Ужесточение',
      value: summary.tightening,
      icon: <TrendingUpIcon fontSize="small" />,
      color: summary.tightening > 0 ? ('warning.main' as const) : ('text.secondary' as const),
    },
    {
      key: 'loosening',
      label: 'Ослабление',
      value: summary.loosening,
      icon: <TrendingDownIcon fontSize="small" />,
      color: summary.loosening > 0 ? ('success.main' as const) : ('text.secondary' as const),
    },
    {
      key: 'other',
      label: 'Без явного эффекта',
      value: summary.other,
      icon: <HelpOutlineOutlinedIcon fontSize="small" />,
      color: 'text.secondary' as const,
    },
  ];

  return (
    <Stack spacing={2.5} data-testid="prod-trend-panel">
      <ProductivityTrendFilters
        dateFrom={dateFrom}
        dateTo={dateTo}
        mode={mode}
        onModeChange={setMode}
        metric={PRODUCTIVITY_INDEX_METRIC}
        indexLoading={indexLoading}
        indexResult={indexResult}
        indexError={indexError}
        hasIndexPoints={hasIndexPoints}
        indexPoints={indexPoints}
        indexVersions={indexVersions}
        indexWaterfallGroups={indexWaterfallGroups}
        hours={hours}
      />

      <AnalyticsExtraAccordion testId="prod-trend-extra">
        <Box>
          <Typography variant="h6" component="h2">
            Нормы IIC: смена формул и часов при опорном объёме
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Как менялась сама норма: часы N(V*) при опорном объёме и смены формул расчёта за период.
          </Typography>
        </Box>

        {loading && !result && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={32} data-testid="prod-trend-loading" />
          </Box>
        )}
        {loading && result ? (
          <LinearProgress sx={{ borderRadius: 1 }} data-testid="prod-trend-refresh-progress" />
        ) : null}
        {error && (
          <Alert severity="error" data-testid="prod-trend-error">
            {error}
          </Alert>
        )}

        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }} data-testid="prod-trend-kpi-row">
          {loading && !result
            ? Array.from({ length: 4 }).map((_, i) => (
                <Skeleton key={i} variant="rounded" sx={{ flex: '1 1 220px', minWidth: 200 }} height={80} />
              ))
            : result && hasSeries
              ? kpiCards.map((card) => (
                  <Paper
                    key={card.key}
                    variant="outlined"
                    sx={{
                      flex: '1 1 220px',
                      minWidth: 200,
                      p: 2,
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1.5,
                    }}
                  >
                    <Box sx={{ color: card.color, display: 'flex' }}>{card.icon}</Box>
                    <Box>
                      <Typography variant="h6" component="div" sx={{ color: card.color, lineHeight: 1.2 }}>
                        {card.value}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {card.label}
                      </Typography>
                    </Box>
                  </Paper>
                ))
              : null}
        </Box>

        {result && result.warnings.length > 0 && (
          <Alert severity="info" data-testid="prod-trend-warnings" sx={{ py: 0.5 }}>
            {result.warnings.slice(0, 3).join(' ')}
          </Alert>
        )}
        {result && !hasSeries && (
          <Alert severity="info" data-testid="prod-trend-empty">
            Нет норм IIC, пересекающих выбранный период.
          </Alert>
        )}

        {result && hasSeries && (
          <>
            <Typography variant="h6" component="h2">
              Индекс часов при опорном объёме V*
            </Typography>
            <FormulaTypeLegend legend={result.legend} />
            <ProductivityTrendChart
              group={result.overall}
              metric="index"
              height={320}
              title="Индекс Σ N(V*)·V* относительно начала периода"
              testId="prod-trend-overall-chart"
            />
            {result.byGroup.length > 0 && (
              <>
                <Typography variant="h6" component="h2" sx={{ mt: 1 }}>
                  Часы N(V*) по группам IIC
                </Typography>
                <Grid container spacing={2} data-testid="prod-trend-groups-grid">
                  {result.byGroup.map((g) => (
                    <Grid item xs={12} md={6} lg={4} key={g.groupId}>
                      <ProductivityTrendChart
                        group={g}
                        metric="hours"
                        height={260}
                        title={`${g.groupName}${g.vStar != null ? ` · V*=${formatNum(g.vStar, 1)}` : ''}`}
                        testId={`prod-trend-group-${g.groupId}`}
                      />
                    </Grid>
                  ))}
                </Grid>
              </>
            )}
            <Typography variant="h6" component="h2" sx={{ mt: 1 }}>
              Смены формулы
            </Typography>
            <ProductivityTrendTransitionsGrid rowData={sortedTransitions} />
          </>
        )}
      </AnalyticsExtraAccordion>
    </Stack>
  );
}
