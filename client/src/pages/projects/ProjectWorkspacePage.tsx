import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  IconButton,
  Paper,
  Stack,
  Tab,
  Tabs,
  Tooltip,
  Typography,
} from '@mui/material';
import SettingsIcon from '@mui/icons-material/Settings';
import PeopleIcon from '@mui/icons-material/People';
import ListAltIcon from '@mui/icons-material/ListAlt';
import RefreshIcon from '@mui/icons-material/Refresh';
import type { ColDef } from 'ag-grid-community';
import { fetchProjectPermissions, type ProjectPermission } from '../../api/projectPermissionsApi';
import {
  copyProjectVersion,
  createProjectVersion,
  fetchProjectVersions,
  type ProjectVersion,
} from '../../api/projectVersionsApi';
import { fetchProjects } from '../../api/projectsApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { EditModal } from '../../components/EditModal/EditModal';
import { ParticipantsModal } from '../../components/ParticipantsModal/ParticipantsModal';
import { ProjectSelectionDialog } from '../../components/ProjectSelectionDialog/ProjectSelectionDialog';
import { ProjectCharacteristicsDialog } from '../../components/ProjectCharacteristicsDialog/ProjectCharacteristicsDialog';
import { ProjectParametersDialog } from '../../components/ProjectParametersDialog/ProjectParametersDialog';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';
import { usePermission } from '../../hooks/usePermission';
import { useAuthStore } from '../../store/useAuthStore';
import type { Project } from '../../types/project';
import type { ProjectTabId } from './projectTabs';
import { getVisibleTabs, getDefaultTab } from './projectTabs';
import { ForecastTab } from './ForecastTab';
import { AnalysisTab } from './AnalysisTab';
import { CompareTab } from './CompareTab';
import { ProjectDataTab } from './ProjectDataTab';

export function ProjectWorkspacePage() {
  const navigate = useNavigate();
  const { projectId } = useParams<{ projectId: string }>();
  const currentPid = projectId ? Number(projectId) : null;

  const { canEdit: canEditVersions } = usePermission('ProjectAnalysisVersions');
  const user = useAuthStore((s) => s.user);

  // Project selection dialog
  const [selectionDialogOpen, setSelectionDialogOpen] = useState(!currentPid);

  // Main project state
  const [project, setProject] = useState<Project | null>(null);
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Version management
  const [versions, setVersions] = useState<ProjectVersion[]>([]);
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);
  const [versionModalOpen, setVersionModalOpen] = useState(false);
  const [versionName, setVersionName] = useState('');

  // Participants (loaded for count display)
  const [, setParticipants] = useState<ProjectPermission[]>([]);
  const [participantsOpen, setParticipantsOpen] = useState(false);

  // Characteristics
  const [characteristicsOpen, setCharacteristicsOpen] = useState(false);

  // Parameters (new feature)
  const [parametersOpen, setParametersOpen] = useState(false);

  // Tabs
  const [activeTabId, setActiveTabId] = useState<ProjectTabId>(getDefaultTab(user?.permissions, user?.roleName));

  // Load projects list (for selection dialog)
  const loadProjects = useCallback(async () => {
    try {
      const data = await fetchProjects();
      setProjects(data);
    } catch {
      setError('Не удалось загрузить список проектов.');
    }
  }, []);

  // Load current project data
  const loadProject = useCallback(async (pid: number) => {
    setLoading(true);
    setError(null);
    try {
      const [projectsData, versionsData, participantsData] = await Promise.all([
        fetchProjects(),
        fetchProjectVersions(pid),
        fetchProjectPermissions(pid).catch(() => [] as ProjectPermission[]),
      ]);

      setProjects(projectsData);
      const currentProject = projectsData.find((p) => p.id === pid) ?? null;
      setProject(currentProject);

      setVersions(versionsData);
      // Initialize selected version if not set
      setSelectedVersionId((prev) => {
        if (prev) return prev;
        return versionsData[0]?.id ?? null;
      });

      setParticipants(participantsData);
    } catch {
      setError('Не удалось загрузить данные проекта.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadProjects();
  }, [loadProjects]);

  useEffect(() => {
    if (currentPid) {
      void loadProject(currentPid);
    }
  }, [currentPid, loadProject]);

  const handleSelectProject = (pid: number) => {
    setSelectionDialogOpen(false);
    navigate(`/projects/${pid}/workspace`);
  };

  const handleCreateVersion = async () => {
    if (!currentPid || !versionName.trim()) return;
    const created = await createProjectVersion(currentPid, {
      versionName: versionName.trim(),
      direction: 0,
      name: versionName.trim(),
    });
    setVersionModalOpen(false);
    setVersionName('');
    setSelectedVersionId(created.id);
    await loadProject(currentPid);
  };

  const handleCopyVersion = async (version: ProjectVersion) => {
    if (!currentPid) return;
    const copied = await copyProjectVersion(currentPid, version.id, {
      newVersionName: `${version.versionName} (копия)`,
    });
    setSelectedVersionId(copied.id);
    await loadProject(currentPid);
  };

  const visibleTabs = useMemo(
    () => getVisibleTabs(user?.permissions, user?.roleName),
    [user?.permissions, user?.roleName]
  );

  const versionColumns = useMemo<ColDef<ProjectVersion>[]>(
    () => [
      { field: 'versionName', headerName: 'Версия', minWidth: 120 },
      { field: 'name', headerName: 'Наименование', minWidth: 200 },
      { field: 'isArchived', headerName: 'Архив', width: 90 },
    ],
    []
  );

  // If no project selected, show selection dialog
  if (!currentPid) {
    return (
      <Box sx={{ p: 3 }}>
        <Card sx={{ maxWidth: 600, mx: 'auto', mt: 4 }}>
          <CardContent sx={{ textAlign: 'center', py: 6 }}>
            <Typography variant="h5" gutterBottom>
              Проект не выбран
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
              Для работы необходимо выбрать проект из списка
            </Typography>
            <Button
              variant="contained"
              size="large"
              onClick={() => setSelectionDialogOpen(true)}
            >
              Выбрать проект
            </Button>
          </CardContent>
        </Card>

        <ProjectSelectionDialog
          open={selectionDialogOpen}
          projects={projects}
          onSelect={handleSelectProject}
          onClose={() => setSelectionDialogOpen(false)}
          onCreateNew={() => navigate('/projects/new')}
        />
      </Box>
    );
  }

  const selectedVersion = versions.find((v) => v.id === selectedVersionId);

  return (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <WorkingOverlay open={loading} delayMs={1000} />
      {/* Header Section - WPF Style */}
      <Paper elevation={1} sx={{ p: 2, mb: 2 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} md={6}>
            <Stack direction="row" spacing={2} alignItems="center">
              <Button
                variant="outlined"
                onClick={() => setSelectionDialogOpen(true)}
                startIcon={<ListAltIcon />}
              >
                Выбрать проект
              </Button>
              {project && (
                <Box>
                  <Typography variant="h6" component="span">
                    {project.code}
                  </Typography>
                  <Typography variant="body1" color="text.secondary">
                    {project.name}
                  </Typography>
                </Box>
              )}
            </Stack>
          </Grid>
          <Grid item xs={12} md={6}>
            <Stack direction="row" spacing={1} justifyContent="flex-end">
              <Tooltip title="Участники проекта">
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<PeopleIcon />}
                  onClick={() => setParticipantsOpen(true)}
                >
                  Участники
                </Button>
              </Tooltip>
              <Tooltip title="Характеристики проекта">
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<ListAltIcon />}
                  onClick={() => setCharacteristicsOpen(true)}
                >
                  Характеристики
                </Button>
              </Tooltip>
              <Tooltip title="Параметры проекта">
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<SettingsIcon />}
                  onClick={() => setParametersOpen(true)}
                >
                  Параметры
                </Button>
              </Tooltip>
              <Tooltip title="Обновить">
                <IconButton onClick={() => currentPid && void loadProject(currentPid)}>
                  <RefreshIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          </Grid>
        </Grid>

        {selectedVersion && (
          <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
            <Stack direction="row" spacing={2} alignItems="center">
              <Typography variant="subtitle2">Версия анализа:</Typography>
              <Typography variant="body2">
                {selectedVersion.versionName} — {selectedVersion.name}
              </Typography>
            </Stack>
          </Box>
        )}
      </Paper>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {/* Version Selector */}
      {versions.length > 0 && (
        <Box sx={{ mb: 2, px: 2 }}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Typography variant="body2" color="text.secondary">
              Версия:
            </Typography>
            <Tabs
              value={selectedVersionId ?? false}
              onChange={(_, value) => setSelectedVersionId(value)}
              variant="scrollable"
              scrollButtons="auto"
            >
              {versions.map((v) => (
                <Tab
                  key={v.id}
                  value={v.id}
                  label={`${v.versionName}`}
                />
              ))}
            </Tabs>
            {canEditVersions && (
              <Button
                size="small"
                variant="outlined"
                onClick={() => setVersionModalOpen(true)}
              >
                Новая версия
              </Button>
            )}
          </Stack>
        </Box>
      )}

      <Divider sx={{ mb: 2 }} />

      {/* Main Content Tabs */}
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
        <Tabs
          value={visibleTabs.findIndex((t) => t.id === activeTabId)}
          onChange={(_, index) => setActiveTabId(visibleTabs[index]?.id ?? activeTabId)}
          sx={{ px: 2, borderBottom: 1, borderColor: 'divider' }}
        >
          {visibleTabs.map((tab) => (
            <Tab key={tab.id} label={tab.label} />
          ))}
        </Tabs>

        <Box sx={{ flex: 1, p: 2, overflow: 'auto' }}>
          {/* Version Management Tab */}
          {activeTabId === 'versions' && (
            <Stack spacing={2}>
              {canEditVersions && (
                <Button
                  variant="contained"
                  sx={{ alignSelf: 'flex-start' }}
                  onClick={() => setVersionModalOpen(true)}
                >
                  Новая версия
                </Button>
              )}
              <DataTable
                rowData={versions}
                columnDefs={versionColumns}
                loading={loading}
                objectName="ProjectAnalysisVersions"
                gridId={`project-${currentPid}-versions`}
                contextMenuItems={
                  canEditVersions
                    ? [{
                        name: 'Копировать',
                        action: (row) => row && void handleCopyVersion(row),
                        requiredLevel: 2
                      }]
                    : []
                }
              />
            </Stack>
          )}

          {/* Project Data Tab */}
          {activeTabId === 'data' && currentPid && selectedVersionId && (
            <ProjectDataTab
              projectId={currentPid}
              versionId={selectedVersionId}
            />
          )}

          {/* Forecast Tab */}
          {activeTabId === 'forecast' && currentPid && (
            <ForecastTab projectId={currentPid} versionId={selectedVersionId} />
          )}

          {/* Analysis Tab */}
          {activeTabId === 'analysis' && currentPid && selectedVersionId && (
            <AnalysisTab projectId={currentPid} versionId={selectedVersionId} />
          )}

          {/* Compare Tab */}
          {activeTabId === 'compare' && currentPid && (
            <CompareTab projectId={currentPid} />
          )}
        </Box>
      </Box>

      {/* Dialogs */}
      <ProjectSelectionDialog
        open={selectionDialogOpen}
        projects={projects}
        onSelect={handleSelectProject}
        onClose={() => setSelectionDialogOpen(false)}
        onCreateNew={() => navigate('/projects/new')}
      />

      <ParticipantsModal
        open={participantsOpen}
        projectId={currentPid}
        onClose={() => setParticipantsOpen(false)}
        onSaved={() => currentPid && void loadProject(currentPid)}
      />

      <ProjectCharacteristicsDialog
        open={characteristicsOpen}
        projectId={currentPid}
        onClose={() => setCharacteristicsOpen(false)}
      />

      <ProjectParametersDialog
        open={parametersOpen}
        projectId={currentPid}
        onClose={() => setParametersOpen(false)}
      />

      <EditModal
        open={versionModalOpen}
        title="Новая версия анализа"
        onClose={() => setVersionModalOpen(false)}
        onSubmit={handleCreateVersion}
      >
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Создание новой версии анализа для проекта
        </Typography>
        <Typography variant="body1" gutterBottom>
          {project?.name}
        </Typography>
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2" component="label" sx={{ display: 'block', mb: 1 }}>
            Название версии
          </Typography>
          <input
            type="text"
            value={versionName}
            onChange={(e) => setVersionName(e.target.value)}
            style={{ width: '100%', padding: '8px 12px', fontSize: '14px' }}
            placeholder="Введите название версии"
          />
        </Box>
      </EditModal>
    </Box>
  );
}
