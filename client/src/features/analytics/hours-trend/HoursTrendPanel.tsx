import { useEffect, useMemo, useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import {
  Alert,
  Box,
  FormControl,
  InputLabel,
  LinearProgress,
  MenuItem,
  Paper,
  Select,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { fetchTrendDetail, fetchTrends, type TrendDetailRow } from '../../../api/analyticsApi';
import { fetchZTrendFilters } from '../../../api/zTrendApi';
import { exportTrendsPdf } from '../../../api/visualReportsApi';
import { DataTable } from '../../../components/DataTable/DataTable';
import { EMPTY_ROWS } from '../../../components/DataTable/emptyRows';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { FACT_ACCENT_COLOR, PLAN_ACCENT_COLOR } from '../../../constants/planFactColors';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import { LAST_PROJECT_PREF_KEY } from '../../../utils/projectWorkspacePrefs';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';

const HOURS_PROJECT_PREF = 'analytics.hoursTrend.projectId.v1';

interface Props {
  dateFrom: string;
  dateTo: string;
}

function toMillis(iso: string): number {
  const t = new Date(iso).getTime();
  return Number.isFinite(t) ? t : 0;
}

export function HoursTrendPanel({ dateFrom, dateTo }: Props) {
  const [projects, setProjects] = useState<Array<{ id: number; code: string; name: string }>>([]);
  const [projectId, setProjectId] = useState<number | null>(null);
  const [actionError, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);
  const [detailVersionId, setDetailVersionId] = useState<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    void Promise.all([fetchZTrendFilters(), getUserPreferences()])
      .then(([filters, prefs]) => {
        if (cancelled) return;
        setProjects(filters.projects);
        const saved = Number(prefs[HOURS_PROJECT_PREF] ?? prefs[LAST_PROJECT_PREF_KEY] ?? 0);
        const initial =
          filters.projects.find((p) => p.id === saved)?.id ?? filters.projects[0]?.id ?? null;
        setProjectId(initial);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(getApiErrorMessage(err, 'Не удалось загрузить список проектов.'));
      });
    return () => {
      cancelled = true;
    };
  }, []);

  // Смена проекта/периода закрывает детализацию — как раньше при перезагрузке тренда.
  useEffect(() => {
    setDetailVersionId(null);
  }, [projectId, dateFrom, dateTo]);

  const trendsQuery = useQuery({
    queryKey: queryKeys.analytics.hoursTrend(projectId ?? undefined, dateFrom, dateTo),
    // fetchTrends не принимает AbortSignal — без { signal }.
    queryFn: () =>
      fetchTrends({
        projectId: projectId!,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
      }),
    enabled: !!projectId,
    placeholderData: keepPreviousData,
  });

  // Ленивая детализация версии: запрос только после клика по точке графика.
  const detailQuery = useQuery({
    queryKey: queryKeys.analytics.hoursTrendDetail(projectId ?? undefined, detailVersionId ?? undefined),
    queryFn: () => fetchTrendDetail(projectId!, detailVersionId!),
    enabled: projectId != null && detailVersionId != null,
  });

  const result = trendsQuery.data ?? null;
  const loading = trendsQuery.isFetching;
  const detail = detailQuery.data ?? null;
  const detailLoading = detailQuery.isFetching;
  const trendsError =
    trendsQuery.isError && !isRequestCanceled(trendsQuery.error)
      ? getApiErrorMessage(trendsQuery.error, 'Не удалось загрузить тренд часов.')
      : null;
  const detailError =
    detailQuery.isError && !isRequestCanceled(detailQuery.error)
      ? getApiErrorMessage(detailQuery.error, 'Не удалось загрузить детализацию версии.')
      : null;
  const error = actionError ?? trendsError ?? detailError;

  const handleProject = (id: number) => {
    setProjectId(id);
    void putUserPreferences({ [HOURS_PROJECT_PREF]: String(id) });
  };

  const chartOptions = useMemo<Highcharts.Options>(() => {
    const series = (result?.series ?? []).map((s) => {
      const color =
        s.metric === 'factLaborHours' || s.metric === 'cumulativeFactHours'
          ? FACT_ACCENT_COLOR
          : s.metric === 'forecastLaborHours'
            ? PLAN_ACCENT_COLOR
            : '#C45C26';
      return {
        type: 'line' as const,
        name: s.displayName,
        color,
        data: s.points.map((p) => ({
          x: toMillis(p.timestamp),
          y: p.value,
          custom: { versionId: p.versionId },
        })),
      };
    });
    const plotLines = (result?.events ?? []).slice(0, 12).map((e) => ({
      value: toMillis(e.timestamp),
      color: '#94A3B8',
      width: 1,
      dashStyle: 'Dot' as const,
      label: { text: e.label, rotation: 270, style: { fontSize: '9px' } },
    }));
    return {
      chart: { type: 'line', height: 340 },
      title: { text: undefined },
      xAxis: { type: 'datetime', plotLines: plotLines.length > 0 ? plotLines : undefined },
      yAxis: { title: { text: 'Часы' } },
      legend: { enabled: true },
      credits: { enabled: false },
      tooltip: { xDateFormat: '%d.%m.%Y', shared: true },
      plotOptions: {
        series: {
          cursor: 'pointer',
          point: {
            events: {
              click() {
                const versionId = (this as Highcharts.Point & { custom?: { versionId?: number } }).custom
                  ?.versionId;
                if (versionId) setDetailVersionId(versionId);
              },
            },
          },
        },
      },
      series,
    };
  }, [result]);

  const columnDefs = useMemo<ColDef<TrendDetailRow>[]>(
    () => [
      { field: 'facilityCode', headerName: 'Шифр объекта', width: 140 },
      { field: 'facilityName', headerName: 'Объект', flex: 1, minWidth: 180 },
      { field: 'pvCode', headerName: 'Шифр ФО', width: 140 },
      { field: 'pvName', headerName: 'Наименование ФО', flex: 1, minWidth: 180 },
      {
        field: 'factLaborHours',
        headerName: 'Часы',
        width: 110,
        valueFormatter: (p) =>
          p.value == null
            ? '—'
            : Number(p.value).toLocaleString('ru-RU', { maximumFractionDigits: 1 }),
      },
      {
        field: 'zScore',
        headerName: 'Оценка Z',
        width: 90,
        valueFormatter: (p) =>
          p.value == null
            ? '—'
            : Number(p.value).toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
      },
      { field: 'interpretation', headerName: 'Интерпретация', width: 180 },
    ],
    [],
  );

  const handlePdf = async () => {
    if (!projectId) return;
    setExporting(true);
    try {
      await exportTrendsPdf(projectId, {
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
      });
    } catch (err: unknown) {
      setError(getApiErrorMessage(err, 'Не удалось выгрузить PDF.'));
    } finally {
      setExporting(false);
    }
  };

  return (
    <Stack spacing={2.5} data-testid="hours-trend-panel">
      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
        <FormControl size="small" sx={{ minWidth: 280 }}>
          <InputLabel id="hours-trend-project-label">Проект</InputLabel>
          <Select
            labelId="hours-trend-project-label"
            label="Проект"
            value={projectId ?? ''}
            onChange={(e) => handleProject(Number(e.target.value))}
            data-testid="hours-trend-project"
          >
            {projects.map((p) => (
              <MenuItem key={p.id} value={p.id}>
                {p.code} — {p.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <ExportActionButton
          kind="pdf"
          loading={exporting}
          disabled={!projectId || exporting}
          onClick={() => void handlePdf()}
          data-testid="hours-trend-export-pdf"
        />
      </Stack>

      {loading && !result ? (
        <Skeleton variant="rounded" height={340} data-testid="hours-trend-loading" />
      ) : null}
      {loading && result ? <LinearProgress /> : null}
      {error ? (
        <Alert severity="error" data-testid="hours-trend-error">
          {error}
        </Alert>
      ) : null}

      {result && result.series.every((s) => s.points.length === 0) ? (
        <Alert severity="info" data-testid="hours-trend-empty">
          Нет серий часов в выбранном периоде.
        </Alert>
      ) : null}

      {result && result.series.some((s) => s.points.length > 0) ? (
        <Paper variant="outlined" sx={{ p: 2 }} data-testid="hours-trend-chart">
          <Typography variant="h6" component="h2" sx={{ mb: 1 }}>
            Динамика часов по версиям
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Клик по точке версии открывает объекты и позиции ФО.
          </Typography>
          <Box sx={{ minHeight: 340 }}>
            <HighchartsReact highcharts={Highcharts} options={chartOptions} />
          </Box>
        </Paper>
      ) : null}

      {detailVersionId != null ? (
        <Box data-testid="hours-trend-detail">
          <Typography variant="h6" component="h2" sx={{ mb: 1 }}>
            Объекты версии
          </Typography>
          <DataTable
            rowData={detail ?? EMPTY_ROWS}
            columnDefs={columnDefs}
            objectName="GroupTrend"
            gridId="analytics-hours-trend-detail"
            height={360}
            loading={detailLoading}
            getRowIdValue={(row, i) => `${row.versionId}-${row.pvCode}-${i}`}
          />
        </Box>
      ) : null}
    </Stack>
  );
}
