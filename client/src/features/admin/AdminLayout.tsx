import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { Box, Button, Stack } from '@mui/material';
import { wpColors } from '../../theme/wpColors';

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

const ADMIN_TABS = [
  { to: '/admin/logs', label: 'Логи системы' },
  { to: '/admin/health', label: 'Состояние сервисов' },
  { to: '/admin/audit', label: 'Аудит настроек' },
  { to: '/admin/user-actions', label: 'Журнал действий' },
] as const;

export function AdminLayout() {
  const location = useLocation();

  if (location.pathname === '/admin' && ADMIN_TABS.length > 0) {
    return <Navigate to={ADMIN_TABS[0].to} replace />;
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
        {ADMIN_TABS.map((tab) => (
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
      <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Outlet />
      </Box>
    </Box>
  );
}
