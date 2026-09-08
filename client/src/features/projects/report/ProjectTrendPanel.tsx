import { useMemo } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Alert, Chip, Paper, Skeleton, Stack, Typography } from '@mui/material';
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined';
import { fetchProjectTrend, type ProjectTrendDto } from '../../../api/projectTrendApi';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import { FACT_ACCENT_COLOR, PLAN_ACCENT_COLOR } from '../../../constants/planFactColors';
import { AnalyticsExtraAccordion } from '../../analytics/shared/AnalyticsExtraAccordion';
import { FactorWaterfallChart } from '../../analytics/shared/FactorWaterfallChart';
import { HoursBridgeChart } from '../../analytics/shared/HoursBridgeChart';
import { HoursCoverageChips } from '../../analytics/shared/HoursCoverageChips';
import { PeriodDynamicsChart, type DynamicsSeries } from '../../analytics/shared/PeriodDynamicsChart';
import { SCurveChart } from '../../analytics/shared/SCurveChart';
import { ZeroBaselineChart } from '../../analytics/shared/ZeroBaselineChart';

const PORTFOLIO_COLOR = '#64748B';

interface Props {
  projectId: number;
}

function formatDate(iso: string | null): string {
  if (!iso) return '';
  const d = new Date(iso);
  return Number.isFinite(d.getTime()) ? d.toLocaleDateString('ru-RU') : iso.slice(0, 10);
}

function formatRawDelta(n: number | null): string {
  if (n == null) return '—';
  const sign = n > 0 ? '+' : '';
  return `${sign}${n.toLocaleString('ru-RU', { maximumFractionDigits: 1 })} ч`;
}

export function ProjectTrendPanel({ projectId }: Props) {
  const trendQuery = useQuery({
    queryKey: queryKeys.analytics.projectTrend(projectId),
    queryFn: ({ signal }) => fetchProjectTrend(projectId, signal),
    enabled: Boolean(projectId),
    placeholderData: keepPreviousData,
  });

  const data: ProjectTrendDto | null = trendQuery.data ?? null;
  const loading = trendQuery.isFetching;
  const error =
    trendQuery.isError && !isRequestCanceled(trendQuery.error)
      ? getApiErrorMessage(trendQuery.error, 'Не удалось загрузить тренд проекта.')
      : null;

  const zSeries = useMemo<DynamicsSeries[]>(() => {
    if (!data) return [];
    const plan = data.zHistory.filter((p) => p.direction !== 'fact' && p.direction !== 'portfolio');
    const fact = data.zHistory.filter((p) => p.direction === 'fact');
    const series: DynamicsSeries[] = [];
    if (plan.length > 0) {
      series.push({
        id: 'plan',
        name: 'План',
        color: PLAN_ACCENT_COLOR,
        dashStyle: 'Dash',
        points: plan.map((p) => ({ date: p.date, value: p.zScore })),
      });
    }
    if (fact.length > 0) {
      series.push({
        id: 'fact',
        name: 'Факт',
        color: FACT_ACCENT_COLOR,
        points: fact.map((p) => ({ date: p.date, value: p.zScore })),
      });
    }
    if (data.portfolioZHistory.length > 0) {
      series.push({
        id: 'portfolio',
        name: 'Портфель',
        color: PORTFOLIO_COLOR,
        dashStyle: 'ShortDash',
        points: data.portfolioZHistory.map((p) => ({ date: p.date, value: p.zScore })),
      });
    }
    return series;
  }, [data]);

  if (loading && !data) {
    return (
      <Stack spacing={2} data-testid="project-trend-loading">
        <Skeleton variant="rounded" height={36} />
        <Skeleton variant="rounded" height={280} />
        <Skeleton variant="rounded" height={280} />
      </Stack>
    );
  }

  return (
    <Stack spacing={2.5} data-testid="project-trend-panel">
      {error ? (
        <Alert severity="error" data-testid="project-trend-error">
          {error}
        </Alert>
      ) : null}

      {data?.warnings.slice(0, 3).map((w) => (
        <Alert key={w} severity="info" sx={{ py: 0.5 }}>
          {w}
        </Alert>
      ))}

      {data ? (
        <>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              size="small"
              variant="outlined"
              label={`${data.projectCode} — ${data.projectName}`}
              data-testid="project-trend-project-chip"
            />
            {data.factVersionName ? (
              <Chip
                size="small"
                icon={<CalendarMonthOutlinedIcon fontSize="small" />}
                variant="outlined"
                label={`Факт: ${data.factVersionName}${data.factVersionDate ? ` · ${formatDate(data.factVersionDate)}` : ''}`}
                data-testid="project-trend-fact-chip"
              />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет основной факт-версии.
              </Typography>
            )}
            {data.planVersionName ? (
              <Chip size="small" variant="outlined" label={`План: ${data.planVersionName}`} />
            ) : null}
          </Stack>

          <Typography variant="h6" component="h2">
            Динамика Z по датам
          </Typography>
          <Paper variant="outlined" sx={{ p: 2 }}>
            {zSeries.length > 0 ? (
              <PeriodDynamicsChart
                series={zSeries}
                yAxisTitle="Средневзвешенная Z-оценка"
                zeroLine
                band={data.zPercentileBand}
                events={data.events}
                testId="project-trend-z-history"
              />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет точек Z по версиям.
              </Typography>
            )}
          </Paper>

          <Typography variant="h6" component="h2">
            Отклонения по дисциплинам
          </Typography>
          <HoursCoverageChips coverage={data.coverage} testId="project-trend-coverage" />
          <Paper variant="outlined" sx={{ p: 2 }}>
            {data.hoursBridge.lines.length > 0 ? (
              <HoursBridgeChart lines={data.hoursBridge.lines} testId="project-trend-hours-bridge" />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет пары план/факт с объёмом и часами для моста.
              </Typography>
            )}
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }} data-testid="project-trend-raw-delta">
              Сырой Δ часов: {formatRawDelta(data.rawHoursDelta)}
            </Typography>
          </Paper>

          {data.planDeltaWaterfall && data.planDeltaWaterfallStart != null ? (
            <>
              <Typography variant="h6" component="h2">
                Изменение Z плана за период
              </Typography>
              <Paper variant="outlined" sx={{ p: 2 }}>
                <FactorWaterfallChart
                  startLabel="Было"
                  startValue={data.planDeltaWaterfallStart}
                  endLabel="Стало"
                  steps={data.planDeltaWaterfall}
                  goodDirection="positive"
                  testId="project-trend-plan-waterfall"
                />
              </Paper>
            </>
          ) : (
            <Typography variant="body2" color="text.secondary">
              Недостаточно истории плана для разбора ΔZ.
            </Typography>
          )}

          <Typography variant="h6" component="h2">
            Z по группам IIC относительно нормы
          </Typography>
          <Paper variant="outlined" sx={{ p: 2 }}>
            {data.zFromZero.length > 0 ? (
              <ZeroBaselineChart bars={data.zFromZero} testId="project-trend-z-from-zero" />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет Z по корневым группам IIC.
              </Typography>
            )}
          </Paper>

          <Typography variant="h6" component="h2">
            S-curve часов
          </Typography>
          <Paper variant="outlined" sx={{ p: 2 }}>
            {data.sCurve.length > 0 || data.planSCurve.length > 0 ? (
              <SCurveChart
                points={data.sCurve}
                planPoints={data.planSCurve}
                events={data.events}
                testId="project-trend-scurve"
              />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет версий с часами.
              </Typography>
            )}
          </Paper>

          <AnalyticsExtraAccordion testId="project-trend-extra">
            {data.deltaWaterfall && data.deltaWaterfallStart != null ? (
              <>
                <Typography variant="h6" component="h2" sx={{ mb: 1 }}>
                  Изменение Z факта за период по группам
                </Typography>
                <Paper variant="outlined" sx={{ p: 2 }}>
                  <FactorWaterfallChart
                    startLabel="Было"
                    startValue={data.deltaWaterfallStart}
                    endLabel="Стало"
                    steps={data.deltaWaterfall}
                    goodDirection="positive"
                    testId="project-trend-delta-waterfall"
                  />
                </Paper>
              </>
            ) : (
              <Typography variant="body2" color="text.secondary">
                Недостаточно истории факта для разбора ΔZ.
              </Typography>
            )}
          </AnalyticsExtraAccordion>
        </>
      ) : null}
    </Stack>
  );
}
