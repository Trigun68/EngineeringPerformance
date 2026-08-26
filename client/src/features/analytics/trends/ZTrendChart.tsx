import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';
import type { ZTrendGroup, ZTrendSeries } from '../../../api/zTrendApi';

interface Props {
  group: ZTrendGroup;
  showPlan: boolean;
  showFact: boolean;
  height?: number;
  title?: string;
  testId?: string;
}

function seriesToTraces(
  series: ZTrendSeries,
  dashStyle: Highcharts.DashStyleValue | undefined,
  seriesLabel: string,
): Highcharts.SeriesLineOptions[] {
  return series.segments.map((seg, idx) => ({
    type: 'line',
    name: `${seriesLabel}: ${seg.normName}`,
    color: seg.color,
    dashStyle,
    showInLegend: false,
    marker: { enabled: seg.points.length === 1, radius: 4 },
    data: seg.points.map((p) => ({
      x: new Date(p.date).getTime(),
      y: p.zScore,
      custom: {
        projectCount: p.projectCount,
        weight: p.weight,
        flag: p.flag,
        normName: seg.normName,
        segmentKey: seg.segmentKey,
      },
    })),
    id: `${series.seriesType}-${seg.segmentKey}-${idx}`,
  }));
}

export function ZTrendChart({
  group,
  showPlan,
  showFact,
  height = 320,
  title,
  testId,
}: Props) {
  const options = useMemo<Highcharts.Options>(() => {
    const traces: Highcharts.SeriesOptionsType[] = [];
    if (showPlan) traces.push(...seriesToTraces(group.plan, 'Solid', 'План'));
    if (showFact) traces.push(...seriesToTraces(group.fact, 'Dash', 'Факт'));

    return {
      chart: { type: 'line', height, zooming: { type: 'x' } },
      title: { text: title ?? group.groupName, align: 'left', style: { fontSize: '14px' } },
      xAxis: { type: 'datetime', title: { text: 'Дата версии' } },
      yAxis: {
        title: { text: 'Z-оценка' },
        plotLines: [
          {
            value: 0,
            color: '#666',
            width: 1,
            dashStyle: 'ShortDash',
            zIndex: 3,
            label: { text: 'Z = 0', align: 'right' },
          },
        ],
      },
      tooltip: {
        shared: false,
        formatter() {
          const pt = this as Highcharts.Point & {
            custom?: { projectCount?: number; weight?: number; flag?: string; normName?: string };
          };
          const custom = pt.custom ?? (pt.options as { custom?: typeof pt.custom })?.custom;
          const date = Highcharts.dateFormat('%d.%m.%Y', Number(this.x));
          const z = this.y == null ? '—' : Number(this.y).toFixed(2);
          return [
            `<b>${this.series.name}</b>`,
            `Дата: ${date}`,
            `Z: ${z}`,
            custom?.normName ? `Норма: ${custom.normName}` : null,
            custom?.projectCount != null ? `Проектов: ${custom.projectCount}` : null,
            custom?.weight != null ? `Вес: ${Number(custom.weight).toFixed(1)}` : null,
            custom?.flag && custom.flag !== 'ok' ? `Флаг: ${custom.flag}` : null,
          ]
            .filter(Boolean)
            .join('<br/>');
        },
      },
      legend: { enabled: false },
      credits: { enabled: false },
      series: traces,
    };
  }, [group, showPlan, showFact, height, title]);

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
