import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  Stack,
  TextField,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import ArchiveIcon from '@mui/icons-material/Archive';
import UnarchiveIcon from '@mui/icons-material/Unarchive';
import type { ColDef } from 'ag-grid-community';
import {
  archiveProject,
  createProject,
  deleteProject,
  fetchProjects,
  unarchiveProject,
  updateProject,
} from '../../api/projectsApi';
import { DictionaryGrid } from '../../components/dictionary/DictionaryGrid';
import { EditModal } from '../../components/EditModal/EditModal';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';
import { usePermission } from '../../hooks/usePermission';
import { useConfirmation } from '../../hooks/useConfirmation';
import { getApiErrorMessage } from '../../services/apiError';
import { queryKeys } from '../../services/queryKeys';
import type { Project, ProjectFormData } from '../../types/project';
import { LAST_PROJECT_PREF_KEY } from '../../utils/projectWorkspacePrefs';
import { putUserPreferences } from '../../api/userPreferencesApi';

const OBJECT_NAME = 'Projects';

const emptyForm: ProjectFormData = {
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
};

export function ProjectsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { canRead, canEdit } = usePermission(OBJECT_NAME);
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();

  const [actionError, setActionError] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState<ProjectFormData>(emptyForm);
  const [editingProject, setEditingProject] = useState<Project | null>(null);
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [showArchived, setShowArchived] = useState(false);

  // Третий элемент ключа (boolean) отделяет кэш от FullProjectsPage: queryKeys.projects.all().
  const projectsQuery = useQuery<Project[]>({
    queryKey: queryKeys.projects.all(showArchived),
    queryFn: () => fetchProjects(showArchived),
    enabled: canRead,
  });

  const projects = projectsQuery.data ?? [];
  const loading = projectsQuery.isFetching;
  const error =
    actionError
    ?? (projectsQuery.error
      ? getApiErrorMessage(projectsQuery.error, 'Не удалось загрузить проекты.')
      : null);

  // Переносим предыдущий выбор на свежий список (тот же объект по id, иначе сброс).
  useEffect(() => {
    const data = projectsQuery.data;
    if (!data) return;
    setSelectedProject((prev) => (prev ? data.find((p) => p.id === prev.id) ?? null : null));
  }, [projectsQuery.data]);

  const invalidateProjectLists = () =>
    queryClient.invalidateQueries({ queryKey: queryKeys.projects.all() });

  const workStatusOptions = useMemo(
    () => ['Черновик', 'в работе', 'завершен', 'отменен'],
    [],
  );

  const columnDefs = useMemo<ColDef<Project>[]>(
    () => [
      { field: 'code', headerName: 'Код' },
      { field: 'name', headerName: 'Наименование', minWidth: 220 },
      { field: 'workNumber', headerName: 'Номер работы' },
      { field: 'contractNumber', headerName: 'Договор' },
      { field: 'customer', headerName: 'Заказчик' },
      { field: 'contractor', headerName: 'Подрядчик' },
      { field: 'gip', headerName: 'ГИП' },
      { field: 'workStatus', headerName: 'Статус' },
      {
        colId: 'versionCounts',
        headerName: 'Версии',
        width: 110,
        valueGetter: (p) => {
          const plan = p.data?.planVersionCount ?? 0;
          const fact = p.data?.factVersionCount ?? 0;
          return `${plan}/${fact}`;
        },
        tooltipValueGetter: () => 'План / Факт',
      },
      {
        field: 'isArchived',
        headerName: 'Архив',
        width: 90,
        valueFormatter: (p) => (p.value ? 'Да' : ''),
        hide: !showArchived,
      },
    ],
    [showArchived],
  );

  const openCreate = () => {
    setEditingProject(null);
    setForm(emptyForm);
    setModalOpen(true);
  };

  const openDetail = (project: Project) => {
    void putUserPreferences({ [LAST_PROJECT_PREF_KEY]: String(project.id) });
    navigate(`/projects/${project.id}`, { state: { project } });
  };

  const openEdit = (project: Project) => {
    setEditingProject(project);
    setForm({
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
    });
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    setIsSubmitting(true);
    try {
      if (editingProject) {
        await updateProject(editingProject.id, form);
      } else {
        await createProject(form);
      }
      setModalOpen(false);
      await invalidateProjectLists();
    } catch (err) {
      setActionError(getApiErrorMessage(err, 'Не удалось сохранить изменения.'));
      throw err;
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleHardDelete = async (project: Project) => {
    if (!(await confirm({
      title: 'Удалить проект безвозвратно?',
      description: `Удалить проект «${project.name}» безвозвратно?\nВсе версии и связанные данные будут удалены. Это действие необратимо.`,
    }))) {
      return;
    }
    try {
      await deleteProject(project.id);
      setSelectedProject(null);
      await invalidateProjectLists();
    } catch (err) {
      setActionError(getApiErrorMessage(err, 'Не удалось удалить проект.'));
    }
  };

  const handleArchive = async (project: Project) => {
    try {
      await archiveProject(project.id);
      setSelectedProject(null);
      await invalidateProjectLists();
    } catch (err) {
      setActionError(getApiErrorMessage(err, 'Не удалось поместить проект в архив.'));
    }
  };

  const handleUnarchive = async (project: Project) => {
    try {
      await unarchiveProject(project.id);
      await invalidateProjectLists();
    } catch (err) {
      setActionError(getApiErrorMessage(err, 'Не удалось извлечь проект из архива.'));
    }
  };

  const filteredProjects = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    if (!query) return projects;

    return projects.filter((project) =>
      [
        project.code,
        project.name,
        project.workNumber,
        project.contractNumber,
        project.customer,
        project.contractor,
        project.gip,
        project.workStatus,
        project.calendarPlanNumber,
        project.type,
        project.program,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(query),
    );
  }, [projects, searchQuery]);

  if (!canRead) {
    return <Alert severity="warning">Нет доступа к разделу «Проекты».</Alert>;
  }

  const selectedIsArchived = selectedProject?.isArchived === true;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0, flex: 1, overflow: 'hidden' }}>
      <WorkingOverlay open={loading} delayMs={1000} />
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2} spacing={1} flexShrink={0} flexWrap="wrap" useFlexGap>
        <TextField
          size="small"
          label="Поиск по проектам"
          placeholder="Код, наименование, договор, заказчик..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          sx={{ minWidth: 360, width: 520 }}
        />
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
          <FormControlLabel
            control={
              <Checkbox
                size="small"
                checked={showArchived}
                onChange={(e) => setShowArchived(e.target.checked)}
              />
            }
            label="Отображать архивные элементы"
          />
          {canEdit && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
              Добавить
            </Button>
          )}
          {canEdit && (
            <Button
              variant="outlined"
              startIcon={<EditIcon />}
              onClick={() => selectedProject && openEdit(selectedProject)}
              disabled={!selectedProject}
            >
              Редактировать
            </Button>
          )}
          {canEdit && (
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteIcon />}
              onClick={() => selectedProject && void handleHardDelete(selectedProject)}
              disabled={!selectedProject}
            >
              Удалить
            </Button>
          )}
          {canEdit && (
            <Button
              variant="outlined"
              startIcon={<ArchiveIcon />}
              onClick={() => selectedProject && void handleArchive(selectedProject)}
              disabled={!selectedProject || selectedIsArchived}
            >
              Поместить в архив
            </Button>
          )}
          {canEdit && showArchived && (
            <Button
              variant="outlined"
              startIcon={<UnarchiveIcon />}
              onClick={() => selectedProject && void handleUnarchive(selectedProject)}
              disabled={!selectedProject || !selectedIsArchived}
            >
              Извлечь из архива
            </Button>
          )}
        </Stack>
      </Stack>

      {error && (
        <Alert
          severity="error"
          sx={{ mb: 2, flexShrink: 0 }}
          onClose={actionError ? () => setActionError(null) : undefined}
        >
          {error}
        </Alert>
      )}

      <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        {loading && projects.length === 0 ? (
          <Stack alignItems="center" py={4}>
            <CircularProgress />
          </Stack>
        ) : (
          <DictionaryGrid<Project>
            rowData={filteredProjects}
            columnDefs={columnDefs}
            gridId="projects-list"
            loading={loading}
            objectName={OBJECT_NAME}
            canEdit={canEdit}
            onRowClicked={setSelectedProject}
            onRowDoubleClicked={openDetail}
            onEdit={canEdit ? openEdit : undefined}
            onDelete={canEdit ? (row) => void handleHardDelete(row) : undefined}
            onArchive={canEdit ? (row) => void handleArchive(row) : undefined}
            contextMenuMode="compact"
          />
        )}
      </Box>

      <EditModal
        open={modalOpen}
        title={editingProject ? 'Редактирование проекта' : 'Новый проект'}
        onClose={() => setModalOpen(false)}
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        maxWidth="md"
      >
        <Stack spacing={2}>
          <TextField
            label="Код"
            value={form.code}
            onChange={(e) => setForm({ ...form, code: e.target.value })}
            required
            fullWidth
          />
          <TextField
            label="Наименование"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            required
            fullWidth
          />
          <TextField
            label="Номер работы"
            value={form.workNumber ?? ''}
            onChange={(e) => setForm({ ...form, workNumber: e.target.value })}
            fullWidth
          />
          <TextField
            label="Договор"
            value={form.contractNumber ?? ''}
            onChange={(e) => setForm({ ...form, contractNumber: e.target.value })}
            fullWidth
          />
          <TextField
            label="Заказчик"
            value={form.customer ?? ''}
            onChange={(e) => setForm({ ...form, customer: e.target.value })}
            fullWidth
          />
          <TextField
            label="Подрядчик"
            value={form.contractor ?? ''}
            onChange={(e) => setForm({ ...form, contractor: e.target.value })}
            fullWidth
          />
          <TextField
            label="ГИП"
            value={form.gip ?? ''}
            onChange={(e) => setForm({ ...form, gip: e.target.value })}
            fullWidth
          />
          <TextField
            label="Статус работ"
            value={form.workStatus ?? ''}
            onChange={(e) => setForm({ ...form, workStatus: e.target.value })}
            helperText={
              workStatusOptions.length > 0
                ? `Справочник: ${workStatusOptions.slice(0, 3).join(', ')}...`
                : undefined
            }
            fullWidth
          />
        </Stack>
      </EditModal>
      {ConfirmDialog}
    </Box>
  );
}
