import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Card,
  CardContent,
  CircularProgress,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Typography,
} from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { format, parseISO } from 'date-fns';
import { fetchResourceMetrics, type MetricsTimeSeriesPoint, type ResourceMetrics } from '../../api/monitoringApi';
import { getApiErrorMessage } from '../../services/apiError';
import { wpColors } from '../../theme/wpColors';
import {
  DEFAULT_RESOURCE_PERIOD_MINUTES,
  RESOURCE_PERIOD_PRESETS,
  resourceRefreshMs,
} from './resourcePeriod';
const BACKEND_COLOR = wpColors.accent;
const REDIS_COLOR = '#E67E22';
const SQL_COLOR = '#27AE60';

function formatSeries(points: MetricsTimeSeriesPoint[] | undefined): [number, number][] {
  return (points ?? []).map((point) => [Date.parse(point.timestampUtc), point.value]);
}

function formatCores(value: number): string {
  return `${value.toFixed(2)} ядра`;
}

function formatMib(value: number): string {
  return `${value.toFixed(0)} МиБ`;
}

function formatOptional(value: number | null, suffix: string): string {
  return value == null ? '—' : `${value.toFixed(2)} ${suffix}`;
}

function KpiCard({
  label,
  value,
  testId,
}: {
  label: string;
  value: string;
  testId: string;
}) {
  return (
    <Grid item xs={12} sm={6} md={3} lg={2}>
      <Card variant="outlined" data-testid={testId}>
        <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
          <Typography variant="body2" color="text.secondary">{label}</Typography>
          <Typography variant="h6" sx={{ fontWeight: 600 }}>{value}</Typography>
        </CardContent>
      </Card>
    </Grid>
  );
}

export function ResourceDashboard() {
  const [windowMinutes, setWindowMinutes] = useState(DEFAULT_RESOURCE_PERIOD_MINUTES);
  const [metrics, setMetrics] = useState<ResourceMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const load = useCallback(async () => {
    try {
      setMetrics(await fetchResourceMetrics(windowMinutes));
      setError(null);
      setLastUpdated(new Date());
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить метрики ресурсов.'));
    } finally {
      setLoading(false);
    }
  }, [windowMinutes]);

  useEffect(() => {
    setLoading(true);
    void load();
    const timer = window.setInterval(() => void load(), resourceRefreshMs(windowMinutes));
    return () => window.clearInterval(timer);
  }, [load, windowMinutes]);

  const cpuOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'area', height: 320 },
    title: { text: 'CPU стека (доли ядра)' },
    xAxis: { type: 'datetime' },
    yAxis: { title: { text: 'Ядра' }, min: 0 },
    plotOptions: { area: { stacking: 'normal', marker: { enabled: false } } },
    series: [
      { type: 'area', name: 'Бэкенд', color: BACKEND_COLOR, data: formatSeries(metrics?.series.backendCpuCores) },
      { type: 'area', name: 'Redis', color: REDIS_COLOR, data: formatSeries(metrics?.series.redisCpuCores) },
      { type: 'area', name: 'SQL Server', color: SQL_COLOR, data: formatSeries(metrics?.series.sqlCpuCores) },
    ],
    credits: { enabled: false },
  }), [metrics?.series]);

  const ramOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'area', height: 320 },
    title: { text: 'Оперативная память (МиБ)' },
    xAxis: { type: 'datetime' },
    yAxis: {
      title: { text: 'МиБ' },
      min: 0,
      plotLines: metrics
        ? [{
            value: metrics.series.sqlMemoryLimitMib,
            color: '#7F8C8D',
            dashStyle: 'Dash',
            width: 1,
            label: { text: `Лимит SQL ${metrics.series.sqlMemoryLimitMib} МиБ` },
          }]
        : [],
    },
    plotOptions: { area: { stacking: 'normal', marker: { enabled: false } } },
    series: [
      { type: 'area', name: 'Бэкенд', color: BACKEND_COLOR, data: formatSeries(metrics?.series.backendRssMib) },
      { type: 'area', name: 'Redis', color: REDIS_COLOR, data: formatSeries(metrics?.series.redisRssMib) },
      { type: 'area', name: 'SQL Server', color: SQL_COLOR, data: formatSeries(metrics?.series.sqlRssMib) },
    ],
    credits: { enabled: false },
  }), [metrics]);

  const connectionsOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'spline', height: 320 },
    title: { text: 'Пользователи и коннекты' },
    xAxis: { type: 'datetime' },
    yAxis: { title: { text: 'Число' }, min: 0 },
    series: [
      { type: 'spline', name: 'Активные пользователи (5 мин)', color: wpColors.heading, data: formatSeries(metrics?.series.activeUsers) },
      { type: 'spline', name: 'HTTP (Kestrel)', color: BACKEND_COLOR, data: formatSeries(metrics?.series.kestrelConnections) },
      { type: 'spline', name: 'SQL Interactive', color: SQL_COLOR, data: formatSeries(metrics?.series.sqlInteractive) },
      { type: 'spline', name: 'SQL Analytics', color: '#1E8449', dashStyle: 'ShortDash', data: formatSeries(metrics?.series.sqlAnalytics) },
      { type: 'spline', name: 'Redis clients', color: REDIS_COLOR, data: formatSeries(metrics?.series.redisClients) },
    ],
    credits: { enabled: false },
  }), [metrics?.series]);

  const correlationOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'spline', height: 320 },
    title: { text: 'Пользователи vs CPU бэкенда' },
    xAxis: { type: 'datetime' },
    yAxis: [
      { title: { text: 'Пользователи' }, min: 0 },
      { title: { text: 'Ядра CPU' }, min: 0, opposite: true },
    ],
    series: [
      { type: 'spline', name: 'Активные пользователи', yAxis: 0, color: wpColors.heading, data: formatSeries(metrics?.series.activeUsers) },
      { type: 'spline', name: 'CPU бэкенда', yAxis: 1, color: BACKEND_COLOR, data: formatSeries(metrics?.series.backendCpuCores) },
    ],
    credits: { enabled: false },
  }), [metrics?.series]);

  const saturationOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'spline', height: 280 },
    title: { text: 'Насыщение' },
    xAxis: { type: 'datetime' },
    yAxis: { title: { text: '' }, min: 0 },
    series: [
      { type: 'spline', name: 'Очередь ThreadPool', data: formatSeries(metrics?.series.threadPoolQueue) },
      { type: 'spline', name: 'SQL runnable', data: formatSeries(metrics?.series.sqlRunnable) },
      { type: 'spline', name: 'Redis hit ratio', data: formatSeries(metrics?.series.redisHitRatio) },
      { type: 'spline', name: 'GC alloc МиБ/с', data: formatSeries(metrics?.series.allocRateMibPerSec) },
      { type: 'spline', name: 'Gen2 (накопительно)', data: formatSeries(metrics?.series.gen2Collections) },
    ],
    credits: { enabled: false },
  }), [metrics?.series]);

  if (loading && !metrics) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  const snap = metrics?.snapshot;
  const historyFromIso = metrics?.historyAvailableFromUtc;
  const historyFromMs = historyFromIso ? Date.parse(historyFromIso) : Number.NaN;
  const windowStartMs = Date.now() - windowMinutes * 60_000;
  const historyIsShorter = Number.isFinite(historyFromMs) && historyFromMs > windowStartMs + 60_000;
  const refreshSeconds = resourceRefreshMs(windowMinutes) / 1000;

  return (
    <Box data-testid="resource-dashboard" sx={{ display: 'flex', flexDirection: 'column', gap: 2, overflow: 'auto' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
        <Typography variant="h6" sx={{ color: wpColors.heading, fontWeight: 600 }}>
          Ресурсы сервера
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
          <FormControl size="small" sx={{ minWidth: 160 }} data-testid="resource-period-select">
            <InputLabel id="resource-period-label">Отчёт за</InputLabel>
            <Select
              labelId="resource-period-label"
              label="Отчёт за"
              value={String(windowMinutes)}
              onChange={(event) => setWindowMinutes(Number(event.target.value))}
            >
              {RESOURCE_PERIOD_PRESETS.map((preset) => (
                <MenuItem
                  key={preset.minutes}
                  value={String(preset.minutes)}
                  data-testid={`resource-period-${preset.minutes}`}
                >
                  {preset.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {lastUpdated && (
            <Typography variant="caption" color="text.secondary">
              Обновлено: {format(lastUpdated, 'HH:mm:ss')} · автообновление каждые {refreshSeconds} с
            </Typography>
          )}
        </Box>
      </Box>

      {error && <Alert severity="error">{error}</Alert>}

      {metrics && snap && (
        <>
          {historyIsShorter && Number.isFinite(historyFromMs) && (
            <Alert severity="info" data-testid="resource-history-from">
              Данные с {format(parseISO(historyFromIso!), 'dd.MM.yyyy HH:mm')} — история ещё копится до выбранного окна.
            </Alert>
          )}
          {metrics.verdicts.map((line) => (
            <Alert key={line} severity="info" data-testid="resource-verdict">{line}</Alert>
          ))}

          <Grid container spacing={1.5}>
            <KpiCard testId="resource-kpi-users" label="Активные пользователи (5 мин)" value={String(snap.activeUsers)} />
            <KpiCard testId="resource-kpi-http" label="HTTP-коннекты" value={String(snap.kestrelConnections)} />
            <KpiCard
              testId="resource-kpi-sql"
              label="SQL Interactive / Analytics"
              value={`${snap.sqlConnections.interactive} / ${snap.sqlConnections.analytics}`}
            />
            <KpiCard testId="resource-kpi-redis" label="Redis clients" value={String(snap.redisClients)} />
            <KpiCard testId="resource-kpi-backend-ram" label="RAM бэкенда" value={formatMib(snap.backend.rssMib)} />
            <KpiCard testId="resource-kpi-backend-cpu" label="CPU бэкенда" value={formatCores(snap.backend.cpuCores)} />
            <KpiCard testId="resource-kpi-cpu-per-user" label="Ядра / пользователь" value={formatOptional(snap.cpuCoresPerUser, 'ядра')} />
            <KpiCard testId="resource-kpi-ram-per-user" label="МиБ / пользователь" value={formatOptional(snap.rssMibPerUser, 'МиБ')} />
            <KpiCard
              testId="resource-kpi-gate"
              label="AnalyticsSqlGate"
              value={`${snap.analyticsGateBusy} / ${snap.analyticsGateLimit}`}
            />
            <KpiCard
              testId="resource-kpi-gc"
              label="GC alloc"
              value={`${(snap.runtime.allocRateBytesPerSecond / (1024 * 1024)).toFixed(2)} МиБ/с`}
            />
            <KpiCard testId="resource-kpi-sessions" label="Сессии (refresh)" value={String(snap.activeSessions)} />
            <KpiCard
              testId="resource-kpi-sql-status"
              label="SQL / Redis"
              value={`${snap.sql.available ? 'SQL OK' : 'SQL —'} · ${snap.redis.available ? 'Redis OK' : 'Redis —'}`}
            />
          </Grid>

          <Grid container spacing={2}>
            <Grid item xs={12} lg={6}>
              <Card variant="outlined"><CardContent><HighchartsReact highcharts={Highcharts} options={cpuOptions} /></CardContent></Card>
            </Grid>
            <Grid item xs={12} lg={6}>
              <Card variant="outlined"><CardContent><HighchartsReact highcharts={Highcharts} options={ramOptions} /></CardContent></Card>
            </Grid>
            <Grid item xs={12} lg={6}>
              <Card variant="outlined"><CardContent><HighchartsReact highcharts={Highcharts} options={connectionsOptions} /></CardContent></Card>
            </Grid>
            <Grid item xs={12} lg={6}>
              <Card variant="outlined"><CardContent><HighchartsReact highcharts={Highcharts} options={correlationOptions} /></CardContent></Card>
            </Grid>
            <Grid item xs={12}>
              <Card variant="outlined"><CardContent><HighchartsReact highcharts={Highcharts} options={saturationOptions} /></CardContent></Card>
            </Grid>
          </Grid>

          <Typography variant="body2" color="text.secondary" sx={{ px: 0.5, pb: 1 }}>
            CPU в долях логического ядра (1.00 = одно ядро занято целиком). RAM — RSS процесса.
            SQL обычно забирает большую часть памяти (buffer pool, лимит {metrics.series.sqlMemoryLimitMib} МиБ).
            При простое SQL ProcessUtilization часто 0 — это не сбой сбора. Redis clients — соединения
            клиента кэша, не пользователи. Очередь ThreadPool &gt; 0 — насыщение API. Сессии —
            неотзванные refresh-токены, не «кто сейчас в гриде». HTTP-коннекты — TCP к Kestrel
            в момент сэмпла; Nginx не держит keep-alive к API, поэтому часто 0.
            Карточки и вердикт — сейчас; графики — выбранный период.
          </Typography>
        </>
      )}
    </Box>
  );
}

export default ResourceDashboard;
