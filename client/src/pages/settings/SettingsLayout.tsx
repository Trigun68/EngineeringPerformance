import { Suspense } from 'react';
import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { Box, Button, Stack } from '@mui/material';
import { RouteContentFallback } from '../../components/WorkingOverlay/RouteContentFallback';
import { wpColors } from '../../theme/wpColors';
import { useAuthStore } from '../../store/useAuthStore';
import { usePermission } from '../../hooks/usePermission';
import { isSuperAdmin } from '../../utils/permissionAccess';

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

export function SettingsLayout() {
  const user = useAuthStore((s) => s.user);
  const { canRead: canReadUsers } = usePermission('Users');
  const isAdmin = isSuperAdmin(user?.roleName, user?.roleId);
  const canReadAccessRights = isAdmin;

  const tabs = [
    { to: '/settings/users', label: 'Пользователи', visible: canReadUsers },
    { to: '/settings/access-rights', label: 'Права пользователей', visible: canReadAccessRights },
    { to: '/settings/system', label: 'Общие настройки', visible: isAdmin },
  ].filter((tab) => tab.visible);

  const location = useLocation();
  if (location.pathname === '/settings' && tabs.length > 0) {
    return <Navigate to={tabs[0].to} replace />;
  }

  if (tabs.length === 0) {
    return null;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, height: '100%' }}>
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
        {tabs.map((tab) => (
          <Button
            key={tab.to}
            component={NavLink}
            to={tab.to}
            variant="outlined"
            size="small"
            sx={subNavSx}
          >
            {tab.label}
          </Button>
        ))}
      </Stack>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        }}
      >
        <Suspense fallback={<RouteContentFallback />}>
          <Outlet />
        </Suspense>
      </Box>
    </Box>
  );
}
