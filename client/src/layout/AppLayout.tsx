import { useEffect, useMemo } from 'react';
import {
  Box,
  Divider,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
} from '@mui/material';
import {
  NavLink,
  Outlet,
  useLocation,
  useMatch,
} from 'react-router-dom';
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined';
import FolderOutlinedIcon from '@mui/icons-material/FolderOutlined';
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined';
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined';
import MonitorHeartOutlinedIcon from '@mui/icons-material/MonitorHeartOutlined';
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import { UserMenu } from '../components/auth/UserMenu';
import { useAccessibleNavigation } from '../hooks/useAccessibleNavigation';
import { isAdministrator } from '../utils/permissionAccess';
import { wpColors } from '../theme/wpColors';
import { useAuthStore } from '../store/useAuthStore';
import { useDictionaryStore } from '../store/useDictionaryStore';

const NAV_ICONS: Record<string, React.ReactNode> = {
  '/': <HomeOutlinedIcon fontSize="small" />,
  '/projects': <FolderOutlinedIcon fontSize="small" />,
  '/references': <MenuBookOutlinedIcon fontSize="small" />,
  '/settings/users': <SettingsOutlinedIcon fontSize="small" />,
  '/help': <HelpOutlineIcon fontSize="small" />,
  '/monitoring': <MonitorHeartOutlinedIcon fontSize="small" />,
  '/admin': <AdminPanelSettingsOutlinedIcon fontSize="small" />,
};

function resolvePageTitle(pathname: string): string {
  if (pathname.startsWith('/projects/') && pathname !== '/projects') return 'Проекты';
  if (pathname.startsWith('/references')) return 'Справочники';
  if (pathname.startsWith('/settings')) return 'Настройки';
  if (pathname.startsWith('/admin')) {
    if (pathname.startsWith('/admin/user-actions')) return 'Журнал действий';
    return 'Администрирование';
  }
  if (pathname.startsWith('/monitoring')) return 'Мониторинг';
  if (pathname.startsWith('/help')) return 'Справка';
  if (pathname === '/projects') return 'Проекты';
  if (pathname === '/') return 'Главная';
  return 'Главная';
}

const navLinkSx = {
  color: wpColors.sidebarText,
  borderRadius: 0,
  py: 1.5,
  px: 2.5,
  '& .MuiListItemIcon-root': { color: 'inherit', minWidth: 36 },
  '&:hover': { bgcolor: wpColors.sidebarHover, color: wpColors.sidebarTextActive },
  '&.active': {
    bgcolor: wpColors.accent,
    color: wpColors.sidebarTextActive,
    '&:hover': { bgcolor: wpColors.accentDark },
  },
};

export function AppLayout() {
  const user = useAuthStore((s) => s.user);
  const fetchDictionaries = useDictionaryStore((s) => s.fetchDictionaries);
  const location = useLocation();
  const projectMatch = useMatch('/projects/:projectId');
  const { visibleItems } = useAccessibleNavigation();
  const isAdmin = isAdministrator(user?.roleName);

  useEffect(() => {
    if (user) {
      void fetchDictionaries();
    }
  }, [user, fetchDictionaries]);

  const navItems = useMemo(
    () =>
      visibleItems.map((item) => ({
        ...item,
        icon: NAV_ICONS[item.path] ?? <HomeOutlinedIcon fontSize="small" />,
      })),
    [visibleItems],
  );

  const pageTitle = projectMatch ? 'Проекты' : resolvePageTitle(location.pathname);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: wpColors.pageBg }}>
      <Box
        component="nav"
        sx={{
          width: 280,
          flexShrink: 0,
          bgcolor: wpColors.sidebar,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Box sx={{ bgcolor: wpColors.sidebarHeader, px: 2.5, py: 2 }}>
          <Typography
            sx={{
              color: '#ECF0F1',
              fontSize: 18,
              fontWeight: 600,
              lineHeight: 1.3,
            }}
          >
            Engineering Performance
          </Typography>
        </Box>

        <List sx={{ flex: 1, py: 0 }}>
          {navItems.map((item) => (
            <ListItemButton
              key={`${item.path}-${item.label}`}
              component={NavLink}
              to={item.path}
              end={item.end}
              sx={navLinkSx}
            >
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14 }} />
            </ListItemButton>
          ))}
          {isAdmin && (
            <>
              <ListItemButton component={NavLink} to="/admin" sx={navLinkSx}>
                <ListItemIcon>{NAV_ICONS['/admin']}</ListItemIcon>
                <ListItemText primary="Администрирование" primaryTypographyProps={{ fontSize: 14 }} />
              </ListItemButton>
              <ListItemButton component={NavLink} to="/monitoring" sx={navLinkSx}>
                <ListItemIcon>{NAV_ICONS['/monitoring']}</ListItemIcon>
                <ListItemText primary="Мониторинг" primaryTypographyProps={{ fontSize: 14 }} />
              </ListItemButton>
            </>
          )}
        </List>

        <Divider sx={{ borderColor: '#34495E' }} />
      </Box>

      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <Box
          sx={{
            bgcolor: wpColors.cardBg,
            borderBottom: `1px solid ${wpColors.cardBorder}`,
            px: 2.5,
            py: 2,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <Typography sx={{ color: wpColors.heading, fontSize: 20, fontWeight: 600 }}>
            {pageTitle}
          </Typography>
          {user && <UserMenu />}
        </Box>

        <Box sx={{ flex: 1, p: 2, overflow: 'hidden', display: 'flex', flexDirection: 'column', minHeight: 0 }}>
          <Box
            sx={{
              bgcolor: wpColors.cardBg,
              border: `1px solid ${wpColors.cardBorder}`,
              borderRadius: 2,
              p: 2.5,
              flex: 1,
              minHeight: 0,
              display: 'flex',
              flexDirection: 'column',
              boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
              overflow: 'hidden',
            }}
          >
            <Outlet />
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
