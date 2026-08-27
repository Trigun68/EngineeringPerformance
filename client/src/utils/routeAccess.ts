import { REFERENCE_ROUTES } from '../pages/references/referenceConfig';
import type { AccessLevel } from '../types/auth';
import {
  canReadPermission,
  canAccessSettings,
  isSuperAdmin,
  isMainNavItemVisible,
  MAIN_NAV_ITEMS,
} from './permissionAccess';

/** Проверяет, доступен ли маршрут текущему пользователю. */
export function isRouteAccessible(
  pathname: string,
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
  roleId?: number | null,
): boolean {
  if (!pathname || pathname === '/') {
    return canReadPermission(permissions, 'Dashboard', roleName, roleId);
  }

  if (pathname === '/projects' || pathname.startsWith('/projects/')) {
    return canReadPermission(permissions, 'Projects', roleName, roleId);
  }

  if (pathname.startsWith('/references')) {
    if (!canReadPermission(permissions, 'References', roleName, roleId)) return false;
    const segment = pathname.replace(/^\/references\/?/, '').split('/')[0];
    if (!segment) return true;
    const route = REFERENCE_ROUTES.find((r) => r.path === segment);
    if (!route) return false;
    return canReadPermission(permissions, route.permission, roleName, roleId);
  }

  if (pathname.startsWith('/settings')) {
    if (!canAccessSettings(permissions, roleName, roleId)) return false;
    if (pathname.startsWith('/settings/users')) {
      return canReadPermission(permissions, 'Users', roleName, roleId) || isSuperAdmin(roleName, roleId);
    }
    if (pathname.startsWith('/settings/access-rights')) {
      return isSuperAdmin(roleName, roleId);
    }
    if (pathname.startsWith('/settings/system')) {
      return isSuperAdmin(roleName, roleId);
    }
    return true;
  }

  if (pathname.startsWith('/analytics')) {
    return canReadPermission(permissions, 'GroupTrend', roleName, roleId);
  }

  if (pathname.startsWith('/help')) {
    return canReadPermission(permissions, 'HelpPage', roleName, roleId);
  }

  if (pathname.startsWith('/admin') || pathname.startsWith('/monitoring')) {
    return isSuperAdmin(roleName, roleId);
  }

  const mainItem = MAIN_NAV_ITEMS.find((item) => item.path === pathname);
  if (mainItem) {
    return isMainNavItemVisible(mainItem, permissions, roleName, roleId);
  }

  return false;
}
