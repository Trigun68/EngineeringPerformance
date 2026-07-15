import { useEffect, useMemo, useRef } from 'react';
import Highcharts from 'highcharts';
import HighchartsMore from 'highcharts/highcharts-more';
import HighchartsReact from 'highcharts-react-official';
import type { NormPreviewPoint } from '../../api/normsApi';

if (typeof HighchartsMore === 'function') {
  (HighchartsMore as (hc: typeof Highcharts) => void)(Highcharts);
}

export interface NormBandChartProps {
  band: NormPreviewPoint[];
  /** Доля отклонения (0.1 = ±10%). */
  deviationRatio?: number;
  /** Перерисовка после открытия модального окна. */
  chartVisible?: boolean;
}

export function NormBandChart({ band, deviationRatio = 0, chartVisible = true }: NormBandChartProps) {
  const chartRef = useRef<HighchartsReact.RefObject>(null);

  const options = useMemo<Highcharts.Options>(() => {
    const hasBand = deviationRatio > 0 && band.length > 0;
    const series: Highcharts.SeriesOptionsType[] = [];

    if (hasBand) {
      series.push({
        type: 'arearange',
        name: `Коридор ±${(deviationRatio * 100).toFixed(1)}%`,
        data: band.map((p) => [p.volume, p.lower, p.upper]),
        color: '#1976d2',
        fillOpacity: 0.2,
        lineWidth: 0,
        zIndex: 1,
        enableMouseTracking: true,
      } as Highcharts.SeriesArearangeOptions);
    }

    series.push({
      type: 'line',
      name: 'Базовая норма N(V)',
      data: band.map((p) => [p.volume, p.center]),
      color: '#1565c0',
      lineWidth: 2.5,
      zIndex: 2,
      marker: { enabled: false },
    });

    const yValues = band.flatMap((p) => [p.center, p.upper, p.lower]);
    const yMin = yValues.length > 0 ? Math.max(0, Math.min(...yValues) * 0.9) : 0;
    const yMax = yValues.length > 0 ? Math.max(...yValues) * 1.1 : undefined;

    return {
      chart: { height: 260, backgroundColor: 'transparent', spacing: [12, 12, 16, 12] },
      title: { text: undefined },
      credits: { enabled: false },
      legend: { enabled: true },
      xAxis: {
        title: { text: 'Объём V' },
        min: band.length > 0 ? band[0].volume : 0,
        max: band.length > 0 ? band[band.length - 1].volume : undefined,
      },
      yAxis: { title: { text: 'N(V)' }, min: yMin, max: yMax, softMin: 0 },
      tooltip: {
        shared: true,
        pointFormatter: function formatter(this: Highcharts.Point) {
          if ((this as Highcharts.Point & { low?: number }).low != null) return '';
          const pt = band[this.index];
          if (!pt) return '';
          return `N(V)=${pt.center.toFixed(2)}<br/>Верх=${pt.upper.toFixed(2)}<br/>Низ=${pt.lower.toFixed(2)}`;
        },
      },
      series,
    };
  }, [band, deviationRatio]);

  useEffect(() => {
    if (!chartVisible || band.length === 0) return;
    const t1 = window.setTimeout(() => chartRef.current?.chart?.reflow(), 50);
    const t2 = window.setTimeout(() => chartRef.current?.chart?.reflow(), 350);
    return () => {
      window.clearTimeout(t1);
      window.clearTimeout(t2);
    };
  }, [band, chartVisible, options]);

  if (band.length === 0) {
    return (
      <small style={{ color: '#666' }}>
        Задайте коэффициенты формулы и процент отклонения — здесь появится кривая N(V) и коридор.
      </small>
    );
  }

  return (
    <div>
      <HighchartsReact highcharts={Highcharts} options={options} ref={chartRef} />
      <small style={{ color: '#666' }}>
        Заштрихованная область — диапазон возможных значений производительности в пределах заданного процента отклонения.
      </small>
    </div>
  );
}
