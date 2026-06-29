import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { Box, Button, Stack } from '@mui/material';
import { wpColors } from '../../theme/wpColors';
import { useAuthStore } from '../../store/useAuthStore';
import { usePermission } from '../../hooks/usePermission';

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
  const isAdmin = user?.roleName === 'Administrator';
  const canReadAccessRights = isAdmin;

  const tabs = [
    { to: '/settings/users', label: 'Пользователи', visible: canReadUsers },
    { to: '/settings/access-rights', label: 'Права пользователей', visible: canReadAccessRights },
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
      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap', gap: 1, flexShrink: 0 }}>
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
        <Outlet />
      </Box>
    </Box>
  );
}
