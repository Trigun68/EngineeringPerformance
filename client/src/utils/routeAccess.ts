import { REFERENCE_ROUTES } from '../pages/references/referenceConfig';
import type { AccessLevel } from '../types/auth';
import {
  canReadPermission,
  canAccessSettings,
  isAdministrator,
  isMainNavItemVisible,
  MAIN_NAV_ITEMS,
} from './permissionAccess';

/** Проверяет, доступен ли маршрут текущему пользователю. */
export function isRouteAccessible(
  pathname: string,
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
): boolean {
  if (!pathname || pathname === '/') {
    return canReadPermission(permissions, 'Dashboard', roleName);
  }

  if (pathname === '/projects' || pathname.startsWith('/projects/')) {
    return canReadPermission(permissions, 'Projects', roleName);
  }

  if (pathname.startsWith('/references')) {
    if (!canReadPermission(permissions, 'References', roleName)) return false;
    const segment = pathname.replace(/^\/references\/?/, '').split('/')[0];
    if (!segment) return true;
    const route = REFERENCE_ROUTES.find((r) => r.path === segment);
    if (!route) return false;
    return canReadPermission(permissions, route.permission, roleName);
  }

  if (pathname.startsWith('/settings')) {
    if (!canAccessSettings(permissions, roleName)) return false;
    if (pathname.startsWith('/settings/users')) {
      return canReadPermission(permissions, 'Users', roleName) || isAdministrator(roleName);
    }
    if (pathname.startsWith('/settings/access-rights')) {
      return isAdministrator(roleName);
    }
    if (pathname.startsWith('/settings/system')) {
      return isAdministrator(roleName);
    }
    return true;
  }

  if (pathname.startsWith('/help')) {
    return canReadPermission(permissions, 'HelpPage', roleName);
  }

  if (pathname.startsWith('/admin') || pathname.startsWith('/monitoring')) {
    return isAdministrator(roleName);
  }

  const mainItem = MAIN_NAV_ITEMS.find((item) => item.path === pathname);
  if (mainItem) {
    return isMainNavItemVisible(mainItem, permissions, roleName);
  }

  return false;
}
