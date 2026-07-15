import { describe, expect, it } from 'vitest';
import {
  canAccessSettings,
  canReadPermission,
  getFirstAccessiblePath,
  isMainNavItemVisible,
  MAIN_NAV_ITEMS,
} from '../permissionAccess';

describe('permissionAccess', () => {
  const observerPermissions = {
    Projects: 1,
    References: 1,
    PhysicalVolumes: 1,
  } as const;

  it('grants admin full edit on any object', () => {
    expect(canReadPermission({}, 'Dashboard', 'Administrator')).toBe(true);
  });

  it('hides nav items without read permission', () => {
    const byPath = Object.fromEntries(MAIN_NAV_ITEMS.map((item) => [item.path, item]));
    expect(isMainNavItemVisible(byPath['/'], observerPermissions, 'Observer')).toBe(false);
    expect(isMainNavItemVisible(byPath['/projects'], observerPermissions, 'Observer')).toBe(true);
    expect(isMainNavItemVisible(byPath['/analytics/trends'], observerPermissions, 'Observer')).toBe(false);
    expect(isMainNavItemVisible(byPath['/references'], observerPermissions, 'Observer')).toBe(true);
    expect(isMainNavItemVisible(byPath['/settings/users'], observerPermissions, 'Observer')).toBe(false);
  });

  it('shows settings when Users permission exists', () => {
    expect(canAccessSettings({ Users: 1 }, 'ProjectManager')).toBe(true);
    expect(canAccessSettings({ SettingsPage: 1 }, 'ProjectManager')).toBe(true);
    expect(canAccessSettings({ Projects: 1 }, 'Observer')).toBe(false);
  });

  it('returns first accessible main nav path', () => {
    expect(getFirstAccessiblePath(observerPermissions, 'Observer')).toBe('/projects');
    expect(getFirstAccessiblePath({ Dashboard: 1 }, 'Observer')).toBe('/');
  });
});
