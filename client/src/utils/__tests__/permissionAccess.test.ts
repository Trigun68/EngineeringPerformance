import { describe, expect, it } from 'vitest';
import {
  canAccessSettings,
  canReadPermission,
  getFirstAccessiblePath,
  getPermissionLevel,
  isMainNavItemVisible,
  isSuperAdmin,
  MAIN_NAV_ITEMS,
} from '../permissionAccess';

describe('permissionAccess', () => {
  const observerPermissions = {
    Projects: 1,
    References: 1,
    PhysicalVolumes: 1,
  } as const;

  it('grants SuperAdmin full edit on any object', () => {
    expect(canReadPermission({}, 'Dashboard', 'SuperAdmin')).toBe(true);
    expect(getPermissionLevel({}, 'ForecastSummary', 'SuperAdmin')).toBe(2);
  });

  it('treats legacy Administrator and roleId 1 as SuperAdmin', () => {
    expect(isSuperAdmin('Administrator')).toBe(true);
    expect(isSuperAdmin(undefined, 1)).toBe(true);
    expect(canReadPermission({}, 'Reports', 'Administrator')).toBe(true);
    expect(canReadPermission({}, 'Planning', undefined, 1)).toBe(true);
  });

  it('allows Planning read for Admin without SuperAdmin bypass', () => {
    expect(canReadPermission({ Planning: 2 }, 'Planning', 'Admin')).toBe(true);
    expect(canReadPermission({ Planning: 2 }, 'ForecastSummary', 'Admin')).toBe(false);
  });

  it('hides nav items without read permission', () => {
    const byPath = Object.fromEntries(MAIN_NAV_ITEMS.map((item) => [item.path, item]));
    expect(isMainNavItemVisible(byPath['/'], observerPermissions, 'Engineer')).toBe(false);
    expect(isMainNavItemVisible(byPath['/projects'], observerPermissions, 'Engineer')).toBe(true);
    expect(isMainNavItemVisible(byPath['/analytics'], observerPermissions, 'Engineer')).toBe(false);
    expect(isMainNavItemVisible(byPath['/references'], observerPermissions, 'Engineer')).toBe(true);
    expect(isMainNavItemVisible(byPath['/settings'], observerPermissions, 'Engineer')).toBe(false);
  });

  it('shows settings when Users permission exists', () => {
    expect(canAccessSettings({ Users: 1 }, 'Admin')).toBe(true);
    expect(canAccessSettings({ SettingsPage: 1 }, 'Admin')).toBe(true);
    expect(canAccessSettings({ Projects: 1 }, 'Engineer')).toBe(false);
  });

  it('returns first accessible main nav path', () => {
    expect(getFirstAccessiblePath(observerPermissions, 'Engineer')).toBe('/projects');
    expect(getFirstAccessiblePath({ Dashboard: 1 }, 'Engineer')).toBe('/');
  });
});
