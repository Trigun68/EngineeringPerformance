import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Chip,
  Grid,
  LinearProgress,
  Paper,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material';
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined';
import { fetchZTrends, type ZTrendWeightMode } from '../../../api/zTrendApi';
import { exportAnalyticsTrendExcel, exportAnalyticsTrendPdf } from '../../../api/projectTrendApi';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { FACT_ACCENT_COLOR, PLAN_ACCENT_COLOR } from '../../../constants/planFactColors';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import { AnalyticsExtraAccordion } from '../shared/AnalyticsExtraAccordion';
import { FactPlanToggle, type FactPlanMode } from '../shared/FactPlanToggle';
import { FactorWaterfallChart } from '../shared/FactorWaterfallChart';
import { HoursBridgeChart } from '../shared/HoursBridgeChart';
import { HoursCoverageChips } from '../shared/HoursCoverageChips';
import { MoversTable } from '../shared/MoversTable';
import { PeriodDynamicsChart, type DynamicsSeries } from '../shared/PeriodDynamicsChart';
import { SCurveChart } from '../shared/SCurveChart';
import { ZeroBaselineChart } from '../shared/ZeroBaselineChart';
import { NormLegend } from './NormLegend';
import { ZTrendChart } from './ZTrendChart';

function formatPeriodChip(dateFrom: string, dateTo: string): string {
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

function formatRawDelta(n: number | null): string {
  if (n == null) return '—';
  const sign = n > 0 ? '+' : '';
  return `${sign}${n.toLocaleString('ru-RU', { maximumFractionDigits: 1 })} ч`;
}

interface Props {
  dateFrom: string;
  dateTo: string;
  weightMode: ZTrendWeightMode;
  mode: FactPlanMode;
  onModeChange: (mode: FactPlanMode) => void;
  onSelectWorkDirection?: (workDirectionId: number) => void;
  onToolbarExtras?: (node: ReactNode) => void;
}

export function TrendAnalysisPanel({
  dateFrom,
  dateTo,
  weightMode,
  mode,
  onModeChange,
  onSelectWorkDirection,
  onToolbarExtras,
}: Props) {
  const navigate = useNavigate();
  const [actionError, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState<'excel' | 'pdf' | null>(null);

  // Период и вес в queryKey — смена дат даёт новый запрос без debounce.
  const trendsQuery = useQuery({
    queryKey: queryKeys.analytics.zTrends(dateFrom, dateTo, weightMode),
    queryFn: ({ signal }) =>
      fetchZTrends(
        {
          periodStart: dateFrom || null,
          periodEnd: dateTo || null,
          weightMode,
        },
        signal,
      ),
    placeholderData: keepPreviousData,
  });

  const result = trendsQuery.data ?? null;
  const loading = trendsQuery.isFetching;
  const queryError =
    trendsQuery.isError && !isRequestCanceled(trendsQuery.error)
      ? getApiErrorMessage(trendsQuery.error, 'Не удалось загрузить тренды')
      : null;
  const error = actionError ?? queryError;

  const requestBody = useMemo(
    () => ({
      periodStart: dateFrom || null,
      periodEnd: dateTo || null,
      weightMode,
    }),
    [dateFrom, dateTo, weightMode],
  );

  const handleExport = useCallback(
    async (kind: 'excel' | 'pdf') => {
      setExporting(kind);
      try {
        if (kind === 'excel') await exportAnalyticsTrendExcel(requestBody);
        else await exportAnalyticsTrendPdf(requestBody);
      } catch (err: unknown) {
        setError(getApiErrorMessage(err, 'Не удалось выгрузить тренд.'));
      } finally {
        setExporting(null);
      }
    },
    [requestBody],
  );

  useEffect(() => {
    onToolbarExtras?.(
      <Stack direction="row" spacing={1}>
        <ExportActionButton
          kind="excel"
          loading={exporting === 'excel'}
          disabled={exporting !== null || !result}
          onClick={() => void handleExport('excel')}
          data-testid="ztrend-export-excel"
        />
        <ExportActionButton
          kind="pdf"
          loading={exporting === 'pdf'}
          disabled={exporting !== null || !result}
          onClick={() => void handleExport('pdf')}
          data-testid="ztrend-export-pdf"
        />
      </Stack>,
    );
    return () => onToolbarExtras?.(null);
  }, [onToolbarExtras, exporting, result, handleExport]);

  const hasSegments = useMemo(() => {
    if (!result) return false;
    const o = result.overall;
    return o.plan.segments.length > 0 || o.fact.segments.length > 0 || result.byGroup.length > 0;
  }, [result]);

  const hasContent = useMemo(() => {
    if (!result) return false;
    return (
      hasSegments ||
      result.disciplineBridge.lines.length > 0 ||
      result.groupZeroBars.length > 0 ||
      result.portfolioSCurve.length > 0 ||
      result.movers.length > 0 ||
      (result.detail?.planMonthly.length ?? 0) > 0 ||
      (result.detail?.factMonthly.length ?? 0) > 0
    );
  }, [result, hasSegments]);

  const showPlan = mode !== 'fact';
  const showFact = mode !== 'plan';

  const zSeries = useMemo<DynamicsSeries[]>(() => {
    if (!result) return [];
    const series: DynamicsSeries[] = [];
    const planMonthly = result.detail?.planMonthly ?? [];
    const factMonthly = result.detail?.factMonthly ?? [];
    if (showPlan && planMonthly.some((p) => p.value !== null)) {
      series.push({
        id: 'plan',
        name: 'План (основные версии)',
        color: PLAN_ACCENT_COLOR,
        dashStyle: 'Dash',
        points: planMonthly,
      });
    }
    if (showFact && factMonthly.some((p) => p.value !== null)) {
      series.push({
        id: 'fact',
        name: 'Факт (основные версии)',
        color: FACT_ACCENT_COLOR,
        points: factMonthly,
      });
    }
    return series;
  }, [result, showPlan, showFact]);

  const deltaWaterfall = result?.detail?.factWaterfall ?? null;
  const deltaStart = result?.detail?.factKpi.initial ?? null;
  const planWaterfall = result?.detail?.planWaterfall ?? null;
  const planStart = result?.detail?.planKpi.initial ?? null;

  return (
    <Stack spacing={2.5} data-testid="ztrend-panel">
      {loading && !result && (
        <Box data-testid="ztrend-loading" sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Skeleton variant="rounded" height={40} sx={{ borderRadius: 2 }} />
          <Skeleton variant="rounded" height={320} sx={{ borderRadius: 2.5 }} />
          <Skeleton variant="rounded" height={280} sx={{ borderRadius: 2.5 }} />
        </Box>
      )}

      {loading && result ? (
        <LinearProgress sx={{ borderRadius: 1 }} data-testid="ztrend-refresh-progress" />
      ) : null}

      {error && (
        <Alert severity="error" data-testid="ztrend-error">
          {error}
        </Alert>
      )}

      {result && result.warnings.length > 0 && (
        <Alert severity="info" data-testid="ztrend-warnings" sx={{ py: 0.5 }}>
          {result.warnings.slice(0, 3).join(' ')}
        </Alert>
      )}

      {result && !hasContent && (
        <Alert severity="info" data-testid="ztrend-empty">
          Нет основных версий (isPrimary) с рассчитанным Z в выбранном периоде.
        </Alert>
      )}

      {result && hasContent && (
        <>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              flexWrap: 'wrap',
              gap: 1.5,
            }}
          >
            <Chip
              icon={<CalendarMonthOutlinedIcon fontSize="small" />}
              label={formatPeriodChip(dateFrom, dateTo)}
              variant="outlined"
              data-testid="ztrend-period-chip"
            />
            <FactPlanToggle value={mode} onChange={onModeChange} testId="ztrend-fact-plan-toggle" />
          </Box>

          <Typography variant="h6" component="h2">
            Кто тянет оценку
          </Typography>
          <MoversTable
            rows={result.movers}
            onProjectClick={(id) => navigate(`/projects/${id}?tab=report&reportTab=trend`)}
            testId="ztrend-movers"
          />

          <Typography variant="h6" component="h2">
            Динамика Z по датам
          </Typography>
          <Paper variant="outlined" sx={{ p: 2 }}>
            {zSeries.length > 0 ? (
              <PeriodDynamicsChart
                series={zSeries}
                yAxisTitle="Средневзвешенная Z-оценка"
                zeroLine
                band={result.zPercentileBand}
                events={result.events}
                testId="ztrend-dynamics"
              />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет точек Z основных версий в выбранном режиме.
              </Typography>
            )}
          </Paper>

          <Typography variant="h6" component="h2">
            Отклонения по дисциплинам
          </Typography>
          <HoursCoverageChips coverage={result.coverage} testId="ztrend-coverage" />
          <Paper variant="outlined" sx={{ p: 2 }}>
            {result.disciplineBridge.lines.length > 0 ? (
              <HoursBridgeChart
                lines={result.disciplineBridge.lines}
                onGroupClick={onSelectWorkDirection}
                testId="ztrend-hours-bridge"
              />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет пары план/факт с объёмом и часами для моста.
              </Typography>
            )}
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }} data-testid="ztrend-raw-delta">
              Сырой Δ часов: {formatRawDelta(result.rawHoursDelta)}
            </Typography>
          </Paper>

          {planWaterfall && planStart != null ? (
            <>
              <Typography variant="h6" component="h2">
                Изменение Z плана за период
              </Typography>
              <Paper variant="outlined" sx={{ p: 2 }}>
                <FactorWaterfallChart
                  startLabel="Было"
                  startValue={planStart}
                  endLabel="Стало"
                  steps={planWaterfall}
                  goodDirection="positive"
                  testId="ztrend-plan-waterfall"
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
          {result.groupZeroBars.length > 0 ? (
            <Grid container spacing={2} data-testid="ztrend-group-zero">
              {result.groupZeroBars.map((g) => (
                <Grid item xs={12} md={6} key={g.groupId}>
                  <Typography
                    variant="subtitle2"
                    sx={{ mb: 1, cursor: onSelectWorkDirection ? 'pointer' : 'default' }}
                    onClick={() => onSelectWorkDirection?.(g.groupId)}
                  >
                    {formatIisNorm({ name: g.groupName })}
                  </Typography>
                  <Paper
                    variant="outlined"
                    sx={{ p: 2, cursor: onSelectWorkDirection ? 'pointer' : 'default' }}
                    onClick={() => onSelectWorkDirection?.(g.groupId)}
                  >
                    <ZeroBaselineChart bars={g.bars} height={260} testId={`ztrend-group-zero-${g.groupId}`} />
                  </Paper>
                </Grid>
              ))}
            </Grid>
          ) : (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Нет вкладов проектов по группам IIC.
              </Typography>
            </Paper>
          )}

          <Typography variant="h6" component="h2">
            S-curve часов
          </Typography>
          <Paper variant="outlined" sx={{ p: 2 }}>
            {result.portfolioSCurve.length > 0 || result.planSCurve.length > 0 ? (
              <SCurveChart
                points={result.portfolioSCurve}
                planPoints={result.planSCurve}
                events={result.events}
                testId="ztrend-scurve"
              />
            ) : (
              <Typography variant="body2" color="text.secondary">
                Нет факт-версий с часами.
              </Typography>
            )}
          </Paper>

          <AnalyticsExtraAccordion testId="ztrend-extra">
            {result.projectSCurves.length > 0 ? (
              <>
                <Typography variant="h6" component="h2" sx={{ mb: 1 }}>
                  S-curve по проектам
                </Typography>
                <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
                  <SCurveChart
                    points={result.portfolioSCurve}
                    extraLines={result.projectSCurves.map((s) => ({ name: s.name, points: s.points }))}
                    testId="ztrend-scurve-projects"
                  />
                </Paper>
              </>
            ) : null}

            {deltaWaterfall && deltaStart != null ? (
              <>
                <Typography variant="h6" component="h2" sx={{ mb: 1 }}>
                  Изменение Z факта за период по группам
                </Typography>
                <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
                  <FactorWaterfallChart
                    startLabel="Было"
                    startValue={deltaStart}
                    endLabel="Стало"
                    steps={deltaWaterfall}
                    goodDirection="positive"
                    testId="ztrend-delta-waterfall"
                  />
                </Paper>
              </>
            ) : (
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Недостаточно истории факта для разбора ΔZ.
              </Typography>
            )}

            <Box>
              <Typography variant="h6" component="h2">
                Динамика по нормативным режимам
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, mb: 1.5 }}>
                Тот же тренд с разбивкой на сегменты действия норм IIC — видно, где смена нормы
                изменила оценку.
              </Typography>
            </Box>
            <NormLegend legend={result.legend} />
            <ZTrendChart
              group={result.overall}
              showPlan={showPlan}
              showFact={showFact}
              height={360}
              title="Средневзвешенный Z по проектам"
              testId="ztrend-overall-chart"
            />

            {result.byGroup.length > 0 && (
              <>
                <Typography variant="h6" component="h2" sx={{ mt: 1 }}>
                  По направлениям IIC
                </Typography>
                <Grid container spacing={2} data-testid="ztrend-groups-grid">
                  {result.byGroup.map((g) => (
                    <Grid item xs={12} md={6} lg={4} key={g.groupId}>
                      <Box
                        onClick={() => onSelectWorkDirection?.(g.groupId)}
                        sx={{ cursor: onSelectWorkDirection ? 'pointer' : 'default' }}
                      >
                        <ZTrendChart
                          group={{
                            ...g,
                            groupName: formatIisNorm({ name: g.groupName }),
                          }}
                          showPlan={showPlan}
                          showFact={showFact}
                          height={260}
                          testId={`ztrend-group-${g.groupId}`}
                        />
                      </Box>
                    </Grid>
                  ))}
                </Grid>
              </>
            )}
          </AnalyticsExtraAccordion>
        </>
      )}
    </Stack>
  );
}
