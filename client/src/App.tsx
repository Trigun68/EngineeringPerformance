import { lazy, Suspense, useEffect, useState } from 'react';
import { Box, CircularProgress } from '@mui/material';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { getUserPreferences } from './api/userPreferencesApi';
import { AppLayout } from './layout/AppLayout';
import { AdminRouteGuard } from './components/auth/AdminRouteGuard';
import { MfaSetupDialog } from './components/auth/MfaSetupDialog';
import { PermissionGuard, SettingsGuard } from './components/auth/PermissionGuard';
import { AdminLayout } from './features/admin/AdminLayout';
import { useFirstAccessiblePath } from './hooks/useAccessibleNavigation';
import { ProjectsPage as ProjectsListPage } from './pages/projects/ProjectsPage';
import { FullProjectsPage } from './pages/projects/FullProjectsPage';
import { ReferencesLayout } from './pages/references/ReferencesLayout';
import { DirectoryPage } from './pages/references/DirectoryPage';
import { IicNormsPage } from './pages/references/IicNormsPage';
import { WorkCostsPage } from './pages/references/WorkCostsPage';
import { REFERENCE_ROUTES } from './pages/references/referenceConfig';
import { ReferencesIndexRedirect } from './pages/references/ReferencesIndexRedirect';
import { DashboardPage } from './pages/dashboard/DashboardPage';
import { LoginPage } from './pages/auth/LoginPage';
import { SettingsLayout } from './pages/settings/SettingsLayout';
import { useAuthStore } from './store/useAuthStore';
import { resolvePreferredPath } from './utils/projectWorkspacePrefs';

// Тяжёлые страницы — отдельные чанки. Каркасы AdminLayout / SettingsLayout грузятся сразу,
// чтобы вторичное меню не исчезало, пока подгружается вкладка.
const AnalyticsPage = lazy(() =>
  import('./pages/analytics/AnalyticsPage').then((m) => ({ default: m.AnalyticsPage })),
);
const HelpPage = lazy(() => import('./pages/help/HelpPage').then((m) => ({ default: m.HelpPage })));
const LogViewer = lazy(() => import('./features/admin/LogViewer').then((m) => ({ default: m.LogViewer })));
const HealthDashboard = lazy(() =>
  import('./features/admin/HealthDashboard').then((m) => ({ default: m.HealthDashboard })),
);
const ConfigAudit = lazy(() => import('./features/admin/ConfigAudit').then((m) => ({ default: m.ConfigAudit })));
const MonitoringDashboard = lazy(() =>
  import('./features/monitoring/MonitoringDashboard').then((m) => ({ default: m.MonitoringDashboard })),
);
const UserActionLogPage = lazy(() =>
  import('./pages/audit/UserActionLogPage').then((m) => ({ default: m.UserActionLogPage })),
);
const UsersPage = lazy(() => import('./pages/settings/UsersPage').then((m) => ({ default: m.UsersPage })));
const AccessRightsPage = lazy(() =>
  import('./pages/settings/AccessRightsPage').then((m) => ({ default: m.AccessRightsPage })),
);
const SystemSettingsPage = lazy(() =>
  import('./pages/settings/SystemSettingsPage').then((m) => ({ default: m.SystemSettingsPage })),
);
// Дизайн-превью — только для администраторов, без AppLayout.
const PdfDesignMockups = lazy(() => import('./pages/dev/PdfDesignMockups'));
const TrendDetailMockup = lazy(() => import('./pages/dev/TrendDetailMockup'));
const FactNormAnalysisMockup = lazy(() => import('./pages/dev/FactNormAnalysisMockup'));
const NormAnalysisMockup = lazy(() => import('./pages/dev/NormAnalysisMockup'));

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
        if (cancelled || !user) return;
        setTarget(resolvePreferredPath(prefs, user.permissions, user.roleName, fallbackPath));
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
  const user = useAuthStore((state) => state.user);
  const requiresMfaSetup = useAuthStore((state) => state.requiresMfaSetup);
  const completeMfaSetup = useAuthStore((state) => state.completeMfaSetup);

  useEffect(() => {
    void initialize();
  }, [initialize]);

  if (!isInitialized) {
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
      <>
        <Routes>
        {user ? (
          <>
          <Route
            path="/dev/pdf-mockups"
            element={(
              <AdminRouteGuard>
                <Suspense fallback={null}>
                  <PdfDesignMockups />
                </Suspense>
              </AdminRouteGuard>
            )}
          />
          <Route
            path="/dev/trend-detail-mockup"
            element={(
              <AdminRouteGuard>
                <Suspense fallback={null}>
                  <TrendDetailMockup />
                </Suspense>
              </AdminRouteGuard>
            )}
          />
          <Route
            path="/dev/fact-norm-mockup"
            element={(
              <AdminRouteGuard>
                <Suspense fallback={null}>
                  <FactNormAnalysisMockup />
                </Suspense>
              </AdminRouteGuard>
            )}
          />
          <Route
            path="/dev/norm-analysis-mockup"
            element={(
              <AdminRouteGuard>
                <Suspense fallback={null}>
                  <NormAnalysisMockup />
                </Suspense>
              </AdminRouteGuard>
            )}
          />
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
              path="/analytics"
              element={(
                <PermissionGuard objectName="GroupTrend">
                  <AnalyticsPage />
                </PermissionGuard>
              )}
            />
            <Route path="/analytics/trends" element={<Navigate to="/analytics?tab=hours-trend" replace />} />
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
                ) : route.path === 'work-costs' ? (
                  <Route
                    key={route.path}
                    path={route.path}
                    element={(
                      <PermissionGuard objectName={route.permission}>
                        <WorkCostsPage />
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
              <Route path="monitoring" element={<MonitoringDashboard />} />
              <Route path="audit" element={<ConfigAudit />} />
              <Route path="user-actions" element={<UserActionLogPage />} />
            </Route>
            <Route path="/monitoring" element={<Navigate to="/admin/monitoring" replace />} />
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
          </>
        ) : (
          <Route path="*" element={<LoginPage />} />
        )}
        </Routes>
        <MfaSetupDialog
          open={Boolean(user && requiresMfaSetup)}
          onCompleted={completeMfaSetup}
          onDismiss={completeMfaSetup}
        />
      </>
    </BrowserRouter>
  );
}
