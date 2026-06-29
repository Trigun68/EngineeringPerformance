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

  const visibleItems = useMemo(
    () =>
      MAIN_NAV_ITEMS.filter((item) => isMainNavItemVisible(item, permissions, roleName)).map(
        (item) => ({ ...item }),
      ),
    [permissions, roleName],
  );

  const firstAccessiblePath = useMemo(
    () => getFirstAccessiblePath(permissions, roleName),
    [permissions, roleName],
  );

  return { visibleItems, firstAccessiblePath };
}

export function useFirstAccessiblePath(): string | null {
  return useAccessibleNavigation().firstAccessiblePath;
}
