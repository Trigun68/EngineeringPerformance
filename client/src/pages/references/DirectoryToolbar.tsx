import { Box, Checkbox, Divider, FormControlLabel, IconButton, Stack, Tooltip, Typography } from '@mui/material';

const ICON = (name: string) => `/assets/icons/${name}`;

interface DirectoryToolbarProps {
  canEdit: boolean;
  hasSelection: boolean;
  showExcel?: boolean;
  showArchiveActions?: boolean;
  showArchivedCheckbox?: boolean;
  showArchived: boolean;
  onShowArchivedChange: (value: boolean) => void;
  onAdd: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onArchive: () => void;
  onUnarchive: () => void;
  onRefresh: () => void;
  onExport: () => void;
}

function ToolbarIconButton({
  title,
  icon,
  onClick,
  disabled,
}: {
  title: string;
  icon: string;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <Tooltip title={title}>
      <span>
        <IconButton size="small" onClick={onClick} disabled={disabled} sx={{ p: 0.5 }} aria-label={title}>
          <Box component="img" src={icon} alt="" sx={{ width: 20, height: 20, display: 'block' }} />
        </IconButton>
      </span>
    </Tooltip>
  );
}

export function DirectoryToolbar({
  canEdit,
  hasSelection,
  showExcel = true,
  showArchiveActions = true,
  showArchivedCheckbox = true,
  showArchived,
  onShowArchivedChange,
  onAdd,
  onEdit,
  onDelete,
  onArchive,
  onUnarchive,
  onRefresh,
  onExport,
}: DirectoryToolbarProps) {
  return (
    <Stack direction="row" alignItems="center" spacing={0.5} sx={{ mb: 1, flexWrap: 'wrap', gap: 0.5, flexShrink: 0 }}>
      {canEdit && (
        <>
          <ToolbarIconButton title="Добавить" icon={ICON('icons8-add-96.png')} onClick={onAdd} />
          <ToolbarIconButton title="Редактировать" icon={ICON('icons8-edit-90.png')} onClick={onEdit} disabled={!hasSelection} />
          <ToolbarIconButton title="Удалить" icon={ICON('icons8-delete-96.png')} onClick={onDelete} disabled={!hasSelection} />
          {showArchiveActions && (
            <>
              <ToolbarIconButton title="В архив" icon={ICON('icons8-pack-luggage-100.png')} onClick={onArchive} disabled={!hasSelection} />
              <ToolbarIconButton title="Из архива" icon={ICON('icons8-unpack-luggage-100.png')} onClick={onUnarchive} disabled={!hasSelection} />
            </>
          )}
        </>
      )}
      <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />
      <ToolbarIconButton title="Обновить" icon={ICON('icons8-update-100.png')} onClick={onRefresh} />
      {showArchivedCheckbox && (
        <FormControlLabel
          control={
            <Checkbox
              size="small"
              checked={showArchived}
              onChange={(e) => onShowArchivedChange(e.target.checked)}
            />
          }
          label="Показать архивные"
          sx={{ ml: 0.5, mr: 1, '& .MuiFormControlLabel-label': { fontSize: 13 } }}
        />
      )}
      {showExcel && (
        <Box sx={{ flex: 1, display: 'flex', justifyContent: 'flex-end' }}>
          <Tooltip title="Выгрузить в Excel">
            <IconButton size="small" onClick={onExport} sx={{ borderRadius: 1, px: 1 }} aria-label="Выгрузить в Excel">
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <Box component="img" src={ICON('icons8-save-96.png')} alt="" sx={{ width: 20, height: 20 }} />
                <Typography variant="body2" sx={{ fontSize: 13 }}>
                  Выгрузить в Excel
                </Typography>
              </Stack>
            </IconButton>
          </Tooltip>
        </Box>
      )}
    </Stack>
  );
}
