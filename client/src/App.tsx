import { useEffect, useState } from 'react';
import { Box, CircularProgress } from '@mui/material';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { getUserPreferences } from './api/userPreferencesApi';
import { LAST_ROUTE_PREF_KEY } from './hooks/usePersistLastRoute';
import { AppLayout } from './layout/AppLayout';
import { AdminRouteGuard } from './components/auth/AdminRouteGuard';
import { PermissionGuard, SettingsGuard } from './components/auth/PermissionGuard';
import { useFirstAccessiblePath } from './hooks/useAccessibleNavigation';
import { ProjectsPage as ProjectsListPage } from './pages/projects/ProjectsPage';
import { FullProjectsPage } from './pages/projects/FullProjectsPage';
import { ReferencesLayout } from './pages/references/ReferencesLayout';
import { DirectoryPage } from './pages/references/DirectoryPage';
import { IicNormsPage } from './pages/references/IicNormsPage';
import { REFERENCE_ROUTES } from './pages/references/referenceConfig';
import { ReferencesIndexRedirect } from './pages/references/ReferencesIndexRedirect';
import { UserActionLogPage } from './pages/audit/UserActionLogPage';
import { SettingsLayout } from './pages/settings/SettingsLayout';
import { UsersPage } from './pages/settings/UsersPage';
import { AccessRightsPage } from './pages/settings/AccessRightsPage';
import { SystemSettingsPage } from './pages/settings/SystemSettingsPage';
import { HelpPage } from './pages/help/HelpPage';
import { DashboardPage } from './pages/dashboard/DashboardPage';
import { TrendsAnalyticsPage } from './pages/analytics/TrendsAnalyticsPage';
import { LoginPage } from './pages/auth/LoginPage';
import { MonitoringDashboard } from './features/monitoring/MonitoringDashboard';
import { AdminLayout } from './features/admin/AdminLayout';
import { LogViewer } from './features/admin/LogViewer';
import { HealthDashboard } from './features/admin/HealthDashboard';
import { ConfigAudit } from './features/admin/ConfigAudit';
import { useAuthStore } from './store/useAuthStore';
import { isRouteAccessible } from './utils/routeAccess';

const LOGO_SRC = '/assets/logo.png';
const LOGIN_PANEL_BG = '#0D1B2A';

function DefaultRedirect() {
  const user = useAuthStore((s) => s.user);
  const fallbackPath = useFirstAccessiblePath();
  const [target, setTarget] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        const lastRoute = prefs[LAST_ROUTE_PREF_KEY];
        if (
          lastRoute
          && user
          && isRouteAccessible(lastRoute, user.permissions, user.roleName)
        ) {
          setTarget(lastRoute);
          return;
        }
        setTarget(fallbackPath);
      })
      .catch(() => {
        if (!cancelled) setTarget(fallbackPath);
      });
    return () => {
      cancelled = true;
    };
  }, [fallbackPath, user]);

  if (!target) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress size={28} />
      </Box>
    );
  }

  return <Navigate to={target} replace />;
}

export default function App() {
  const initialize = useAuthStore((state) => state.initialize);
  const isInitialized = useAuthStore((state) => state.isInitialized);
  const isLoading = useAuthStore((state) => state.isLoading);
  const user = useAuthStore((state) => state.user);

  useEffect(() => {
    void initialize();
  }, [initialize]);

  if (!isInitialized || isLoading) {
    return (
      <Box
        sx={{
          minHeight: '100vh',
          bgcolor: LOGIN_PANEL_BG,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 2,
        }}
      >
        <Box
          component="img"
          src={LOGO_SRC}
          alt="Engineering Performance"
          sx={{ width: 200, height: 200, objectFit: 'contain' }}
        />
        <CircularProgress size={28} sx={{ color: '#8899AA' }} />
      </Box>
    );
  }

  return (
    <BrowserRouter>
      {user ? (
        <Routes>
          <Route element={<AppLayout />}>
            <Route
              path="/"
              element={(
                <PermissionGuard objectName="Dashboard">
                  <DashboardPage />
                </PermissionGuard>
              )}
            />
            <Route
              path="/projects"
              element={(
                <PermissionGuard objectName="Projects">
                  <ProjectsListPage />
                </PermissionGuard>
              )}
            />
            <Route
              path="/projects/:projectId"
              element={(
                <PermissionGuard objectName="Projects">
                  <FullProjectsPage />
                </PermissionGuard>
              )}
            />
            <Route
              path="/analytics/trends"
              element={(
                <PermissionGuard objectName="GroupTrend">
                  <TrendsAnalyticsPage />
                </PermissionGuard>
              )}
            />
            <Route
              path="/references"
              element={(
                <PermissionGuard objectName="References">
                  <ReferencesLayout />
                </PermissionGuard>
              )}
            >
              <Route index element={<ReferencesIndexRedirect />} />
              {REFERENCE_ROUTES.map((route) =>
                route.path === 'iic-norms' ? (
                  <Route
                    key={route.path}
                    path={route.path}
                    element={(
                      <PermissionGuard objectName={route.permission}>
                        <IicNormsPage />
                      </PermissionGuard>
                    )}
                  />
                ) : (
                  <Route
                    key={route.path}
                    path={route.path}
                    element={(
                      <PermissionGuard objectName={route.permission}>
                        <DirectoryPage config={route} />
                      </PermissionGuard>
                    )}
                  />
                ),
              )}
            </Route>
            <Route
              path="/help"
              element={(
                <PermissionGuard objectName="HelpPage">
                  <HelpPage />
                </PermissionGuard>
              )}
            />
            <Route path="/audit" element={<Navigate to="/admin/user-actions" replace />} />
            <Route
              path="/admin"
              element={(
                <AdminRouteGuard>
                  <AdminLayout />
                </AdminRouteGuard>
              )}
            >
              <Route path="logs" element={<LogViewer />} />
              <Route path="health" element={<HealthDashboard />} />
              <Route path="audit" element={<ConfigAudit />} />
              <Route path="user-actions" element={<UserActionLogPage />} />
            </Route>
            <Route
              path="/monitoring"
              element={(
                <AdminRouteGuard>
                  <MonitoringDashboard />
                </AdminRouteGuard>
              )}
            />
            <Route
              path="/settings"
              element={(
                <SettingsGuard>
                  <SettingsLayout />
                </SettingsGuard>
              )}
            >
              <Route
                path="users"
                element={(
                  <PermissionGuard objectName="Users">
                    <UsersPage />
                  </PermissionGuard>
                )}
              />
              <Route path="access-rights" element={<AccessRightsPage />} />
              <Route
                path="system"
                element={(
                  <AdminRouteGuard>
                    <SystemSettingsPage />
                  </AdminRouteGuard>
                )}
              />
            </Route>
            <Route path="*" element={<DefaultRedirect />} />
          </Route>
        </Routes>
      ) : (
        <LoginPage />
      )}
    </BrowserRouter>
  );
}
