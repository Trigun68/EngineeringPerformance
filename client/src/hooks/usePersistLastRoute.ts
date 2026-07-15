import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { putUserPreferences } from '../api/userPreferencesApi';
import { useAuthStore } from '../store/useAuthStore';
import { isRouteAccessible } from '../utils/routeAccess';

export const LAST_ROUTE_PREF_KEY = 'app.lastRoute';

/** Сохраняет последний посещённый маршрут в user preferences. */
export function usePersistLastRoute(): void {
  const location = useLocation();
  const user = useAuthStore((s) => s.user);

  useEffect(() => {
    if (!user) return;
    const pathname = location.pathname;
    if (!isRouteAccessible(pathname, user.permissions, user.roleName)) return;
    void putUserPreferences({ [LAST_ROUTE_PREF_KEY]: pathname });
  }, [location.pathname, user]);
}
