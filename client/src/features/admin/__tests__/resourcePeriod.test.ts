import { describe, expect, it } from 'vitest';
import { DEFAULT_RESOURCE_PERIOD_MINUTES, resourceRefreshMs } from '../resourcePeriod';

describe('resourcePeriod', () => {
  it('uses 10s refresh for the default hour and 60s for longer windows', () => {
    expect(DEFAULT_RESOURCE_PERIOD_MINUTES).toBe(60);
    expect(resourceRefreshMs(60)).toBe(10_000);
    expect(resourceRefreshMs(1440)).toBe(60_000);
  });
});
