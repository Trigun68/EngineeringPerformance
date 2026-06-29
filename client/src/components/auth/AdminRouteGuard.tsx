import type { ReactNode } from 'react';
import { Alert, Box } from '@mui/material';
import { Navigate } from 'react-router-dom';
import { useFirstAccessiblePath } from '../../hooks/useAccessibleNavigation';
import { useAuthStore } from '../../store/useAuthStore';
import { isAdministrator } from '../../utils/permissionAccess';

export function AdminRouteGuard({ children }: { children: ReactNode }) {
  const roleName = useAuthStore((s) => s.user?.roleName);
  const fallbackPath = useFirstAccessiblePath();

  if (!isAdministrator(roleName)) {
    if (fallbackPath) {
      return <Navigate to={fallbackPath} replace />;
    }
    return (
      <Box sx={{ p: 2 }}>
        <Alert severity="warning">Раздел «Мониторинг» доступен только администратору.</Alert>
      </Box>
    );
  }

  return children;
}
