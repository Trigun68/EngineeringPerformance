import type { ColumnState } from 'ag-grid-community';
import { getUserPreferences, putUserPreferences } from '../api/userPreferencesApi';
import { getUserSetting, putUserSetting } from '../api/userSettingsApi';

/** Кэш layout в рамках вкладки браузера — чтобы смена sub-tab не ждала API. */
const layoutMemoryCache = new Map<string, ColumnState[]>();

export function layoutPrefKey(gridId: string): string {
  return `grid.${gridId}.layout`;
}

export function filterPrefKey(gridId: string): string {
  return `grid.${gridId}.filter`;
}

/** Убираем flex из состояния: иначе старый default flex:1 залипает и ломает resize. */
export function stripFlexFromLayout(state: ColumnState[]): ColumnState[] {
  return state.map(({ flex: _flex, ...rest }) => rest);
}

export function getCachedGridLayout(gridId: string): ColumnState[] | undefined {
  return layoutMemoryCache.get(gridId);
}

export function setCachedGridLayout(gridId: string, state: ColumnState[]): void {
  layoutMemoryCache.set(gridId, stripFlexFromLayout(state));
}

/** Сохраняет layout в memory + preferences (+ legacy userSettings). */
export async function persistGridLayout(
  gridId: string,
  state: ColumnState[],
  options?: { legacyUserSetting?: boolean },
): Promise<void> {
  const layout = stripFlexFromLayout(state);
  setCachedGridLayout(gridId, layout);
  const payload = JSON.stringify(layout);
  await putUserPreferences({ [layoutPrefKey(gridId)]: payload });
  if (options?.legacyUserSetting !== false) {
    await putUserSetting(`grid-${gridId}`, payload);
  }
}

/**
 * Загружает layout: сначала memory (мгновенно при смене вкладок),
 * иначе preferences / legacy setting.
 */
export async function loadGridLayout(gridId: string): Promise<ColumnState[] | null> {
  const cached = getCachedGridLayout(gridId);
  if (cached && cached.length > 0) {
    return cached;
  }

  let raw: string | undefined;
  try {
    const prefs = await getUserPreferences();
    raw = prefs[layoutPrefKey(gridId)];
  } catch {
    raw = undefined;
  }
  if (!raw) {
    raw = (await getUserSetting(`grid-${gridId}`)) ?? undefined;
  }
  if (!raw) return null;

  try {
    const state = stripFlexFromLayout(JSON.parse(raw) as ColumnState[]);
    setCachedGridLayout(gridId, state);
    return state;
  } catch {
    return null;
  }
}

/** Только для тестов. */
export function clearGridLayoutMemoryCache(): void {
  layoutMemoryCache.clear();
}
