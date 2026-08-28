import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsMoreImport from 'highcharts/highcharts-more';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';
import type { TrendEventDto, PercentileBandPointDto } from '../../../api/projectTrendApi';

const HighchartsMore = HighchartsMoreImport as unknown as (hc: typeof Highcharts) => void;
if (typeof HighchartsMore === 'function') {
  HighchartsMore(Highcharts);
}

export interface DynamicsPoint {
  /** ISO-дата начала под-периода (бакета: неделя/месяц). */
  date: string;
  value: number | null;
}

export interface DynamicsSeries {
  id: string;
  name: string;
  color: string;
  dashStyle?: Highcharts.DashStyleValue;
  points: DynamicsPoint[];
}

interface Props {
  series: DynamicsSeries[];
  title?: string;
  yAxisTitle?: string;
  height?: number;
  testId?: string;
  /** Показать опорную линию y = 0 (уместно для Z-оценки). */
  zeroLine?: boolean;
  valueSuffix?: string;
  valueDigits?: number;
  band?: PercentileBandPointDto[];
  events?: TrendEventDto[];
}

const BAND_COLOR = '#94A3B8';

/** Динамика среднего показателя по под-периодам выбранного диапазона (недели/месяцы). */
export function PeriodDynamicsChart({
  series,
  title,
  yAxisTitle,
  height = 320,
  testId,
  zeroLine,
  valueSuffix = '',
  valueDigits = 2,
  band,
  events,
}: Props) {
  const options = useMemo<Highcharts.Options>(() => {
    const chartSeries: Highcharts.SeriesOptionsType[] = [];
    if (band && band.length > 0) {
      chartSeries.push({
        type: 'arearange',
        name: 'P10–P90',
        color: BAND_COLOR,
        fillOpacity: 0.18,
        lineWidth: 0,
        zIndex: 0,
        enableMouseTracking: true,
        data: band.map((p) => [new Date(p.date).getTime(), p.p10, p.p90]),
      });
    }
    for (const s of series) {
      chartSeries.push({
        type: 'line',
        id: s.id,
        name: s.name,
        color: s.color,
        dashStyle: s.dashStyle,
        zIndex: 1,
        marker: { enabled: true, radius: 4 },
        data: s.points.map((p) => [new Date(p.date).getTime(), p.value]),
      });
    }

    const plotLines = (events ?? [])
      .slice(0, 12)
      .map((e) => ({
        value: new Date(e.timestamp).getTime(),
        color: '#94A3B8',
        width: 1,
        dashStyle: 'Dot' as const,
        zIndex: 2,
        label: {
          text: e.label,
          rotation: 270,
          style: { fontSize: '9px', color: '#64748B' },
        },
      }));

    return {
      chart: { type: 'line', height },
      title: title ? { text: title, align: 'left', style: { fontSize: '14px' } } : { text: undefined },
      xAxis: {
        type: 'datetime',
        title: { text: 'Период' },
        plotLines: plotLines.length > 0 ? plotLines : undefined,
      },
      yAxis: {
        title: { text: yAxisTitle },
        plotLines: zeroLine
          ? [
              {
                value: 0,
                color: '#999',
                width: 1,
                dashStyle: 'ShortDash',
                zIndex: 3,
              },
            ]
          : undefined,
      },
      tooltip: {
        shared: true,
        valueSuffix,
        valueDecimals: valueDigits,
        xDateFormat: '%d.%m.%Y',
      },
      legend: { enabled: series.length > 1 || Boolean(band && band.length > 0) },
      credits: { enabled: false },
      series: chartSeries,
    };
  }, [series, title, yAxisTitle, height, zeroLine, valueSuffix, valueDigits, band, events]);

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
