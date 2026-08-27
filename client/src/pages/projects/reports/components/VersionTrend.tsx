import { useMemo } from 'react';
import { Box, Stack, Typography } from '@mui/material';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import type { VersionTrendPoint } from '../reportCalc';
import {
  ACCENT_FACT,
  ACCENT_PLAN,
  DIRECTION_FACT,
  DIRECTION_PLAN,
  formatTrendPointLabel,
  formatTrendScenarioTooltipHtml,
  MUTED_COLOR,
} from '../reportCalc';
import { PRIMARY_TEXT, ReportMockupCard, SectionLabel } from './reportChrome';

interface VersionTrendProps {
  points: VersionTrendPoint[];
}

export function VersionTrend({ points }: VersionTrendProps) {
  const chronological = useMemo(
    () => [...points].filter((p) => p.weightedZScore !== null).sort((a, b) => a.sortDate - b.sortDate || a.versionId - b.versionId),
    [points],
  );
  const categories = chronological.map((p) => p.date ?? p.versionName);

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
      chart: {
        height: 240,
        spacingTop: 28,
        style: { fontFamily: 'inherit' },
        backgroundColor: 'transparent',
      },
      title: { text: undefined },
      credits: { enabled: false },
      xAxis: {
        categories,
        lineColor: '#E4E9ED',
        tickLength: 0,
        labels: {
          style: { color: MUTED_COLOR, fontSize: '10px' },
          formatter() {
            const index = this.pos;
            const p = chronological[index];
            const label = String(this.value ?? '');
            return p?.direction === DIRECTION_FACT
              ? `<span style="font-weight:700;color:${PRIMARY_TEXT}">${label}</span>`
              : label;
          },
        },
      },
      yAxis: {
        title: { text: undefined },
        gridLineColor: '#EEF1F4',
        labels: { style: { color: MUTED_COLOR, fontSize: '9.5px' } },
        plotLines: [{ value: 0, color: '#E4E9ED', dashStyle: 'ShortDash', width: 1 }],
      },
      legend: { enabled: false },
      tooltip: {
        useHTML: true,
        formatter() {
          const point = this as unknown as { x: number };
          const p = chronological[point.x];
          return p ? formatTrendScenarioTooltipHtml(p) : undefined;
        },
      },
      plotOptions: {
        series: {
          dataLabels: {
            enabled: true,
            allowOverlap: true,
            crop: false,
            overflow: 'allow',
            y: -10,
            style: {
              fontSize: '9px',
              fontWeight: '600',
              color: MUTED_COLOR,
              textOutline: 'none',
            },
            formatter() {
              const point = this as unknown as { x: number };
              const p = chronological[point.x];
              return p ? formatTrendPointLabel(p.weightedZScore, p.totalHours) : '';
            },
          },
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
    <ReportMockupCard testId="report-v1-version-trend">
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1 }} flexWrap="wrap" useFlexGap>
        <SectionLabel>Динамика Z-оценки по версиям</SectionLabel>
        <Stack direction="row" spacing={2.5}>
          <Stack direction="row" alignItems="center" spacing={0.7}>
            <Box sx={{ width: 20, height: 2.5, bgcolor: ACCENT_PLAN, borderRadius: 2 }} />
            <Typography sx={{ fontSize: 11.5, color: MUTED_COLOR, fontWeight: 600 }}>План (по версиям)</Typography>
          </Stack>
          <Stack direction="row" alignItems="center" spacing={0.7}>
            <Box
              sx={{
                width: 10,
                height: 10,
                borderRadius: '50%',
                bgcolor: ACCENT_FACT,
                border: '2px solid #FFFFFF',
                boxShadow: `0 0 0 1.5px ${ACCENT_FACT}`,
              }}
            />
            <Typography sx={{ fontSize: 11.5, color: MUTED_COLOR, fontWeight: 600 }}>Факт (основная версия)</Typography>
          </Stack>
        </Stack>
      </Stack>

      {chronological.length === 0 ? (
        <Typography variant="body2" color="text.secondary" data-testid="report-version-trend-empty">
          Недостаточно данных для построения динамики.
        </Typography>
      ) : (
        <>
          <HighchartsReact highcharts={Highcharts} options={options} />
          <Typography sx={{ fontSize: 11, color: MUTED_COLOR, mt: 0.5 }}>
            Версии отсортированы по дате создания (хронологически) — план показывает эволюцию по всем версиям плана,
            факт — единственная точка по основной версии факта.
          </Typography>
        </>
      )}
    </ReportMockupCard>
  );
}
