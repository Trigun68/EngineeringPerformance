import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';

export interface ZeroBaselineBar {
  name: string;
  zScore: number;
}

interface Props {
  bars: ZeroBaselineBar[];
  height?: number;
  testId?: string;
}

const ABOVE_NORM = '#c62828';
const AT_OR_BELOW = '#2e7d32';

/** Столбцы Z относительно 0 (норма). Не водопад «было → стало». */
export function ZeroBaselineChart({ bars, height = 280, testId }: Props) {
  const options = useMemo<Highcharts.Options>(
    () => ({
      chart: { type: 'column', height },
      title: { text: undefined },
      xAxis: {
        categories: bars.map((b) => b.name),
        labels: { style: { fontSize: '11px' } },
      },
      yAxis: {
        title: { text: 'Z-оценка' },
        plotLines: [{ value: 0, color: '#999', width: 1, zIndex: 3 }],
      },
      tooltip: { valueDecimals: 2 },
      legend: { enabled: false },
      credits: { enabled: false },
      plotOptions: {
        column: {
          borderWidth: 0,
          dataLabels: { enabled: true, format: '{y:.2f}' },
        },
      },
      series: [
        {
          type: 'column',
          name: 'Z',
          data: bars.map((b) => ({
            y: b.zScore,
            color: b.zScore > 0 ? ABOVE_NORM : AT_OR_BELOW,
          })),
        },
      ],
    }),
    [bars, height],
  );

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
