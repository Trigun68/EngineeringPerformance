import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack } from '@mui/material';
import type { ProjectVersion } from '../../api/projectVersionsApi';
import { WorkingOverlay } from '../WorkingOverlay/WorkingOverlay';
import { VersionPickerFormDialogs } from './VersionPickerFormDialogs';
import { VersionPickerGrid } from './VersionPickerGrid';
import { VersionPickerToolbar } from './VersionPickerToolbar';
import { useVersionPickerActions } from './useVersionPickerActions';
import {
  ANALYSIS_FACT,
  ANALYSIS_PLAN,
  buildVersionLabel,
  type VersionPickerDirection,
} from './versionPickerModel';

export type { VersionPickerDirection };
export { ANALYSIS_FACT, ANALYSIS_PLAN, buildVersionLabel };
export { invalidateAfterVersionMutation } from './versionPickerInvalidation';

export interface ProjectVersionPickerDialogProps {
  open: boolean;
  title: string;
  projectId: number;
  versions?: ProjectVersion[];
  directionFilter?: VersionPickerDirection;
  selectedVersionId?: number | null;
  onSelect: (version: ProjectVersion) => void;
  onClose: () => void;
  onVersionsChanged?: () => void;
}

export function ProjectVersionPickerDialog({
  open,
  title,
  projectId,
  versions: versionsProp,
  directionFilter = 'any',
  selectedVersionId,
  onSelect,
  onClose,
  onVersionsChanged,
}: ProjectVersionPickerDialogProps) {
  const a = useVersionPickerActions({
    open,
    projectId,
    versionsProp,
    directionFilter,
    selectedVersionId,
    onSelect,
    onClose,
    onVersionsChanged,
  });

  return (
    <>
      <WorkingOverlay open={a.submitting} />

      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth data-testid="version-picker-dialog">
        <DialogTitle sx={{ bgcolor: '#2C3E50', color: '#ECF0F1' }}>{title}</DialogTitle>
        <DialogContent dividers sx={{ p: 2 }}>
          <VersionPickerToolbar
            selected={a.selected}
            submitting={a.submitting}
            allowCopyFromPlan={a.allowCopyFromPlan}
            planVersionsCount={a.planVersions.length}
            onCreate={a.openCreate}
            onEdit={a.openEdit}
            onDelete={a.requestDelete}
            onCopy={a.openCopy}
            onSetPrimary={() => void a.handleSetPrimary()}
            onCopyFromPlan={a.openCopyFromPlan}
          />

          {a.error && (
            <Alert severity="error" sx={{ mb: 1 }} onClose={() => a.setError(null)}>
              {a.error}
            </Alert>
          )}

          {a.filteredVersions.length === 0 ? (
            <Alert severity="info">Нет версий для выбора. Создайте версию анализа.</Alert>
          ) : (
            <VersionPickerGrid
              rowData={a.filteredVersions}
              gridId="version-picker"
              onRowClicked={(row) => a.pickVersion(row, true)}
              onSelectionChanged={(rows) => {
                const row = rows[0];
                if (row) a.pickVersion(row, true);
              }}
              onRowDoubleClicked={(row) => {
                a.pickVersion(row, true);
                onSelect(row);
                onClose();
              }}
            />
          )}
          {a.selected && (
            <Stack sx={{ mt: 1 }}>
              <Alert severity="success" variant="outlined">
                Выбрано: {buildVersionLabel(a.selected)}
              </Alert>
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Отмена</Button>
          <Button variant="contained" onClick={a.handleConfirm} disabled={!a.selected}>
            Выбрать
          </Button>
        </DialogActions>
      </Dialog>

      <VersionPickerFormDialogs
        editOpen={a.editOpen}
        onCloseEdit={() => a.setEditOpen(false)}
        editMode={a.editMode}
        form={a.form}
        onFormChange={a.setForm}
        catalogOptions={a.catalogOptions}
        submitting={a.submitting}
        onSaveVersion={() => void a.handleSaveVersion()}
        copyOpen={a.copyOpen}
        onCloseCopy={() => a.setCopyOpen(false)}
        copyMode={a.copyMode}
        planVersions={a.planVersions}
        planSource={a.planSource}
        onPlanSourceChange={a.selectPlanSource}
        actionTarget={a.actionTarget}
        selected={a.selected}
        copyName={a.copyName}
        onCopyNameChange={a.setCopyName}
        copyDescription={a.copyDescription}
        onCopyDescriptionChange={a.setCopyDescription}
        onCopy={() => void a.handleCopy()}
      />

      <Dialog
        open={a.deleteConfirmOpen}
        onClose={() => !a.submitting && a.setDeleteConfirmOpen(false)}
        maxWidth="xs"
        fullWidth
        data-testid="version-delete-confirm"
      >
        <DialogTitle>Удалить версию?</DialogTitle>
        <DialogContent dividers>
          <Alert severity="warning">
            Версия «{a.actionTarget?.versionName ?? a.selected?.versionName}» будет скрыта из списка (архивирование).
            Связанные данные сохраняются в БД, но версия перестанет отображаться в выборе.
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => a.setDeleteConfirmOpen(false)} disabled={a.submitting}>
            Отмена
          </Button>
          <Button
            color="error"
            variant="contained"
            onClick={() => void a.handleDelete()}
            disabled={a.submitting}
            data-testid="version-delete-confirm-button"
          >
            Удалить
          </Button>
        </DialogActions>
      </Dialog>
      {a.ConfirmDialog}
    </>
  );
}
