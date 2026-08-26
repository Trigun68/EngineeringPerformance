import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ZScale } from '../ZScale';

describe('ZScale', () => {
  it('renders bold −3…+3 ticks', () => {
    render(
      <ZScale
        planZ={-0.4}
        factZ={0.2}
        planPercentile="60%"
        factPercentile="40%"
        normSourcesUsed={1}
      />,
    );
    const ticks = screen.getAllByTestId('report-z-tick');
    expect(ticks).toHaveLength(7);
    expect(ticks.map((t) => t.textContent)).toEqual(['−3', '−2', '−1', '0', '+1', '+2', '+3']);
    ticks.forEach((tick) => {
      expect(tick.parentElement?.className).toMatch(/font-semibold/);
    });
  });
});
