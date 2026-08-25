import { useMemo } from 'react';
import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import type { VersionTrendPoint } from '../reportCalc';
import { fmt, interpretationColor } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

interface VersionTrendProps {
  points: VersionTrendPoint[];
}

export function VersionTrend({ points }: VersionTrendProps) {
  const withScore = points.filter((p) => p.weightedZScore !== null);

  const coloredOptions = useMemo<Highcharts.Options>(
    () => ({
      chart: { height: 220, style: { fontFamily: 'inherit' } },
      title: { text: undefined },
      credits: { enabled: false },
      xAxis: {
        categories: withScore.map((p) => p.versionName),
        lineColor: wpColors.cardBorder,
      },
      yAxis: {
        title: { text: 'Z-оценка' },
        plotLines: [
          { value: 0, color: wpColors.muted, dashStyle: 'ShortDash', width: 1, label: { text: 'норма (Z = 0)' } },
        ],
      },
      legend: { enabled: false },
      tooltip: {
        formatter() {
          const point = this as unknown as { x: number; y: number };
          const p = withScore[point.x];
          return `<b>${p?.versionName ?? ''}</b><br/>Z = ${fmt(point.y, 2)}<br/>${fmt(p?.totalHours ?? null, 0)} ч`;
        },
      },
      plotOptions: {
        line: {
          marker: { radius: 5, symbol: 'circle', lineWidth: 2, lineColor: '#FFFFFF' },
          dataLabels: { enabled: true, format: '{point.y:.2f}' },
        },
      },
      series: [
        {
          type: 'line',
          name: 'Средневзвешенная Z-оценка',
          color: wpColors.accent,
          data: withScore.map((p) => ({
            y: p.weightedZScore ?? undefined,
            marker: { fillColor: interpretationColor(p.weightedZScore) },
          })),
        },
      ],
    }),
    [withScore],
  );

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={0.5} sx={{ mb: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            Динамика Z-оценки по версиям
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Средневзвешенная Z-оценка проекта от версии плана до текущей версии факта.
          </Typography>
        </Stack>

        {withScore.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Недостаточно данных для построения динамики.
          </Typography>
        ) : (
          <>
            <HighchartsReact highcharts={Highcharts} options={coloredOptions} />

            <Grid container spacing={2} sx={{ borderTop: 1, borderColor: 'divider', pt: 2, mt: 1 }}>
              {points.map((p) => (
                <Grid item xs={6} sm={3} key={p.versionId}>
                  <Box>
                    <Typography variant="caption" color="text.secondary" display="block">
                      {p.date ?? '—'}
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {p.versionName}
                    </Typography>
                    <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.muted }}>
                      {fmt(p.totalHours, 0)} ч
                    </Typography>
                  </Box>
                </Grid>
              ))}
            </Grid>
          </>
        )}
      </CardContent>
    </Card>
  );
}
