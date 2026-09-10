import { useState, useEffect, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams, useLocation, useSearchParams } from 'react-router-dom';
import { Alert, Box, Divider } from '@mui/material';
import { usePermission } from '../../hooks/usePermission';
import { OBJECT_NAME, type PermissionObjectName } from '../../constants/accessControlConstants';
import { fetchProjects } from '../../api/projectsApi';
import type { Project } from '../../types/project';
import type { ProjectFormData } from '../../types/project';
import { ProjectSelectionDialog } from '../../components/ProjectSelectionDialog/ProjectSelectionDialog';
import { ParticipantsModal } from '../../components/ParticipantsModal/ParticipantsModal';
import { ProjectCharacteristicsDialog } from '../../components/ProjectCharacteristicsDialog/ProjectCharacteristicsDialog';
import { useProjectWorkspace } from '../../features/projects/shared/useProjectWorkspace';
import { WorkspaceChromeProvider } from '../../features/projects/shared/workspaceChromeContext';
import { PanelBusyBar } from '../../components/WorkingOverlay/PanelBusyBar';
import { usePersistedTabState } from '../../hooks/usePersistedTabState';
import {
  LAST_PROJECT_PREF_KEY,
  readHeaderExpanded,
  writeHeaderExpanded,
} from '../../utils/projectWorkspacePrefs';
import { putUserPreferences } from '../../api/userPreferencesApi';
import { queryKeys } from '../../services/queryKeys';
import { EMPTY_PROJECT_FORM, toProjectForm } from './projectForm';
import { FullProjectsToolbar } from './FullProjectsToolbar';
import { FullProjectsGrid, type WorkspaceMainTab } from './FullProjectsGrid';
import { ProjectEditorDialog } from './ProjectEditorDialog';

const WORKSPACE_TAB_CONFIG: ReadonlyArray<{
  value: WorkspaceMainTab;
  label: string;
  permission: PermissionObjectName;
}> = [
  { value: 'forecast', label: 'Прогнозирование', permission: OBJECT_NAME.Planning },
  { value: 'fact', label: 'Оценка факта', permission: OBJECT_NAME.Assessment },
  { value: 'compare', label: 'Сравнение версий', permission: OBJECT_NAME.VersionComparison },
  { value: 'report', label: 'Отчет', permission: OBJECT_NAME.Reports },
];

function parseProjectId(raw: string | undefined): number | null {
  if (!raw || !/^\d+$/.test(raw)) return null;
  const id = Number(raw);
  return Number.isFinite(id) && id > 0 ? id : null;
}

export function FullProjectsPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { projectId: projectIdParam } = useParams<{ projectId: string }>();
  const currentProjectId = parseProjectId(projectIdParam);
  const navProject = (location.state as { project?: Project } | null)?.project ?? null;
  const { canRead, canEdit } = usePermission(OBJECT_NAME.Projects);

  const projectsQuery = useQuery<Project[]>({
    queryKey: queryKeys.projects.all(),
    queryFn: () => fetchProjects(),
    enabled: canRead,
  });

  const { data: workspace, loading: workspaceLoading, error: workspaceError, visibleWorkspaceTabs } =
    useProjectWorkspace(currentProjectId, canRead);

  const projects = projectsQuery.data ?? [];
  const resolvedProject =
    workspace?.project ?? (navProject && navProject.id === currentProjectId ? navProject : null);
  const hasProject = resolvedProject != null;
  const dataFetching = projectsQuery.isFetching || workspaceLoading;
  const error =
    projectsQuery.isError
      ? 'Не удалось загрузить список проектов.'
      : workspaceError
        ? 'Не удалось загрузить данные проекта.'
        : null;

  const [mainTab, setMainTab] = usePersistedTabState<WorkspaceMainTab>(
    currentProjectId ? `projects.${currentProjectId}.mainTab` : 'projects.mainTab',
    'forecast',
  );
  const [searchParams] = useSearchParams();
  useEffect(() => {
    if (searchParams.get('tab') === 'report') setMainTab('report');
  }, [searchParams, setMainTab]);

  const [headerExpanded, setHeaderExpandedState] = useState(readHeaderExpanded);
  const setHeaderExpanded = useCallback((expanded: boolean) => {
    setHeaderExpandedState(expanded);
    writeHeaderExpanded(expanded);
  }, []);

  const activeMainTabLabel =
    visibleWorkspaceTabs.find((t) => t.value === mainTab)?.label
    ?? WORKSPACE_TAB_CONFIG.find((t) => t.value === mainTab)?.label
    ?? '';

  useEffect(() => {
    if (visibleWorkspaceTabs.length === 0) return;
    if (!visibleWorkspaceTabs.some((t) => t.value === mainTab)) {
      setMainTab(visibleWorkspaceTabs[0].value as WorkspaceMainTab);
    }
  }, [mainTab, setMainTab, visibleWorkspaceTabs]);

  useEffect(() => {
    if (currentProjectId) {
      void putUserPreferences({ [LAST_PROJECT_PREF_KEY]: String(currentProjectId) });
    }
  }, [currentProjectId]);

  const [selectionDialogOpen, setSelectionDialogOpen] = useState(false);
  const [participantsOpen, setParticipantsOpen] = useState(false);
  const [characteristicsOpen, setCharacteristicsOpen] = useState(false);
  const [projectModalOpen, setProjectModalOpen] = useState(false);
  const [projectModalMode, setProjectModalMode] = useState<'create' | 'edit'>('edit');
  const [projectEditingId, setProjectEditingId] = useState<number | null>(null);
  const [projectForm, setProjectForm] = useState<ProjectFormData>(EMPTY_PROJECT_FORM);

  const handleSelectProject = (pid: number) => {
    setSelectionDialogOpen(false);
    void putUserPreferences({ [LAST_PROJECT_PREF_KEY]: String(pid) });
    navigate(`/projects/${pid}`);
  };

  const openCreateProject = () => {
    setProjectModalMode('create');
    setProjectEditingId(null);
    setProjectForm(EMPTY_PROJECT_FORM);
    setProjectModalOpen(true);
  };

  const openEditProject = (project: Project) => {
    setProjectModalMode('edit');
    setProjectEditingId(project.id);
    setProjectForm(toProjectForm(project));
    setProjectModalOpen(true);
  };

  const openCopyProject = (project: Project) => {
    setProjectModalMode('create');
    setProjectEditingId(null);
    const copyCode = project.code.length <= 44 ? `${project.code}-copy` : `${project.code.slice(0, 44)}-copy`;
    setProjectForm({
      ...toProjectForm(project),
      code: copyCode,
      name: `${project.name} (копия)`,
    });
    setProjectModalOpen(true);
  };

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр проектов.</Alert>;
  }

  return (
    <WorkspaceChromeProvider chromeExpanded={headerExpanded} setChromeExpanded={setHeaderExpanded}>
      <PanelBusyBar open={dataFetching} delayMs={600} data-testid="workspace-busy" />
      <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'hidden' }}>
        <FullProjectsToolbar
          headerExpanded={headerExpanded}
          hasProject={hasProject}
          resolvedProject={resolvedProject}
          deviationPercent={workspace?.deviationPercent ?? null}
          workspaceLoading={workspaceLoading}
          currentProjectId={currentProjectId}
          workspaceError={Boolean(workspaceError)}
          canEdit={canEdit}
          error={error}
          refreshDisabled={workspaceLoading || projectsQuery.isLoading}
          activeMainTabLabel={activeMainTabLabel}
          onSelectProject={() => setSelectionDialogOpen(true)}
          onEditProject={() => resolvedProject && openEditProject(resolvedProject)}
          onCharacteristics={() => setCharacteristicsOpen(true)}
          onParticipants={() => setParticipantsOpen(true)}
          onRefresh={() => void projectsQuery.refetch()}
        />

        {headerExpanded && <Divider sx={{ mb: 2 }} />}

        <FullProjectsGrid
          headerExpanded={headerExpanded}
          mainTab={mainTab}
          onMainTabChange={setMainTab}
          visibleWorkspaceTabs={visibleWorkspaceTabs}
          currentProjectId={currentProjectId}
          hasProject={hasProject}
          workspace={workspace}
          resolvedProject={resolvedProject}
        />

        <ProjectSelectionDialog
          open={selectionDialogOpen}
          projects={projects}
          onSelect={handleSelectProject}
          onClose={() => setSelectionDialogOpen(false)}
          onCreateNew={openCreateProject}
          onEditSelected={openEditProject}
          onCopySelected={openCopyProject}
        />
        <ParticipantsModal
          open={participantsOpen && hasProject}
          projectId={currentProjectId ?? 0}
          onClose={() => setParticipantsOpen(false)}
          onSaved={() => void projectsQuery.refetch()}
        />
        <ProjectCharacteristicsDialog
          open={characteristicsOpen && hasProject}
          projectId={currentProjectId ?? 0}
          onClose={() => setCharacteristicsOpen(false)}
          onSaved={() => void projectsQuery.refetch()}
        />
        <ProjectEditorDialog
          open={projectModalOpen}
          mode={projectModalMode}
          projectId={projectEditingId}
          form={projectForm}
          onFormChange={setProjectForm}
          canEdit={canEdit}
          onClose={() => setProjectModalOpen(false)}
          onCreated={(project) => navigate(`/projects/${project.id}`)}
        />
      </Box>
    </WorkspaceChromeProvider>
  );
}
