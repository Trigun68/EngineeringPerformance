import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
  Divider,
  Grid,
  LinearProgress,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material';
import FunctionsOutlinedIcon from '@mui/icons-material/FunctionsOutlined';
import {
  fetchZTrends,
  type ZTrendResponse,
  type ZTrendSeries,
  type ZTrendWeightMode,
} from '../../../api/zTrendApi';
import { FACT_ACCENT_COLOR, PLAN_ACCENT_COLOR } from '../../../constants/planFactColors';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import type { FactPlanMode } from '../shared/FactPlanToggle';
import { TrendDetailView, type TrendDetailMetricConfig } from '../shared/TrendDetailView';
import type {
  GroupTrendLike,
  TrendPointLike,
  TrendVersionRow,
} from '../shared/trendDetailCalc';
import { NormLegend } from './NormLegend';
import { ZTrendChart } from './ZTrendChart';

const WEIGHT_MODE_LABEL: Record<ZTrendWeightMode, string> = {
  basisHours: 'Нормативные часы (basisHours)',
};

const Z_TREND_METRIC: TrendDetailMetricConfig = {
  yAxisTitle: 'Средневзвешенная Z-оценка',
  unit: '',
  digits: 2,
  zeroLine: true,
  // Для Z-оценки снижение (ближе к норме и ниже) — улучшение.
  goodDirection: 'negative',
  factColor: FACT_ACCENT_COLOR,
  planColor: PLAN_ACCENT_COLOR,
};

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

interface Props {
  dateFrom: string;
  dateTo: string;
  weightMode: ZTrendWeightMode;
  mode: FactPlanMode;
  onModeChange: (mode: FactPlanMode) => void;
}

/** Плоский список точек серии (сегменты нормативных режимов схлопываются в хронологию). */
function flattenSeriesPoints(series: ZTrendSeries | undefined): TrendPointLike[] {
  if (!series) return [];
  const points: TrendPointLike[] = [];
  for (const seg of series.segments) {
    for (const p of seg.points) {
      points.push({ date: p.date, value: p.zScore, weight: p.weight });
    }
  }
  return points.sort((a, b) => (a.date < b.date ? -1 : 1));
}

/** Версии выборки из contributions точек серии; свежие сверху. */
function collectVersionRows(series: ZTrendSeries | undefined): TrendVersionRow[] {
  if (!series) return [];
  const map = new Map<string, TrendVersionRow>();
  for (const seg of series.segments) {
    for (const p of seg.points) {
      for (const c of p.contributions ?? []) {
        const key = `${c.projectId}-${c.versionId}`;
        if (!map.has(key)) {
          map.set(key, {
            projectCode: c.projectCode,
            projectName: c.projectName,
            versionName: c.versionName,
            date: p.date,
            value: c.zScore,
          });
        }
      }
    }
  }
  return [...map.values()].sort((a, b) => (a.date < b.date ? 1 : -1));
}

export function TrendAnalysisPanel({ dateFrom, dateTo, weightMode, mode, onModeChange }: Props) {
  const [result, setResult] = useState<ZTrendResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const load = useCallback(() => {
    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    setLoading(true);
    setError(null);

    void fetchZTrends(
      {
        periodStart: dateFrom || null,
        periodEnd: dateTo || null,
        weightMode,
      },
      ac.signal,
    )
      .then((data) => {
        if (!ac.signal.aborted) {
          setResult(data);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (ac.signal.aborted) return;
        const message = err instanceof Error ? err.message : 'Не удалось загрузить тренды';
        setError(message);
        setLoading(false);
      });
  }, [dateFrom, dateTo, weightMode]);

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => load(), 400);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      abortRef.current?.abort();
    };
  }, [load]);

  const hasSegments = useMemo(() => {
    if (!result) return false;
    const o = result.overall;
    return o.plan.segments.length > 0 || o.fact.segments.length > 0 || result.byGroup.length > 0;
  }, [result]);

  const planPoints = useMemo(() => flattenSeriesPoints(result?.overall.plan), [result]);
  const factPoints = useMemo(() => flattenSeriesPoints(result?.overall.fact), [result]);
  const planVersions = useMemo(() => collectVersionRows(result?.overall.plan), [result]);
  const factVersions = useMemo(() => collectVersionRows(result?.overall.fact), [result]);

  const waterfallGroups = useMemo<GroupTrendLike[]>(() => {
    if (!result) return [];
    return result.byGroup.map((g) => ({
      groupId: g.groupId,
      groupName: formatIisNorm({ name: g.groupName }),
      points: flattenSeriesPoints(g.fact),
    }));
  }, [result]);

  // Вторичные графики нормативных режимов следуют режиму переключателя.
  const showPlan = mode !== 'fact';
  const showFact = mode !== 'plan';

  return (
    <Stack spacing={2.5} data-testid="ztrend-panel">
      {loading && !result && (
        <Box data-testid="ztrend-loading" sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
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

      {result && !hasSegments && (
        <Alert severity="info" data-testid="ztrend-empty">
          Нет основных версий (isPrimary) с рассчитанным Z в выбранном периоде.
        </Alert>
      )}

      {result && hasSegments && (
        <>
          <TrendDetailView
            mode={mode}
            onModeChange={onModeChange}
            config={Z_TREND_METRIC}
            periodLabel={formatPeriodChip(dateFrom, dateTo)}
            planPoints={planPoints}
            factPoints={factPoints}
            planVersions={planVersions}
            factVersions={factVersions}
            waterfallGroups={waterfallGroups}
            headerExtras={
              <Chip
                size="small"
                icon={<FunctionsOutlinedIcon sx={{ fontSize: 15 }} />}
                label={`Вес: ${WEIGHT_MODE_LABEL[weightMode] ?? weightMode}`}
                data-testid="ztrend-weight-chip"
                sx={{ bgcolor: '#F1F5F9', color: '#475569', fontWeight: 500, cursor: 'default' }}
              />
            }
            testId="ztrend-detail"
          />

          <Divider sx={{ my: 1 }} />

          <Box>
            <Typography variant="h6" component="h2">
              Динамика по нормативным режимам
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
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
                  </Grid>
                ))}
              </Grid>
            </>
          )}
        </>
      )}
    </Stack>
  );
}
