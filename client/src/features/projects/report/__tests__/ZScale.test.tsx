import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ZScale } from '../ZScale';

describe('ZScale', () => {
  it('renders ticks left-to-right +3…−3 (worse → better)', () => {
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
    expect(ticks.map((t) => t.textContent)).toEqual(['+3', '+2', '+1', '0', '−1', '−2', '−3']);
    ticks.forEach((tick) => {
      expect(tick.parentElement?.className).toMatch(/font-semibold/);
    });
  });

  it('places a worse (higher) Z marker to the left of a better one', () => {
    render(
      <ZScale
        planZ={-1.5}
        factZ={1.5}
        planPercentile="—"
        factPercentile="—"
        normSourcesUsed={1}
      />,
    );
    const plan = screen.getByTestId('report-z-marker-plan');
    const fact = screen.getByTestId('report-z-marker-fact');
    const planLeft = Number.parseFloat(plan.style.left);
    const factLeft = Number.parseFloat(fact.style.left);
    expect(factLeft).toBeLessThan(50);
    expect(planLeft).toBeGreaterThan(50);
  });
});
