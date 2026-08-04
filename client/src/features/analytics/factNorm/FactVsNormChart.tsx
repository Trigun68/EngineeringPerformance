import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box, Typography } from '@mui/material';
import type { ChartPoint, ReferenceFormula } from '../../../api/factNormAnalyticsApi';

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
}

export function FactVsNormChart({
  title,
  points,
  currentFormula,
  currentParams,
  proposedFormula,
  proposedParams,
}: Props) {
  const options = useMemo<Highcharts.Options>(() => {
    const xs = points.map((p) => p.x).sort((a, b) => a - b);
    const minX = xs[0] ?? 0;
    const maxX = xs[xs.length - 1] ?? 1;
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

    const series: Highcharts.SeriesOptionsType[] = [
      {
        type: 'scatter',
        name: 'Факт',
        data: points.map((p) => [p.x, p.y]),
        color: '#1976d2',
      },
    ];
    const current = curve(currentFormula, currentParams ?? null, 'Текущая норма', '#9e9e9e');
    const proposed = curve(proposedFormula, proposedParams ?? null, 'Предложение', '#d32f2f');
    if (current) series.push(current);
    if (proposed) series.push(proposed);

    return {
      chart: { height: 320 },
      title: { text: title },
      xAxis: { title: { text: 'Объём (x)' } },
      yAxis: { title: { text: 'Производительность (y)' } },
      credits: { enabled: false },
      series,
      accessibility: { enabled: true, description: 'График факт vs норма' },
    };
  }, [title, points, currentFormula, currentParams, proposedFormula, proposedParams]);

  if (points.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary" data-testid="fact-vs-norm-empty">
        Нет точек для графика
      </Typography>
    );
  }

  return (
    <Box data-testid="fact-vs-norm-chart" aria-label="График факт против нормы">
      <HighchartsReact highcharts={Highcharts} options={options} />
      <table className="sr-only" style={{ position: 'absolute', left: -9999 }} aria-hidden={false}>
        <caption>Табличная альтернатива графика</caption>
        <thead>
          <tr>
            <th>x</th>
            <th>y</th>
          </tr>
        </thead>
        <tbody>
          {points.map((p, i) => (
            <tr key={i}>
              <td>{p.x}</td>
              <td>{p.y}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </Box>
  );
}
