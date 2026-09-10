import type { ReactNode } from 'react';
import { Box, Button, Stack, Typography } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import { panelPrimaryToolbarSx } from '../shared/projectsPageTabStyles';

export type ForecastToolbarSubTab = 'forecast-total' | 'forecast-facility' | 'forecast-detail';

export interface ForecastToolbarProps {
  planVersionLabel: string;
  catalogLabel: string | null;
  hasPlanVersion: boolean;
  canImport: boolean;
  loading: boolean;
  exporting: boolean;
  detailGridReady: boolean;
  subTab: ForecastToolbarSubTab;
  onOpenVersionPicker: () => void;
  onOpenPlanning: () => void;
  onOpenReport: () => void;
  onImportPhysicalVolumes: () => void;
  onDeletePhysicalVolumes: () => void;
  onImportEquipment: () => void;
  onDeleteEquipment: () => void;
  onExportSummary: (type: 'excel' | 'pdf') => void;
  onExportFacility: (type: 'excel' | 'pdf') => void;
  onExportDetail: () => void;
  onExportDetailVisibleExcel: () => void;
  onExportDetailSummaryTable: () => void;
  children?: ReactNode;
}

/** Первая строка тулбара прогноза: версия, импорт/удаление, выгрузка. Без запросов. */
export function ForecastToolbar({
  planVersionLabel,
  catalogLabel,
  hasPlanVersion,
  canImport,
  loading,
  exporting,
  detailGridReady,
  subTab,
  onOpenVersionPicker,
  onOpenPlanning,
  onOpenReport,
  onImportPhysicalVolumes,
  onDeletePhysicalVolumes,
  onImportEquipment,
  onDeleteEquipment,
  onExportSummary,
  onExportFacility,
  onExportDetail,
  onExportDetailVisibleExcel,
  onExportDetailSummaryTable,
  children,
}: ForecastToolbarProps) {
  const importDisabled = !hasPlanVersion || loading;

  return (
    <>
      <Stack direction="row" sx={panelPrimaryToolbarSx}>
        <Button
          variant="contained"
          onClick={onOpenVersionPicker}
          sx={{
            backgroundColor: '#2980B9',
            color: 'white',
            '&:hover': { backgroundColor: '#2471A3' },
          }}
        >
          📋 Версия (план)
        </Button>
        {planVersionLabel && (
          <Box
            data-testid="plan-version-label"
            sx={{
              backgroundColor: '#EBF5FB',
              border: '1px solid #AED6F1',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
              maxWidth: { xs: '100%', sm: 320 },
            }}
          >
            <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>
              {planVersionLabel}
            </Typography>
          </Box>
        )}
        {catalogLabel && (
          <Box
            data-testid="plan-catalog-label"
            sx={{
              backgroundColor: '#FEF9E7',
              border: '1px solid #F9E79F',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
            }}
          >
            <Typography variant="body2" sx={{ color: '#7D6608' }} noWrap>
              {catalogLabel}
            </Typography>
          </Box>
        )}
        {hasPlanVersion && (
          <Button
            variant="outlined"
            onClick={onOpenPlanning}
            sx={{ textTransform: 'none' }}
            data-testid="forecast-planning-parameters-button"
          >
            Параметры
          </Button>
        )}
        {canImport && (
          <>
            <ToolbarActionButton
              disabled={importDisabled}
              onClick={onImportPhysicalVolumes}
              startIcon={<UploadFileIcon />}
            >
              {ACTION_LABELS.importPhysicalVolumes}
            </ToolbarActionButton>
            <ToolbarActionButton
              disabled={importDisabled}
              onClick={onDeletePhysicalVolumes}
              startIcon={<DeleteIcon />}
              color="error"
            >
              {ACTION_LABELS.deletePhysicalVolumes}
            </ToolbarActionButton>
            <ToolbarActionButton
              disabled={importDisabled}
              onClick={onImportEquipment}
              startIcon={<UploadFileIcon />}
            >
              {ACTION_LABELS.importEquipment}
            </ToolbarActionButton>
            <ToolbarActionButton
              disabled={importDisabled}
              onClick={onDeleteEquipment}
              startIcon={<DeleteIcon />}
              color="error"
            >
              {ACTION_LABELS.deleteEquipment}
            </ToolbarActionButton>
          </>
        )}
      </Stack>

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        {children}
        <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
          {subTab === 'forecast-total' && (
            <>
              <Button
                variant="contained"
                disabled={!hasPlanVersion}
                onClick={onOpenReport}
                sx={{ backgroundColor: '#27AE60', color: 'white', height: 36, textTransform: 'none' }}
              >
                📊 Прогноз
              </Button>
              <ExportActionButton
                kind="excel"
                disabled={!hasPlanVersion}
                loading={exporting}
                onClick={() => onExportSummary('excel')}
                data-testid="forecast-summary-export-excel"
              />
              <ExportActionButton
                kind="pdf"
                disabled={!hasPlanVersion}
                loading={exporting}
                onClick={() => onExportSummary('pdf')}
                data-testid="forecast-summary-export-pdf"
              />
            </>
          )}
          {subTab === 'forecast-facility' && (
            <>
              <ExportActionButton
                kind="excel"
                disabled={!hasPlanVersion}
                loading={exporting}
                onClick={() => onExportFacility('excel')}
                data-testid="forecast-facility-export-excel"
              />
              <ExportActionButton
                kind="pdf"
                disabled={!hasPlanVersion}
                loading={exporting}
                onClick={() => onExportFacility('pdf')}
                data-testid="forecast-facility-export-pdf"
              />
            </>
          )}
          {subTab === 'forecast-detail' && (
            <>
              <ExportActionButton
                kind="excel"
                disabled={!hasPlanVersion}
                loading={exporting}
                onClick={onExportDetail}
              />
              <ExportActionButton
                kind="excelSelected"
                disabled={!hasPlanVersion || !detailGridReady}
                loading={exporting}
                onClick={onExportDetailVisibleExcel}
              />
              <ExportActionButton
                kind="excelSummary"
                disabled={!hasPlanVersion}
                loading={exporting}
                onClick={onExportDetailSummaryTable}
              />
            </>
          )}
        </Stack>
      </Box>
    </>
  );
}
