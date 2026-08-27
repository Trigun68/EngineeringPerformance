import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';
import { FACT_ACCENT_COLOR } from '../../../constants/planFactColors';

export interface SCurvePoint {
  date: string;
  cumulativeHours: number;
  label?: string | null;
}

interface Props {
  points: SCurvePoint[];
  height?: number;
  testId?: string;
}

/** Накопительные часы факта по датам версий. */
export function SCurveChart({ points, height = 280, testId }: Props) {
  const options = useMemo<Highcharts.Options>(
    () => ({
      chart: { type: 'area', height },
      title: { text: undefined },
      xAxis: { type: 'datetime', title: { text: 'Дата' } },
      yAxis: { title: { text: 'Накопительно, ч' }, gridLineColor: '#F1F5F9' },
      legend: { enabled: false },
      credits: { enabled: false },
      tooltip: { xDateFormat: '%d.%m.%Y', valueDecimals: 0, valueSuffix: ' ч' },
      series: [
        {
          type: 'area',
          name: 'Накопительный факт',
          color: FACT_ACCENT_COLOR,
          fillOpacity: 0.15,
          marker: { enabled: true, radius: 4 },
          data: points.map((p) => [new Date(p.date).getTime(), p.cumulativeHours]),
        },
      ],
    }),
    [points, height],
  );

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
