import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';

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
}

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
}: Props) {
  const options = useMemo<Highcharts.Options>(
    () => ({
      chart: { type: 'line', height },
      title: title ? { text: title, align: 'left', style: { fontSize: '14px' } } : { text: undefined },
      xAxis: { type: 'datetime', title: { text: 'Период' } },
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
      legend: { enabled: series.length > 1 },
      credits: { enabled: false },
      series: series.map((s) => ({
        type: 'line',
        id: s.id,
        name: s.name,
        color: s.color,
        dashStyle: s.dashStyle,
        marker: { enabled: true, radius: 4 },
        data: s.points.map((p) => [new Date(p.date).getTime(), p.value]),
      })) as Highcharts.SeriesLineOptions[],
    }),
    [series, title, yAxisTitle, height, zeroLine, valueSuffix, valueDigits],
  );

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
