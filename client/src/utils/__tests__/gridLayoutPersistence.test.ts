import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ColumnState } from 'ag-grid-community';
import {
  clearGridLayoutMemoryCache,
  filterPrefKey,
  getCachedGridLayout,
  layoutPrefKey,
  loadGridLayout,
  persistGridLayout,
  setCachedGridLayout,
  stripFlexFromLayout,
} from '../gridLayoutPersistence';

vi.mock('../../api/userPreferencesApi', () => ({
  getUserPreferences: vi.fn(async () => ({})),
  putUserPreferences: vi.fn(async () => undefined),
}));

vi.mock('../../api/userSettingsApi', () => ({
  getUserSetting: vi.fn(async () => null),
  putUserSetting: vi.fn(async () => undefined),
}));

import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import { getUserSetting, putUserSetting } from '../../api/userSettingsApi';

describe('gridLayoutPersistence', () => {
  beforeEach(() => {
    clearGridLayoutMemoryCache();
    vi.clearAllMocks();
  });

  it('strips flex from column state', () => {
    const state = stripFlexFromLayout([
      { colId: 'a', width: 120, flex: 1 },
      { colId: 'b', width: 80 },
    ] as ColumnState[]);
    expect(state[0]).toEqual({ colId: 'a', width: 120 });
    expect(state[1]).toEqual({ colId: 'b', width: 80 });
  });

  it('persists to memory and preferences', async () => {
    await persistGridLayout('forecast-summary-1', [
      { colId: 'baseHours', width: 220, flex: 1 },
    ] as ColumnState[]);

    expect(getCachedGridLayout('forecast-summary-1')?.[0]).toMatchObject({
      colId: 'baseHours',
      width: 220,
    });
    expect(getCachedGridLayout('forecast-summary-1')?.[0]).not.toHaveProperty('flex');
    expect(putUserPreferences).toHaveBeenCalledWith({
      [layoutPrefKey('forecast-summary-1')]: expect.stringContaining('"width":220'),
    });
    expect(putUserSetting).toHaveBeenCalled();
  });

  it('loadGridLayout prefers memory cache over API', async () => {
    setCachedGridLayout('g1', [{ colId: 'x', width: 333 }]);
    const loaded = await loadGridLayout('g1');
    expect(loaded?.[0]).toMatchObject({ colId: 'x', width: 333 });
    expect(getUserPreferences).not.toHaveBeenCalled();
  });

  it('loadGridLayout falls back to preferences then legacy setting', async () => {
    vi.mocked(getUserPreferences).mockResolvedValueOnce({
      [layoutPrefKey('g2')]: JSON.stringify([{ colId: 'y', width: 150, flex: 1 }]),
    });
    const loaded = await loadGridLayout('g2');
    expect(loaded?.[0]).toMatchObject({ colId: 'y', width: 150 });
    expect(loaded?.[0]).not.toHaveProperty('flex');
    expect(getUserSetting).not.toHaveBeenCalled();
  });

  it('scopes layout keys per project and falls back to global', async () => {
    expect(layoutPrefKey('forecast-summary', 12)).toBe('grid.forecast-summary.p12.layout');
    expect(filterPrefKey('forecast-summary', 12)).toBe('grid.forecast-summary.p12.filter');

    await persistGridLayout(
      'forecast-summary',
      [{ colId: 'code', width: 90 } as ColumnState],
      { projectId: 12 },
    );
    expect(putUserSetting).not.toHaveBeenCalled();
    expect(putUserPreferences).toHaveBeenCalledWith({
      [layoutPrefKey('forecast-summary', 12)]: expect.stringContaining('"width":90'),
    });
    expect(getCachedGridLayout('forecast-summary', 12)?.[0]).toMatchObject({ colId: 'code', width: 90 });
    expect(getCachedGridLayout('forecast-summary')).toBeUndefined();
  });

  it('loadGridLayout falls back from project key to global grid key', async () => {
    vi.mocked(getUserPreferences).mockResolvedValueOnce({
      [layoutPrefKey('forecast-detail')]: JSON.stringify([{ colId: 'name', width: 200 }]),
    });
    const loaded = await loadGridLayout('forecast-detail', 8);
    expect(loaded?.[0]).toMatchObject({ colId: 'name', width: 200 });
    expect(getCachedGridLayout('forecast-detail', 8)?.[0]).toMatchObject({ colId: 'name', width: 200 });
  });
});
