import { Box, TextField } from '@mui/material';
import { DirectoryToolbar } from './DirectoryToolbar';

export interface IicNormsToolbarProps {
  canEdit: boolean;
  hasSelection: boolean;
  asOfDate: string;
  onAsOfDateChange: (value: string) => void;
  onAdd: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onRefresh: () => void;
  onExport: () => void;
}

export function IicNormsToolbar({
  canEdit,
  hasSelection,
  asOfDate,
  onAsOfDateChange,
  onAdd,
  onEdit,
  onDelete,
  onRefresh,
  onExport,
}: IicNormsToolbarProps) {
  return (
    <>
      <Box sx={{ flexShrink: 0 }}>
        <DirectoryToolbar
          canEdit={canEdit}
          hasSelection={hasSelection}
          showArchiveActions={false}
          showArchivedCheckbox={false}
          showArchived={false}
          onShowArchivedChange={() => undefined}
          onAdd={onAdd}
          onEdit={onEdit}
          onDelete={onDelete}
          onArchive={() => undefined}
          onUnarchive={() => undefined}
          onRefresh={onRefresh}
          onExport={onExport}
        />
      </Box>

      <Box sx={{ mb: 1.5, maxWidth: 220, flexShrink: 0 }}>
        <TextField
          label="Дата"
          type="date"
          size="small"
          fullWidth
          value={asOfDate}
          onChange={(e) => onAsOfDateChange(e.target.value)}
          InputLabelProps={{ shrink: true }}
        />
      </Box>
    </>
  );
}
