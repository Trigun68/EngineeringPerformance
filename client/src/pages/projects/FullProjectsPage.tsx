import { useState, useEffect, useMemo, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
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
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import { exportCombinedProjectPdf } from '../../api/visualReportsApi';
import { fetchProjectPermissions, type ProjectPermission } from '../../api/projectPermissionsApi';
import {
  fetchProjectVersions,
  type ProjectVersion,
} from '../../api/projectVersionsApi';
import { createProject, fetchProjectById, fetchProjects, updateProject } from '../../api/projectsApi';
import { fetchProjectCharacteristics } from '../../api/projectCharacteristicsApi';
import { getApiErrorMessage } from '../../services/apiError';
import { usePermission } from '../../hooks/usePermission';
import type { Project } from '../../types/project';
import type { ProjectFormData } from '../../types/project';
import { ProjectSelectionDialog } from '../../components/ProjectSelectionDialog/ProjectSelectionDialog';
import { ParticipantsModal } from '../../components/ParticipantsModal/ParticipantsModal';
import { EditModal } from '../../components/EditModal/EditModal';
import { ProjectCharacteristicsDialog } from '../../components/ProjectCharacteristicsDialog/ProjectCharacteristicsDialog';
import { ForecastPanel } from '../../features/projects/forecast/ForecastPanel';
import { FactPanel } from '../../features/projects/fact/FactPanel';
import { ComparePanel } from '../../features/projects/compare/ComparePanel';
import { WorkspaceChromeProvider, useWorkspaceChrome } from '../../features/projects/shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../../features/projects/shared/WorkspaceChromeToggleRow';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';
import { usePersistedTabState } from '../../hooks/usePersistedTabState';
import { useAuthStore } from '../../store/useAuthStore';
import { canReadPermission } from '../../utils/permissionAccess';
import {
  LAST_PROJECT_PREF_KEY,
  readHeaderExpanded,
  writeHeaderExpanded,
} from '../../utils/projectWorkspacePrefs';
import { putUserPreferences } from '../../api/userPreferencesApi';

type WorkspaceMainTab = 'forecast' | 'fact' | 'compare' | 'reports';

const WORKSPACE_TAB_CONFIG: ReadonlyArray<{
  value: WorkspaceMainTab;
  label: string;
  permission: string;
}> = [
  { value: 'forecast', label: 'Прогнозирование', permission: 'Planning' },
  { value: 'fact', label: 'Оценка факта', permission: 'Assessment' },
  { value: 'compare', label: 'Сравнение версий', permission: 'VersionComparison' },
  { value: 'reports', label: 'Отчёты', permission: 'Reports' },
];

// Типы workspace query (верхняя панель проекта и вкладка «Отчёты»)

interface ProjectsPageData {
  project: Project | null;
  versions: ProjectVersion[];
  participants: ProjectPermission[];
  // Прогнозирование
  planVersionId: number | null;
  planVersionLabel: string;
  // Оценка факта
  factVersionId: number | null;
  factVersionLabel: string;
  // Сравнение
  compareVersionAId: number | null;
  compareVersionBId: number | null;
  compareShowOnlyDifferences: boolean;
  // Метрики проекта
  accuracyScore: number | null;
  deviationPercent: number | null;
}

// Пустые данные для заглушек
const EMPTY_DATA: ProjectsPageData = {
  project: null,
  versions: [],
  participants: [],
  planVersionId: null,
  planVersionLabel: '',
  factVersionId: null,
  factVersionLabel: '',
  compareVersionAId: null,
  compareVersionBId: null,
  compareShowOnlyDifferences: false,
  accuracyScore: null,
  deviationPercent: null,
};

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

async function fetchWorkspaceData(projectId: number): Promise<ProjectsPageData> {
  const [project, versionsData, participantsData, characteristicsData] = await Promise.all([
    fetchProjectById(projectId),
    fetchProjectVersions(projectId),
    fetchProjectPermissions(projectId).catch(() => [] as ProjectPermission[]),
    fetchProjectCharacteristics(projectId).catch(() => []),
  ]);
  if (!project) {
    throw new Error('Проект не найден.');
  }
  const planVersion = versionsData.find((v) => v.direction === 1) ?? versionsData[0] ?? null;
  const factVersion = versionsData.find((v) => v.direction === 2) ?? planVersion;

  const percents = characteristicsData
    .map((c) => c.selectedValuePercent)
    .filter((v): v is number => v != null);
  const deviationPercent =
    percents.length > 0
      ? Math.round((percents.reduce((a, b) => a + b, 0) / percents.length) * 100) / 100
      : null;

  return {
    project,
    versions: versionsData,
    participants: participantsData,
    planVersionId: planVersion?.id ?? null,
    planVersionLabel: planVersion ? `${planVersion.versionName} — ${planVersion.name}` : '',
    factVersionId: factVersion?.id ?? null,
    factVersionLabel: factVersion ? `${factVersion.versionName} — ${factVersion.name}` : '',
    compareVersionAId: planVersion?.id ?? null,
    compareVersionBId: factVersion?.id ?? null,
    compareShowOnlyDifferences: false,
    accuracyScore: null,
    deviationPercent,
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
  const user = useAuthStore((s) => s.user);
  const permissions = user?.permissions;
  const roleName = user?.roleName;

  const visibleWorkspaceTabs = useMemo(
    () =>
      WORKSPACE_TAB_CONFIG.filter((tab) =>
        canReadPermission(permissions, tab.permission, roleName),
      ),
    [permissions, roleName],
  );

  // Активные вкладки
  const [mainTab, setMainTab] = usePersistedTabState<WorkspaceMainTab>(
    currentProjectId ? `projects.${currentProjectId}.mainTab` : 'projects.mainTab',
    'forecast',
  );
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

  const [selectionDialogOpen, setSelectionDialogOpen] = useState(false);
  const [participantsOpen, setParticipantsOpen] = useState(false);
  const [characteristicsOpen, setCharacteristicsOpen] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [projectModalOpen, setProjectModalOpen] = useState(false);
  const [projectModalMode, setProjectModalMode] = useState<'create' | 'edit'>('edit');
  const [projectEditingId, setProjectEditingId] = useState<number | null>(null);
  const [projectForm, setProjectForm] = useState<ProjectFormData>(EMPTY_PROJECT_FORM);
  const [projectSubmitting, setProjectSubmitting] = useState(false);
  const [combinedExporting, setCombinedExporting] = useState(false);

  useEffect(() => {
    if (currentProjectId) {
      setSelectionDialogOpen(false);
      void putUserPreferences({ [LAST_PROJECT_PREF_KEY]: String(currentProjectId) });
    }
  }, [currentProjectId]);

  const projectsQuery = useQuery<Project[]>({
    queryKey: ['projects', 'all'],
    queryFn: () => fetchProjects(),
    enabled: canRead,
  });

  const projectDataQuery = useQuery<ProjectsPageData>({
    queryKey: ['projects', 'workspace', currentProjectId],
    queryFn: () => fetchWorkspaceData(currentProjectId as number),
    enabled: Boolean(currentProjectId && canRead),
  });

  const projects = projectsQuery.data ?? [];
  const data = projectDataQuery.data ?? EMPTY_DATA;
  const resolvedProject =
    data.project ?? (navProject && navProject.id === currentProjectId ? navProject : null);
  const hasProject = resolvedProject != null;
  const loading = projectsQuery.isLoading || (Boolean(currentProjectId) && projectDataQuery.isLoading);
  const error = pageError
    ?? (projectsQuery.isError
      ? 'Не удалось загрузить список проектов.'
      : projectDataQuery.isError
        ? 'Не удалось загрузить данные проекта.'
        : null);

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
    setPageError(null);
    try {
      if (projectModalMode === 'edit' && projectEditingId) {
        await updateProject(projectEditingId, projectForm);
        setProjectModalOpen(false);
        await projectDataQuery.refetch();
        await projectsQuery.refetch();
      } else {
        const created = await createProject(projectForm);
        setProjectModalOpen(false);
        await projectsQuery.refetch();
        navigate(`/projects/${created.id}`);
      }
    } catch (err) {
      setPageError(getApiErrorMessage(err, 'Не удалось сохранить проект.'));
      throw err;
    } finally {
      setProjectSubmitting(false);
    }
  };

  // Вкладка «Отчёты» — навигация к функциям на других вкладках
  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр проектов.</Alert>;
  }

  const { planVersionId, factVersionId, compareVersionAId, compareVersionBId } = data;

  return (
    <WorkspaceChromeProvider
      chromeExpanded={headerExpanded}
      setChromeExpanded={setHeaderExpanded}
    >
      <WorkingOverlay open={combinedExporting} />
      <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
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

                    {data.deviationPercent !== null && (
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
                          Отклонение: <strong>{data.deviationPercent.toFixed(2)}%</strong>
                        </Typography>
                      </Box>
                    )}
                  </>
                ) : loading && currentProjectId ? (
                  <Typography variant="body1" color="text.secondary">
                    Загрузка проекта…
                  </Typography>
                ) : currentProjectId && projectDataQuery.isError ? (
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
                  onClick={() => void projectDataQuery.refetch()}
                  disabled={loading}
                  size="small"
                >
                  <RefreshIcon />
                </IconButton>
              </Tooltip>
            </Stack>

            {error && (
              <Alert severity="error" sx={{ mt: 2 }} onClose={() => setPageError(null)}>
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

        <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
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
        {mainTab === 'forecast' && currentProjectId && hasProject && (
          <ForecastPanel
            projectId={currentProjectId}
            projectName={resolvedProject!.name}
            projectCode={resolvedProject!.code}
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

        {mainTab === 'fact' && currentProjectId && hasProject && (
          <FactPanel projectId={currentProjectId} />
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
        {mainTab === 'compare' && currentProjectId && hasProject && (
          <ComparePanel projectId={currentProjectId} />
        )}
        {mainTab === 'compare' && (!currentProjectId || !hasProject) && (
          <>
            <WorkspaceChromeToggleRow />
            <Alert severity="info" sx={{ mt: 1 }}>
              Выберите проект для сравнения версий.
            </Alert>
          </>
        )}

        {/* ═════════════════════════════════════════════════════════════════
            ВКЛАДКА: ОТЧЁТЫ
           ═════════════════════════════════════════════════════════════════ */}
        {mainTab === 'reports' && (
          <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
            <WorkspaceChromeToggleRow />
            <Stack spacing={3}>
              <Card sx={{ backgroundColor: '#F4F6F7', border: '1px solid #D5D8DC' }}>
                <CardContent>
                  <Typography variant="h6" sx={{ color: '#2C3E50', mb: 1 }}>
                    Сводный отчёт PDF
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#5D6D7E', mb: 2 }}>
                    Единый документ с KPI и ключевыми графиками: прогноз, факт, сравнение версий, тренды.
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Button
                      variant="contained"
                      disabled={
                        !currentProjectId
                        || !planVersionId
                        || !factVersionId
                        || !compareVersionAId
                        || !compareVersionBId
                        || combinedExporting
                      }
                      startIcon={<PictureAsPdfIcon />}
                      onClick={async () => {
                        if (!currentProjectId || !planVersionId || !factVersionId || !compareVersionAId || !compareVersionBId) return;
                        setCombinedExporting(true);
                        try {
                          await exportCombinedProjectPdf(currentProjectId, {
                            planVersionId,
                            factVersionId,
                            compareVersionAId,
                            compareVersionBId,
                          });
                        } finally {
                          setCombinedExporting(false);
                        }
                      }}
                    >
                      Скачать сводный PDF
                    </Button>
                  </Stack>
                </CardContent>
              </Card>

              {/* Группа: Оценка факта */}
              <Card sx={{ backgroundColor: '#EBF5FB', border: '1px solid #AED6F1' }}>
                <CardContent>
                  <Typography variant="h6" sx={{ color: '#1A5276', mb: 1 }}>
                    Оценка факта
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#5D6D7E', mb: 2 }}>
                    Отчёт по производительности: физ. объёмы, трудоёмкость, Z-score по направлениям IIC.
                    На странице «Проекты» доступен только для версии факта (вкладка «Оценка факта»).
                    Для версии плана откройте карточку проекта → «Анализ версии проекта».
                  </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Button
                      variant="contained"
                      disabled={!factVersionId}
                      onClick={() => setMainTab('fact')}
                      sx={{ backgroundColor: '#3498DB', color: 'white' }}
                    >
                      📊 Перейти к оценке факта
                    </Button>
                    <Typography variant="body2" sx={{ color: '#5D6D7E', width: '100%' }}>
                      Экспорт Excel/PDF и анализ версии доступны на вкладке «Оценка факта» → «По объектам» / «Итого».
                    </Typography>
                  </Stack>
                </CardContent>
              </Card>

              {/* Группа: Прогноз */}
              <Card sx={{ backgroundColor: '#EAFAF1', border: '1px solid #A9DFBF' }}>
                <CardContent>
                  <Typography variant="h6" sx={{ color: '#1E8449', mb: 1 }}>
                    Прогноз трудозатрат
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#5D6D7E', mb: 2 }}>
                    Три сценария трудозатрат (базовый, оптимистичный, пессимистичный) по плановым объёмам.
                    Требуется выбранная плановая версия на вкладке «Прогнозирование».
                  </Typography>
                  <Button
                    variant="contained"
                    disabled={!planVersionId}
                    onClick={() => setMainTab('forecast')}
                    sx={{ backgroundColor: '#27AE60', color: 'white' }}
                  >
                    Перейти к прогнозированию
                  </Button>
                </CardContent>
              </Card>

              {/* Группа: Сравнение версий */}
              <Card sx={{ backgroundColor: '#FEF9E7', border: '1px solid #F9E79F' }}>
                <CardContent>
                  <Typography variant="h6" sx={{ color: '#7D6608', mb: 1 }}>
                    Сравнение версий
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#5D6D7E' }}>
                    Для сравнения двух версий перейдите на вкладку «Сравнение версий»,
                    выберите Версию А и Версию Б. Таблица с Δ Z-score отобразится там же.
                  </Typography>
                </CardContent>
              </Card>
            </Stack>
          </Box>
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
        onSaved={() => void projectDataQuery.refetch()}
      />

      <ProjectCharacteristicsDialog
        open={characteristicsOpen && hasProject}
        projectId={currentProjectId ?? 0}
        onClose={() => setCharacteristicsOpen(false)}
        onSaved={() => void projectDataQuery.refetch()}
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
