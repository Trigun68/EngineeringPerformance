import { useMemo } from 'react';
import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import type { VersionTrendPoint } from '../reportCalc';
import { ACCENT_FACT, ACCENT_PLAN, DIRECTION_FACT, DIRECTION_PLAN, fmt } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

interface VersionTrendProps {
  points: VersionTrendPoint[];
}

export function VersionTrend({ points }: VersionTrendProps) {
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
        lineColor: wpColors.cardBorder,
      },
      yAxis: {
        title: { text: 'Z-оценка' },
        plotLines: [
          { value: 0, color: wpColors.muted, dashStyle: 'ShortDash', width: 1, label: { text: 'норма (Z = 0)' } },
        ],
      },
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
  );

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={0.5} sx={{ mb: 2 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            Динамика Z-оценки по версиям
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Средневзвешенная Z-оценка по версиям плана (хронологически) и основной версии факта.
          </Typography>
        </Stack>

        {chronological.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Недостаточно данных для построения динамики.
          </Typography>
        ) : (
          <>
            <HighchartsReact highcharts={Highcharts} options={options} />

            <Grid container spacing={2} sx={{ borderTop: 1, borderColor: 'divider', pt: 2, mt: 1 }}>
              {chronological.map((p) => (
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
