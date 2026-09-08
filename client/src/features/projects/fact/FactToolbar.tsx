import { Box, Button, Stack, Typography } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import type { ImportDataType } from '../../../api/importApi';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { panelPrimaryToolbarSx } from '../shared/projectsPageTabStyles';

export interface FactToolbarProps {
  factVersionId: number | null;
  factVersionLabel: string;
  catalogShortLabel: string | null;
  catalogTypeMissing: boolean;
  importBlocked: boolean;
  canImport: boolean;
  loading: boolean;
  onOpenVersionPicker: () => void;
  onOpenPlanning: () => void;
  onImport: (type: ImportDataType) => void;
  onDelete: (type: ImportDataType) => void;
}

/** Первый ряд тулбара факта: версия, каталог, параметры, импорт/удаление. Без запросов. */
export function FactToolbar({
  factVersionId,
  factVersionLabel,
  catalogShortLabel,
  catalogTypeMissing,
  importBlocked,
  canImport,
  loading,
  onOpenVersionPicker,
  onOpenPlanning,
  onImport,
  onDelete,
}: FactToolbarProps) {
  return (
    <Stack direction="row" sx={panelPrimaryToolbarSx}>
      <Button
        variant="contained"
        onClick={onOpenVersionPicker}
        sx={{
          backgroundColor: '#2980B9',
          color: 'white',
          flexShrink: 0,
          '&:hover': { backgroundColor: '#2471A3' },
        }}
      >
        📋 Версия (факт)
      </Button>
      {factVersionLabel && (
        <Box
          sx={{
            backgroundColor: '#EBF5FB',
            border: '1px solid #AED6F1',
            px: 1.5,
            py: 0.5,
            borderRadius: 1,
            minWidth: 0,
            maxWidth: { xs: '100%', sm: 280 },
          }}
        >
          <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>
            {factVersionLabel}
          </Typography>
        </Box>
      )}
      {catalogShortLabel && (
        <Box
          data-testid="fact-catalog-label"
          sx={{
            backgroundColor: '#FEF9E7',
            border: '1px solid #F9E79F',
            px: 1.5,
            py: 0.5,
            borderRadius: 1,
          }}
        >
          <Typography variant="body2" sx={{ color: '#7D6608' }} noWrap>
            {catalogShortLabel}
          </Typography>
        </Box>
      )}
      {factVersionId && (
        <Button
          variant="outlined"
          onClick={onOpenPlanning}
          sx={{ textTransform: 'none' }}
          data-testid="fact-planning-parameters-button"
        >
          Параметры
        </Button>
      )}
      {canImport && (
        <>
          <ToolbarActionButton
            disabled={importBlocked || loading}
            title={catalogTypeMissing ? 'Укажите справочник КФО или БЕР в версии проекта' : undefined}
            onClick={() => onImport('physical-volumes')}
            startIcon={<UploadFileIcon />}
          >
            {ACTION_LABELS.importPhysicalVolumes}
          </ToolbarActionButton>
          <ToolbarActionButton
            disabled={importBlocked || loading}
            onClick={() => onDelete('physical-volumes')}
            startIcon={<DeleteIcon />}
            color="error"
          >
            {ACTION_LABELS.deletePhysicalVolumes}
          </ToolbarActionButton>
          <ToolbarActionButton
            disabled={importBlocked || loading}
            onClick={() => onImport('equipment')}
            startIcon={<UploadFileIcon />}
          >
            {ACTION_LABELS.importEquipment}
          </ToolbarActionButton>
          <ToolbarActionButton
            disabled={importBlocked || loading}
            onClick={() => onDelete('equipment')}
            startIcon={<DeleteIcon />}
            color="error"
          >
            {ACTION_LABELS.deleteEquipment}
          </ToolbarActionButton>
          <ToolbarActionButton
            disabled={importBlocked || loading}
            onClick={() => onImport('labor-intensities')}
            startIcon={<UploadFileIcon />}
          >
            {ACTION_LABELS.importLabor}
          </ToolbarActionButton>
          <ToolbarActionButton
            disabled={importBlocked || loading}
            onClick={() => onDelete('labor-intensities')}
            startIcon={<DeleteIcon />}
            color="error"
          >
            {ACTION_LABELS.deleteLabor}
          </ToolbarActionButton>
        </>
      )}
    </Stack>
  );
}
