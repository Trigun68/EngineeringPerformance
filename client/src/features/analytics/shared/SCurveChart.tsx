import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';
import { FACT_ACCENT_COLOR, PLAN_ACCENT_COLOR } from '../../../constants/planFactColors';
import type { TrendEventDto } from '../../../api/projectTrendApi';

export interface SCurvePoint {
  date: string;
  cumulativeHours: number;
  label?: string | null;
}

export interface SCurveExtraLine {
  name: string;
  points: SCurvePoint[];
}

interface Props {
  points: SCurvePoint[];
  planPoints?: SCurvePoint[];
  extraLines?: SCurveExtraLine[];
  events?: TrendEventDto[];
  height?: number;
  testId?: string;
}

const EXTRA_COLORS = ['#64748B', '#0F766E', '#B45309', '#7C3AED', '#BE123C', '#0369A1', '#4D7C0F', '#9F1239'];

/** Накопительные часы факта (и опционально плана / проектов) по датам версий. */
export function SCurveChart({
  points,
  planPoints,
  extraLines,
  events,
  height = 280,
  testId,
}: Props) {
  const options = useMemo<Highcharts.Options>(() => {
    const series: Highcharts.SeriesOptionsType[] = [
      {
        type: 'area',
        name: 'Накопительный факт',
        color: FACT_ACCENT_COLOR,
        fillOpacity: 0.15,
        marker: { enabled: true, radius: 4 },
        data: points.map((p) => [new Date(p.date).getTime(), p.cumulativeHours]),
      },
    ];
    if (planPoints && planPoints.length > 0) {
      series.push({
        type: 'area',
        name: 'Накопительный план',
        color: PLAN_ACCENT_COLOR,
        fillOpacity: 0.08,
        marker: { enabled: true, radius: 3 },
        data: planPoints.map((p) => [new Date(p.date).getTime(), p.cumulativeHours]),
      });
    }
    (extraLines ?? []).forEach((line, i) => {
      series.push({
        type: 'line',
        name: line.name,
        color: EXTRA_COLORS[i % EXTRA_COLORS.length],
        lineWidth: 1,
        marker: { enabled: false },
        data: line.points.map((p) => [new Date(p.date).getTime(), p.cumulativeHours]),
      });
    });

    const plotLines = (events ?? []).slice(0, 12).map((e) => ({
      value: new Date(e.timestamp).getTime(),
      color: '#94A3B8',
      width: 1,
      dashStyle: 'Dot' as const,
      label: { text: e.label, rotation: 270, style: { fontSize: '9px', color: '#64748B' } },
    }));

    return {
      chart: { type: 'area', height },
      title: { text: undefined },
      xAxis: {
        type: 'datetime',
        title: { text: 'Дата' },
        plotLines: plotLines.length > 0 ? plotLines : undefined,
      },
      yAxis: { title: { text: 'Накопительно, ч' }, gridLineColor: '#F1F5F9' },
      legend: { enabled: series.length > 1 },
      credits: { enabled: false },
      tooltip: { xDateFormat: '%d.%m.%Y', valueDecimals: 0, valueSuffix: ' ч' },
      series,
    };
  }, [points, planPoints, extraLines, events, height]);

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
