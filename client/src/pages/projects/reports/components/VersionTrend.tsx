import { useMemo } from 'react';
import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import type { VersionTrendPoint } from '../reportCalc';
<<<<<<< HEAD
import { fmt, interpretationColor } from '../reportCalc';
=======
import { ACCENT_FACT, ACCENT_PLAN, DIRECTION_FACT, DIRECTION_PLAN, fmt } from '../reportCalc';
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
import { wpColors } from '../../../../theme/wpColors';

interface VersionTrendProps {
  points: VersionTrendPoint[];
}

export function VersionTrend({ points }: VersionTrendProps) {
<<<<<<< HEAD
  const withScore = points.filter((p) => p.weightedZScore !== null);

  const coloredOptions = useMemo<Highcharts.Options>(
    () => ({
      chart: { height: 220, style: { fontFamily: 'inherit' } },
      title: { text: undefined },
      credits: { enabled: false },
      xAxis: {
        categories: withScore.map((p) => p.versionName),
=======
  // Единая хронологическая шкала X по дате создания версии — план и факт совмещены на ней.
  const chronological = useMemo(
    () => [...points].filter((p) => p.weightedZScore !== null).sort((a, b) => a.sortDate - b.sortDate),
    [points],
  );
  const categories = chronological.map((p) => p.versionName);

  const planIndices = useMemo(
    () =>
      chronological
        .map((p, index) => (p.direction === DIRECTION_PLAN ? index : null))
        .filter((i): i is number => i !== null),
    [chronological],
  );
  const factIndices = useMemo(
    () =>
      chronological
        .map((p, index) => (p.direction === DIRECTION_FACT ? index : null))
        .filter((i): i is number => i !== null),
    [chronological],
  );

  const options = useMemo<Highcharts.Options>(
    () => ({
      chart: { height: 240, style: { fontFamily: 'inherit' } },
      title: { text: undefined },
      credits: { enabled: false },
      xAxis: {
        categories,
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
        lineColor: wpColors.cardBorder,
      },
      yAxis: {
        title: { text: 'Z-оценка' },
        plotLines: [
          { value: 0, color: wpColors.muted, dashStyle: 'ShortDash', width: 1, label: { text: 'норма (Z = 0)' } },
        ],
      },
<<<<<<< HEAD
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
=======
      legend: { enabled: true },
      tooltip: {
        formatter() {
          const point = this as unknown as { x: number; y: number };
          const p = chronological[point.x];
          return `<b>${p?.versionName ?? ''}</b><br/>Z = ${fmt(point.y, 2)}<br/>${fmt(p?.totalHours ?? null, 0)} ч`;
        },
      },
      series: [
        {
          type: 'line',
          name: 'План (по версиям)',
          color: ACCENT_PLAN,
          lineWidth: 2.25,
          marker: { radius: 3.5, symbol: 'circle', fillColor: '#FFFFFF', lineWidth: 2, lineColor: ACCENT_PLAN },
          data: planIndices.map((i) => [i, chronological[i].weightedZScore ?? undefined]),
        },
        {
          // Гало под основным маркером факта — акцент по мокапу ReportRedesignMockup.
          type: 'scatter',
          name: 'Факт (гало)',
          color: ACCENT_FACT,
          marker: { radius: 9, symbol: 'circle', fillColor: Highcharts.color(ACCENT_FACT).setOpacity(0.16).get() },
          showInLegend: false,
          enableMouseTracking: false,
          dataLabels: { enabled: false },
          data: factIndices.map((i) => [i, chronological[i].weightedZScore ?? undefined]),
        },
        {
          type: 'line',
          name: 'Факт (основная версия)',
          color: ACCENT_FACT,
          marker: { radius: 6, symbol: 'circle', lineWidth: 2.5, lineColor: '#FFFFFF' },
          lineWidth: 0,
          data: factIndices.map((i) => [i, chronological[i].weightedZScore ?? undefined]),
        },
      ],
    }),
    [categories, chronological, planIndices, factIndices],
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
  );

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={0.5} sx={{ mb: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            Динамика Z-оценки по версиям
          </Typography>
          <Typography variant="body2" color="text.secondary">
<<<<<<< HEAD
            Средневзвешенная Z-оценка проекта от версии плана до текущей версии факта.
          </Typography>
        </Stack>

        {withScore.length === 0 ? (
=======
            Средневзвешенная Z-оценка по версиям плана (хронологически) и основной версии факта.
          </Typography>
        </Stack>

        {chronological.length === 0 ? (
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
          <Typography variant="body2" color="text.secondary">
            Недостаточно данных для построения динамики.
          </Typography>
        ) : (
          <>
<<<<<<< HEAD
            <HighchartsReact highcharts={Highcharts} options={coloredOptions} />

            <Grid container spacing={2} sx={{ borderTop: 1, borderColor: 'divider', pt: 2, mt: 1 }}>
              {points.map((p) => (
=======
            <HighchartsReact highcharts={Highcharts} options={options} />

            <Grid container spacing={2} sx={{ borderTop: 1, borderColor: 'divider', pt: 2, mt: 1 }}>
              {chronological.map((p) => (
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
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
