import {
  copyProjectVersion,
  createProjectVersion,
  deleteProjectVersion,
  setPrimaryProjectVersion,
  updateProjectVersion,
  type ProjectVersion,
  type ProjectVersionWriteRequest,
} from '../../api/projectVersionsApi';
import { useConfirmation } from '../../hooks/useConfirmation';
import { getApiErrorMessage } from '../../services/apiError';
import { ANALYSIS_FACT, emptyVersionForm, toDateInputValue } from './versionPickerModel';
import { useVersionPickerList, type UseVersionPickerListParams } from './useVersionPickerList';

export { invalidateAfterVersionMutation } from './versionPickerInvalidation';

export type UseVersionPickerActionsParams = UseVersionPickerListParams & {
  onSelect: (version: ProjectVersion) => void;
  onClose: () => void;
};

export function useVersionPickerActions({
  onSelect,
  onClose,
  ...listParams
}: UseVersionPickerActionsParams) {
  const list = useVersionPickerList(listParams);
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();
  const {
    selected,
    setError,
    setEditOpen,
    setEditMode,
    form,
    setForm,
    filteredVersions,
    setActionTarget,
    setSubmitting,
    actionTarget,
    defaultDirection,
    pickVersion,
    refreshAfterMutation,
    projectId,
    setCopyMode,
    setPlanSource,
    setCopyName,
    setCopyDescription,
    setCopyOpen,
    setDeleteConfirmOpen,
    planVersions,
    copyMode,
    planSource,
    copyName,
    copyDescription,
    directionFilter,
    clearUserPick,
  } = list;

  const openCreate = () => {
    setEditMode('create');
    setForm({ ...emptyVersionForm(), isPrimary: filteredVersions.length === 0 });
    setEditOpen(true);
  };

  const openEdit = () => {
    if (!selected) return;
    setActionTarget(selected);
    setEditMode('edit');
    setForm({
      versionName: selected.versionName,
      description: selected.description ?? '',
      physicalVolumeTypeId: selected.physicalVolumeTypeId?.toString() ?? '',
      asOfDate: toDateInputValue(selected.createdAt),
      isPrimary: selected.isPrimary === true,
    });
    setEditOpen(true);
  };

  const handleSaveVersion = async () => {
    if (!form.versionName.trim() || !form.physicalVolumeTypeId || !form.asOfDate) return;
    const editTarget = actionTarget ?? selected;
    if (list.editMode === 'create' && form.isPrimary) {
      const existingPrimary = filteredVersions.find((v) => v.isPrimary);
      if (existingPrimary) {
        const ok = await confirm({
          title: 'Сделать основной?',
          description: 'Вы действительно хотите сделать эту версию основной?',
        });
        if (!ok) return;
      }
    }
    setSubmitting(true);
    setError(null);
    try {
      const body: ProjectVersionWriteRequest = {
        versionName: form.versionName.trim(),
        description: form.description.trim() || null,
        direction: list.editMode === 'edit' && editTarget ? editTarget.direction : defaultDirection,
        physicalVolumeTypeId: form.physicalVolumeTypeId ? Number(form.physicalVolumeTypeId) : null,
        asOfDate: `${form.asOfDate}T12:00:00Z`,
      };
      if (list.editMode === 'edit' && editTarget) {
        const updated = await updateProjectVersion(projectId, editTarget.id, {
          ...body,
          rowVersion: editTarget.rowVersion ?? null,
        });
        pickVersion(updated, true);
      } else {
        if (form.isPrimary) body.isPrimary = true;
        const created = await createProjectVersion(projectId, body);
        pickVersion(created, true);
      }
      setEditOpen(false);
      setActionTarget(null);
      await refreshAfterMutation();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить версию.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleSetPrimary = async () => {
    if (!selected || selected.isPrimary) return;
    setSubmitting(true);
    setError(null);
    try {
      const updated = await setPrimaryProjectVersion(projectId, selected.id);
      pickVersion(updated, true);
      await refreshAfterMutation();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось назначить основную версию.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    const target = actionTarget ?? selected;
    if (!target) return;
    setDeleteConfirmOpen(false);
    setSubmitting(true);
    setError(null);
    try {
      await deleteProjectVersion(projectId, target.id);
      clearUserPick();
      setActionTarget(null);
      pickVersion(null);
      const loaded = await refreshAfterMutation();
      const stillVisible = loaded.filter((v) =>
        directionFilter === 'any' ? true : v.direction === directionFilter,
      );
      pickVersion(stillVisible[0] ?? null);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось удалить версию.'));
    } finally {
      setSubmitting(false);
    }
  };

  const openCopy = () => {
    if (!selected) return;
    setCopyMode('same');
    setActionTarget(selected);
    setPlanSource(null);
    setCopyName(`${selected.versionName} (копия)`);
    setCopyDescription(selected.description ?? '');
    setCopyOpen(true);
  };

  const openCopyFromPlan = () => {
    const source = planVersions.find((v) => v.isPrimary) ?? planVersions[0] ?? null;
    setCopyMode('from-plan');
    setActionTarget(null);
    setPlanSource(source);
    setCopyName(source ? `${source.versionName} (факт)` : '');
    setCopyDescription(source?.description ?? '');
    setCopyOpen(true);
  };

  const handleCopy = async () => {
    const target = copyMode === 'from-plan' ? planSource : (actionTarget ?? selected);
    if (!target || !copyName.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      const copy = await copyProjectVersion(projectId, target.id, {
        newVersionName: copyName.trim(),
        description: copyDescription.trim() || null,
        ...(copyMode === 'from-plan' ? { targetDirection: ANALYSIS_FACT } : {}),
      });
      setCopyOpen(false);
      setActionTarget(null);
      setPlanSource(null);
      pickVersion(copy, true);
      await refreshAfterMutation();
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          copyMode === 'from-plan'
            ? 'Не удалось скопировать версию из плана.'
            : 'Не удалось скопировать версию.',
        ),
      );
    } finally {
      setSubmitting(false);
    }
  };

  const handleConfirm = () => {
    if (selected) {
      onSelect(selected);
      onClose();
    }
  };

  const requestDelete = () => {
    if (!selected) return;
    setActionTarget(selected);
    setDeleteConfirmOpen(true);
  };

  const selectPlanSource = (row: ProjectVersion) => {
    setPlanSource(row);
    setCopyName(`${row.versionName} (факт)`);
    setCopyDescription(row.description ?? '');
  };

  return {
    ...list,
    ConfirmDialog,
    openCreate,
    openEdit,
    handleSaveVersion,
    handleSetPrimary,
    handleDelete,
    openCopy,
    openCopyFromPlan,
    handleCopy,
    handleConfirm,
    requestDelete,
    selectPlanSource,
  };
}
