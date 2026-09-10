import { Alert, Box, Paper, Tab, Tabs } from '@mui/material';
import type { Project } from '../../types/project';
import type { ProjectWorkspaceData } from '../../features/projects/shared/useProjectWorkspace';
import { ForecastPanel } from '../../features/projects/forecast/ForecastPanel';
import { FactPanel } from '../../features/projects/fact/FactPanel';
import { ComparePanel } from '../../features/projects/compare/ComparePanel';
import { ReportPanel } from '../../features/projects/report/ReportPanel';
import { WorkspaceChromeToggleRow } from '../../features/projects/shared/WorkspaceChromeToggleRow';

export type WorkspaceMainTab = 'forecast' | 'fact' | 'compare' | 'report';

export interface FullProjectsGridProps {
  headerExpanded: boolean;
  mainTab: WorkspaceMainTab;
  onMainTabChange: (tab: WorkspaceMainTab) => void;
  visibleWorkspaceTabs: ReadonlyArray<{ value: string; label: string }>;
  currentProjectId: number | null;
  hasProject: boolean;
  workspace: ProjectWorkspaceData | null;
  resolvedProject: Project | null;
}

export function FullProjectsGrid({
  headerExpanded,
  mainTab,
  onMainTabChange,
  visibleWorkspaceTabs,
  currentProjectId,
  hasProject,
  workspace,
  resolvedProject,
}: FullProjectsGridProps) {
  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'auto' }}>
      {headerExpanded && (
        <Paper
          elevation={0}
          sx={{
            mb: 2,
            p: 1,
            border: '1px solid #D5DDE5',
            backgroundColor: '#EAF0F6',
          }}
        >
          <Tabs
            value={mainTab}
            onChange={(_, value) => onMainTabChange(value)}
            variant="scrollable"
            scrollButtons="auto"
            allowScrollButtonsMobile
            sx={{
              '& .MuiTabs-indicator': { display: 'none' },
              '& .MuiTab-root': { minWidth: { xs: 72, md: 120 }, px: { xs: 1, md: 2 } },
            }}
          >
            {visibleWorkspaceTabs.map((tab) => (
              <Tab
                key={tab.value}
                label={tab.label}
                value={tab.value}
                sx={{
                  textTransform: 'none',
                  bgcolor: '#DCE5EE',
                  color: '#2C3E50',
                  '&.Mui-selected': {
                    bgcolor: '#D6EAF8',
                    color: '#2C3E50',
                    fontWeight: 600,
                  },
                }}
              />
            ))}
          </Tabs>
        </Paper>
      )}

      {mainTab === 'forecast' && currentProjectId && hasProject && workspace && (
        <ForecastPanel
          projectId={currentProjectId}
          projectName={resolvedProject!.name}
          projectCode={resolvedProject!.code}
          workspaceVersions={workspace.versions}
          workspacePlanVersionId={workspace.planVersionId}
          workspacePlanVersionLabel={workspace.planVersionLabel}
        />
      )}
      {mainTab === 'forecast' && (!currentProjectId || !hasProject) && (
        <>
          <WorkspaceChromeToggleRow />
          <Alert severity="info" sx={{ mt: 1 }}>
            Выберите проект для работы с прогнозированием.
          </Alert>
        </>
      )}

      {mainTab === 'fact' && currentProjectId && hasProject && workspace && (
        <FactPanel
          projectId={currentProjectId}
          projectName={resolvedProject!.name}
          projectCode={resolvedProject!.code}
          workspaceVersions={workspace.versions}
          workspaceFactVersionId={workspace.factVersionId}
          workspaceFactVersionLabel={workspace.factVersionLabel}
        />
      )}
      {mainTab === 'fact' && (!currentProjectId || !hasProject) && (
        <>
          <WorkspaceChromeToggleRow />
          <Alert severity="info" sx={{ mt: 1 }}>
            Выберите проект для работы с оценкой факта.
          </Alert>
        </>
      )}

      {mainTab === 'compare' && currentProjectId && hasProject && workspace && (
        <ComparePanel
          projectId={currentProjectId}
          workspaceVersions={workspace.versions}
          workspaceCompareVersionAId={workspace.compareVersionAId}
          workspaceCompareVersionBId={workspace.compareVersionBId}
        />
      )}
      {mainTab === 'compare' && (!currentProjectId || !hasProject) && (
        <>
          <WorkspaceChromeToggleRow />
          <Alert severity="info" sx={{ mt: 1 }}>
            Выберите проект для сравнения версий.
          </Alert>
        </>
      )}

      {mainTab === 'report' && currentProjectId && hasProject && workspace && (
        <ReportPanel
          projectId={currentProjectId}
          project={resolvedProject!}
          versions={workspace.versions}
        />
      )}
      {mainTab === 'report' && (!currentProjectId || !hasProject) && (
        <>
          <WorkspaceChromeToggleRow />
          <Alert severity="info" sx={{ mt: 1 }}>
            Выберите проект для просмотра отчёта.
          </Alert>
        </>
      )}
    </Box>
  );
}
