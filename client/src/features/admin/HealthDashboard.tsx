import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Card,
  CardContent,
  CircularProgress,
  Grid,
  Typography,
} from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { fetchSystemHealth, type HealthCheckResult, type ServiceHealth } from '../../api/adminApi';
import { getApiErrorMessage } from '../../services/apiError';
import { wpColors } from '../../theme/wpColors';

const REFRESH_MS = 30_000;

function TrafficLight({ service }: { service: ServiceHealth }) {
  const color = !service.isConfigured
    ? '#9E9E9E'
    : service.isHealthy
      ? '#2E7D32'
      : '#C62828';

  return (
    <Card variant="outlined" data-testid={`health-service-${service.name.replace(/\s+/g, '-')}`}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
          <Box
            data-testid="health-indicator"
            sx={{
              width: 18,
              height: 18,
              borderRadius: '50%',
              bgcolor: color,
              boxShadow: `0 0 8px ${color}`,
            }}
          />
          <Box>
            <Typography fontWeight={600}>{service.name}</Typography>
            <Typography variant="body2" color="text.secondary">{service.status}</Typography>
            {service.responseTimeMs != null && (
              <Typography variant="caption" color="text.secondary">
                {service.responseTimeMs.toFixed(1)} мс
              </Typography>
            )}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}

export function HealthDashboard() {
  const [health, setHealth] = useState<HealthCheckResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setHealth(await fetchSystemHealth());
      setError(null);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить состояние сервисов.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
    const timer = window.setInterval(() => void load(), REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [load]);

  const chartOptions = useMemo<Highcharts.Options>(() => ({
    chart: { type: 'spline', height: 320 },
    title: { text: 'Среднее время отклика API (24 ч)' },
    xAxis: { type: 'datetime' },
    yAxis: { title: { text: 'мс' }, min: 0 },
    series: [{
      type: 'spline',
      name: 'Response time',
      color: wpColors.accent,
      data: (health?.responseTimeTrend24h ?? []).map((p) => [Date.parse(p.timestampUtc), p.value]),
    }],
    credits: { enabled: false },
    legend: { enabled: false },
  }), [health?.responseTimeTrend24h]);

  if (loading && !health) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, overflow: 'auto' }}>
      {error && <Alert severity="error">{error}</Alert>}

      {health && (
        <>
          <Grid container spacing={2}>
            {health.services.map((service) => (
              <Grid item xs={12} sm={6} md={4} key={service.name}>
                <TrafficLight service={service} />
              </Grid>
            ))}
          </Grid>

          <Card variant="outlined">
            <CardContent>
              <HighchartsReact highcharts={Highcharts} options={chartOptions} />
            </CardContent>
          </Card>
        </>
      )}
    </Box>
  );
}

export default HealthDashboard;
