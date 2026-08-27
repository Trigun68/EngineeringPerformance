<<<<<<< HEAD
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  FormControl,
  InputLabel,
  LinearProgress,
  MenuItem,
  Paper,
  Select,
  Skeleton,
  Stack,
  TextField,
  Tooltip as MuiTooltip,
  Typography,
} from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import EventNoteOutlinedIcon from '@mui/icons-material/EventNoteOutlined';
import FunctionsOutlinedIcon from '@mui/icons-material/FunctionsOutlined';
import DateRangeOutlinedIcon from '@mui/icons-material/DateRangeOutlined';
import { useSearchParams } from 'react-router-dom';
import {
  fetchTrendDetail,
  fetchTrendFormulas,
  fetchTrends,
  type TrendAnalyticsResult,
  type TrendDetailRow,
} from '../../api/analyticsApi';
import { exportTrendsPdf } from '../../api/visualReportsApi';
import { fetchProjects } from '../../api/projectsApi';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import { getApiErrorMessage } from '../../services/apiError';
import type { Project } from '../../types/project';
import { wpColors } from '../../theme/wpColors';
import { TrendsKpiCards, type TrendsKpiData } from './TrendsKpiCards';
import { TrendsDetailTable } from './TrendsDetailTable';

const TRENDS_FILTER_KEY = 'analytics.trends.filters.v1';

const FACT_COLOR = wpColors.accent;
const FORECAST_COLOR = '#94A3B8';
const DELTA_POS_COLOR = '#27AE60';
const DELTA_NEG_COLOR = '#E74C3C';
const EVENT_LINE_COLOR = '#CBD5E1';

declare module 'highcharts' {
  interface Point {
    highlight?: (event: Highcharts.PointerEventObject) => void;
  }
}

interface TrendChartPoint {
  x: number;
  y: number | null;
  versionId: number;
  versionName: string;
}

function toChartPoints(series: TrendAnalyticsResult['series'] | undefined, metric: string): TrendChartPoint[] {
  const found = series?.find((s) => s.metric === metric);
  return (
    found?.points.map((p) => ({
      x: new Date(p.timestamp).getTime(),
      y: p.value,
      versionId: p.versionId,
      versionName: p.versionName,
    })) ?? []
  );
}

/** Классическая связка синхронизации кроссхейра/тултипа между несколькими независимыми Highcharts-графиками. */
let highlightPatched = false;
function ensureHighlightPrototype() {
  if (highlightPatched) return;
  highlightPatched = true;
  Highcharts.Point.prototype.highlight = function highlight(event: Highcharts.PointerEventObject) {
    this.onMouseOver();
    this.series.chart.tooltip?.refresh(this);
    this.series.chart.xAxis[0]?.drawCrosshair(event, this);
  };
}

/**
 * Архивная страница «Трендовый анализ» (Highcharts факт/прогноз, дельта, S-curve, KPI, детализация, PDF).
 * Встраивается как содержимое одной из вкладок AnalyticsPage — самодостаточна, со своим набором фильтров
 * (проект/формула/период), независимым от общих фильтров периода на странице «Аналитика».
 */
export function TrendsAnalyticsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [projects, setProjects] = useState<Project[]>([]);
  const [projectId, setProjectId] = useState<number | null>(null);
  const [formulaId, setFormulaId] = useState('v1-docset-proportional');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [formulas, setFormulas] = useState<{ id: string; displayName: string; description: string }[]>([]);
  const [data, setData] = useState<TrendAnalyticsResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [exportingPdf, setExportingPdf] = useState(false);

  const [selectedVersion, setSelectedVersion] = useState<{ id: number; name: string } | null>(null);
  const [detailRows, setDetailRows] = useState<TrendDetailRow[]>([]);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  const mainChartRef = useRef<HighchartsReact.RefObject>(null);
  const deltaChartRef = useRef<HighchartsReact.RefObject>(null);
  const cumulativeChartRef = useRef<HighchartsReact.RefObject>(null);

  useEffect(() => {
    void fetchProjects().then((list) => {
      setProjects(list);
      if (!projectId && list.length > 0) {
        setProjectId(list[0].id);
      }
    }).catch(() => setProjects([]));
    void fetchTrendFormulas()
      .then((list) => setFormulas(list))
      .catch(() => setFormulas([]));
    void getUserPreferences()
      .then((prefs) => {
        const raw = prefs[TRENDS_FILTER_KEY];
        if (!raw) return;
        try {
          const saved = JSON.parse(raw) as { projectId?: number; formulaId?: string; dateFrom?: string; dateTo?: string };
          if (saved.projectId) setProjectId(saved.projectId);
          if (saved.formulaId) setFormulaId(saved.formulaId);
          if (saved.dateFrom) setDateFrom(saved.dateFrom);
          if (saved.dateTo) setDateTo(saved.dateTo);
        } catch {
          // ignore invalid prefs
        }
      })
      .catch(() => undefined);
  }, []);

  useEffect(() => {
    const fromQuery = Number(searchParams.get('projectId'));
    if (Number.isFinite(fromQuery) && fromQuery > 0) {
      setProjectId(fromQuery);
    }
  }, [searchParams]);

  const loadTrends = useCallback(async () => {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    setSelectedVersion(null);
    setDetailRows([]);
    try {
      const result = await fetchTrends({
        projectId,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        granularity: 'version',
        formulaId,
      });
      setData(result);
      void putUserPreferences({
        [TRENDS_FILTER_KEY]: JSON.stringify({ projectId, formulaId, dateFrom, dateTo }),
      });
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить тренды.'));
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [dateFrom, dateTo, formulaId, projectId]);

  useEffect(() => {
    if (projectId) void loadTrends();
  }, [loadTrends, projectId]);

  const handlePointSelect = useCallback(
    (versionId: number, versionName: string) => {
      if (!projectId) return;
      setSelectedVersion({ id: versionId, name: versionName });
      setDetailLoading(true);
      setDetailError(null);
      void fetchTrendDetail(projectId, versionId, formulaId)
        .then(setDetailRows)
        .catch(() => setDetailError('Не удалось загрузить детализацию по версии.'))
        .finally(() => setDetailLoading(false));
    },
    [formulaId, projectId],
  );

  const selectedFormula = useMemo(
    () => formulas.find((f) => f.id === formulaId) ?? null,
    [formulas, formulaId],
  );

  const factPoints = useMemo(() => toChartPoints(data?.series, 'factLaborHours'), [data]);
  const forecastPoints = useMemo(() => toChartPoints(data?.series, 'forecastLaborHours'), [data]);
  const deltaPoints = useMemo(() => toChartPoints(data?.series, 'hoursDelta'), [data]);
  const cumulativePoints = useMemo(() => toChartPoints(data?.series, 'cumulativeFactHours'), [data]);

  const kpi: TrendsKpiData = useMemo(() => {
    const lastFact = [...factPoints].reverse().find((p) => p.y != null) ?? null;
    const matchedForecast = lastFact
      ? forecastPoints.find((p) => p.versionId === lastFact.versionId) ?? null
      : null;
    const deltaAbs =
      lastFact?.y != null && matchedForecast?.y != null ? lastFact.y - matchedForecast.y : null;
    const deltaPct =
      deltaAbs != null && matchedForecast?.y ? (deltaAbs / matchedForecast.y) * 100 : null;

    const validDeltas = deltaPoints.filter((p) => p.y != null);
    const cumulativeDeltaAbs = validDeltas.length
      ? validDeltas.reduce((sum, p) => sum + (p.y ?? 0), 0)
      : null;
    const forecastTotal = forecastPoints.reduce((sum, p) => sum + (p.y ?? 0), 0);
    const cumulativeDeltaPct =
      cumulativeDeltaAbs != null && forecastTotal ? (cumulativeDeltaAbs / forecastTotal) * 100 : null;

    return {
      lastFactValue: lastFact?.y ?? null,
      lastFactVersionName: lastFact?.versionName ?? null,
      lastFactTimestamp: lastFact ? new Date(lastFact.x).toISOString() : null,
      matchedForecastValue: matchedForecast?.y ?? null,
      deltaAbs,
      deltaPct,
      cumulativeDeltaAbs,
      cumulativeDeltaPct,
      versionsCount: factPoints.filter((p) => p.y != null).length,
    };
  }, [factPoints, forecastPoints, deltaPoints]);

  const summaryInfo = useMemo(() => {
    const timestamps = factPoints.filter((p) => p.y != null).map((p) => p.x);
    if (!timestamps.length) return null;
    const min = new Date(Math.min(...timestamps));
    const max = new Date(Math.max(...timestamps));
    const fmt = (d: Date) => d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
    return {
      rangeLabel: min.getTime() === max.getTime() ? fmt(min) : `${fmt(min)} — ${fmt(max)}`,
      eventsCount: data?.events?.length ?? 0,
    };
  }, [factPoints, data]);

  const eventPlotLines = useMemo<Highcharts.XAxisPlotLinesOptions[]>(() => {
    const events = data?.events ?? [];
    return events.map((ev) => ({
      value: new Date(ev.timestamp).getTime(),
      color: EVENT_LINE_COLOR,
      dashStyle: 'Dash',
      width: 1,
      zIndex: 3,
      label: {
        text: ev.label,
        rotation: 90,
        align: 'left',
        y: 10,
        x: 4,
        style: { color: '#94A3B8', fontSize: '10px' },
      },
    }));
  }, [data]);

  const mainChartOptions = useMemo<Highcharts.Options>(
    () => ({
      chart: { height: 380, style: { fontFamily: 'inherit' } },
      title: { text: undefined },
      credits: { enabled: false },
      xAxis: { type: 'datetime', plotLines: eventPlotLines, crosshair: true },
      yAxis: { title: { text: 'Часы' }, gridLineColor: '#F1F5F9' },
      legend: { enabled: true },
      tooltip: {
        shared: true,
        xDateFormat: '%d.%m.%Y',
        valueDecimals: 0,
        valueSuffix: ' ч',
      },
      plotOptions: {
        series: {
          cursor: 'pointer',
          point: {
            events: {
              click() {
                const p = this as unknown as TrendChartPoint;
                handlePointSelect(p.versionId, p.versionName);
              },
            },
          },
        },
      },
      series: [
        {
          type: 'line',
          name: 'Факт',
          color: FACT_COLOR,
          data: factPoints as unknown as Highcharts.SeriesLineOptions['data'],
          marker: { enabled: true, radius: 4 },
        },
        {
          type: 'line',
          name: 'Прогноз / План',
          color: FORECAST_COLOR,
          dashStyle: 'ShortDash',
          data: forecastPoints as unknown as Highcharts.SeriesLineOptions['data'],
          marker: { enabled: true, radius: 3, symbol: 'diamond' },
        },
      ],
    }),
    [factPoints, forecastPoints, eventPlotLines, handlePointSelect],
  );

  const deltaChartOptions = useMemo<Highcharts.Options>(
    () => ({
      chart: { height: 220, style: { fontFamily: 'inherit' } },
      title: { text: 'Δ Факт − Прогноз', style: { fontSize: '13px', color: '#475569' } },
      credits: { enabled: false },
      xAxis: { type: 'datetime', plotLines: eventPlotLines, crosshair: true },
      yAxis: { title: { text: 'Δ, ч' }, gridLineColor: '#F1F5F9' },
      legend: { enabled: false },
      tooltip: { xDateFormat: '%d.%m.%Y', valueDecimals: 0, valueSuffix: ' ч' },
      plotOptions: {
        series: {
          cursor: 'pointer',
          point: {
            events: {
              click() {
                const p = this as unknown as TrendChartPoint;
                handlePointSelect(p.versionId, p.versionName);
              },
            },
          },
        },
      },
      series: [
        {
          type: 'column',
          name: 'Δ',
          data: deltaPoints.map((p) => ({
            ...p,
            color: (p.y ?? 0) >= 0 ? DELTA_POS_COLOR : DELTA_NEG_COLOR,
          })) as unknown as Highcharts.SeriesColumnOptions['data'],
        },
      ],
    }),
    [deltaPoints, eventPlotLines, handlePointSelect],
  );

  const cumulativeChartOptions = useMemo<Highcharts.Options>(
    () => ({
      chart: { height: 220, style: { fontFamily: 'inherit' } },
      title: { text: 'S-curve (накопительный факт)', style: { fontSize: '13px', color: '#475569' } },
      credits: { enabled: false },
      xAxis: { type: 'datetime', plotLines: eventPlotLines, crosshair: true },
      yAxis: { title: { text: 'Накопительно, ч' }, gridLineColor: '#F1F5F9' },
      legend: { enabled: false },
      tooltip: { xDateFormat: '%d.%m.%Y', valueDecimals: 0, valueSuffix: ' ч' },
      plotOptions: {
        series: {
          cursor: 'pointer',
          point: {
            events: {
              click() {
                const p = this as unknown as TrendChartPoint;
                handlePointSelect(p.versionId, p.versionName);
              },
            },
          },
        },
      },
      series: [
        {
          type: 'area',
          name: 'Накопительный факт',
          color: FACT_COLOR,
          fillOpacity: 0.15,
          data: cumulativePoints as unknown as Highcharts.SeriesAreaOptions['data'],
        },
      ],
    }),
    [cumulativePoints, eventPlotLines, handlePointSelect],
  );

  // Синхронизация hover/crosshair между тремя независимыми графиками.
  useEffect(() => {
    ensureHighlightPrototype();
    const refs = [mainChartRef, deltaChartRef, cumulativeChartRef];
    const charts = refs.map((r) => r.current?.chart).filter(Boolean) as Highcharts.Chart[];
    if (charts.length < 2) return;

    const listeners: { el: Element; type: string; fn: EventListener }[] = [];

    charts.forEach((chart) => {
      ['mousemove', 'touchmove', 'touchstart'].forEach((eventType) => {
        const fn: EventListener = (e) => {
          const event = chart.pointer?.normalize(e as MouseEvent);
          if (!event) return;
          charts.forEach((c) => {
            const point = c.series[0]?.searchPoint(event, true);
            point?.highlight?.(event as unknown as Highcharts.PointerEventObject);
          });
        };
        chart.container.addEventListener(eventType, fn);
        listeners.push({ el: chart.container, type: eventType, fn });
      });
    });

    return () => {
      listeners.forEach(({ el, type, fn }) => el.removeEventListener(type, fn));
    };
  }, [mainChartOptions, deltaChartOptions, cumulativeChartOptions]);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }} data-testid="trends-legacy-panel">
      <Stack direction="row" spacing={1.5} alignItems="center" flexWrap="wrap">
        <Typography variant="h6" component="h2">Трендовый анализ (архив)</Typography>
        {selectedFormula && (
          <MuiTooltip title={selectedFormula.description}>
            <Chip
              size="small"
              icon={<FunctionsOutlinedIcon sx={{ fontSize: 15 }} />}
              label={selectedFormula.displayName}
              sx={{ bgcolor: '#F1F5F9', color: '#475569', fontWeight: 500, cursor: 'default' }}
            />
          </MuiTooltip>
        )}
        {summaryInfo && (
          <Chip
            size="small"
            icon={<DateRangeOutlinedIcon sx={{ fontSize: 15 }} />}
            label={summaryInfo.rangeLabel}
            sx={{ bgcolor: '#F1F5F9', color: '#475569', fontWeight: 500, cursor: 'default' }}
          />
        )}
        {summaryInfo && summaryInfo.eventsCount > 0 && (
          <MuiTooltip title="Количество отмеченных на графике вех версий">
            <Chip
              size="small"
              icon={<EventNoteOutlinedIcon sx={{ fontSize: 15 }} />}
              label={`${summaryInfo.eventsCount} ${summaryInfo.eventsCount === 1 ? 'веха' : 'вехи'}`}
              sx={{ bgcolor: '#F1F5F9', color: '#475569', fontWeight: 500, cursor: 'default' }}
            />
          </MuiTooltip>
        )}
      </Stack>

      <Typography variant="body2" color="text.secondary">
        Классический анализ факт vs прогноз/план по версиям с детализацией по объектам. Архивная страница,
        сохранена для сравнения с новым Z-оценочным анализом на соседней вкладке.
      </Typography>

      <Stack direction="row" spacing={2} flexWrap="wrap" alignItems="center">
        <FormControl size="small" sx={{ minWidth: 220 }}>
          <InputLabel>Проект</InputLabel>
          <Select
            label="Проект"
            value={projectId ?? ''}
            onChange={(e) => {
              const id = Number(e.target.value);
              setProjectId(Number.isFinite(id) ? id : null);
              setSearchParams(id ? { projectId: String(id) } : {});
            }}
          >
            {projects.map((p) => (
              <MenuItem key={p.id} value={p.id}>{p.code} — {p.name}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 260 }}>
          <InputLabel>Формула оценки</InputLabel>
          <Select label="Формула оценки" value={formulaId} onChange={(e) => setFormulaId(e.target.value)}>
            {formulas.map((f) => (
              <MenuItem key={f.id} value={f.id}>{f.displayName}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <TextField size="small" label="Дата с" type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} InputLabelProps={{ shrink: true }} />
        <TextField size="small" label="Дата по" type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} InputLabelProps={{ shrink: true }} />
        <Button variant="contained" disabled={!projectId || loading} onClick={() => void loadTrends()}>
          Применить
        </Button>
        <Button
          variant="outlined"
          disabled={!projectId || exportingPdf}
          startIcon={<PictureAsPdfIcon />}
          onClick={async () => {
            if (!projectId) return;
            setExportingPdf(true);
            try {
              await exportTrendsPdf(projectId, { dateFrom: dateFrom || undefined, dateTo: dateTo || undefined, formulaId });
            } catch {
              setError('Не удалось экспортировать PDF.');
            } finally {
              setExportingPdf(false);
            }
          }}
        >
          Экспорт PDF
        </Button>
      </Stack>

      {error && <Alert severity="error">{error}</Alert>}
      {!projectId && <Alert severity="info">Выберите проект для анализа трендов.</Alert>}

      {projectId && loading && !data && (
        <>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr 1fr', sm: 'repeat(5, 1fr)' }, gap: 1.5 }}>
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} variant="rounded" height={84} sx={{ borderRadius: 2.5 }} />
            ))}
          </Box>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '1.6fr 1fr' }, gap: 2 }}>
            <Skeleton variant="rounded" height={412} sx={{ borderRadius: 2.5 }} />
            <Stack spacing={2}>
              <Skeleton variant="rounded" height={198} sx={{ borderRadius: 2.5 }} />
              <Skeleton variant="rounded" height={198} sx={{ borderRadius: 2.5 }} />
            </Stack>
          </Box>
        </>
      )}

      {projectId && data && factPoints.every((p) => p.y == null) && !loading && (
        <Alert severity="info">Нет данных факта за выбранный период — измените диапазон дат или проект.</Alert>
      )}

      {projectId && data && factPoints.some((p) => p.y != null) && (
        <>
          {loading && <LinearProgress sx={{ borderRadius: 1, height: 3 }} />}
          <TrendsKpiCards kpi={kpi} />

          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '1.6fr 1fr' }, gap: 2 }}>
            <Paper variant="outlined" sx={{ p: 1.5, borderColor: '#E2E8F0', borderRadius: 2.5 }}>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                <Typography sx={{ fontSize: 13, fontWeight: 600, color: '#475569' }}>
                  Факт vs Прогноз/План — вехи версий отмечены пунктиром
                </Typography>
                {selectedVersion && (
                  <MuiTooltip title={`ID версии: ${selectedVersion.id}`}>
                    <Typography
                      sx={{
                        fontSize: 12,
                        fontWeight: 600,
                        color: wpColors.accentDark,
                        bgcolor: wpColors.accentHover,
                        px: 1,
                        py: 0.25,
                        borderRadius: 1,
                      }}
                    >
                      Выбрана: {selectedVersion.name}
                    </Typography>
                  </MuiTooltip>
                )}
              </Stack>
              <HighchartsReact highcharts={Highcharts} options={mainChartOptions} ref={mainChartRef} />
            </Paper>

            <Stack spacing={2}>
              <Paper variant="outlined" sx={{ p: 1.5, borderColor: '#E2E8F0', borderRadius: 2.5 }}>
                <HighchartsReact highcharts={Highcharts} options={deltaChartOptions} ref={deltaChartRef} />
              </Paper>
              <Paper variant="outlined" sx={{ p: 1.5, borderColor: '#E2E8F0', borderRadius: 2.5 }}>
                <HighchartsReact highcharts={Highcharts} options={cumulativeChartOptions} ref={cumulativeChartRef} />
              </Paper>
            </Stack>
          </Box>

          <Paper variant="outlined" sx={{ p: 2, borderColor: '#E2E8F0', borderRadius: 2.5 }}>
            <TrendsDetailTable
              versionName={selectedVersion?.name ?? null}
              rows={detailRows}
              loading={detailLoading}
              error={detailError}
            />
          </Paper>
        </>
      )}
    </Box>
  );
}
=======
/** @deprecated Используйте AnalyticsPage — реэкспорт для совместимости импортов. */
export { AnalyticsPage as TrendsAnalyticsPage } from './AnalyticsPage';
export { AnalyticsPage } from './AnalyticsPage';
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
