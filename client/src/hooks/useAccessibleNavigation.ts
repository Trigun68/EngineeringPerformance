import { useMemo } from 'react';
import { useAuthStore } from '../store/useAuthStore';
import {
  getFirstAccessiblePath,
  isMainNavItemVisible,
  MAIN_NAV_ITEMS,
  type MainNavItemConfig,
} from '../utils/permissionAccess';

export interface VisibleNavItem extends MainNavItemConfig {
  label: string;
}

export function useAccessibleNavigation() {
  const user = useAuthStore((s) => s.user);
  const permissions = user?.permissions;
  const roleName = user?.roleName;
  const roleId = user?.roleId;

  const visibleItems = useMemo(
    () =>
      MAIN_NAV_ITEMS.filter((item) => isMainNavItemVisible(item, permissions, roleName, roleId)).map(
        (item) => ({ ...item }),
      ),
    [permissions, roleName, roleId],
  );

  const firstAccessiblePath = useMemo(
    () => getFirstAccessiblePath(permissions, roleName, roleId),
    [permissions, roleName, roleId],
  );

  return { visibleItems, firstAccessiblePath };
}

export function useFirstAccessiblePath(): string | null {
  return useAccessibleNavigation().firstAccessiblePath;
}
