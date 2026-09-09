import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { useEffect, useMemo, useRef } from 'react';
import { Box, Button, Stack } from '@mui/material';
import { putUserPreferences } from '../../api/userPreferencesApi';
import { wpColors } from '../../theme/wpColors';
import { useAuthStore } from '../../store/useAuthStore';
import { useFirstAccessiblePath } from '../../hooks/useAccessibleNavigation';
import { usePermission } from '../../hooks/usePermission';
import { canReadPermission } from '../../utils/permissionAccess';
import { REFERENCE_ROUTES } from './referenceConfig';
import { REFERENCES_LAST_TAB_KEY } from './ReferencesIndexRedirect';

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

  const visibleRoutes = useMemo(
    () => REFERENCE_ROUTES.filter((route) =>
      !route.hidden
      && canReadPermission(user?.permissions, route.permission, user?.roleName),
    ),
    [user?.permissions, user?.roleName],
  );

  const location = useLocation();
  const isReferencesIndex =
    location.pathname === '/references' || location.pathname === '/references/';
  const currentSegment = isReferencesIndex ? '' : (location.pathname.split('/').pop() ?? '');
  const currentAllowed =
    isReferencesIndex || visibleRoutes.some((route) => route.path === currentSegment);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!currentAllowed || !currentSegment) return undefined;
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    saveTimerRef.current = setTimeout(() => {
      void putUserPreferences({ [REFERENCES_LAST_TAB_KEY]: currentSegment });
    }, 400);
    return () => {
      if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    };
  }, [currentAllowed, currentSegment]);

  if (!canReadReferences) {
    return fallbackPath ? <Navigate to={fallbackPath} replace /> : null;
  }

  if (visibleRoutes.length === 0) {
    return fallbackPath ? <Navigate to={fallbackPath} replace /> : null;
  }

  if (!currentAllowed && !isReferencesIndex) {
    const target = visibleRoutes[0]?.path;
    if (!target) return null;
    return <Navigate to={`/references/${target}`} replace />;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <Stack
        direction="row"
        spacing={1}
        sx={{
          mb: 2,
          flexWrap: 'nowrap',
          gap: 1,
          flexShrink: 0,
          overflowX: 'auto',
          pb: 0.5,
          WebkitOverflowScrolling: 'touch',
          '& .MuiButton-root': { flexShrink: 0, minHeight: 40 },
        }}
      >
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
      <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Outlet />
      </Box>
    </Box>
  );
}
