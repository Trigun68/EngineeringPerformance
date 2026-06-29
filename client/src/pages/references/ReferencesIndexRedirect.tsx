import { Navigate } from 'react-router-dom';
import { Alert, Box } from '@mui/material';
import { useAuthStore } from '../../store/useAuthStore';
import { useFirstAccessiblePath } from '../../hooks/useAccessibleNavigation';
import { canReadPermission } from '../../utils/permissionAccess';
import { REFERENCE_ROUTES } from './referenceConfig';

/** Редирект на первую доступную вкладку справочников. */
export function ReferencesIndexRedirect() {
  const user = useAuthStore((s) => s.user);
  const fallbackPath = useFirstAccessiblePath();

  const firstRoute = REFERENCE_ROUTES.find((route) =>
    canReadPermission(user?.permissions, route.permission, user?.roleName),
  );

  if (firstRoute) {
    return <Navigate to={firstRoute.path} replace />;
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
