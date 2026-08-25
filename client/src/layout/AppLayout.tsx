import { Suspense, useCallback, useEffect, useMemo, useState } from 'react';
import apiClient, { refreshSessionHeartbeat } from '../services/apiClient';
import {
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
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
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import { UserMenu } from '../components/auth/UserMenu';
import { PanelBusyBar } from '../components/WorkingOverlay/PanelBusyBar';
import { RouteContentFallback } from '../components/WorkingOverlay/RouteContentFallback';
import { useAccessibleNavigation } from '../hooks/useAccessibleNavigation';
import { usePersistLastRoute } from '../hooks/usePersistLastRoute';
import { isSuperAdmin } from '../utils/permissionAccess';
import { wpColors } from '../theme/wpColors';
import { useAuthStore } from '../store/useAuthStore';
import { useDictionaryStore } from '../store/useDictionaryStore';

const SIDEBAR_WIDTH = 280;

/** Запас относительно минимально допустимого срока access-токена (5 мин). */
const DEFAULT_HEARTBEAT_MS = 10 * 60 * 1000;

const NAV_ICONS: Record<string, React.ReactNode> = {
  '/': <HomeOutlinedIcon fontSize="small" />,
  '/projects': <FolderOutlinedIcon fontSize="small" />,
  '/analytics': <MenuBookOutlinedIcon fontSize="small" />,
  '/references': <MenuBookOutlinedIcon fontSize="small" />,
  '/settings': <SettingsOutlinedIcon fontSize="small" />,
  '/help': <HelpOutlineIcon fontSize="small" />,
  '/admin': <AdminPanelSettingsOutlinedIcon fontSize="small" />,
};

function resolvePageTitle(pathname: string): string {
  if (pathname.startsWith('/projects/') && pathname !== '/projects') return 'Проекты';
  if (pathname.startsWith('/analytics')) return 'Аналитика';
  if (pathname.startsWith('/references')) return 'Справочники';
  if (pathname.startsWith('/settings')) return 'Настройки';
  if (pathname.startsWith('/admin')) {
    if (pathname.startsWith('/admin/user-actions')) return 'Журнал действий';
    return 'Администрирование';
  }
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
  minHeight: 44,
  '& .MuiListItemIcon-root': { color: 'inherit', minWidth: 36 },
  '&:hover': { bgcolor: wpColors.sidebarHover, color: wpColors.sidebarTextActive },
  '&.active': {
    bgcolor: wpColors.accent,
    color: wpColors.sidebarTextActive,
    '&:hover': { bgcolor: wpColors.accentDark },
  },
};

type NavItem = {
  path: string;
  label: string;
  end?: boolean;
  icon: React.ReactNode;
};

function AppNavList({
  navItems,
  isAdmin,
  onNavigate,
}: {
  navItems: NavItem[];
  isAdmin: boolean;
  onNavigate?: () => void;
}) {
  return (
    <>
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

      <List sx={{ flex: 1, py: 0 }} data-testid="app-nav-list">
        {navItems.map((item) => (
          <ListItemButton
            key={`${item.path}-${item.label}`}
            component={NavLink}
            to={item.path}
            end={item.end}
            sx={navLinkSx}
            onClick={onNavigate}
          >
            <ListItemIcon>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14 }} />
          </ListItemButton>
        ))}
        {isAdmin && (
          <ListItemButton component={NavLink} to="/admin" sx={navLinkSx} onClick={onNavigate}>
            <ListItemIcon>{NAV_ICONS['/admin']}</ListItemIcon>
            <ListItemText primary="Администрирование" primaryTypographyProps={{ fontSize: 14 }} />
          </ListItemButton>
        )}
      </List>

      <Divider sx={{ borderColor: '#34495E' }} />
    </>
  );
}

export function AppLayout() {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [navOpen, setNavOpen] = useState(false);
  const user = useAuthStore((s) => s.user);
  const fetchDictionaries = useDictionaryStore((s) => s.fetchDictionaries);
  const dictionariesLoading = useDictionaryStore((s) => s.isLoading);
  const location = useLocation();
  const projectMatch = useMatch('/projects/:projectId');
  const { visibleItems } = useAccessibleNavigation();
  const isAdmin = isSuperAdmin(user?.roleName, user?.roleId);

  usePersistLastRoute();

  const closeNav = useCallback(() => setNavOpen(false), []);

  // При смене маршрута на mobile закрываем меню.
  useEffect(() => {
    if (isMobile) setNavOpen(false);
  }, [location.pathname, isMobile]);

  useEffect(() => {
    if (!user) return undefined;

    let intervalId: ReturnType<typeof setInterval> | undefined;
    let cancelled = false;

    const startHeartbeat = (intervalMs: number) => {
      if (cancelled) return;
      intervalId = window.setInterval(() => {
        void refreshSessionHeartbeat();
      }, intervalMs);
    };

    // Настройки сессии доступны только администраторам; остальным ролям запрос вернёт 403
    // и покажет ложную ошибку «Недостаточно прав», поэтому используем безопасный интервал.
    if (!isAdmin) {
      startHeartbeat(DEFAULT_HEARTBEAT_MS);
    } else {
      void apiClient
        .get<{ sessionExpirationMinutes: number }>('/admin/system-settings')
        .then(({ data }) => startHeartbeat(Math.floor(data.sessionExpirationMinutes * 60 * 1000 * 0.8)))
        .catch(() => startHeartbeat(DEFAULT_HEARTBEAT_MS));
    }

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, [user, isAdmin]);

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
  const isAnalyticsRoute = location.pathname.startsWith('/analytics');

  const navBody = (
    <Box
      sx={{
        width: SIDEBAR_WIDTH,
        height: '100%',
        bgcolor: wpColors.sidebar,
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <AppNavList navItems={navItems} isAdmin={isAdmin} onNavigate={isMobile ? closeNav : undefined} />
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', height: '100%', overflow: 'hidden', bgcolor: wpColors.pageBg }}>
      {!isMobile && (
        <Box component="nav" sx={{ width: SIDEBAR_WIDTH, flexShrink: 0, display: 'flex' }}>
          {navBody}
        </Box>
      )}

      {isMobile && (
        <Drawer
          variant="temporary"
          open={navOpen}
          onClose={closeNav}
          ModalProps={{ keepMounted: true }}
          sx={{
            '& .MuiDrawer-paper': {
              width: SIDEBAR_WIDTH,
              boxSizing: 'border-box',
              bgcolor: wpColors.sidebar,
            },
          }}
        >
          {navBody}
        </Drawer>
      )}

      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, overflow: 'hidden' }}>
        <Box
          sx={{
            bgcolor: wpColors.cardBg,
            borderBottom: `1px solid ${wpColors.cardBorder}`,
            px: { xs: 1.5, md: 2.5 },
            py: { xs: 1.25, md: 2 },
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 1,
            minHeight: { xs: 52, md: 'auto' },
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, minWidth: 0 }}>
            {isMobile && (
              <IconButton
                edge="start"
                onClick={() => setNavOpen(true)}
                aria-label="Открыть меню"
                data-testid="mobile-nav-toggle"
                sx={{ color: wpColors.heading }}
              >
                <MenuIcon />
              </IconButton>
            )}
            <Typography
              sx={{
                color: wpColors.heading,
                fontSize: { xs: 16, md: 20 },
                fontWeight: 600,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {pageTitle}
            </Typography>
          </Box>
          {user && <UserMenu />}
        </Box>

        {/* Справочники грузятся фоном: страницы остаются доступными (ISS-018). */}
        <PanelBusyBar open={dictionariesLoading} delayMs={600} data-testid="dictionaries-busy" />

        <Box
          sx={{
            flex: 1,
            p: { xs: 1, md: 2 },
            overflow: isAnalyticsRoute ? 'auto' : 'hidden',
            display: 'flex',
            flexDirection: 'column',
            minHeight: 0,
          }}
        >
          <Box
            sx={{
              bgcolor: wpColors.cardBg,
              border: `1px solid ${wpColors.cardBorder}`,
              borderRadius: { xs: 1, md: 2 },
              p: { xs: 1, sm: 1.5, md: 2.5 },
              flex: isAnalyticsRoute ? '0 0 auto' : 1,
              minHeight: isAnalyticsRoute ? 'auto' : 0,
              display: 'flex',
              flexDirection: 'column',
              boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
              overflow: isAnalyticsRoute ? 'visible' : 'hidden',
            }}
          >
            <Suspense fallback={<RouteContentFallback />}>
              <Outlet />
            </Suspense>
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
