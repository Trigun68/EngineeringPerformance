import { describe, expect, it } from 'vitest';
import { rankBestAndWorst } from '../TopPerformersPanel';

describe('rankBestAndWorst', () => {
  it('does not exclude best leaves from worst when there are five or fewer items', () => {
    const scored = [
      { id: 'a', z: -1.2 },
      { id: 'b', z: -0.4 },
      { id: 'c', z: 0.1 },
      { id: 'd', z: 0.8 },
      { id: 'e', z: 1.5 },
    ];

    const { best, worst } = rankBestAndWorst(scored, (row) => row.z, 5);

    expect(best.map((row) => row.id)).toEqual(['a', 'b', 'c', 'd', 'e']);
    expect(worst.map((row) => row.id)).toEqual(['e', 'd', 'c', 'b', 'a']);
    expect(worst).toHaveLength(5);
  });

  it('keeps overlapping names when ranking more items than topCount', () => {
    const scored = [
      { id: 'best', z: -2 },
      { id: 'mid1', z: -0.2 },
      { id: 'mid2', z: 0 },
      { id: 'mid3', z: 0.3 },
      { id: 'worst', z: 2 },
      { id: 'extra', z: 0.1 },
    ];

    const { best, worst } = rankBestAndWorst(scored, (row) => row.z, 5);

    expect(best[0].id).toBe('best');
    expect(worst[0].id).toBe('worst');
    expect(best).toHaveLength(5);
    expect(worst).toHaveLength(5);
  });
});
