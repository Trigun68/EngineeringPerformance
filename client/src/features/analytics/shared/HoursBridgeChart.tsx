import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';

export interface HoursBridgeLine {
  groupId: number;
  groupName: string;
  volumeEffect: number;
  productivityEffect: number;
}

const VOLUME_COLOR = '#3D7A6A';
const PRODUCTIVITY_COLOR = '#C45C26';

interface Props {
  lines: HoursBridgeLine[];
  height?: number;
  testId?: string;
}

/** Кластер: эффект объёма и эффект производительности по корневым группам IIC. */
export function HoursBridgeChart({ lines, height = 320, testId }: Props) {
  const options = useMemo<Highcharts.Options>(
    () => ({
      chart: { type: 'column', height },
      title: { text: undefined },
      xAxis: {
        categories: lines.map((l) => l.groupName),
        labels: { style: { fontSize: '11px' } },
      },
      yAxis: {
        title: { text: 'Часы: объём / производительность' },
        plotLines: [{ value: 0, color: '#999', width: 1, zIndex: 3 }],
      },
      tooltip: { shared: true, valueDecimals: 1, valueSuffix: ' ч' },
      legend: { enabled: true },
      credits: { enabled: false },
      plotOptions: { column: { grouping: true, borderWidth: 0 } },
      series: [
        {
          type: 'column',
          name: 'Эффект объёма',
          color: VOLUME_COLOR,
          data: lines.map((l) => l.volumeEffect),
        },
        {
          type: 'column',
          name: 'Эффект производительности',
          color: PRODUCTIVITY_COLOR,
          data: lines.map((l) => l.productivityEffect),
        },
      ],
    }),
    [lines, height],
  );

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
