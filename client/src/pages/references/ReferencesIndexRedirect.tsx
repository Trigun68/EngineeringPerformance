import { useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { Alert, Box, CircularProgress } from '@mui/material';
import { getUserPreferences } from '../../api/userPreferencesApi';
import { useAuthStore } from '../../store/useAuthStore';
import { useFirstAccessiblePath } from '../../hooks/useAccessibleNavigation';
import { canReadPermission } from '../../utils/permissionAccess';
import { REFERENCE_ROUTES } from './referenceConfig';

export const REFERENCES_LAST_TAB_KEY = 'references.lastTab';

function resolveVisibleRoutes(
  permissions: Record<string, 0 | 1 | 2> | undefined,
  roleName: string | undefined,
) {
  return REFERENCE_ROUTES.filter((route) =>
    canReadPermission(permissions, route.permission, roleName),
  );
}

/** Редирект на последнюю или первую доступную вкладку справочников. */
export function ReferencesIndexRedirect() {
  const user = useAuthStore((s) => s.user);
  const fallbackPath = useFirstAccessiblePath();
  const [targetPath, setTargetPath] = useState<string | null | undefined>(undefined);

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        const visibleRoutes = resolveVisibleRoutes(user?.permissions, user?.roleName);
        const persisted = prefs[REFERENCES_LAST_TAB_KEY];
        const persistedRoute = visibleRoutes.find((route) => route.path === persisted);
        setTargetPath(persistedRoute?.path ?? visibleRoutes[0]?.path ?? null);
      })
      .catch(() => {
        if (cancelled) return;
        const visibleRoutes = resolveVisibleRoutes(user?.permissions, user?.roleName);
        setTargetPath(visibleRoutes[0]?.path ?? null);
      });
    return () => {
      cancelled = true;
    };
  }, [user?.permissions, user?.roleName]);

  if (targetPath === undefined) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress size={28} />
      </Box>
    );
  }

  if (targetPath) {
    return <Navigate to={`/references/${targetPath}`} replace />;
  }

  if (fallbackPath) {
    return <Navigate to={fallbackPath} replace />;
  }

  return (
    <Box sx={{ p: 2 }}>
      <Alert severity="warning">Нет доступных справочников для вашей роли.</Alert>
    </Box>
  );
}
