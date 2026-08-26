import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';
import type { ProductivityTrendGroup } from '../../../api/productivityTrendApi';

interface Props {
  group: ProductivityTrendGroup;
  metric: 'hours' | 'index';
  height?: number;
  title?: string;
  testId?: string;
}

export function ProductivityTrendChart({
  group,
  metric,
  height = 320,
  title,
  testId,
}: Props) {
  const options = useMemo<Highcharts.Options>(() => {
    const traces: Highcharts.SeriesLineOptions[] = group.segments.map((seg, idx) => ({
      type: 'line',
      name: seg.normName,
      color: seg.color,
      step: 'left',
      marker: { enabled: seg.points.length === 1, radius: 4 },
      data: seg.points.map((p) => ({
        x: new Date(p.date).getTime(),
        y: metric === 'index' ? p.index : p.hoursAtVStar,
        custom: {
          formulaType: seg.formulaType,
          flag: p.flag,
          hours: p.hoursAtVStar,
          index: p.index,
        },
      })),
      id: `${seg.segmentKey}-${idx}`,
    }));

    return {
      chart: { type: 'line', height, zooming: { type: 'x' } },
      title: { text: title ?? group.groupName, align: 'left', style: { fontSize: '14px' } },
      xAxis: { type: 'datetime', title: { text: 'Дата' } },
      yAxis: {
        title: { text: metric === 'index' ? 'Индекс часов N(V*)' : 'Часы N(V*)' },
        plotLines:
          metric === 'index'
            ? [{ value: 1, color: '#666', width: 1, dashStyle: 'ShortDash', zIndex: 3 }]
            : [],
      },
      tooltip: {
        formatter() {
          const pt = this as Highcharts.Point & {
            custom?: { formulaType?: string; flag?: string; hours?: number | null; index?: number | null };
          };
          const custom = pt.custom ?? (pt.options as { custom?: typeof pt.custom }).custom;
          const date = Highcharts.dateFormat('%d.%m.%Y', Number(this.x));
          const y = this.y == null ? '—' : Highcharts.numberFormat(Number(this.y), metric === 'index' ? 3 : 2);
          const formula = custom?.formulaType ? `<br/>Формула: ${custom.formulaType}` : '';
          const flag = custom?.flag && custom.flag !== 'ok' ? `<br/>Флаг: ${custom.flag}` : '';
          return `${date}<br/><b>${this.series.name}</b>: ${y}${formula}${flag}`;
        },
      },
      legend: { enabled: group.segments.length > 1 },
      credits: { enabled: false },
      series: traces,
    };
  }, [group, metric, height, title]);

  return (
    <Box data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
