import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import type { DictionaryItem } from '../../types/dictionary';
import type { ProjectVersion } from '../../api/projectVersionsApi';
import type { VersionFormState } from './versionPickerModel';
import { VersionPickerGrid } from './VersionPickerGrid';

export interface VersionPickerFormDialogsProps {
  editOpen: boolean;
  onCloseEdit: () => void;
  editMode: 'create' | 'edit';
  form: VersionFormState;
  onFormChange: (next: VersionFormState) => void;
  catalogOptions: DictionaryItem[];
  submitting: boolean;
  onSaveVersion: () => void;
  copyOpen: boolean;
  onCloseCopy: () => void;
  copyMode: 'same' | 'from-plan';
  planVersions: ProjectVersion[];
  planSource: ProjectVersion | null;
  onPlanSourceChange: (row: ProjectVersion) => void;
  actionTarget: ProjectVersion | null;
  selected: ProjectVersion | null;
  copyName: string;
  onCopyNameChange: (value: string) => void;
  copyDescription: string;
  onCopyDescriptionChange: (value: string) => void;
  onCopy: () => void;
}

export function VersionPickerFormDialogs({
  editOpen,
  onCloseEdit,
  editMode,
  form,
  onFormChange,
  catalogOptions,
  submitting,
  onSaveVersion,
  copyOpen,
  onCloseCopy,
  copyMode,
  planVersions,
  planSource,
  onPlanSourceChange,
  actionTarget,
  selected,
  copyName,
  onCopyNameChange,
  copyDescription,
  onCopyDescriptionChange,
  onCopy,
}: VersionPickerFormDialogsProps) {
  return (
    <>
      <Dialog open={editOpen} onClose={onCloseEdit} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editMode === 'create' ? 'Новая версия анализа' : 'Редактирование версии'}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Наименование версии"
              value={form.versionName}
              onChange={(e) => onFormChange({ ...form, versionName: e.target.value })}
              required
              fullWidth
            />
            <TextField
              label="Описание"
              value={form.description}
              onChange={(e) => onFormChange({ ...form, description: e.target.value })}
              fullWidth
              multiline
              minRows={2}
            />
            <FormControl fullWidth required>
              <InputLabel id="pv-catalog-type-label">Справочник физобъёмов</InputLabel>
              <Select
                labelId="pv-catalog-type-label"
                label="Справочник физобъёмов"
                value={form.physicalVolumeTypeId}
                onChange={(e) => onFormChange({ ...form, physicalVolumeTypeId: e.target.value })}
              >
                {catalogOptions.map((t) => (
                  <MenuItem key={t.id} value={String(t.id)}>
                    {t.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Дата фиксации норм"
              type="date"
              value={form.asOfDate}
              onChange={(e) => onFormChange({ ...form, asOfDate: e.target.value })}
              required
              fullWidth
              InputLabelProps={{ shrink: true }}
              helperText="Снимок норм IIC на эту дату. Смена БЕР↔КФО после импорта ФО недоступна."
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.isPrimary}
                  disabled={editMode === 'edit'}
                  onChange={(e) => onFormChange({ ...form, isPrimary: e.target.checked })}
                  data-testid="version-primary-checkbox"
                />
              }
              label={
                editMode === 'edit'
                  ? 'Основная (меняется только в списке версий)'
                  : 'Основная'
              }
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onCloseEdit} disabled={submitting}>
            Отмена
          </Button>
          <Button
            variant="contained"
            onClick={onSaveVersion}
            disabled={submitting || !form.physicalVolumeTypeId || !form.asOfDate}
          >
            Сохранить
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={copyOpen} onClose={onCloseCopy} maxWidth="sm" fullWidth>
        <DialogTitle>
          {copyMode === 'from-plan' ? 'Копировать версию из плана' : 'Копировать версию'}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {copyMode === 'from-plan' ? (
              <>
                <Alert severity="info" variant="outlined">
                  Будут скопированы физические объёмы и оборудование. Трудоёмкость добавьте
                  импортом после создания версии факта.
                </Alert>
                {planVersions.length === 0 ? (
                  <Alert severity="warning">Нет версий плана для копирования.</Alert>
                ) : (
                  <VersionPickerGrid
                    rowData={planVersions}
                    gridId="version-picker-plan-source"
                    height={220}
                    onRowClicked={onPlanSourceChange}
                    onSelectionChanged={(rows) => {
                      const row = rows[0];
                      if (row) onPlanSourceChange(row);
                    }}
                  />
                )}
                {planSource && (
                  <Alert severity="success" variant="outlined">
                    Источник (план): {planSource.versionName}
                  </Alert>
                )}
              </>
            ) : (
              actionTarget && (
                <Alert severity="info" variant="outlined">
                  Источник: {actionTarget.versionName}
                </Alert>
              )
            )}
            <TextField
              label="Имя новой версии"
              value={copyName}
              onChange={(e) => onCopyNameChange(e.target.value)}
              required
              fullWidth
            />
            <TextField
              label="Описание"
              value={copyDescription}
              onChange={(e) => onCopyDescriptionChange(e.target.value)}
              fullWidth
              multiline
              minRows={2}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onCloseCopy} disabled={submitting}>
            Отмена
          </Button>
          <Button
            variant="contained"
            onClick={onCopy}
            disabled={
              submitting
              || !copyName.trim()
              || (copyMode === 'from-plan' ? !planSource : !(actionTarget ?? selected))
            }
            data-testid="version-copy-confirm-button"
          >
            {submitting ? 'Работаю…' : 'Копировать'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
