import { useCallback, useEffect, useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link as RouterLink, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { fetchProjectPermissions, type ProjectPermission } from '../../api/projectPermissionsApi';
import {
  fetchEquipment,
  fetchLaborIntensities,
  fetchPhysicalVolumes,
  type ProjectEquipment,
  type ProjectLaborIntensity,
  type ProjectPhysicalVolume,
} from '../../api/projectDataApi';
import {
  copyProjectVersion,
  createProjectVersion,
  fetchProjectVersions,
  setMainProjectVersion,
  type ProjectVersion,
} from '../../api/projectVersionsApi';
import { fetchProjects } from '../../api/projectsApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { EditModal } from '../../components/EditModal/EditModal';
import { ImportWizard } from '../../components/ImportWizard/ImportWizard';
import { PaginationBar } from '../../components/PaginationBar/PaginationBar';
import { ParticipantsModal } from '../../components/ParticipantsModal/ParticipantsModal';
import { usePermission } from '../../hooks/usePermission';
import { getApiErrorMessage } from '../../services/apiError';
import { useAuthStore } from '../../store/useAuthStore';
import { canReadPermission } from '../../utils/permissionAccess';
import type { Project } from '../../types/project';
import { AssessmentTab } from './AssessmentTab';
import { CompareTab } from './CompareTab';
import { GroupAnalysisTab } from './GroupAnalysisTab';
import { GroupTrendTab } from './GroupTrendTab';
import { ForecastPage } from '../../features/forecast/ForecastPage';
import { createVersionSchema } from './projectWriteSchema';

type ProjectTabId =
  | 'versions'
  | 'ppv'
  | 'pli'
  | 'peq'
  | 'participants'
  | 'import'
  | 'assessment'
  | 'forecast'
  | 'compare'
  | 'group'
  | 'trend';

const PROJECT_TAB_PERMISSIONS: Record<ProjectTabId, string> = {
  versions: 'ProjectAnalysisVersions',
  ppv: 'ProjectPhysicalVolumes',
  pli: 'ProjectLaborIntensities',
  peq: 'ProjectEquipments',
  participants: 'Projects',
  import: 'ProjectDataUpload',
  assessment: 'Assessment',
  forecast: 'Planning',
  compare: 'VersionComparison',
  group: 'GroupAnalysis',
  trend: 'GroupTrend',
};

export function ProjectDetailPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const pid = Number(projectId);
  const { canEdit: canEditProject } = usePermission('Projects');
  const { canEdit: canEditVersions } = usePermission('ProjectAnalysisVersions');
  const user = useAuthStore((s) => s.user);

  const [activeTabId, setActiveTabId] = useState<ProjectTabId>('versions');
  const [project, setProject] = useState<Project | null>(null);
  const [versions, setVersions] = useState<ProjectVersion[]>([]);
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);
  const [ppvRows, setPpvRows] = useState<ProjectPhysicalVolume[]>([]);
  const [pliRows, setPliRows] = useState<ProjectLaborIntensity[]>([]);
  const [peqRows, setPeqRows] = useState<ProjectEquipment[]>([]);
  const [ppvTotal, setPpvTotal] = useState(0);
  const [pliTotal, setPliTotal] = useState(0);
  const [peqTotal, setPeqTotal] = useState(0);
  const [ppvPage, setPpvPage] = useState(1);
  const [pliPage, setPliPage] = useState(1);
  const [peqPage, setPeqPage] = useState(1);
  const [pageSize, setPageSize] = useState(100);
  const [participants, setParticipants] = useState<ProjectPermission[]>([]);
  const [participantsOpen, setParticipantsOpen] = useState(false);
  const [versionModalOpen, setVersionModalOpen] = useState(false);
  const [versionName, setVersionName] = useState('');
  const [error, setError] = useState<string | null>(null);

  const metaQuery = useQuery({
    queryKey: ['project-detail-meta', pid],
    enabled: Number.isFinite(pid) && pid > 0,
    staleTime: 30_000,
    queryFn: async () => {
      const [projects, vers, participantsList] = await Promise.all([
        fetchProjects(),
        fetchProjectVersions(pid),
        fetchProjectPermissions(pid).catch(() => [] as ProjectPermission[]),
      ]);
      return {
        project: projects.find((p) => p.id === pid) ?? null,
        versions: vers,
        participants: participantsList,
      };
    },
  });

  const versionIdForData = selectedVersionId ?? metaQuery.data?.versions[0]?.id ?? null;

  useEffect(() => {
    if (!selectedVersionId && metaQuery.data?.versions[0]) {
      setSelectedVersionId(metaQuery.data.versions[0].id);
    }
  }, [metaQuery.data?.versions, selectedVersionId]);

  const dataQuery = useQuery({
    queryKey: ['project-detail-data', pid, versionIdForData, ppvPage, pliPage, peqPage, pageSize],
    enabled: Number.isFinite(pid) && pid > 0 && versionIdForData != null,
    staleTime: 15_000,
    queryFn: async () => {
      const [ppv, pli, peq] = await Promise.all([
        fetchPhysicalVolumes(pid, versionIdForData!, { page: ppvPage, pageSize, sort: 'id' }),
        fetchLaborIntensities(pid, versionIdForData!, { page: pliPage, pageSize, sort: 'id' }),
        fetchEquipment(pid, versionIdForData!, { page: peqPage, pageSize, sort: 'id' }),
      ]);
      return { ppv, pli, peq };
    },
  });

  useEffect(() => {
    if (metaQuery.data) {
      setProject(metaQuery.data.project);
      setVersions(metaQuery.data.versions);
      setParticipants(metaQuery.data.participants);
    }
  }, [metaQuery.data]);

  useEffect(() => {
    if (!dataQuery.data) return;
    setPpvRows(dataQuery.data.ppv.items);
    setPpvTotal(dataQuery.data.ppv.totalCount);
    setPliRows(dataQuery.data.pli.items);
    setPliTotal(dataQuery.data.pli.totalCount);
    setPeqRows(dataQuery.data.peq.items);
    setPeqTotal(dataQuery.data.peq.totalCount);
  }, [dataQuery.data]);

  const loading = metaQuery.isFetching || dataQuery.isFetching;

  useEffect(() => {
    if (metaQuery.isError || dataQuery.isError) {
      setError('Не удалось загрузить данные проекта.');
    } else if (!metaQuery.isFetching && !dataQuery.isFetching) {
      setError(null);
    }
  }, [metaQuery.isError, dataQuery.isError, metaQuery.isFetching, dataQuery.isFetching]);

  const load = useCallback(async () => {
    await Promise.all([metaQuery.refetch(), dataQuery.refetch()]);
  }, [metaQuery, dataQuery]);

  const handleCreateVersion = async () => {
    const parsed = createVersionSchema.safeParse({
      versionName: versionName.trim(),
      name: versionName.trim(),
      direction: 0,
    });
    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? 'Некорректное имя версии');
      return;
    }
    const created = await createProjectVersion(pid, {
      versionName: parsed.data.versionName,
      name: parsed.data.name ?? parsed.data.versionName,
      description: parsed.data.description ?? undefined,
      direction: parsed.data.direction ?? 0,
    });
    setVersionModalOpen(false);
    setVersionName('');
    setSelectedVersionId(created.id);
    await load();
  };

  const handleCopyVersion = async (version: ProjectVersion) => {
    const copied = await copyProjectVersion(pid, version.id, {
      newVersionName: `${version.versionName} (копия)`,
    });
    setSelectedVersionId(copied.id);
    await load();
  };

  const handleSetMainVersion = async (version: ProjectVersion) => {
    try {
      await setMainProjectVersion(pid, version.id);
      await load();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сделать версию основной.'));
    }
  };

  const ppvColumns = useMemo<ColDef<ProjectPhysicalVolume>[]>(
    () => [
      { field: 'facilityCode', headerName: 'Код объекта', minWidth: 120 },
      { field: 'facilityName', headerName: 'Объект', minWidth: 160 },
      { field: 'workName', headerName: 'Работа', minWidth: 180 },
      { field: 'quantity', headerName: 'Кол-во' },
      { field: 'laborHoursPerUnit', headerName: 'Чел-ч/ед.' },
    ],
    [],
  );

  const pliColumns = useMemo<ColDef<ProjectLaborIntensity>[]>(
    () => [
      { field: 'laborIntensityId', headerName: 'Трудоёмкость ID' },
      { field: 'plannedHours', headerName: 'План' },
      { field: 'actualHours', headerName: 'Факт' },
    ],
    [],
  );

  const peqColumns = useMemo<ColDef<ProjectEquipment>[]>(
    () => [
      { field: 'equipmentId', headerName: 'Оборудование ID' },
      { field: 'quantity', headerName: 'Кол-во' },
      { field: 'quantityInWork', headerName: 'В работе' },
    ],
    [],
  );

  const participantColumns = useMemo<ColDef<ProjectPermission>[]>(
    () => [
      { field: 'displayName', headerName: 'ФИО', minWidth: 160 },
      { field: 'username', headerName: 'Логин' },
      { field: 'roleName', headerName: 'Роль' },
      { field: 'level', headerName: 'Уровень', width: 110 },
    ],
    [],
  );

  const versionColumns = useMemo<ColDef<ProjectVersion>[]>(
    () => [
      { field: 'versionName', headerName: 'Версия', minWidth: 120 },
      { field: 'name', headerName: 'Наименование', minWidth: 200 },
      {
        field: 'isMain',
        headerName: 'Основная',
        width: 110,
        valueFormatter: (p) => (p.value ? 'Да' : ''),
      },
      { field: 'isArchived', headerName: 'Архив', width: 90 },
    ],
    [],
  );

  const visibleTabs = useMemo(() => {
    const labels: Record<ProjectTabId, string> = {
      versions: `Версии (${versions.length})`,
      ppv: `Физ. объёмы (${ppvTotal})`,
      pli: `Трудоёмкости (${pliTotal})`,
      peq: `Оборудование (${peqTotal})`,
      participants: `Участники (${participants.length})`,
      import: 'Импорт',
      assessment: 'Оценка',
      forecast: 'Прогноз',
      compare: 'Сравнение',
      group: 'Группа',
      trend: 'Тренд',
    };

    return (Object.keys(PROJECT_TAB_PERMISSIONS) as ProjectTabId[])
      .filter((tabId) =>
        canReadPermission(user?.permissions, PROJECT_TAB_PERMISSIONS[tabId], user?.roleName),
      )
      .map((tabId) => ({ id: tabId, label: labels[tabId] }));
  }, [participants.length, peqTotal, pliTotal, ppvTotal, user?.permissions, user?.roleName, versions.length]);

  useEffect(() => {
    if (visibleTabs.length === 0) return;
    if (!visibleTabs.some((tab) => tab.id === activeTabId)) {
      setActiveTabId(visibleTabs[0].id);
    }
  }, [activeTabId, visibleTabs]);

  if (error) return <Alert severity="error">{error}</Alert>;

  return (
    <Box>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">
          {project ? `${project.code} — ${project.name}` : `Проект #${projectId}`}
        </Typography>
        <Typography variant="body2" color="text.secondary" component={RouterLink} to="/projects" sx={{ ml: 1 }}>
          ← К списку
        </Typography>
        <Typography
          variant="body2"
          color="primary"
          component={RouterLink}
          to={`/projects/${pid}/grids`}
          sx={{ ml: 1 }}
        >
          9 гридов →
        </Typography>
      </Stack>

      {versions.length > 0 && (
        <FormControl size="small" sx={{ mb: 2, minWidth: 280 }}>
          <InputLabel>Версия анализа</InputLabel>
          <Select
            label="Версия анализа"
            value={selectedVersionId ?? ''}
            onChange={(e) => setSelectedVersionId(Number(e.target.value))}
          >
            {versions.map((v) => (
              <MenuItem key={v.id} value={v.id}>
                {v.versionName} — {v.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      )}

      <Tabs
        value={Math.max(0, visibleTabs.findIndex((tab) => tab.id === activeTabId))}
        onChange={(_, index) => setActiveTabId(visibleTabs[index]?.id ?? activeTabId)}
        sx={{ mb: 2 }}
      >
        {visibleTabs.map((tab) => (
          <Tab key={tab.id} label={tab.label} />
        ))}
      </Tabs>

      {visibleTabs.length === 0 && (
        <Alert severity="info">Нет доступных вкладок для вашей роли.</Alert>
      )}

      {activeTabId === 'versions' && (
        <Stack spacing={1}>
          {canEditVersions && (
            <Button variant="contained" sx={{ alignSelf: 'flex-start' }} onClick={() => setVersionModalOpen(true)}>
              Новая версия
            </Button>
          )}
          <DataTable
            rowData={versions}
            columnDefs={versionColumns}
            loading={loading}
            objectName="ProjectAnalysisVersions"
            gridId={`project-${pid}-versions`}
            contextMenuItems={
              canEditVersions
                ? [
                    { name: 'Копировать', action: (row) => row && void handleCopyVersion(row), requiredLevel: 2 },
                    {
                      name: 'Сделать основной',
                      action: (row) => row && void handleSetMainVersion(row),
                      // Основной может быть только версия плана (1) или факта (2) — не прогноза.
                      disabled: (row) => !row || row.direction === 3 || row.isMain,
                      requiredLevel: 2,
                    },
                  ]
                : []
            }
          />
        </Stack>
      )}

      {activeTabId === 'ppv' && (
        <>
          <PaginationBar
            page={ppvPage}
            pageSize={pageSize}
            totalCount={ppvTotal}
            onPageChange={setPpvPage}
            onPageSizeChange={setPageSize}
          />
          <DataTable
            rowData={ppvRows}
            columnDefs={ppvColumns}
            loading={loading}
            objectName="ProjectPhysicalVolumes"
            gridId={`project-${pid}-ppv`}
          />
        </>
      )}

      {activeTabId === 'pli' && (
        <>
          <PaginationBar
            page={pliPage}
            pageSize={pageSize}
            totalCount={pliTotal}
            onPageChange={setPliPage}
            onPageSizeChange={setPageSize}
          />
          <DataTable
            rowData={pliRows}
            columnDefs={pliColumns}
            loading={loading}
            objectName="ProjectLaborIntensities"
            gridId={`project-${pid}-pli`}
          />
        </>
      )}

      {activeTabId === 'peq' && (
        <>
          <PaginationBar
            page={peqPage}
            pageSize={pageSize}
            totalCount={peqTotal}
            onPageChange={setPeqPage}
            onPageSizeChange={setPageSize}
          />
          <DataTable
            rowData={peqRows}
            columnDefs={peqColumns}
            loading={loading}
            objectName="ProjectEquipments"
            gridId={`project-${pid}-peq`}
          />
        </>
      )}

      {activeTabId === 'participants' && (
        <Stack spacing={2}>
          {canEditProject && (
            <Button variant="contained" onClick={() => setParticipantsOpen(true)} sx={{ alignSelf: 'flex-start' }}>
              Управление участниками
            </Button>
          )}
          <DataTable
            rowData={participants}
            columnDefs={participantColumns}
            loading={loading}
            objectName="ProjectPermissions"
            gridId={`project-${pid}-participants`}
          />
        </Stack>
      )}

      {activeTabId === 'import' && (
        <ImportWizard projectId={pid} versionId={selectedVersionId} onImported={() => void load()} />
      )}

      {activeTabId === 'assessment' && (
        <AssessmentTab projectId={pid} versionId={selectedVersionId} />
      )}

      {activeTabId === 'forecast' && (
        <ForecastPage projectId={pid} />
      )}

      {activeTabId === 'compare' && <CompareTab projectId={pid} />}

      {activeTabId === 'group' && <GroupAnalysisTab projectId={pid} />}

      {activeTabId === 'trend' && <GroupTrendTab projectId={pid} />}

      <ParticipantsModal
        open={participantsOpen}
        projectId={pid}
        onClose={() => setParticipantsOpen(false)}
        onSaved={() => void load()}
      />

      <EditModal
        open={versionModalOpen}
        title="Новая версия анализа"
        onClose={() => setVersionModalOpen(false)}
        onSubmit={handleCreateVersion}
      >
        <TextField
          label="Название версии"
          value={versionName}
          onChange={(e) => setVersionName(e.target.value)}
          fullWidth
          required
        />
      </EditModal>
    </Box>
  );
}
