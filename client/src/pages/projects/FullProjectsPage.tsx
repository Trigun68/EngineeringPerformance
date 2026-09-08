import { useState, useEffect, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams, useLocation, useSearchParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Divider,
  IconButton,
  Paper,
  Stack,
  Tab,
  TextField,
  Tabs,
  Tooltip,
  Typography,
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import { usePermission } from '../../hooks/usePermission';
import type { PermissionObjectName } from '../../constants/accessControlConstants';
import { createProject, fetchProjects, updateProject } from '../../api/projectsApi';
import type { Project } from '../../types/project';
import type { ProjectFormData } from '../../types/project';
import { ProjectSelectionDialog } from '../../components/ProjectSelectionDialog/ProjectSelectionDialog';
import { ParticipantsModal } from '../../components/ParticipantsModal/ParticipantsModal';
import { EditModal } from '../../components/EditModal/EditModal';
import { ProjectCharacteristicsDialog } from '../../components/ProjectCharacteristicsDialog/ProjectCharacteristicsDialog';
import { ForecastPanel } from '../../features/projects/forecast/ForecastPanel';
import { FactPanel } from '../../features/projects/fact/FactPanel';
import { ComparePanel } from '../../features/projects/compare/ComparePanel';
import { ReportPanel } from '../../features/projects/report/ReportPanel';
import {
  useProjectWorkspace,
} from '../../features/projects/shared/useProjectWorkspace';
import { WorkspaceChromeProvider, useWorkspaceChrome } from '../../features/projects/shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../../features/projects/shared/WorkspaceChromeToggleRow';
import { PanelBusyBar } from '../../components/WorkingOverlay/PanelBusyBar';
import { usePersistedTabState } from '../../hooks/usePersistedTabState';
import {
  LAST_PROJECT_PREF_KEY,
  readHeaderExpanded,
  writeHeaderExpanded,
} from '../../utils/projectWorkspacePrefs';
import { putUserPreferences } from '../../api/userPreferencesApi';
import { queryKeys } from '../../services/queryKeys';

type WorkspaceMainTab = 'forecast' | 'fact' | 'compare' | 'report';

const WORKSPACE_TAB_CONFIG: ReadonlyArray<{
  value: WorkspaceMainTab;
  label: string;
  permission: PermissionObjectName;
}> = [
  { value: 'forecast', label: 'Прогнозирование', permission: 'Planning' },
  { value: 'fact', label: 'Оценка факта', permission: 'Assessment' },
  { value: 'compare', label: 'Сравнение версий', permission: 'VersionComparison' },
  { value: 'report', label: 'Отчет', permission: 'Reports' },
];

const EMPTY_PROJECT_FORM: ProjectFormData = {
  code: '',
  name: '',
  workNumber: '',
  contractNumber: '',
  calendarPlanNumber: '',
  type: '',
  customer: '',
  contractor: '',
  gip: '',
  workStatus: '',
  workStart: '',
  program: '',
  description: '',
};

function toProjectForm(project: Project): ProjectFormData {
  return {
    code: project.code,
    name: project.name,
    workNumber: project.workNumber ?? '',
    contractNumber: project.contractNumber ?? '',
    calendarPlanNumber: project.calendarPlanNumber ?? '',
    type: project.type ?? '',
    customer: project.customer ?? '',
    contractor: project.contractor ?? '',
    gip: project.gip ?? '',
    workStatus: project.workStatus ?? '',
    workStart: project.workStart ?? '',
    program: project.program ?? '',
    description: project.description ?? '',
  };
}

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

  const { canRead, canEdit } = usePermission('Projects');

  const projectsQuery = useQuery<Project[]>({
    queryKey: queryKeys.projects.all(),
    queryFn: () => fetchProjects(),
    enabled: canRead,
  });

  const { data: workspace, loading: workspaceLoading, error: workspaceError, visibleWorkspaceTabs } = useProjectWorkspace(
    currentProjectId,
    canRead,
  );

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

  // Активные вкладки
  const [mainTab, setMainTab] = usePersistedTabState<WorkspaceMainTab>(
    currentProjectId ? `projects.${currentProjectId}.mainTab` : 'projects.mainTab',
    'forecast',
  );
  const [searchParams] = useSearchParams();
  useEffect(() => {
    if (searchParams.get('tab') === 'report') {
      setMainTab('report');
    }
  }, [searchParams, setMainTab]);
  // Только localStorage: сворачивание шапки не должно бить API / перезагружать данные.
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
      setMainTab(visibleWorkspaceTabs[0].value);
    }
  }, [mainTab, setMainTab, visibleWorkspaceTabs]);

  useEffect(() => {
    if (currentProjectId) {
      void putUserPreferences({ [LAST_PROJECT_PREF_KEY]: String(currentProjectId) });
    }
  }, [currentProjectId]);

  // Модалки
  const [selectionDialogOpen, setSelectionDialogOpen] = useState(false);
  const [participantsOpen, setParticipantsOpen] = useState(false);
  const [characteristicsOpen, setCharacteristicsOpen] = useState(false);
  const [projectModalOpen, setProjectModalOpen] = useState(false);
  const [projectModalMode, setProjectModalMode] = useState<'create' | 'edit'>('edit');
  const [projectEditingId, setProjectEditingId] = useState<number | null>(null);
  const [projectForm, setProjectForm] = useState<ProjectFormData>(EMPTY_PROJECT_FORM);
  const [projectSubmitting, setProjectSubmitting] = useState(false);

  // Обработчики
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

  const handleSaveProject = async () => {
    if (!canEdit) return;
    setProjectSubmitting(true);
    try {
      if (projectModalMode === 'edit' && projectEditingId) {
        await updateProject(projectEditingId, projectForm);
        setProjectModalOpen(false);
        void projectsQuery.refetch();
      } else {
        const created = await createProject(projectForm);
        setProjectModalOpen(false);
        void projectsQuery.refetch();
        navigate(`/projects/${created.id}`);
      }
    } finally {
      setProjectSubmitting(false);
    }
  };

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр проектов.</Alert>;
  }

  return (
    <WorkspaceChromeProvider
      chromeExpanded={headerExpanded}
      setChromeExpanded={setHeaderExpanded}
    >
      {/* Шапка проекта и вкладки остаются доступны, пока догружаются данные (ISS-018). */}
      <PanelBusyBar open={dataFetching} delayMs={600} data-testid="workspace-busy" />
      <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'hidden' }}>
        {headerExpanded ? (
          <Paper elevation={1} sx={{ p: { xs: 1.5, md: 2 }, mb: 0.5, backgroundColor: '#F8F9FA' }}>
            <Box sx={{ mb: 2 }}>
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                <Button
                  variant="contained"
                  size="small"
                  onClick={() => setSelectionDialogOpen(true)}
                  sx={{
                    backgroundColor: '#3498DB',
                    color: 'white',
                    '&:hover': { backgroundColor: '#2980B9' },
                  }}
                >
                  Выбрать проект
                </Button>

                {hasProject ? (
                  <>
                    <Box
                      sx={{
                        backgroundColor: '#2C3E50',
                        color: 'white',
                        px: 1.5,
                        py: 0.5,
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '0.875rem',
                      }}
                    >
                      {resolvedProject!.code}
                    </Box>

                    <Typography
                      variant="h6"
                      sx={{
                        color: '#2C3E50',
                        fontWeight: 500,
                        fontSize: { xs: '1rem', md: '1.25rem' },
                        maxWidth: { xs: '100%', md: 'none' },
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                      }}
                    >
                      {resolvedProject!.name}
                    </Typography>

                    {workspace?.deviationPercent != null && (
                      <Box
                        sx={{
                          backgroundColor: '#FDFEFE',
                          border: '1px solid #D5D8DC',
                          px: 1.5,
                          py: 0.5,
                          borderRadius: 1,
                        }}
                      >
                        <Typography variant="body2" sx={{ color: '#616A6B' }}>
                          Отклонение: <strong>{workspace.deviationPercent.toFixed(2)}%</strong>
                        </Typography>
                      </Box>
                    )}
                  </>
                ) : workspaceLoading && currentProjectId ? (
                  <Typography variant="body1" color="text.secondary">
                    Загрузка проекта…
                  </Typography>
                ) : currentProjectId && workspaceError ? (
                  <Typography variant="body1" color="error">
                    Не удалось загрузить проект
                  </Typography>
                ) : (
                  <Typography variant="body2" sx={{ color: '#95A5A6' }}>
                    Проект не выбран
                  </Typography>
                )}
              </Stack>
            </Box>

            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Button
                variant="outlined"
                size="small"
                disabled={!hasProject || !canEdit}
                onClick={() => resolvedProject && openEditProject(resolvedProject)}
              >
                Основные данные
              </Button>

              <Button
                variant="outlined"
                size="small"
                disabled={!hasProject}
                onClick={() => setCharacteristicsOpen(true)}
              >
                Характеристики
              </Button>

              <Button
                variant="outlined"
                size="small"
                disabled={!hasProject}
                onClick={() => setParticipantsOpen(true)}
              >
                Участники
              </Button>

              <Tooltip title="Обновить">
                <IconButton
                  onClick={() => void projectsQuery.refetch()}
                  disabled={workspaceLoading || projectsQuery.isLoading}
                  size="small"
                >
                  <RefreshIcon />
                </IconButton>
              </Tooltip>
            </Stack>

            {error && (
              <Alert severity="error" sx={{ mt: 2 }} onClose={() => {}}>
                {error}
              </Alert>
            )}
          </Paper>
        ) : (
          <WorkspaceCompactBar
            projectName={
              hasProject
                ? (resolvedProject!.code
                  ? `${resolvedProject!.code} — ${resolvedProject!.name}`
                  : resolvedProject!.name)
                : null
            }
            tabLabel={activeMainTabLabel}
          />
        )}

        {headerExpanded && <Divider sx={{ mb: 2 }} />}

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
                onChange={(_, value) => setMainTab(value)}
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

        {/* ═════════════════════════════════════════════════════════════════
            ВКЛАДКА: ПРОГНОЗИРОВАНИЕ
           ═════════════════════════════════════════════════════════════════ */}
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

        {/* ═════════════════════════════════════════════════════════════════
            ВКЛАДКА: СРАВНЕНИЕ ВЕРСИЙ
           ═════════════════════════════════════════════════════════════════ */}
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

      {/* Диалоги */}
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

      <EditModal
        open={projectModalOpen}
        title={projectModalMode === 'edit' ? 'Основные данные проекта' : 'Создание проекта'}
        onClose={() => setProjectModalOpen(false)}
        onSubmit={handleSaveProject}
        isSubmitting={projectSubmitting}
        maxWidth="md"
      >
        <Stack spacing={2}>
          <TextField
            label="Код"
            value={projectForm.code}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, code: e.target.value }))}
            required
            fullWidth
          />
          <TextField
            label="Наименование"
            value={projectForm.name}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, name: e.target.value }))}
            required
            fullWidth
          />
          <TextField
            label="Номер работы"
            value={projectForm.workNumber ?? ''}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, workNumber: e.target.value }))}
            fullWidth
          />
          <TextField
            label="Номер договора"
            value={projectForm.contractNumber ?? ''}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, contractNumber: e.target.value }))}
            fullWidth
          />
          <TextField
            label="Заказчик"
            value={projectForm.customer ?? ''}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, customer: e.target.value }))}
            fullWidth
          />
          <TextField
            label="Подрядчик"
            value={projectForm.contractor ?? ''}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, contractor: e.target.value }))}
            fullWidth
          />
          <TextField
            label="ГИП"
            value={projectForm.gip ?? ''}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, gip: e.target.value }))}
            fullWidth
          />
          <TextField
            label="Статус работ"
            value={projectForm.workStatus ?? ''}
            onChange={(e) => setProjectForm((prev) => ({ ...prev, workStatus: e.target.value }))}
            fullWidth
          />
        </Stack>
      </EditModal>
    </Box>
    </WorkspaceChromeProvider>
  );
}

function WorkspaceCompactBar({
  projectName,
  tabLabel,
}: {
  projectName: string | null;
  tabLabel: string;
}) {
  const { versionSummary } = useWorkspaceChrome();
  return (
    <Paper
      elevation={0}
      data-testid="workspace-compact-bar"
      sx={{
        mb: 1,
        px: { xs: 1.5, md: 2 },
        py: 1,
        border: '1px solid #D5DDE5',
        backgroundColor: '#F8F9FA',
      }}
    >
      <Stack direction="row" spacing={1.5} alignItems="center" flexWrap="wrap" useFlexGap>
        {projectName && (
          <Typography
            data-testid="workspace-compact-project"
            sx={{
              color: '#2C3E50',
              fontWeight: 600,
              fontSize: { xs: 14, md: 16 },
              maxWidth: { xs: '100%', md: 360 },
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {projectName}
          </Typography>
        )}
        <Typography
          data-testid="workspace-compact-tab"
          sx={{ color: '#5D6D7E', fontWeight: 500, fontSize: { xs: 13, md: 14 } }}
        >
          {tabLabel}
        </Typography>
        {versionSummary && (
          <Box
            data-testid="workspace-compact-version"
            sx={{
              backgroundColor: '#EBF5FB',
              border: '1px solid #AED6F1',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
              maxWidth: { xs: '100%', sm: 420 },
              minWidth: 0,
            }}
          >
            <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>
              {versionSummary}
            </Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  );
}
