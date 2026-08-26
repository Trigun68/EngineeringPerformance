import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsMoreImport from 'highcharts/highcharts-more';
import HighchartsReact from 'highcharts-react-official';
import { Box, Typography } from '@mui/material';
import type { ChartPoint, ReferenceFormula } from '../../../api/factNormAnalyticsApi';

// Тип пакета описывает default-экспорт как пространство имён Highcharts, но во время
// выполнения это UMD-фабрика, регистрирующая тип серии `arearange` (полоса допуска).
const HighchartsMore = HighchartsMoreImport as unknown as (hc: typeof Highcharts) => void;
if (typeof HighchartsMore === 'function') {
  HighchartsMore(Highcharts);
}

function predict(
  code: string,
  params: Record<string, number>,
  x: number,
): number | null {
  const a = params.a ?? 0;
  const b = params.b ?? 0;
  const c = params.c ?? 0;
  switch (code.toUpperCase()) {
    case 'CONSTANT':
      return a;
    case 'LINEAR':
      return a + b * x;
    case 'POWER':
      return x <= 0 ? null : a * x ** b;
    case 'LOGARITHMIC':
      return x <= 0 ? null : a + b * Math.log(x);
    case 'EXPONENTIAL':
      return a * Math.exp(b * x);
    case 'POLYNOMIAL2':
      return a + b * x + c * x * x;
    case 'HYPERBOLIC':
      return x <= 0 ? null : a + b / x;
    default:
      return null;
  }
}

interface Props {
  title: string;
  points: ChartPoint[];
  currentFormula: string;
  currentParams?: Record<string, number> | null;
  proposedFormula: string;
  proposedParams?: Record<string, number> | null;
  formulas?: ReferenceFormula[];
  /** σ допуска вокруг текущей нормы — рисует закрашенную полосу допуска и подсвечивает точки-выбросы. */
  deviationSigma?: number | null;
}

export function FactVsNormChart({
  title,
  points,
  currentFormula,
  currentParams,
  proposedFormula,
  proposedParams,
  deviationSigma,
}: Props) {
  const options = useMemo<Highcharts.Options>(() => {
    const xs = points.map((p) => p.x).sort((a, b) => a - b);
    const minX = xs[0] ?? 0;
    const maxX = xs.length > 1 ? xs[xs.length - 1]! : minX + 1;
    const curve = (code: string, params: Record<string, number> | null | undefined, name: string, color: string) => {
      if (!params) return null;
      const data: [number, number][] = [];
      const steps = 40;
      for (let i = 0; i <= steps; i++) {
        const x = minX + ((maxX - minX) * i) / steps;
        const y = predict(code, params, x);
        if (y != null && Number.isFinite(y)) data.push([x, y]);
      }
      return {
        type: 'line' as const,
        name,
        data,
        color,
        marker: { enabled: false },
      };
    };

    const sigma = deviationSigma && deviationSigma > 0 ? deviationSigma : null;
    const bandData: [number, number, number][] = [];
    if (sigma && currentParams) {
      const steps = 40;
      for (let i = 0; i <= steps; i++) {
        const x = minX + ((maxX - minX) * i) / steps;
        const y = predict(currentFormula, currentParams, x);
        if (y != null && Number.isFinite(y)) bandData.push([x, y - sigma, y + sigma]);
      }
    }

    // Точки вне полосы допуска ±σ вокруг текущей нормы подсвечиваются как выбросы.
    const isOutlier = (p: ChartPoint): boolean => {
      if (!sigma || !currentParams) return false;
      const y = predict(currentFormula, currentParams, p.x);
      if (y == null || !Number.isFinite(y)) return false;
      return Math.abs(p.y - y) > sigma;
    };
    const inTolerancePoints = points.filter((p) => !isOutlier(p));
    const outlierPoints = points.filter((p) => isOutlier(p));

    const toScatterData = (list: ChartPoint[]) =>
      list.map((p) => ({
        x: p.x,
        y: p.y,
        projectCode: p.projectCode,
        projectName: p.projectName,
        versionName: p.versionName,
      }));

    const series: Highcharts.SeriesOptionsType[] = [];
    if (bandData.length > 0) {
      series.push({
        type: 'arearange',
        name: 'Полоса допуска ±σ',
        data: bandData,
        color: '#9e9e9e',
        fillOpacity: 0.16,
        lineWidth: 0,
        marker: { enabled: false },
        enableMouseTracking: false,
        zIndex: 0,
      } as Highcharts.SeriesArearangeOptions);
    }
    series.push({
      type: 'scatter',
      name: 'Факт (в допуске)',
      data: toScatterData(inTolerancePoints),
      color: '#1976d2',
      zIndex: 2,
      tooltip: {
        pointFormat:
          '<b>{point.projectCode}</b> {point.projectName}<br/>Версия: {point.versionName}<br/>Объём: {point.x:.3f}<br/>Производительность: {point.y:.4f}',
      },
    });
    if (outlierPoints.length > 0) {
      series.push({
        type: 'scatter',
        name: 'Факт (выброс за допуск)',
        data: toScatterData(outlierPoints),
        color: '#d32f2f',
        marker: { symbol: 'diamond', radius: 5 },
        zIndex: 3,
        tooltip: {
          pointFormat:
            '<b>{point.projectCode}</b> {point.projectName}<br/>Версия: {point.versionName}<br/>Объём: {point.x:.3f}<br/>Производительность: {point.y:.4f}<br/><b>вне полосы допуска</b>',
        },
      });
    }
    const current = curve(currentFormula, currentParams ?? null, 'Текущая норма', '#616161');
    const proposed = curve(proposedFormula, proposedParams ?? null, 'Предложение', '#2e7d32');
    if (current) series.push({ ...current, zIndex: 1 });
    if (proposed) series.push({ ...proposed, zIndex: 1, dashStyle: 'Dash' } as Highcharts.SeriesLineOptions);

    return {
      chart: { height: 360 },
      title: { text: title },
      xAxis: { title: { text: 'Объём' } },
      yAxis: { title: { text: 'Производительность' } },
      credits: { enabled: false },
      legend: { enabled: true },
      series,
      accessibility: { enabled: true, description: 'График факт vs норма: X объём, Y производительность' },
    };
  }, [title, points, currentFormula, currentParams, proposedFormula, proposedParams, deviationSigma]);

  if (points.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary" data-testid="fact-vs-norm-empty">
        Нет точек для графика: в периоде нет primary Fact с объёмом и производительностью по выбранной
        группе.
      </Typography>
    );
  }

  return (
    <Box data-testid="fact-vs-norm-chart" aria-label="График факт против нормы">
      <Typography variant="subtitle1" component="h3" sx={{ mb: 1 }}>
        График: производительность (Y) от объёма (X)
      </Typography>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
