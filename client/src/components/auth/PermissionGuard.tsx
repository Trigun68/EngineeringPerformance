import type { ReactNode } from 'react';
import { Alert, Box } from '@mui/material';
import { Navigate } from 'react-router-dom';
import { usePermission } from '../../hooks/usePermission';
import { useFirstAccessiblePath } from '../../hooks/useAccessibleNavigation';
import { useAuthStore } from '../../store/useAuthStore';
import { canAccessSettings } from '../../utils/permissionAccess';

interface PermissionGuardProps {
  objectName: string;
  children: ReactNode;
}

/** Защита маршрута: без Read — редирект на первый доступный раздел. */
export function PermissionGuard({ objectName, children }: PermissionGuardProps) {
  const { canRead } = usePermission(objectName);
  const fallbackPath = useFirstAccessiblePath();

  if (!canRead) {
    if (fallbackPath) {
      return <Navigate to={fallbackPath} replace />;
    }
    return (
      <Box sx={{ p: 2 }}>
        <Alert severity="warning">Нет доступных разделов для вашей роли.</Alert>
      </Box>
    );
  }

  return children;
}

/** Защита раздела настроек: SettingsPage или Users. */
export function SettingsGuard({ children }: { children: ReactNode }) {
  const user = useAuthStore((s) => s.user);
  const fallbackPath = useFirstAccessiblePath();

  if (!canAccessSettings(user?.permissions, user?.roleName, user?.roleId)) {
    if (fallbackPath) {
      return <Navigate to={fallbackPath} replace />;
    }
    return (
      <Box sx={{ p: 2 }}>
        <Alert severity="warning">Нет доступа к настройкам.</Alert>
      </Box>
    );
  }

  return children;
}
