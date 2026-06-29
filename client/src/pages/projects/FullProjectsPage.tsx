import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Divider,
  FormControlLabel,
  Grid,
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
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import DeleteIcon from '@mui/icons-material/Delete';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import type { ColDef } from 'ag-grid-community';
import { fetchProjectPermissions, type ProjectPermission } from '../../api/projectPermissionsApi';
import {
  fetchProjectVersions,
  type ProjectVersion,
} from '../../api/projectVersionsApi';
import { createProject, fetchProjects, updateProject } from '../../api/projectsApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { usePermission } from '../../hooks/usePermission';
import type { Project } from '../../types/project';
import type { ProjectFormData } from '../../types/project';
import { ProjectSelectionDialog } from '../../components/ProjectSelectionDialog/ProjectSelectionDialog';
import { ParticipantsModal } from '../../components/ParticipantsModal/ParticipantsModal';
import { EditModal } from '../../components/EditModal/EditModal';
import { ProjectCharacteristicsDialog } from '../../components/ProjectCharacteristicsDialog/ProjectCharacteristicsDialog';

// Типы для данных гридов
type ForecastTabId = 'forecast-total' | 'forecast-facility' | 'forecast-detail';
type FactTabId = 'fact-total' | 'fact-facility' | 'fact-detail';
type CompareTabId = 'compare-total' | 'compare-facility' | 'compare-detail';

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
  const [projectsData, versionsData, participantsData] = await Promise.all([
    fetchProjects(),
    fetchProjectVersions(projectId),
    fetchProjectPermissions(projectId).catch(() => [] as ProjectPermission[]),
  ]);

  const currentProject = projectsData.find((p) => p.id === projectId) ?? null;
  const planVersion = versionsData[0] ?? null;
  const factVersion = versionsData[1] ?? planVersion;

  return {
    project: currentProject,
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
    deviationPercent: null,
  };
}

export function FullProjectsPage() {
  const navigate = useNavigate();
  const { projectId: projectIdParam } = useParams<{ projectId: string }>();
  const currentProjectId = projectIdParam ? Number(projectIdParam) : null;

  const { canRead } = usePermission('Projects');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');

  // Диалоги
  const [selectionDialogOpen, setSelectionDialogOpen] = useState(!currentProjectId);
  const [participantsOpen, setParticipantsOpen] = useState(false);
  const [characteristicsOpen, setCharacteristicsOpen] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [compareShowOnlyDifferences, setCompareShowOnlyDifferences] = useState(false);
  const [projectModalOpen, setProjectModalOpen] = useState(false);
  const [projectModalMode, setProjectModalMode] = useState<'create' | 'edit'>('edit');
  const [projectEditingId, setProjectEditingId] = useState<number | null>(null);
  const [projectForm, setProjectForm] = useState<ProjectFormData>(EMPTY_PROJECT_FORM);
  const [projectSubmitting, setProjectSubmitting] = useState(false);

  // Активные вкладки
  const [mainTab, setMainTab] = useState<'forecast' | 'fact' | 'compare' | 'reports'>('forecast');
  const [forecastTab, setForecastTab] = useState<ForecastTabId>('forecast-total');
  const [factTab, setFactTab] = useState<FactTabId>('fact-total');
  const [compareTab, setCompareTab] = useState<CompareTabId>('compare-total');

  const projectsQuery = useQuery<Project[]>({
    queryKey: ['projects', 'all'],
    queryFn: fetchProjects,
    enabled: canRead,
  });

  const projectDataQuery = useQuery<ProjectsPageData>({
    queryKey: ['projects', 'workspace', currentProjectId],
    queryFn: () => fetchWorkspaceData(currentProjectId as number),
    enabled: Boolean(currentProjectId && canRead),
  });

  const projects = projectsQuery.data ?? [];
  const data = projectDataQuery.data ?? EMPTY_DATA;
  const loading = projectsQuery.isLoading || projectDataQuery.isLoading;
  const error = pageError
    ?? (projectsQuery.isError
      ? 'Не удалось загрузить список проектов.'
      : projectDataQuery.isError
        ? 'Не удалось загрузить данные проекта.'
        : null);

  // Обработчики
  const handleSelectProject = (pid: number) => {
    setSelectionDialogOpen(false);
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
    setProjectSubmitting(true);
    setPageError(null);
    try {
      if (projectModalMode === 'edit' && projectEditingId) {
        await updateProject(projectEditingId, projectForm);
        setProjectModalOpen(false);
        await Promise.all([projectsQuery.refetch(), projectDataQuery.refetch()]);
      } else {
        const created = await createProject(projectForm);
        setProjectModalOpen(false);
        await projectsQuery.refetch();
        navigate(`/projects/${created.id}`);
      }
    } catch {
      setPageError('Не удалось сохранить проект.');
    } finally {
      setProjectSubmitting(false);
    }
  };

  // Заглушки для импорта/удаления
  const handleImport = (type: string) => {
    window.alert(`Импорт ${type} — в разработке`);
  };

  const handleDelete = (type: string) => {
    if (window.confirm(`Удалить ${type}?`)) {
      window.alert(`Удаление ${type} — в разработке`);
    }
  };

  // Колонки для гридов (заглушки)
  const forecastSummaryColumns: ColDef[] = useMemo(() => [
    { field: 'name', headerName: 'Наименование', minWidth: 200 },
    { field: 'unit', headerName: 'Ед. изм.', width: 100 },
    { field: 'factVolume', headerName: 'Факт ФО', width: 120 },
    { field: 'normProductivity', headerName: 'Норм. произв.', width: 140 },
    { field: 'baseHours', headerName: 'Базовый, ч', width: 120 },
    { field: 'optimisticHours', headerName: 'Оптимист., ч', width: 130 },
    { field: 'pessimisticHours', headerName: 'Пессимист., ч', width: 140 },
  ], []);

  const forecastFacilityColumns: ColDef[] = useMemo(() => [
    { field: 'facilityCode', headerName: 'Шифр объекта', width: 120 },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 200 },
    { field: 'baseHours', headerName: 'Базовый, ч', width: 120 },
    { field: 'optimisticHours', headerName: 'Оптимист., ч', width: 130 },
    { field: 'pessimisticHours', headerName: 'Пессимист., ч', width: 140 },
  ], []);

  const forecastDetailColumns: ColDef[] = useMemo(() => [
    { field: 'facilityCode', headerName: 'Шифр объекта', width: 100 },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 150 },
    { field: 'bindingType', headerName: 'Тип привязки', width: 100 },
    { field: 'documentSet', headerName: 'Комплект документов', width: 140 },
    { field: 'catalogId', headerName: 'ID по каталогу', width: 110 },
    { field: 'tag', headerName: 'TAG', width: 80 },
    { field: 'workId', headerName: 'ID работы', width: 100 },
    { field: 'itemName', headerName: 'Наименование', minWidth: 150 },
    { field: 'pvCode', headerName: 'Код', width: 80 },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 150 },
    { field: 'coefficient', headerName: 'Коэфф.', width: 80 },
    { field: 'iicGroup', headerName: 'Группа ФО IIC', width: 130 },
    { field: 'quantity', headerName: 'Кол-во', width: 90 },
    { field: 'baseHours', headerName: 'Базовый, ч', width: 100 },
    { field: 'optimisticHours', headerName: 'Оптимист., ч', width: 110 },
    { field: 'pessimisticHours', headerName: 'Пессимист., ч', width: 120 },
  ], []);

  const factPerformanceColumns: ColDef[] = useMemo(() => [
    { field: 'code', headerName: 'Код', width: 80 },
    { field: 'name', headerName: 'Наименование', minWidth: 200 },
    { field: 'unit', headerName: 'Ед. изм.', width: 80 },
    { field: 'factLaborHours', headerName: 'Факт.трудоёмк., ч', width: 140 },
    { field: 'factProductivity', headerName: 'Факт произв.', width: 120 },
    { field: 'normProductivity', headerName: 'Норм произв.', width: 120 },
    { field: 'zScore', headerName: 'Z-score', width: 100 },
    { field: 'percentile', headerName: 'Перцентиль', width: 100 },
    { field: 'interpretation', headerName: 'Интерпретация', minWidth: 180 },
  ], []);

  const factFacilityColumns: ColDef[] = useMemo(() => [
    { field: 'code', headerName: 'Код', width: 100 },
    { field: 'name', headerName: 'Наименование', minWidth: 200 },
    { field: 'factLaborHours', headerName: 'Факт.трудоёмк., ч', width: 140 },
    { field: 'zScore', headerName: 'Z-score', width: 100 },
    { field: 'percentile', headerName: 'Перцентиль', width: 100 },
    { field: 'interpretation', headerName: 'Интерпретация', minWidth: 180 },
  ], []);

  const factDetailColumns: ColDef[] = useMemo(() => [
    { field: 'facilityCode', headerName: 'Шифр объекта', width: 100 },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 150 },
    { field: 'documentSet', headerName: 'Комплект', width: 100 },
    { field: 'pvCode', headerName: 'Код ФО', width: 80 },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 150 },
    { field: 'workId', headerName: 'ID работы', width: 90 },
    { field: 'workName', headerName: 'Наименование работы', minWidth: 150 },
    { field: 'iicGroup', headerName: 'Группа IIC', width: 120 },
    { field: 'unit', headerName: 'Ед. изм.', width: 70 },
    { field: 'quantity', headerName: 'Кол-во', width: 80 },
    { field: 'factLaborHours', headerName: 'Факт.трудоёмк., ч', width: 130 },
    { field: 'factProductivity', headerName: 'Факт произв.', width: 110 },
    { field: 'normProductivity', headerName: 'Норм произв.', width: 110 },
    { field: 'zScore', headerName: 'Z-score', width: 90 },
    { field: 'percentile', headerName: 'Перцентиль', width: 100 },
    { field: 'interpretation', headerName: 'Интерпретация', minWidth: 150 },
  ], []);

  const compareSummaryColumns: ColDef[] = useMemo(() => [
    { field: 'name', headerName: 'Направление', minWidth: 200 },
    { field: 'zScoreA', headerName: 'Z-score А', width: 100 },
    { field: 'interpretationA', headerName: 'Оценка А', width: 120 },
    { field: 'zScoreB', headerName: 'Z-score Б', width: 100 },
    { field: 'interpretationB', headerName: 'Оценка Б', width: 120 },
    { field: 'zDelta', headerName: 'Δ Z-score', width: 100 },
  ], []);

  // Если нет проекта
  if (!currentProjectId) {
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
          onCreateNew={openCreateProject}
          onEditSelected={openEditProject}
          onCopySelected={openCopyProject}
        />
      </Box>
    );
  }

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр проектов.</Alert>;
  }

  const { project, versions: _versions, participants: _participants, planVersionId, planVersionLabel, factVersionId, factVersionLabel } = data;

  return (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* ═════════════════════════════════════════════════════════════════
          ВЕРХНЯЯ ПАНЕЛЬ: Информация о проекте и кнопки действий
         ═════════════════════════════════════════════════════════════════ */}
      <Paper elevation={1} sx={{ p: 2, mb: 2, backgroundColor: '#F8F9FA' }}>
        {/* Строка 1: Код, Название, Точность, Отклонение */}
        <Box sx={{ mb: 2 }}>
          {project ? (
            <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
              {/* Код проекта */}
              <Box sx={{ 
                backgroundColor: '#2C3E50', 
                color: 'white', 
                px: 1.5, 
                py: 0.5, 
                borderRadius: 1,
                fontWeight: 'bold',
                fontSize: '0.875rem'
              }}>
                {project.code}
              </Box>
              
              {/* Название проекта */}
              <Typography variant="h6" sx={{ color: '#2C3E50', fontWeight: 500 }}>
                {project.name}
              </Typography>

              {/* Точность оценки */}
              {data.accuracyScore !== null && (
                <Box sx={{ 
                  backgroundColor: '#EBF5FB', 
                  border: '1px solid #AED6F1',
                  px: 1.5, 
                  py: 0.5, 
                  borderRadius: 1 
                }}>
                  <Typography variant="body2" sx={{ color: '#1A5276' }}>
                    Точность оценки: <strong>{data.accuracyScore.toFixed(1)}%</strong>
                  </Typography>
                </Box>
              )}

              {/* Отклонение */}
              {data.deviationPercent !== null && (
                <Box sx={{ 
                  backgroundColor: '#FDFEFE', 
                  border: '1px solid #D5D8DC',
                  px: 1.5, 
                  py: 0.5, 
                  borderRadius: 1 
                }}>
                  <Typography variant="body2" sx={{ color: '#616A6B' }}>
                    Отклонение: <strong>{data.deviationPercent.toFixed(2)}%</strong>
                  </Typography>
                </Box>
              )}
            </Stack>
          ) : (
            <Typography variant="body1" color="text.secondary">
              Проект не выбран
            </Typography>
          )}
        </Box>

        {/* Строка 2: Кнопки действий */}
        <Stack direction="row" spacing={1} flexWrap="wrap">
          <Button
            variant="contained"
            onClick={() => setSelectionDialogOpen(true)}
            sx={{ 
              backgroundColor: '#3498DB', 
              color: 'white',
              '&:hover': { backgroundColor: '#2980B9' }
            }}
          >
            Выбрать проект
          </Button>
          
          <Button
            variant="outlined"
            disabled={!project}
            onClick={() => project && openEditProject(project)}
          >
            Основные данные
          </Button>
          
          <Button
            variant="outlined"
            disabled={!project}
            onClick={() => setCharacteristicsOpen(true)}
          >
            Характеристики
          </Button>
          
          <Button
            variant="outlined"
            disabled={!project}
            onClick={() => setParticipantsOpen(true)}
          >
            Участники
          </Button>

          <Tooltip title="Обновить">
            <IconButton 
              onClick={() => void projectDataQuery.refetch()}
              disabled={loading}
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

      <Divider sx={{ mb: 2 }} />

      {/* ═════════════════════════════════════════════════════════════════
          ГЛАВНЫЕ ВКЛАДКИ: Прогнозирование / Оценка факта / Сравнение / Отчёты
         ═════════════════════════════════════════════════════════════════ */}
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
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
            sx={{
              '& .MuiTabs-indicator': { display: 'none' },
            }}
          >
            <Tab
              label="Прогнозирование"
              value="forecast"
              sx={{
                textTransform: 'none',
                bgcolor: '#DCE5EE',
                color: '#2C3E50',
                '&.Mui-selected': {
                  bgcolor: '#5D6D7E',
                  color: '#FFFFFF',
                },
              }}
            />
            <Tab
              label="Оценка факта"
              value="fact"
              sx={{
                textTransform: 'none',
                bgcolor: '#DCE5EE',
                color: '#2C3E50',
                '&.Mui-selected': {
                  bgcolor: '#5D6D7E',
                  color: '#FFFFFF',
                },
              }}
            />
            <Tab
              label="Сравнение версий"
              value="compare"
              sx={{
                textTransform: 'none',
                bgcolor: '#DCE5EE',
                color: '#2C3E50',
                '&.Mui-selected': {
                  bgcolor: '#5D6D7E',
                  color: '#FFFFFF',
                },
              }}
            />
            <Tab
              label="Отчёты"
              value="reports"
              sx={{
                textTransform: 'none',
                bgcolor: '#DCE5EE',
                color: '#2C3E50',
                '&.Mui-selected': {
                  bgcolor: '#5D6D7E',
                  color: '#FFFFFF',
                },
              }}
            />
          </Tabs>
        </Paper>

        {/* ═════════════════════════════════════════════════════════════════
            ВКЛАДКА: ПРОГНОЗИРОВАНИЕ
           ═════════════════════════════════════════════════════════════════ */}
        {mainTab === 'forecast' && (
          <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
            {/* Верхняя панель версии и кнопок импорта */}
            <Grid container spacing={2} alignItems="center" sx={{ mb: 2 }}>
              <Grid item xs={12} md={6}>
                <Stack direction="row" spacing={2} alignItems="center">
                  <Button
                    variant="contained"
                    onClick={() => window.alert('Выбор версии плана — в разработке')}
                    sx={{ 
                      backgroundColor: '#2980B9', 
                      color: 'white',
                      '&:hover': { backgroundColor: '#2471A3' }
                    }}
                  >
                    📋 Версия (план)
                  </Button>
                  {planVersionLabel && (
                    <Box sx={{ 
                      backgroundColor: '#EBF5FB', 
                      border: '1px solid #AED6F1',
                      px: 1.5, 
                      py: 0.5, 
                      borderRadius: 1 
                    }}>
                      <Typography variant="body2" sx={{ color: '#1A5276' }}>
                        {planVersionLabel}
                      </Typography>
                    </Box>
                  )}
                </Stack>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Stack direction="row" spacing={1} justifyContent="flex-end" flexWrap="wrap">
                  {canImport && (
                    <>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!planVersionId}
                        onClick={() => handleImport('физ.объёмов')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Импорт ФО
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!planVersionId}
                        onClick={() => handleDelete('физ.объёмов')}
                        startIcon={<DeleteIcon />}
                        color="error"
                      >
                        Удалить ФО
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!planVersionId}
                        onClick={() => handleImport('оборудования')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Импорт оборуд.
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!planVersionId}
                        onClick={() => handleDelete('оборудования')}
                        startIcon={<DeleteIcon />}
                        color="error"
                      >
                        Удалить оборуд.
                      </Button>
                    </>
                  )}
                </Stack>
              </Grid>
            </Grid>

            {/* Внутренние вкладки */}
            <Tabs
              value={forecastTab}
              onChange={(_, value) => setForecastTab(value)}
              sx={{ mb: 2 }}
            >
              <Tab label="Итого" value="forecast-total" />
              <Tab label="По объектам" value="forecast-facility" />
              <Tab label="Детально" value="forecast-detail" />
            </Tabs>

            {/* Содержимое вкладок */}
            <Box sx={{ flex: 1 }}>
              {forecastTab === 'forecast-total' && (
                <Grid container spacing={2} sx={{ height: '100%' }}>
                  <Grid item xs={12} md={10}>
                    <DataTable
                      rowData={[]} // TODO: загрузить данные
                      columnDefs={forecastSummaryColumns}
                      objectName="ForecastSummary"
                      gridId="forecast-summary"
                      loading={loading}
                      height="100%"
                    />
                  </Grid>
                  <Grid item xs={12} md={2}>
                    <Button
                      variant="contained"
                      fullWidth
                      disabled={!planVersionId}
                      onClick={() => window.alert('Открыть прогноз — в разработке')}
                      sx={{ 
                        backgroundColor: '#27AE60', 
                        color: 'white',
                        height: 40
                      }}
                    >
                      📊 Прогноз
                    </Button>
                  </Grid>
                </Grid>
              )}

              {forecastTab === 'forecast-facility' && (
                <Grid container spacing={2} sx={{ height: '100%' }}>
                  <Grid item xs={12} md={10}>
                    <DataTable
                      rowData={[]}
                      columnDefs={forecastFacilityColumns}
                      objectName="ForecastFacility"
                      gridId="forecast-facility"
                      loading={loading}
                      height="100%"
                    />
                  </Grid>
                  <Grid item xs={12} md={2}>
                    <Stack spacing={1}>
                      <Button
                        variant="outlined"
                        fullWidth
                        disabled={!planVersionId}
                        onClick={() => window.alert('Экспорт Excel — в разработке')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Экспорт Excel
                      </Button>
                      <Button
                        variant="outlined"
                        fullWidth
                        disabled={!planVersionId}
                        onClick={() => window.alert('Экспорт PDF — в разработке')}
                        startIcon={<PictureAsPdfIcon />}
                      >
                        Экспорт PDF
                      </Button>
                    </Stack>
                  </Grid>
                </Grid>
              )}

              {forecastTab === 'forecast-detail' && (
                <Grid container spacing={2} sx={{ height: '100%' }}>
                  <Grid item xs={12} md={10}>
                    <DataTable
                      rowData={[]}
                      columnDefs={forecastDetailColumns}
                      objectName="ForecastDetail"
                      gridId="forecast-detail"
                      loading={loading}
                      height="100%"
                    />
                  </Grid>
                  <Grid item xs={12} md={2}>
                    <Stack spacing={1}>
                      <Button
                        variant="outlined"
                        fullWidth
                        disabled={!planVersionId}
                        onClick={() => window.alert('Экспорт Excel — в разработке')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Экспорт Excel
                      </Button>
                      <Button
                        variant="outlined"
                        fullWidth
                        disabled={!planVersionId}
                        onClick={() => window.alert('Сводная таблица — в разработке')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Сводная таблица
                      </Button>
                    </Stack>
                  </Grid>
                </Grid>
              )}
            </Box>
          </Box>
        )}

        {/* ═════════════════════════════════════════════════════════════════
            ВКЛАДКА: ОЦЕНКА ФАКТА
           ═════════════════════════════════════════════════════════════════ */}
        {mainTab === 'fact' && (
          <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
            {/* Верхняя панель версии и кнопок импорта */}
            <Grid container spacing={2} alignItems="center" sx={{ mb: 2 }}>
              <Grid item xs={12} md={6}>
                <Stack direction="row" spacing={2} alignItems="center">
                  <Button
                    variant="contained"
                    onClick={() => window.alert('Выбор версии факта — в разработке')}
                    sx={{ 
                      backgroundColor: '#2980B9', 
                      color: 'white',
                      '&:hover': { backgroundColor: '#2471A3' }
                    }}
                  >
                    📋 Версия (факт)
                  </Button>
                  {factVersionLabel && (
                    <Box sx={{ 
                      backgroundColor: '#EBF5FB', 
                      border: '1px solid #AED6F1',
                      px: 1.5, 
                      py: 0.5, 
                      borderRadius: 1 
                    }}>
                      <Typography variant="body2" sx={{ color: '#1A5276' }}>
                        {factVersionLabel}
                      </Typography>
                    </Box>
                  )}
                </Stack>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Stack direction="row" spacing={1} justifyContent="flex-end" flexWrap="wrap">
                  {canImport && (
                    <>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!factVersionId}
                        onClick={() => handleImport('физ.объёмов')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Импорт ФО
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!factVersionId}
                        onClick={() => handleDelete('физ.объёмов')}
                        startIcon={<DeleteIcon />}
                        color="error"
                      >
                        Удалить ФО
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!factVersionId}
                        onClick={() => handleImport('оборудования')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Импорт оборуд.
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!factVersionId}
                        onClick={() => handleDelete('оборудования')}
                        startIcon={<DeleteIcon />}
                        color="error"
                      >
                        Удалить оборуд.
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!factVersionId}
                        onClick={() => handleImport('трудоёмкости')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Импорт труд.
                      </Button>
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={!factVersionId}
                        onClick={() => handleDelete('трудоёмкости')}
                        startIcon={<DeleteIcon />}
                        color="error"
                      >
                        Удалить труд.
                      </Button>
                    </>
                  )}
                </Stack>
              </Grid>
            </Grid>

            {/* Внутренние вкладки */}
            <Tabs
              value={factTab}
              onChange={(_, value) => setFactTab(value)}
              sx={{ mb: 2 }}
            >
              <Tab label="Итого" value="fact-total" />
              <Tab label="По объектам" value="fact-facility" />
              <Tab label="Детально" value="fact-detail" />
            </Tabs>

            {/* Содержимое вкладок */}
            <Box sx={{ flex: 1 }}>
              {factTab === 'fact-total' && (
                <Grid container spacing={2} sx={{ height: '100%' }}>
                  <Grid item xs={12} md={10}>
                    <DataTable
                      rowData={[]}
                      columnDefs={factPerformanceColumns}
                      objectName="FactPerformance"
                      gridId="fact-performance"
                      loading={loading}
                      height="100%"
                    />
                  </Grid>
                  <Grid item xs={12} md={2}>
                    <Button
                      variant="contained"
                      fullWidth
                      disabled={!factVersionId}
                      onClick={() => window.alert('Анализ версии — в разработке')}
                      sx={{ 
                        backgroundColor: '#3498DB', 
                        color: 'white',
                        height: 40
                      }}
                    >
                      📊 Анализ версии
                    </Button>
                  </Grid>
                </Grid>
              )}

              {factTab === 'fact-facility' && (
                <Grid container spacing={2} sx={{ height: '100%' }}>
                  <Grid item xs={12} md={10}>
                    <DataTable
                      rowData={[]}
                      columnDefs={factFacilityColumns}
                      objectName="FactFacility"
                      gridId="fact-facility"
                      loading={loading}
                      height="100%"
                    />
                  </Grid>
                  <Grid item xs={12} md={2}>
                    <Stack spacing={1}>
                      <Button
                        variant="outlined"
                        fullWidth
                        disabled={!factVersionId}
                        onClick={() => window.alert('Экспорт Excel — в разработке')}
                        startIcon={<FileDownloadIcon />}
                      >
                        Экспорт Excel
                      </Button>
                      <Button
                        variant="outlined"
                        fullWidth
                        disabled={!factVersionId}
                        onClick={() => window.alert('Экспорт PDF — в разработке')}
                        startIcon={<PictureAsPdfIcon />}
                      >
                        Экспорт PDF
                      </Button>
                    </Stack>
                  </Grid>
                </Grid>
              )}

              {factTab === 'fact-detail' && (
                <Box sx={{ height: '100%' }}>
                  <DataTable
                    rowData={[]}
                    columnDefs={factDetailColumns}
                    objectName="FactDetail"
                    gridId="fact-detail"
                    loading={loading}
                    height="100%"
                  />
                </Box>
              )}
            </Box>
          </Box>
        )}

        {/* ═════════════════════════════════════════════════════════════════
            ВКЛАДКА: СРАВНЕНИЕ ВЕРСИЙ
           ═════════════════════════════════════════════════════════════════ */}
        {mainTab === 'compare' && (
          <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
            {/* Выбор двух версий */}
            <Grid container spacing={2} alignItems="center" sx={{ mb: 2 }}>
              <Grid item xs={12} md={5}>
                <Stack direction="row" spacing={2} alignItems="center">
                  <Button
                    variant="contained"
                    onClick={() => window.alert('Выбор версии А — в разработке')}
                    sx={{ 
                      backgroundColor: '#2980B9', 
                      color: 'white'
                    }}
                  >
                    📋 Версия А
                  </Button>
                  {data.compareVersionAId && (
                    <Box sx={{ 
                      backgroundColor: '#EBF5FB', 
                      border: '1px solid #AED6F1',
                      px: 1.5, 
                      py: 0.5, 
                      borderRadius: 1 
                    }}>
                      <Typography variant="body2" sx={{ color: '#1A5276' }}>
                        Версия {data.compareVersionAId}
                      </Typography>
                    </Box>
                  )}
                </Stack>
              </Grid>
              
              <Grid item xs={12} md={5}>
                <Stack direction="row" spacing={2} alignItems="center">
                  <Button
                    variant="contained"
                    onClick={() => window.alert('Выбор версии Б — в разработке')}
                    sx={{ 
                      backgroundColor: '#8E44AD', 
                      color: 'white'
                    }}
                  >
                    📋 Версия Б
                  </Button>
                  {data.compareVersionBId && (
                    <Box sx={{ 
                      backgroundColor: '#F5EEF8', 
                      border: '1px solid #D7BDE2',
                      px: 1.5, 
                      py: 0.5, 
                      borderRadius: 1 
                    }}>
                      <Typography variant="body2" sx={{ color: '#6C3483' }}>
                        Версия {data.compareVersionBId}
                      </Typography>
                    </Box>
                  )}
                </Stack>
              </Grid>
            </Grid>

            {/* Кнопки импорта для сравнения */}
            {canImport && (
              <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
                <Button
                  variant="outlined"
                  size="small"
                  onClick={() => handleImport('оборудования (версия А)')}
                  startIcon={<FileDownloadIcon />}
                >
                  Импорт оборуд.
                </Button>
                <Button
                  variant="outlined"
                  size="small"
                  onClick={() => handleDelete('оборудования (версия А)')}
                  startIcon={<DeleteIcon />}
                  color="error"
                >
                  Удалить оборуд.
                </Button>
              </Stack>
            )}

            {/* Чекбокс различий */}
            <FormControlLabel
              control={
                <Checkbox
                  checked={compareShowOnlyDifferences}
                  onChange={(e) => setCompareShowOnlyDifferences(e.target.checked)}
                />
              }
              label="Показывать различия"
              sx={{ mb: 2 }}
            />

            {/* Внутренние вкладки */}
            <Tabs
              value={compareTab}
              onChange={(_, value) => setCompareTab(value)}
              sx={{ mb: 2 }}
            >
              <Tab label="Итого" value="compare-total" />
              <Tab label="По объектам" value="compare-facility" />
              <Tab label="Детально" value="compare-detail" />
            </Tabs>

            {/* Содержимое вкладок */}
            <Box sx={{ flex: 1 }}>
              {compareTab === 'compare-total' && (
                <DataTable
                  rowData={[]}
                  columnDefs={compareSummaryColumns}
                  objectName="CompareSummary"
                  gridId="compare-summary"
                  loading={loading}
                  height="100%"
                />
              )}

              {compareTab === 'compare-facility' && (
                <Typography variant="body2" color="text.secondary">
                  Сравнение по объектам — в разработке
                </Typography>
              )}

              {compareTab === 'compare-detail' && (
                <Typography variant="body2" color="text.secondary">
                  Детальное сравнение — в разработке
                </Typography>
              )}
            </Box>
          </Box>
        )}

        {/* ═════════════════════════════════════════════════════════════════
            ВКЛАДКА: ОТЧЁТЫ
           ═════════════════════════════════════════════════════════════════ */}
        {mainTab === 'reports' && (
          <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
            <Stack spacing={3}>
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
                      onClick={() => window.alert('Анализ версии — в разработке')}
                      sx={{ backgroundColor: '#3498DB', color: 'white' }}
                    >
                      📊 Анализ версии (экран)
                    </Button>
                    <Button
                      variant="contained"
                      disabled={!factVersionId}
                      onClick={() => window.alert('Экспорт Excel — в разработке')}
                      sx={{ backgroundColor: '#27AE60', color: 'white' }}
                      startIcon={<FileDownloadIcon />}
                    >
                      Экспорт в Excel
                    </Button>
                    <Button
                      variant="contained"
                      disabled={!factVersionId}
                      onClick={() => window.alert('Экспорт PDF — в разработке')}
                      sx={{ backgroundColor: '#E74C3C', color: 'white' }}
                      startIcon={<PictureAsPdfIcon />}
                    >
                      Экспорт в PDF
                    </Button>
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
                    onClick={() => window.alert('Экспорт прогноза — в разработке')}
                    sx={{ backgroundColor: '#27AE60', color: 'white' }}
                    startIcon={<FileDownloadIcon />}
                  >
                    Экспорт прогноза в Excel
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
        open={participantsOpen}
        projectId={currentProjectId}
        onClose={() => setParticipantsOpen(false)}
        onSaved={() => void projectDataQuery.refetch()}
      />

      <ProjectCharacteristicsDialog
        open={characteristicsOpen}
        projectId={currentProjectId}
        onClose={() => setCharacteristicsOpen(false)}
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
  );
}
