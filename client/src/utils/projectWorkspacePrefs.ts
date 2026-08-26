import type { AccessLevel } from '../types/auth';
import { LAST_ROUTE_PREF_KEY } from '../hooks/usePersistLastRoute';
import { isRouteAccessible } from './routeAccess';

/** Ключи user preferences для рабочего места проекта. */

export const LAST_PROJECT_PREF_KEY = 'projects.lastProjectId';
/** Локальный ключ (localStorage) — без API при сворачивании шапки. */
export const PROJECT_HEADER_EXPANDED_STORAGE_KEY = 'ep.projects.headerExpanded';

export function readHeaderExpanded(): boolean {
  try {
    return localStorage.getItem(PROJECT_HEADER_EXPANDED_STORAGE_KEY) !== 'false';
  } catch {
    return true;
  }
}

export function writeHeaderExpanded(expanded: boolean): void {
  try {
    localStorage.setItem(PROJECT_HEADER_EXPANDED_STORAGE_KEY, expanded ? 'true' : 'false');
  } catch {
    // private mode / quota — игнорируем
  }
}

export function planVersionPrefKey(projectId: number): string {
  return `projects.${projectId}.planVersionId`;
}

export function factVersionPrefKey(projectId: number): string {
  return `projects.${projectId}.factVersionId`;
}

export function compareVersionAPrefKey(projectId: number): string {
  return `projects.${projectId}.compareVersionAId`;
}

export function compareVersionBPrefKey(projectId: number): string {
  return `projects.${projectId}.compareVersionBId`;
}

export function compareViewModePrefKey(projectId: number): string {
  return `projects.${projectId}.compareViewMode`;
}

export function compareShowDiffPrefKey(projectId: number): string {
  return `projects.${projectId}.compareShowDiff`;
}

export function compareScenarioPrefKey(projectId: number): string {
  return `projects.${projectId}.compareScenario`;
}

export function reportDarkPrefKey(projectId: number): string {
  return `projects.${projectId}.report.dark`;
}

export function reportCollapsedPrefKey(projectId: number): string {
  return `projects.${projectId}.report.collapsed`;
}

export function parsePrefId(raw: string | undefined): number | null {
  if (!raw) return null;
  const id = Number.parseInt(raw, 10);
  return Number.isFinite(id) && id > 0 ? id : null;
}

/** Выбор стартового маршрута: lastRoute, либо последний проект. */
export function resolvePreferredPath(
  prefs: Record<string, string>,
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
  fallbackPath: string | null,
): string | null {
  const lastRoute = prefs[LAST_ROUTE_PREF_KEY];
  if (lastRoute && isRouteAccessible(lastRoute, permissions, roleName)) {
    if (lastRoute === '/projects') {
      const lastProjectId = parsePrefId(prefs[LAST_PROJECT_PREF_KEY]);
      if (lastProjectId != null) {
        const projectPath = `/projects/${lastProjectId}`;
        if (isRouteAccessible(projectPath, permissions, roleName)) {
          return projectPath;
        }
      }
    }
    return lastRoute;
  }

  const lastProjectId = parsePrefId(prefs[LAST_PROJECT_PREF_KEY]);
  if (lastProjectId != null) {
    const projectPath = `/projects/${lastProjectId}`;
    if (isRouteAccessible(projectPath, permissions, roleName)) {
      return projectPath;
    }
  }

  return fallbackPath;
}
