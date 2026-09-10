import { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Alert, Stack, TextField } from '@mui/material';
import { createProject, updateProject } from '../../api/projectsApi';
import { EditModal } from '../../components/EditModal/EditModal';
import { getApiErrorMessage } from '../../services/apiError';
import { queryKeys } from '../../services/queryKeys';
import type { Project, ProjectFormData } from '../../types/project';

export interface ProjectEditorDialogProps {
  open: boolean;
  mode: 'create' | 'edit';
  projectId: number | null;
  form: ProjectFormData;
  onFormChange: (next: ProjectFormData) => void;
  canEdit: boolean;
  onClose: () => void;
  onCreated: (project: Project) => void;
}

export function ProjectEditorDialog({
  open,
  mode,
  projectId,
  form,
  onFormChange,
  canEdit,
  onClose,
  onCreated,
}: ProjectEditorDialogProps) {
  const queryClient = useQueryClient();
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    if (open) setSaveError(null);
  }, [open]);

  const saveProjectMutation = useMutation({
    mutationFn: async (vars: { mode: 'create' | 'edit'; id: number | null; form: ProjectFormData }) => {
      if (vars.mode === 'edit' && vars.id) {
        const updated = await updateProject(vars.id, vars.form);
        return { mode: 'edit' as const, project: updated };
      }
      const created = await createProject(vars.form);
      return { mode: 'create' as const, project: created };
    },
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.projects.all() });
      if (result.mode === 'edit') {
        await queryClient.invalidateQueries({ queryKey: queryKeys.project(result.project.id) });
      }
      setSaveError(null);
      onClose();
      if (result.mode === 'create') {
        onCreated(result.project);
      }
    },
    onError: (err) => {
      setSaveError(getApiErrorMessage(err, 'Не удалось сохранить проект.'));
    },
  });

  const handleSave = async () => {
    if (!canEdit) return;
    try {
      await saveProjectMutation.mutateAsync({ mode, id: projectId, form });
    } catch {
      // onError уже записал saveError через getApiErrorMessage
    }
  };

  return (
    <EditModal
      open={open}
      title={mode === 'edit' ? 'Основные данные проекта' : 'Создание проекта'}
      onClose={onClose}
      onSubmit={handleSave}
      isSubmitting={saveProjectMutation.isPending}
      maxWidth="md"
    >
      <Stack spacing={2}>
        {saveError && (
          <Alert severity="error" data-testid="project-save-error">
            {saveError}
          </Alert>
        )}
        <TextField
          label="Код"
          value={form.code}
          onChange={(e) => onFormChange({ ...form, code: e.target.value })}
          required
          fullWidth
        />
        <TextField
          label="Наименование"
          value={form.name}
          onChange={(e) => onFormChange({ ...form, name: e.target.value })}
          required
          fullWidth
        />
        <TextField
          label="Номер работы"
          value={form.workNumber ?? ''}
          onChange={(e) => onFormChange({ ...form, workNumber: e.target.value })}
          fullWidth
        />
        <TextField
          label="Номер договора"
          value={form.contractNumber ?? ''}
          onChange={(e) => onFormChange({ ...form, contractNumber: e.target.value })}
          fullWidth
        />
        <TextField
          label="Заказчик"
          value={form.customer ?? ''}
          onChange={(e) => onFormChange({ ...form, customer: e.target.value })}
          fullWidth
        />
        <TextField
          label="Подрядчик"
          value={form.contractor ?? ''}
          onChange={(e) => onFormChange({ ...form, contractor: e.target.value })}
          fullWidth
        />
        <TextField
          label="ГИП"
          value={form.gip ?? ''}
          onChange={(e) => onFormChange({ ...form, gip: e.target.value })}
          fullWidth
        />
        <TextField
          label="Статус работ"
          value={form.workStatus ?? ''}
          onChange={(e) => onFormChange({ ...form, workStatus: e.target.value })}
          fullWidth
        />
      </Stack>
    </EditModal>
  );
}
