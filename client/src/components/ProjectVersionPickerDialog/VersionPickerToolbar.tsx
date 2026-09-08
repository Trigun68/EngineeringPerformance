import { Stack, Tooltip, IconButton } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import DriveFileMoveIcon from '@mui/icons-material/DriveFileMove';
import StarIcon from '@mui/icons-material/Star';
import StarBorderIcon from '@mui/icons-material/StarBorder';
import type { ProjectVersion } from '../../api/projectVersionsApi';

export interface VersionPickerToolbarProps {
  selected: ProjectVersion | null;
  submitting: boolean;
  allowCopyFromPlan: boolean;
  planVersionsCount: number;
  onCreate: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onCopy: () => void;
  onSetPrimary: () => void;
  onCopyFromPlan: () => void;
}

export function VersionPickerToolbar({
  selected,
  submitting,
  allowCopyFromPlan,
  planVersionsCount,
  onCreate,
  onEdit,
  onDelete,
  onCopy,
  onSetPrimary,
  onCopyFromPlan,
}: VersionPickerToolbarProps) {
  return (
    <Stack direction="row" spacing={0.5} sx={{ mb: 1 }}>
      <Tooltip title="Добавить версию">
        <IconButton size="small" onClick={onCreate} aria-label="Добавить версию">
          <AddIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      <Tooltip title="Редактировать версию">
        <IconButton size="small" onClick={onEdit} disabled={!selected} aria-label="Редактировать версию">
          <EditIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      <Tooltip title="Удалить версию">
        <IconButton
          size="small"
          onClick={onDelete}
          disabled={!selected || submitting}
          aria-label="Удалить версию"
          data-testid="version-delete-button"
        >
          <DeleteIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      <Tooltip title="Копировать версию">
        <IconButton
          size="small"
          onClick={onCopy}
          disabled={!selected || submitting}
          aria-label="Копировать версию"
          data-testid="version-copy-button"
        >
          <ContentCopyIcon fontSize="small" />
        </IconButton>
      </Tooltip>
      <Tooltip title="Сделать основной">
        <span>
          <IconButton
            size="small"
            onClick={onSetPrimary}
            disabled={!selected || selected.isPrimary === true || submitting}
            aria-label="Сделать основной"
            data-testid="version-set-primary-button"
          >
            {selected?.isPrimary ? (
              <StarIcon fontSize="small" color="warning" />
            ) : (
              <StarBorderIcon fontSize="small" />
            )}
          </IconButton>
        </span>
      </Tooltip>
      {allowCopyFromPlan && (
        <Tooltip title="Копировать из плана (ФО и оборудование; трудоёмкость — отдельно)">
          <span>
            <IconButton
              size="small"
              onClick={onCopyFromPlan}
              disabled={submitting || planVersionsCount === 0}
              aria-label="Копировать из плана"
              data-testid="version-copy-from-plan-button"
            >
              <DriveFileMoveIcon fontSize="small" />
            </IconButton>
          </span>
        </Tooltip>
      )}
    </Stack>
  );
}
