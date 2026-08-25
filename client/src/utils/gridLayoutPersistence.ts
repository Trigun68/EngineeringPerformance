import type { ColumnState } from 'ag-grid-community';
import { getUserPreferences, putUserPreferences } from '../api/userPreferencesApi';
import { getUserSetting, putUserSetting } from '../api/userSettingsApi';

/** Кэш layout в рамках вкладки браузера — чтобы смена sub-tab не ждала API. */
const layoutMemoryCache = new Map<string, ColumnState[]>();

export function layoutCacheKey(gridId: string, projectId?: number): string {
  return projectId != null ? `${gridId}::p${projectId}` : gridId;
}

export function layoutPrefKey(gridId: string, projectId?: number): string {
  return projectId != null ? `grid.${gridId}.p${projectId}.layout` : `grid.${gridId}.layout`;
}

export function filterPrefKey(gridId: string, projectId?: number): string {
  return projectId != null ? `grid.${gridId}.p${projectId}.filter` : `grid.${gridId}.filter`;
}

/** Убираем flex из состояния: иначе старый default flex:1 залипает и ломает resize. */
export function stripFlexFromLayout(state: ColumnState[]): ColumnState[] {
  return state.map(({ flex: _flex, ...rest }) => rest);
}

export function getCachedGridLayout(gridId: string, projectId?: number): ColumnState[] | undefined {
  return layoutMemoryCache.get(layoutCacheKey(gridId, projectId));
}

export function setCachedGridLayout(gridId: string, state: ColumnState[], projectId?: number): void {
  layoutMemoryCache.set(layoutCacheKey(gridId, projectId), stripFlexFromLayout(state));
}

/** Сохраняет layout в memory + preferences (+ legacy userSettings). */
export async function persistGridLayout(
  gridId: string,
  state: ColumnState[],
  options?: { legacyUserSetting?: boolean; projectId?: number },
): Promise<void> {
  const layout = stripFlexFromLayout(state);
  const projectId = options?.projectId;
  setCachedGridLayout(gridId, layout, projectId);
  const payload = JSON.stringify(layout);
  await putUserPreferences({ [layoutPrefKey(gridId, projectId)]: payload });
  if (options?.legacyUserSetting !== false && projectId == null) {
    await putUserSetting(`grid-${gridId}`, payload);
  }
}

/**
 * Загружает layout: сначала memory (мгновенно при смене вкладок),
 * иначе preferences (scoped → global) / legacy setting.
 */
export async function loadGridLayout(
  gridId: string,
  projectId?: number,
): Promise<ColumnState[] | null> {
  const cached = getCachedGridLayout(gridId, projectId);
  if (cached && cached.length > 0) {
    return cached;
  }

  let raw: string | undefined;
  try {
    const prefs = await getUserPreferences();
    raw = prefs[layoutPrefKey(gridId, projectId)];
    if (!raw && projectId != null) {
      raw = prefs[layoutPrefKey(gridId)];
    }
  } catch {
    raw = undefined;
  }
  if (!raw) {
    raw = (await getUserSetting(`grid-${gridId}`)) ?? undefined;
  }
  if (!raw) return null;

  try {
    const state = stripFlexFromLayout(JSON.parse(raw) as ColumnState[]);
    setCachedGridLayout(gridId, state, projectId);
    return state;
  } catch {
    return null;
  }
}

/** Только для тестов. */
export function clearGridLayoutMemoryCache(): void {
  layoutMemoryCache.clear();
}
