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
    expect(isMainNavItemVisible(MAIN_NAV_ITEMS[0], observerPermissions, 'Observer')).toBe(false);
    expect(isMainNavItemVisible(MAIN_NAV_ITEMS[1], observerPermissions, 'Observer')).toBe(true);
    expect(isMainNavItemVisible(MAIN_NAV_ITEMS[2], observerPermissions, 'Observer')).toBe(true);
    expect(isMainNavItemVisible(MAIN_NAV_ITEMS[3], observerPermissions, 'Observer')).toBe(false);
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
