import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { Box, Button, Stack } from '@mui/material';
import { wpColors } from '../../theme/wpColors';
import { useAuthStore } from '../../store/useAuthStore';
import { useFirstAccessiblePath } from '../../hooks/useAccessibleNavigation';
import { usePermission } from '../../hooks/usePermission';
import { canReadPermission } from '../../utils/permissionAccess';
import { REFERENCE_ROUTES } from './referenceConfig';

const subNavSx = {
  textTransform: 'none' as const,
  borderColor: wpColors.cardBorder,
  color: wpColors.tabInactive,
  '&.active': {
    bgcolor: wpColors.accent,
    color: '#FFFFFF',
    borderColor: wpColors.accent,
    '&:hover': { bgcolor: wpColors.accentDark, borderColor: wpColors.accentDark },
  },
  '&:hover': { bgcolor: wpColors.accentHover, borderColor: wpColors.accent },
};

export function ReferencesLayout() {
  const user = useAuthStore((s) => s.user);
  const fallbackPath = useFirstAccessiblePath();
  const { canRead: canReadReferences } = usePermission('References');

  const visibleRoutes = REFERENCE_ROUTES.filter((route) =>
    canReadPermission(user?.permissions, route.permission, user?.roleName),
  );

  const location = useLocation();
  const currentPath = location.pathname.split('/').pop() ?? '';
  const currentAllowed = visibleRoutes.some((route) => route.path === currentPath);

  if (!canReadReferences) {
    return fallbackPath ? <Navigate to={fallbackPath} replace /> : null;
  }

  if (visibleRoutes.length === 0) {
    return fallbackPath ? <Navigate to={fallbackPath} replace /> : null;
  }

  if (location.pathname === '/references' || !currentAllowed) {
    return <Navigate to={`/references/${visibleRoutes[0].path}`} replace />;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0, flex: 1 }}>
      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap', gap: 1 }}>
        {visibleRoutes.map((route) => (
          <Button
            key={route.path}
            component={NavLink}
            to={`/references/${route.path}`}
            variant="outlined"
            size="small"
            sx={subNavSx}
          >
            {route.title}
          </Button>
        ))}
      </Stack>
      <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <Outlet />
      </Box>
    </Box>
  );
}
