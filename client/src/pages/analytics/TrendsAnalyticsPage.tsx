import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import { useSearchParams } from 'react-router-dom';
import {
  fetchTrendFormulas,
  fetchTrends,
  type TrendAnalyticsResult,
} from '../../api/analyticsApi';
import { exportTrendsPdf } from '../../api/visualReportsApi';
import { fetchProjects } from '../../api/projectsApi';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import { getApiErrorMessage } from '../../services/apiError';
import type { Project } from '../../types/project';

const TRENDS_FILTER_KEY = 'analytics.trends.filters.v1';

export function TrendsAnalyticsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [projects, setProjects] = useState<Project[]>([]);
  const [projectId, setProjectId] = useState<number | null>(null);
  const [formulaId, setFormulaId] = useState('v1-docset-proportional');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [formulas, setFormulas] = useState<{ id: string; displayName: string }[]>([]);
  const [data, setData] = useState<TrendAnalyticsResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [exportingPdf, setExportingPdf] = useState(false);

  useEffect(() => {
    void fetchProjects().then((list) => {
      setProjects(list);
      if (!projectId && list.length > 0) {
        setProjectId(list[0].id);
      }
    }).catch(() => setProjects([]));
    void fetchTrendFormulas()
      .then((list) => setFormulas(list.map((f) => ({ id: f.id, displayName: f.displayName }))))
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

  const chartOptions = useMemo(() => {
    const fact = data?.series.find((s) => s.metric === 'factLaborHours');
    const forecast = data?.series.find((s) => s.metric === 'forecastLaborHours');
    const delta = data?.series.find((s) => s.metric === 'hoursDelta');
    const cumulative = data?.series.find((s) => s.metric === 'cumulativeFactHours');

    const toSeries = (metric: string, name: string, type: 'line' | 'column' | 'area' = 'line') => {
      const series = data?.series.find((s) => s.metric === metric);
      return {
        type,
        name,
        data: series?.points.map((p) => [new Date(p.timestamp).getTime(), p.value ?? 0]) ?? [],
      } as Highcharts.SeriesOptionsType;
    };

    return {
      spline: {
        title: { text: 'Факт vs Прогноз/План' },
        xAxis: { type: 'datetime' as const },
        yAxis: { title: { text: 'Часы' } },
        series: [
          toSeries('factLaborHours', 'Факт', 'line'),
          toSeries('forecastLaborHours', 'Прогноз/План', 'line'),
        ],
        credits: { enabled: false },
      },
      column: {
        title: { text: 'Δ Факт − Прогноз' },
        xAxis: { type: 'datetime' as const },
        yAxis: { title: { text: 'Δ часы' } },
        series: [toSeries('hoursDelta', 'Δ', 'column')],
        credits: { enabled: false },
      },
      area: {
        title: { text: 'S-curve (накопительный факт)' },
        xAxis: { type: 'datetime' as const },
        yAxis: { title: { text: 'Накопительно, ч' } },
        series: [toSeries('cumulativeFactHours', 'Накопительный факт', 'area')],
        credits: { enabled: false },
      },
      meta: { fact: fact?.points.length ?? 0, forecast: forecast?.points.length ?? 0, delta: delta?.points.length ?? 0, cumulative: cumulative?.points.length ?? 0 },
    };
  }, [data]);

  return (
    <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 2, height: '100%' }}>
      <Typography variant="h5">Трендовый анализ</Typography>

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

      {projectId && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' }, gap: 2, flex: 1 }}>
          <Box sx={{ bgcolor: 'background.paper', p: 1, borderRadius: 1, border: '1px solid #D5DDE5' }}>
            <HighchartsReact highcharts={Highcharts} options={chartOptions.spline} />
          </Box>
          <Box sx={{ bgcolor: 'background.paper', p: 1, borderRadius: 1, border: '1px solid #D5DDE5' }}>
            <HighchartsReact highcharts={Highcharts} options={chartOptions.column} />
          </Box>
          <Box sx={{ gridColumn: { lg: '1 / -1' }, bgcolor: 'background.paper', p: 1, borderRadius: 1, border: '1px solid #D5DDE5' }}>
            <HighchartsReact highcharts={Highcharts} options={chartOptions.area} />
          </Box>
        </Box>
      )}
    </Box>
  );
}
