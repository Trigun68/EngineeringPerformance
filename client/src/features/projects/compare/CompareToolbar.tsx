import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import {
  Box,
  Button,
  Checkbox,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tab,
  Tabs,
  Tooltip,
  Typography,
} from '@mui/material';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { formatGridNumber } from '../../../utils/formatGridNumber';
import type { CompareScenario } from '../gridColumns';
import { panelPrimaryToolbarSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import type { CompareSubTab } from './useCompareExport';

const catalogChipSx = {
  backgroundColor: '#FEF9E7',
  border: '1px solid #F9E79F',
  px: 1.5,
  py: 0.5,
  borderRadius: 1,
} as const;

export interface CompareToolbarProps {
  versionALabel: string;
  versionBLabel: string;
  catalogLabelA: string | null;
  catalogLabelB: string | null;
  showOnlyDifferences: boolean;
  hasCompareData: boolean;
  compareScenario: CompareScenario;
  totalHoursDelta: number | null;
  subTab: CompareSubTab;
  exporting: boolean;
  exportingPdf: boolean;
  activeGridReady: boolean;
  onPickVersionA: () => void;
  onPickVersionB: () => void;
  onShowDiffChange: (checked: boolean) => void;
  onScenarioChange: (scenario: CompareScenario) => void;
  onSubTabChange: (tab: CompareSubTab) => void;
  onExportExcel: () => void;
  onExportExcelSelected: () => void;
  onExportPdf: () => void;
  onExportExcelSummary: () => void;
  onCollapseChrome: () => void;
}

/** Шапка сравнения: версии, фильтры, вкладки и выгрузка. Только props, без fetch. */
export function CompareToolbar({
  versionALabel,
  versionBLabel,
  catalogLabelA,
  catalogLabelB,
  showOnlyDifferences,
  hasCompareData,
  compareScenario,
  totalHoursDelta,
  subTab,
  exporting,
  exportingPdf,
  activeGridReady,
  onPickVersionA,
  onPickVersionB,
  onShowDiffChange,
  onScenarioChange,
  onSubTabChange,
  onExportExcel,
  onExportExcelSelected,
  onExportPdf,
  onExportExcelSummary,
  onCollapseChrome,
}: CompareToolbarProps) {
  return (
    <>
      <Stack direction="row" sx={panelPrimaryToolbarSx}>
        <Button
          variant="contained"
          onClick={onPickVersionA}
          sx={{ backgroundColor: '#2980B9', color: 'white', '&:hover': { backgroundColor: '#2471A3' } }}
        >
          📋 Версия А
        </Button>
        {versionALabel && (
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
              {versionALabel}
            </Typography>
          </Box>
        )}
        {catalogLabelA && (
          <Box data-testid="compare-catalog-label-a" sx={catalogChipSx}>
            <Typography variant="body2" sx={{ color: '#7D6608' }} noWrap>
              {catalogLabelA}
            </Typography>
          </Box>
        )}
        <Button
          variant="contained"
          onClick={onPickVersionB}
          sx={{ backgroundColor: '#8E44AD', color: 'white', '&:hover': { backgroundColor: '#7D3C98' } }}
        >
          📋 Версия Б
        </Button>
        {versionBLabel && (
          <Box
            sx={{
              backgroundColor: '#F5EEF8',
              border: '1px solid #D7BDE2',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
              minWidth: 0,
              maxWidth: { xs: '100%', sm: 280 },
            }}
          >
            <Typography variant="body2" sx={{ color: '#6C3483' }} noWrap>
              {versionBLabel}
            </Typography>
          </Box>
        )}
        {catalogLabelB && (
          <Box data-testid="compare-catalog-label-b" sx={catalogChipSx}>
            <Typography variant="body2" sx={{ color: '#7D6608' }} noWrap>
              {catalogLabelB}
            </Typography>
          </Box>
        )}
        <FormControlLabel
          control={
            <Checkbox
              checked={showOnlyDifferences}
              disabled={!hasCompareData}
              onChange={(e) => onShowDiffChange(e.target.checked)}
              size="small"
            />
          }
          label="Показывать различия"
          sx={{ ml: 0.5, mr: 0 }}
        />
        <FormControl size="small" sx={{ minWidth: 150 }} disabled={!hasCompareData}>
          <InputLabel id="compare-scenario-label">Сценарий</InputLabel>
          <Select
            labelId="compare-scenario-label"
            label="Сценарий"
            value={compareScenario}
            onChange={(event) => onScenarioChange(event.target.value as CompareScenario)}
          >
            <MenuItem value="base">Базовый</MenuItem>
            <MenuItem value="optimistic">Оптимистичный</MenuItem>
            <MenuItem value="pessimistic">Пессимистичный</MenuItem>
          </Select>
        </FormControl>
        {totalHoursDelta != null && (
          <Box
            data-testid="compare-version-differences"
            sx={{
              backgroundColor: '#EBF5FB',
              border: '1px solid #AED6F1',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
            }}
          >
            <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>
              Различия версий: {formatGridNumber(totalHoursDelta, 2)} ч
            </Typography>
          </Box>
        )}
      </Stack>

      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          mb: 2,
          gap: 2,
          flexWrap: 'wrap',
          width: '100%',
        }}
      >
        <Tabs
          value={subTab}
          onChange={(_, v: CompareSubTab) => onSubTabChange(v)}
          variant="scrollable"
          scrollButtons="auto"
          allowScrollButtonsMobile
          sx={{ ...projectsPageSubTabsSx, mb: 0, width: 'auto', flex: '0 1 auto' }}
        >
          <Tab label="Итого" value="compare-total" />
          <Tab label="По объектам" value="compare-facility" />
          <Tab label="Детально" value="compare-detail" />
        </Tabs>
        <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }} alignItems="center">
          <ExportActionButton
            kind="excel"
            disabled={!hasCompareData}
            loading={exporting}
            onClick={onExportExcel}
          />
          <ExportActionButton
            kind="excelSelected"
            disabled={!hasCompareData || !activeGridReady}
            loading={exporting}
            onClick={onExportExcelSelected}
          />
          {subTab !== 'compare-detail' ? (
            <ExportActionButton
              kind="pdf"
              disabled={!hasCompareData}
              loading={exportingPdf}
              onClick={onExportPdf}
            />
          ) : null}
          {subTab === 'compare-detail' && (
            <ExportActionButton
              kind="excelSummary"
              disabled={!hasCompareData}
              loading={exporting}
              onClick={onExportExcelSummary}
            />
          )}
          <Tooltip title="Свернуть шапку">
            <IconButton
              size="small"
              aria-label="Свернуть шапку"
              data-testid="project-header-toggle"
              onClick={onCollapseChrome}
              sx={{
                border: '1px solid #D5D8DC',
                bgcolor: '#F8F9FA',
                borderRadius: 1,
                py: 0.25,
                px: 1.5,
                flexShrink: 0,
                '&:hover': { bgcolor: '#EEF2F5' },
              }}
            >
              <ExpandMoreIcon
                sx={{
                  transform: 'rotate(180deg)',
                  transition: 'transform 0.2s ease',
                  color: '#5D6D7E',
                }}
              />
            </IconButton>
          </Tooltip>
        </Stack>
      </Box>
    </>
  );
}
