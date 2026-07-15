import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { NormBandChart } from './NormBandChart';

vi.mock('highcharts/highcharts-more', () => ({ default: vi.fn() }));
vi.mock('highcharts', () => ({ default: {} }));
vi.mock('highcharts-react-official', () => ({
  default: ({ options }: { options: { series: unknown[] } }) => (
    <div data-testid="chart" data-series-count={options.series.length} />
  ),
}));

describe('NormBandChart', () => {
  it('renders line only when sigma is zero', () => {
    render(
      <NormBandChart
        band={[
          { volume: 0, center: 100, upper: 100, lower: 100 },
          { volume: 10, center: 90, upper: 90, lower: 90 },
        ]}
        deviationRatio={0}
      />,
    );
    expect(screen.getByTestId('chart').getAttribute('data-series-count')).toBe('1');
  });

  it('renders band and line when sigma is non-zero', () => {
    render(
      <NormBandChart
        band={[
          { volume: 0, center: 100, upper: 105, lower: 95 },
          { volume: 10, center: 90, upper: 95, lower: 85 },
        ]}
        deviationRatio={0.1}
      />,
    );
    expect(screen.getByTestId('chart').getAttribute('data-series-count')).toBe('2');
  });
});
