import { useMemo } from 'react';
import Highcharts from 'highcharts';
import HighchartsMoreImport from 'highcharts/highcharts-more';
import HighchartsReact from 'highcharts-react-official';
import { Box } from '@mui/material';

// Тип пакета описывает default-экспорт как пространство имён Highcharts, но во
// время выполнения это UMD-фабрика, инициализирующая тип серии `waterfall`.
const HighchartsMore = HighchartsMoreImport as unknown as (hc: typeof Highcharts) => void;
if (typeof HighchartsMore === 'function') {
  HighchartsMore(Highcharts);
}

export interface WaterfallStep {
  name: string;
  /** Вклад фактора в изменение показателя (может быть отрицательным). */
  value: number;
}

interface Props {
  startLabel: string;
  startValue: number;
  endLabel: string;
  steps: WaterfallStep[];
  height?: number;
  title?: string;
  /** Знак вклада, который считается «улучшением» и подсвечивается зелёным. Остальные — красным. */
  goodDirection?: 'positive' | 'negative';
  valueFormatter?: (value: number) => string;
  testId?: string;
}

const NEUTRAL_COLOR = '#8899aa';
const POSITIVE_COLOR = '#2e7d32';
const NEGATIVE_COLOR = '#c62828';

function defaultFormatter(v: number): string {
  const sign = v > 0 ? '+' : '';
  return `${sign}${v.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/** Водопад «Было → вклад факторов → Стало» — объясняет причину изменения среднего показателя за период. */
export function FactorWaterfallChart({
  startLabel,
  startValue,
  endLabel,
  steps,
  height = 320,
  title,
  goodDirection = 'positive',
  valueFormatter = defaultFormatter,
  testId,
}: Props) {
  const options = useMemo<Highcharts.Options>(() => {
    const data: Highcharts.PointOptionsObject[] = [
      { name: startLabel, y: startValue, color: NEUTRAL_COLOR },
      ...steps.map((s) => ({
        name: s.name,
        y: s.value,
        color:
          s.value === 0
            ? NEUTRAL_COLOR
            : (s.value > 0) === (goodDirection === 'positive')
              ? POSITIVE_COLOR
              : NEGATIVE_COLOR,
      })),
      { name: endLabel, isSum: true, color: NEUTRAL_COLOR },
    ];

    return {
      chart: { type: 'waterfall', height },
      title: title ? { text: title, align: 'left', style: { fontSize: '14px' } } : { text: undefined },
      xAxis: { type: 'category' },
      yAxis: { title: { text: undefined } },
      legend: { enabled: false },
      credits: { enabled: false },
      tooltip: {
        formatter() {
          const p = this as Highcharts.Point;
          return `<b>${p.name}</b><br/>${valueFormatter(Number(p.y ?? 0))}`;
        },
      },
      plotOptions: {
        waterfall: {
          dataLabels: {
            enabled: true,
            formatter() {
              const p = this as unknown as Highcharts.Point;
              return valueFormatter(Number(p.y ?? 0));
            },
          },
        },
      },
      series: [
        {
          type: 'waterfall',
          name: title ?? 'Разбор изменения',
          data,
          borderColor: '#ffffff00',
        },
      ],
    };
  }, [startLabel, startValue, endLabel, steps, height, title, goodDirection, valueFormatter]);

  return (
    <Box sx={{ width: '100%', minHeight: height }} data-testid={testId}>
      <HighchartsReact highcharts={Highcharts} options={options} />
    </Box>
  );
}
