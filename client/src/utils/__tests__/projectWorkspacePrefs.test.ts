import { describe, expect, it } from 'vitest';
import { LAST_ROUTE_PREF_KEY } from '../../hooks/usePersistLastRoute';
import {
  LAST_PROJECT_PREF_KEY,
  compareScenarioPrefKey,
  compareShowDiffPrefKey,
  compareVersionAPrefKey,
  compareVersionBPrefKey,
  compareViewModePrefKey,
  parsePrefId,
  planVersionPrefKey,
  readHeaderExpanded,
  reportCollapsedPrefKey,
  reportDarkPrefKey,
  resolvePreferredPath,
  writeHeaderExpanded,
} from '../projectWorkspacePrefs';

describe('projectWorkspacePrefs', () => {
  it('parsePrefId accepts positive integers only', () => {
    expect(parsePrefId('42')).toBe(42);
    expect(parsePrefId('0')).toBeNull();
    expect(parsePrefId('-1')).toBeNull();
    expect(parsePrefId('x')).toBeNull();
    expect(parsePrefId(undefined)).toBeNull();
  });

  it('planVersionPrefKey is scoped by project', () => {
    expect(planVersionPrefKey(7)).toBe('projects.7.planVersionId');
  });

  it('compare and report pref keys are scoped by project', () => {
    expect(compareVersionAPrefKey(4)).toBe('projects.4.compareVersionAId');
    expect(compareVersionBPrefKey(4)).toBe('projects.4.compareVersionBId');
    expect(compareViewModePrefKey(4)).toBe('projects.4.compareViewMode');
    expect(compareShowDiffPrefKey(4)).toBe('projects.4.compareShowDiff');
    expect(compareScenarioPrefKey(4)).toBe('projects.4.compareScenario');
    expect(reportDarkPrefKey(4)).toBe('projects.4.report.dark');
    expect(reportCollapsedPrefKey(4)).toBe('projects.4.report.collapsed');
  });

  it('resolvePreferredPath prefers last project workspace over bare /projects', () => {
    const path = resolvePreferredPath(
      {
        [LAST_ROUTE_PREF_KEY]: '/projects',
        [LAST_PROJECT_PREF_KEY]: '15',
      },
      { Projects: 1 },
      'Admin',
      '/help',
    );
    expect(path).toBe('/projects/15');
  });

  it('resolvePreferredPath keeps concrete project route', () => {
    const path = resolvePreferredPath(
      {
        [LAST_ROUTE_PREF_KEY]: '/projects/9',
        [LAST_PROJECT_PREF_KEY]: '15',
      },
      { Projects: 1 },
      'Admin',
      '/help',
    );
    expect(path).toBe('/projects/9');
  });

  it('resolvePreferredPath falls back to last project when route missing', () => {
    const path = resolvePreferredPath(
      { [LAST_PROJECT_PREF_KEY]: '3' },
      { Projects: 1 },
      'Admin',
      '/help',
    );
    expect(path).toBe('/projects/3');
  });

  it('headerExpanded persists in localStorage without server prefs', () => {
    localStorage.removeItem('ep.projects.headerExpanded');
    expect(readHeaderExpanded()).toBe(true);
    writeHeaderExpanded(false);
    expect(localStorage.getItem('ep.projects.headerExpanded')).toBe('false');
    expect(readHeaderExpanded()).toBe(false);
    writeHeaderExpanded(true);
    expect(readHeaderExpanded()).toBe(true);
  });
});
