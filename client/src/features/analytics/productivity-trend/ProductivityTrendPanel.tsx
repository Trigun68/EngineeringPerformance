import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
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
import type { ColDef, ValueFormatterParams } from 'ag-grid-community';
import { DataTable } from '../../../components/DataTable/DataTable';
import {
  fetchProductivityTrend,
  type ProductivityTrendResponse,
  type ProductivityTrendTransition,
  type ProductivityVolumeMode,
} from '../../../api/productivityTrendApi';
import {
  fetchProductivityIndexTrend,
  type ProductivityIndexPoint,
  type ProductivityIndexTrendResponse,
} from '../../../api/productivityIndexTrendApi';
import { FACT_ACCENT_COLOR, PLAN_ACCENT_COLOR } from '../../../constants/planFactColors';
import type { FactPlanMode } from '../shared/FactPlanToggle';
import { TrendDetailView, type TrendDetailMetricConfig } from '../shared/TrendDetailView';
import type {
  GroupTrendLike,
  TrendPointLike,
  TrendVersionRow,
} from '../shared/trendDetailCalc';
import { AnalyticsExtraAccordion } from '../shared/AnalyticsExtraAccordion';
import { HoursBridgeChart } from '../shared/HoursBridgeChart';
import { HoursCoverageChips } from '../shared/HoursCoverageChips';
import { SCurveChart } from '../shared/SCurveChart';
import { fetchZTrends, type ZTrendResponse } from '../../../api/zTrendApi';
import { FormulaTypeLegend } from './FormulaTypeLegend';
import { ProductivityTrendChart } from './ProductivityTrendChart';

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
  // Индекс выше 100% — быстрее нормы, рост — улучшение.
  goodDirection: 'positive',
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

function toTrendPoint(p: ProductivityIndexPoint): TrendPointLike {
  return { date: p.date, value: p.index, weight: p.weightHours };
}

function toVersionRow(p: ProductivityIndexPoint): TrendVersionRow {
  return {
    projectCode: p.projectCode,
    projectName: p.projectName,
    versionName: p.versionName,
    date: p.date,
    value: p.index,
  };
}

const VERDICT_LABEL: Record<string, string> = {
  tightening: 'Ужесточение',
  loosening: 'Ослабление',
  ambiguous: 'Неоднозначно',
  insufficient: 'Недостаточно данных',
  stable: 'Без изменения',
};

const VERDICT_COLOR: Record<string, 'warning' | 'success' | 'default'> = {
  tightening: 'warning',
  loosening: 'success',
  ambiguous: 'default',
  insufficient: 'default',
  stable: 'default',
};

// Смены формулы, требующие внимания (ужесточение/ослабление нормы), идут первыми.
const VERDICT_PRIORITY: Record<string, number> = {
  tightening: 0,
  loosening: 1,
  ambiguous: 2,
  insufficient: 3,
  stable: 4,
};

function sortByAttention(transitions: ProductivityTrendTransition[]): ProductivityTrendTransition[] {
  return [...transitions].sort(
    (a, b) => (VERDICT_PRIORITY[a.verdict] ?? 99) - (VERDICT_PRIORITY[b.verdict] ?? 99),
  );
}

const FORMULA_LABEL: Record<string, string> = {
  Linear: 'Линейная',
  Logarithmic: 'Логарифмическая',
  ShiftedLog: 'Сдвинутая логарифмическая',
  Wright: 'Уайт',
  Power: 'Степенная',
  ExponentialDecay: 'Эксп. затухание',
  PiecewiseLinear: 'Кусочно-линейная',
  Polynomial: 'Полиномиальная',
  Constant: 'Постоянная',
  Hyperbolic: 'Гиперболическая',
  Exponential: 'Экспоненциальная',
};

function formatNum(value: number | null | undefined, digits = 2): string {
  if (value == null || Number.isNaN(value)) return '—';
  return value.toLocaleString('ru-RU', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });
}

function pair(oldValue: number | null, newValue: number | null): string {
  return `${formatNum(oldValue)} → ${formatNum(newValue)}`;
}

function formulaLabel(type: string): string {
  return FORMULA_LABEL[type] ?? type;
}

export function ProductivityTrendPanel({ dateFrom, dateTo, volumeMode }: Props) {
  const [result, setResult] = useState<ProductivityTrendResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Первичный блок (мокап TrendDetailMockup): индекс производительности по основным версиям.
  const [indexResult, setIndexResult] = useState<ProductivityIndexTrendResponse | null>(null);
  const [indexLoading, setIndexLoading] = useState(false);
  const [indexError, setIndexError] = useState<string | null>(null);
  const [mode, setMode] = useState<FactPlanMode>('fact');
  const indexAbortRef = useRef<AbortController | null>(null);
  const [hours, setHours] = useState<ZTrendResponse | null>(null);
  const hoursAbortRef = useRef<AbortController | null>(null);

  const load = useCallback(() => {
    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    setLoading(true);
    setError(null);

    void fetchProductivityTrend(
      {
        periodStart: dateFrom || null,
        periodEnd: dateTo || null,
        volumeMode,
      },
      ac.signal,
    )
      .then((data) => {
        if (ac.signal.aborted) return;
        setResult(data);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (ac.signal.aborted) return;
        const message = err instanceof Error ? err.message : 'Не удалось загрузить тренд производительности';
        setError(message);
        setLoading(false);
      });
  }, [dateFrom, dateTo, volumeMode]);

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => load(), 400);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      abortRef.current?.abort();
    };
  }, [load]);

  const loadIndex = useCallback(() => {
    indexAbortRef.current?.abort();
    const ac = new AbortController();
    indexAbortRef.current = ac;
    setIndexLoading(true);
    setIndexError(null);

    void fetchProductivityIndexTrend(
      {
        periodStart: dateFrom || null,
        periodEnd: dateTo || null,
      },
      ac.signal,
    )
      .then((data) => {
        if (ac.signal.aborted) return;
        setIndexResult(data);
        setIndexLoading(false);
      })
      .catch((err: unknown) => {
        if (ac.signal.aborted) return;
        const message =
          err instanceof Error ? err.message : 'Не удалось загрузить индекс производительности';
        setIndexError(message);
        setIndexLoading(false);
      });
  }, [dateFrom, dateTo]);

  useEffect(() => {
    const timer = setTimeout(() => loadIndex(), 400);
    return () => {
      clearTimeout(timer);
      indexAbortRef.current?.abort();
    };
  }, [loadIndex]);

  useEffect(() => {
    hoursAbortRef.current?.abort();
    const ac = new AbortController();
    hoursAbortRef.current = ac;
    void fetchZTrends(
      { periodStart: dateFrom || null, periodEnd: dateTo || null },
      ac.signal,
    )
      .then((data) => {
        if (!ac.signal.aborted) setHours(data);
      })
      .catch(() => {
        if (!ac.signal.aborted) setHours(null);
      });
    return () => ac.abort();
  }, [dateFrom, dateTo]);

  const indexPoints = useMemo(
    () => ({
      plan: (indexResult?.overall.plan ?? []).map(toTrendPoint),
      fact: (indexResult?.overall.fact ?? []).map(toTrendPoint),
    }),
    [indexResult],
  );

  const indexVersions = useMemo(
    () => ({
      plan: (indexResult?.overall.plan ?? []).map(toVersionRow),
      fact: (indexResult?.overall.fact ?? []).map(toVersionRow),
    }),
    [indexResult],
  );

  const indexWaterfallGroups = useMemo<GroupTrendLike[]>(
    () =>
      (indexResult?.byGroup ?? []).map((g) => ({
        groupId: g.groupId,
        // Имя группы уже отформатировано backend (код — наименование (ед.)) — не переформатируем.
        groupName: g.groupName,
        points: g.fact.map(toTrendPoint),
      })),
    [indexResult],
  );

  const hasIndexPoints = indexPoints.plan.length > 0 || indexPoints.fact.length > 0;

  const columnDefs = useMemo<ColDef<ProductivityTrendTransition>[]>(
    () => [
      { field: 'groupName', headerName: 'Группа IIC', flex: 1, minWidth: 220 },
      {
        field: 'effectiveFrom',
        headerName: 'Дата смены',
        width: 120,
        valueFormatter: (p: ValueFormatterParams<ProductivityTrendTransition>) => {
          const raw = p.value as string | undefined;
          if (!raw) return '—';
          const d = new Date(raw);
          return Number.isNaN(d.getTime()) ? raw : d.toLocaleDateString('ru-RU');
        },
      },
      {
        headerName: 'Формула',
        width: 220,
        valueGetter: (p) =>
          p.data
            ? `${formulaLabel(p.data.oldFormulaType)} → ${formulaLabel(p.data.newFormulaType)}`
            : '',
      },
      {
        headerName: 'Часы P10',
        width: 140,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldP10, p.data.hoursNewP10) : ''),
      },
      {
        headerName: 'Часы медиана',
        width: 150,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldMedian, p.data.hoursNewMedian) : ''),
      },
      {
        headerName: 'Часы P90',
        width: 140,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldP90, p.data.hoursNewP90) : ''),
      },
      {
        field: 'standardEffectHours',
        headerName: 'Эффект нормы',
        width: 130,
        valueFormatter: (p) => formatNum(p.value as number | null),
      },
      {
        field: 'mixEffectHours',
        headerName: 'Эффект объёма',
        width: 130,
        valueFormatter: (p) => formatNum(p.value as number | null),
      },
      {
        field: 'verdict',
        headerName: 'Вердикт',
        width: 150,
        cellRenderer: (p: { value?: string }) => {
          const v = String(p.value ?? '');
          return (
            <Chip
              size="small"
              color={VERDICT_COLOR[v] ?? 'default'}
              label={VERDICT_LABEL[v] ?? (v || '—')}
              aria-label={VERDICT_LABEL[v] ?? v}
            />
          );
        },
      },
      {
        field: 'curvesCross',
        headerName: 'Пересечение',
        width: 120,
        valueFormatter: (p) => (p.value ? 'Да' : 'Нет'),
      },
      {
        field: 'logVolumeLtOne',
        headerName: 'ln V<1',
        width: 100,
        valueFormatter: (p) => (p.value ? 'Да' : 'Нет'),
      },
    ],
    [],
  );

  const hasSeries = Boolean(
    result &&
      (result.overall.segments.some((s) => s.points.length > 0) || result.byGroup.length > 0),
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
    const other = total - tightening - loosening;
    return { total, tightening, loosening, other };
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
          onModeChange={setMode}
          config={PRODUCTIVITY_INDEX_METRIC}
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
              <Skeleton
                key={i}
                variant="rounded"
                sx={{ flex: '1 1 220px', minWidth: 200 }}
                height={80}
              />
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
          <Box sx={{ height: 360 }} data-testid="prod-trend-transitions">
            <DataTable
              rowData={sortedTransitions}
              columnDefs={columnDefs}
              objectName="GroupTrend"
              gridId="analytics-productivity-trend-transitions"
              getRowIdValue={(row) => `${row.groupId}-${row.oldNormId}-${row.newNormId}`}
              gridTestId="prod-trend-transitions-grid"
              height="100%"
            />
          </Box>
        </>
      )}
      </AnalyticsExtraAccordion>
    </Stack>
  );
}
