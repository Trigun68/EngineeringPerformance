import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Grid,
  Typography,
} from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { format } from 'date-fns';
import { fetchSystemMetrics, type SystemMetrics } from '../../api/monitoringApi';
import { getApiErrorMessage } from '../../services/apiError';
import { wpColors } from '../../theme/wpColors';

const REFRESH_INTERVAL_MS = 30_000;

function formatSeriesTimestamps(points: { timestampUtc: string; value: number }[]) {
  return points.map((point) => [Date.parse(point.timestampUtc), point.value] as [number, number]);
}

interface MonitoringDashboardProps {
  windowMinutes?: number;
}

export function MonitoringDashboard({ windowMinutes = 15 }: MonitoringDashboardProps) {
  const [metrics, setMetrics] = useState<SystemMetrics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const loadMetrics = useCallback(async () => {
    try {
      const data = await fetchSystemMetrics(windowMinutes);
      setMetrics(data);
      setError(null);
      setLastUpdated(new Date());
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить метрики системы.'));
    } finally {
      setLoading(false);
    }
  }, [windowMinutes]);

  useEffect(() => {
    void loadMetrics();
    const timer = window.setInterval(() => {
      void loadMetrics();
    }, REFRESH_INTERVAL_MS);
    return () => window.clearInterval(timer);
  }, [loadMetrics]);

  const requestRateOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'line', height: 320 },
    title: { text: 'Нагрузка API (запросы/мин)' },
    xAxis: {
      type: 'datetime',
      title: { text: 'Время (UTC)' },
    },
    yAxis: { title: { text: 'Запросов' }, min: 0 },
    series: [{
      type: 'line',
      name: 'Запросы',
      color: wpColors.accent,
      data: formatSeriesTimestamps(metrics?.requestRateSeries ?? []),
    }],
    credits: { enabled: false },
    legend: { enabled: false },
  }), [metrics?.requestRateSeries]);

  const errorPieOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'pie', height: 320 },
    title: { text: 'Распределение ошибок' },
    series: [{
      type: 'pie',
      name: 'Ошибки',
      data: (metrics?.errorDistribution ?? []).map((item) => ({
        name: item.errorType,
        y: item.count,
      })),
    }],
    credits: { enabled: false },
  }), [metrics?.errorDistribution]);

  const responseTrendOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'spline', height: 320 },
    title: { text: 'Тренд времени отклика (24 ч)' },
    xAxis: {
      type: 'datetime',
      title: { text: 'Время (UTC)' },
    },
    yAxis: { title: { text: 'мс' }, min: 0 },
    series: [{
      type: 'spline',
      name: 'Среднее время отклика',
      color: '#2E86AB',
      data: formatSeriesTimestamps(metrics?.responseTimeTrend24h ?? []),
    }],
    credits: { enabled: false },
    legend: { enabled: false },
  }), [metrics?.responseTimeTrend24h]);

  if (loading && !metrics) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, overflow: 'auto' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
        <Typography variant="h6" sx={{ color: wpColors.heading, fontWeight: 600 }}>
          Системный мониторинг (RED)
        </Typography>
        {lastUpdated && (
          <Typography variant="caption" color="text.secondary">
            Обновлено: {format(lastUpdated, 'HH:mm:ss')} · автообновление каждые 30 с
          </Typography>
        )}
      </Box>

      {error && <Alert severity="error">{error}</Alert>}

      {metrics && (
        <>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="body2" color="text.secondary">Запросов ({metrics.red.windowMinutes} мин)</Typography>
                  <Typography variant="h5">{metrics.red.totalRequests}</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="body2" color="text.secondary">Среднее время отклика</Typography>
                  <Typography variant="h5">{metrics.red.averageDurationMs.toFixed(1)} мс</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="body2" color="text.secondary">P95 время отклика</Typography>
                  <Typography variant="h5">{metrics.red.p95DurationMs.toFixed(1)} мс</Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="body2" color="text.secondary">Ошибки 5xx</Typography>
                  <Typography variant="h5" color={metrics.red.serverErrorCount > 0 ? 'error.main' : 'inherit'}>
                    {metrics.red.serverErrorCount}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="body2" color="text.secondary">Зависимости</Typography>
                  <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap' }}>
                    <Chip
                      size="small"
                      label={`БД: ${metrics.database.isConnected ? 'OK' : 'FAIL'}`}
                      color={metrics.database.isConnected ? 'success' : 'error'}
                    />
                    <Chip
                      size="small"
                      label={`Redis: ${metrics.redis.isConfigured ? (metrics.redis.isConnected ? 'OK' : 'N/A') : '—'}`}
                      color={metrics.redis.isConnected ? 'success' : 'default'}
                    />
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <Grid container spacing={2}>
            <Grid item xs={12} lg={6}>
              <Card variant="outlined">
                <CardContent>
                  <HighchartsReact highcharts={Highcharts} options={requestRateOptions} />
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} lg={6}>
              <Card variant="outlined">
                <CardContent>
                  <HighchartsReact highcharts={Highcharts} options={errorPieOptions} />
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12}>
              <Card variant="outlined">
                <CardContent>
                  <HighchartsReact highcharts={Highcharts} options={responseTrendOptions} />
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </>
      )}
    </Box>
  );
}

export default MonitoringDashboard;
