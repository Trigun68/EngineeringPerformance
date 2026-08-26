import type { ReactNode } from 'react';
import { Alert, Box } from '@mui/material';
import { Navigate } from 'react-router-dom';
import { useFirstAccessiblePath } from '../../hooks/useAccessibleNavigation';
import { useAuthStore } from '../../store/useAuthStore';
import { isSuperAdmin } from '../../utils/permissionAccess';

export function AdminRouteGuard({ children }: { children: ReactNode }) {
  const roleName = useAuthStore((s) => s.user?.roleName);
  const roleId = useAuthStore((s) => s.user?.roleId);
  const fallbackPath = useFirstAccessiblePath();

  if (!isSuperAdmin(roleName, roleId)) {
    if (fallbackPath) {
      return <Navigate to={fallbackPath} replace />;
    }
    return (
      <Box sx={{ p: 2 }}>
        <Alert severity="warning">Раздел «Администрирование» доступен только администратору.</Alert>
      </Box>
    );
  }

  return children;
}
