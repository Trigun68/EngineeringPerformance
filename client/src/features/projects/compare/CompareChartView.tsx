import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';
import type { CompareChartResult } from '../../../api/analyticsApi';

export interface CompareChartViewProps {
  chart: CompareChartResult | null;
  height?: number | string;
}

export function CompareChartView({ chart, height = 320 }: CompareChartViewProps) {
  const options = useMemo<Highcharts.Options>(() => {
    if (!chart) {
      return { title: { text: undefined }, series: [] };
    }

    const categories = chart.points.map((p) => p.category);
    const deltaData = chart.points.map((p) => ({
      y: p.delta ?? 0,
      color: (p.delta ?? 0) >= 0 ? '#E74C3C' : '#27AE60',
    }));

    return {
      chart: { type: 'column', height: typeof height === 'number' ? height : undefined },
      title: { text: `Δ ${chart.metric} (${chart.level})` },
      xAxis: { categories, labels: { rotation: -35 } },
      yAxis: { title: { text: chart.metric === 'hours' ? 'Часы' : 'Z-score' } },
      tooltip: {
        shared: true,
        pointFormat: '<span style="color:{point.color}">●</span> {series.name}: <b>{point.y}</b><br/>',
      },
      plotOptions: { column: { borderWidth: 0 } },
      series: [
        {
          type: 'column',
          name: 'Δ',
          data: deltaData,
        },
        {
          type: 'line',
          name: chart.versionAName,
          data: chart.points.map((p) => p.valueA),
          color: '#2980B9',
        },
        {
          type: 'line',
          name: chart.versionBName,
          data: chart.points.map((p) => p.valueB),
          color: '#8E44AD',
        },
      ],
      credits: { enabled: false },
    };
  }, [chart, height]);

  return (
    <Box sx={{ width: '100%', minHeight: height }}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
