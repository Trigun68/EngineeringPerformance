import { Suspense, lazy, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
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
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import type { GridApi } from 'ag-grid-community';
import {
  exportCompareDetailExcel,
  exportCompareDetailSummaryTable,
  exportCompareExcelVisible,
  fetchCompareChart,
  fetchVersionCompare,
  type CompareChartResult,
  type CompareExportSection,
  type VersionCompareDetailRow,
  type VersionCompareFacilityRow,
  type VersionCompareResult,
} from '../../../api/analyticsApi';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { DataTable } from '../../../components/DataTable/DataTable';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import {
  buildVersionLabel,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { getApiErrorMessage } from '../../../services/apiError';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import { collectGridExportData } from '../../../utils/directoryExport';
import {
  createCompareDetailColumns,
  createCompareFacilityColumns,
  createCompareSummaryColumns,
  type CompareScenario,
} from '../gridColumns';
import { shortCatalogName } from '../forecast/catalogShortName';
import { panelPrimaryToolbarSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import {
  filterCompareDetailRows,
  filterCompareFacilityRows,
  filterCompareSummaryRows,
} from './compareFilters';
import { buildCompareTreeRows, type CompareTreeRow } from './compareTree';
import { exportComparePdf, type ComparePdfSection } from '../../../api/visualReportsApi';
// Highcharts подтягивается только при переключении на режим диаграммы (ISS-018).
const CompareChartView = lazy(() =>
  import('./CompareChartView').then((m) => ({ default: m.CompareChartView })),
);

type CompareSubTab = 'compare-total' | 'compare-facility' | 'compare-detail';
type CompareViewMode = 'table' | 'chart';

function subTabToExportSection(subTab: CompareSubTab): CompareExportSection {
  if (subTab === 'compare-facility') return 'facility';
  if (subTab === 'compare-detail') return 'detail';
  return 'summary';
}

const catalogChipSx = {
  backgroundColor: '#FEF9E7',
  border: '1px solid #F9E79F',
  px: 1.5,
  py: 0.5,
  borderRadius: 1,
} as const;

export interface ComparePanelProps {
  projectId: number;
  workspaceVersions: ProjectVersion[];
  workspaceCompareVersionAId: number | null;
  workspaceCompareVersionBId: number | null;
}

function compareSummaryRowId(row: CompareTreeRow, index: number): string {
  return `cmp-${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level ?? 0}-${index}`;
}

function resolveCatalogShortLabel(
  version: ProjectVersion | undefined,
  physicalVolumeTypes: { id: number; name: string }[],
): string | null {
  const typeId = version?.physicalVolumeTypeId;
  if (!typeId) return null;
  const name = physicalVolumeTypes.find((t) => t.id === typeId)?.name ?? null;
  return shortCatalogName(name);
}

export function ComparePanel({ projectId, workspaceVersions, workspaceCompareVersionAId, workspaceCompareVersionBId }: ComparePanelProps) {
  const { canRead } = usePermission('VersionComparison');
  const { chromeExpanded, setChromeExpanded } = useWorkspaceChrome();
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);

  const [subTab, setSubTab] = usePersistedTabState<CompareSubTab>(
    `projects.${projectId}.compareSubTab`,
    'compare-total',
  );
  const [compareData, setCompareData] = useState<VersionCompareResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<CompareViewMode>('table');
  const [chartData, setChartData] = useState<CompareChartResult | null>(null);
  const [exportingPdf, setExportingPdf] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [pickerTarget, setPickerTarget] = useState<'A' | 'B' | null>(null);
  const [showOnlyDifferences, setShowOnlyDifferences] = useState(false);
  const [compareScenario, setCompareScenario] = useState<CompareScenario>('base');
  const [activeGridReady, setActiveGridReady] = useState(false);
  const gridApiRef = useRef<GridApi | null>(null);

  // Пользовательский выбор версии (переопределяет workspace по умолчанию)
  const [userVersionAId, setUserVersionAId] = useState<number | null>(null);
  const [userVersionBId, setUserVersionBId] = useState<number | null>(null);
  const versionAId = userVersionAId ?? workspaceCompareVersionAId;
  const versionBId = userVersionBId ?? workspaceCompareVersionBId;
  const versionALabel = userVersionAId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userVersionAId);
        return v ? buildVersionLabel(v) : '';
      })()
    : (() => {
        const v = workspaceVersions.find((vv) => vv.id === workspaceCompareVersionAId);
        return v ? buildVersionLabel(v) : '';
      })();
  const versionBLabel = userVersionBId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userVersionBId);
        return v ? buildVersionLabel(v) : '';
      })()
    : (() => {
        const v = workspaceVersions.find((vv) => vv.id === workspaceCompareVersionBId);
        return v ? buildVersionLabel(v) : '';
      })();
  const handleExportPdf = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    setExportingPdf(true);
    try {
      const section: ComparePdfSection = subTab === 'compare-facility' ? 'facility' : subTab === 'compare-detail' ? 'detail' : 'summary';
      await exportComparePdf(projectId, versionAId, versionBId, {
        section,
        showOnlyDifferences,
      });
    } catch {
      setError('Не удалось экспортировать PDF.');
    } finally {
      setExportingPdf(false);
    }
  }, [subTab, projectId, showOnlyDifferences, versionAId, versionBId]);

  const handleGridApiReady = useCallback((api: GridApi) => {
    gridApiRef.current = api;
    setActiveGridReady(true);
  }, []);

  useEffect(() => {
    gridApiRef.current = null;
    setActiveGridReady(false);
  }, [subTab, viewMode]);

  const handleExportVisibleExcel = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    const api = gridApiRef.current;
    if (!api) {
      setError('Таблица ещё не загружена.');
      return;
    }
    const { headers, rows: exportRows } = collectGridExportData(api, { includeGroups: true });
    if (headers.length === 0) {
      setError('Нет видимых столбцов для экспорта.');
      return;
    }
    if (exportRows.length === 0) {
      setError('Нет строк для экспорта (проверьте фильтры таблицы).');
      return;
    }
    const section = subTabToExportSection(subTab);
    const tabLabel =
      section === 'facility' ? 'Сравнение_По_объектам' : section === 'detail' ? 'Сравнение_Детально' : 'Сравнение_Итого';
    setExporting(true);
    setError(null);
    try {
      await exportCompareExcelVisible(
        projectId,
        section,
        `${tabLabel}_выделенные`,
        headers,
        exportRows,
      );
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт выделенных данных.'));
    } finally {
      setExporting(false);
    }
  }, [projectId, subTab, versionAId, versionBId]);

  const handleExportAllExcel = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    setExporting(true);
    setError(null);
    try {
      if (subTab === 'compare-detail') {
        await exportCompareDetailExcel(projectId, versionAId, versionBId);
      } else {
        // Итого / По объектам: полный набор из текущего грида (как «все» при отсутствии серверного full).
        const api = gridApiRef.current;
        if (!api) {
          setError('Таблица ещё не загружена.');
          return;
        }
        const { headers, rows: exportRows } = collectGridExportData(api, { includeGroups: true });
        if (headers.length === 0 || exportRows.length === 0) {
          setError('Нет данных для экспорта.');
          return;
        }
        const section = subTabToExportSection(subTab);
        const tabLabel = section === 'facility' ? 'Сравнение_По_объектам' : 'Сравнение_Итого';
        await exportCompareExcelVisible(projectId, section, `${tabLabel}_все`, headers, exportRows);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт Excel.'));
    } finally {
      setExporting(false);
    }
  }, [projectId, subTab, versionAId, versionBId]);

  const handleExportSummaryTable = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    setExporting(true);
    setError(null);
    try {
      await exportCompareDetailSummaryTable(projectId, versionAId, versionBId);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт сводной таблицы.'));
    } finally {
      setExporting(false);
    }
  }, [projectId, versionAId, versionBId]);

  const loadCompare = useCallback(async () => {
    if (!versionAId || !versionBId || !canRead) return;
    setLoading(true);
    setError(null);
    try {
      // Таблица и диаграмма независимы — грузим параллельно, а не каскадом.
      const [data, chart] = await Promise.all([
        fetchVersionCompare(projectId, versionAId, versionBId),
        fetchCompareChart(projectId, versionAId, versionBId, 'iic', 'zScore', 20),
      ]);
      setCompareData(data);
      setChartData(chart);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить сравнение версий.'));
      setCompareData(null);
    } finally {
      setLoading(false);
    }
  }, [canRead, projectId, versionAId, versionBId]);

  useEffect(() => {
    setUserVersionAId(null);
    setUserVersionBId(null);
    setCompareData(null);
    setError(null);
  }, [projectId]);

  useEffect(() => {
    void loadCompare();
  }, [loadCompare]);

  const handleVersionSelect = (version: ProjectVersion) => {
    if (pickerTarget === 'A') {
      setUserVersionAId(version.id);
    } else if (pickerTarget === 'B') {
      setUserVersionBId(version.id);
    }
    setPickerTarget(null);
  };

  const summaryRows = useMemo(() => {
    const rows = compareData?.summary ?? [];
    const filtered = filterCompareSummaryRows(rows, showOnlyDifferences, compareScenario);
    return buildCompareTreeRows(filtered);
  }, [compareData?.summary, compareScenario, showOnlyDifferences]);

  const facilityRows = useMemo(
    () => filterCompareFacilityRows(compareData?.facility ?? [], showOnlyDifferences, compareScenario),
    [compareData?.facility, compareScenario, showOnlyDifferences],
  );

  const detailRows = useMemo(
    () => filterCompareDetailRows(compareData?.detail ?? [], showOnlyDifferences, compareScenario),
    [compareData?.detail, compareScenario, showOnlyDifferences],
  );

  const summaryAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Направление',
      minWidth: 260,
      flex: 1,
      field: 'name' as const,
      cellRendererParams: { suppressCount: true },
    }),
    [],
  );

  const summaryColumns = useMemo(
    () => createCompareSummaryColumns(compareScenario) as unknown as import('ag-grid-community').ColDef<CompareTreeRow>[],
    [compareScenario],
  );
  const facilityColumns = useMemo(
    () => createCompareFacilityColumns(compareScenario),
    [compareScenario],
  );
  const detailColumns = useMemo(() => createCompareDetailColumns(compareScenario), [compareScenario]);

  const summaryGetRowClass = useCallback(
    (params: { data?: CompareTreeRow }) =>
      params.data?.isTotal ? 'forecast-summary-total-row' : undefined,
    [],
  );

  const hasCompareData = Boolean(compareData && versionAId && versionBId);

  const totalHoursDelta = useMemo(() => {
    if (!compareData) return null;
    return compareData.facility.reduce((s, r) => s + (r.hoursDelta ?? 0), 0);
  }, [compareData]);

  const catalogLabelA = useMemo(
    () => resolveCatalogShortLabel(workspaceVersions.find((v) => v.id === versionAId), physicalVolumeTypes),
    [physicalVolumeTypes, versionAId, workspaceVersions],
  );
  const catalogLabelB = useMemo(
    () => resolveCatalogShortLabel(workspaceVersions.find((v) => v.id === versionBId), physicalVolumeTypes),
    [physicalVolumeTypes, versionBId, workspaceVersions],
  );

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр сравнения версий.</Alert>;
  }

  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      <WorkingOverlay open={exportingPdf || exporting} />
      <PanelBusyBar open={loading} />
      {chromeExpanded && (
        <>
          <Stack direction="row" sx={panelPrimaryToolbarSx}>
            <Button
              variant="contained"
              onClick={() => setPickerTarget('A')}
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
              onClick={() => setPickerTarget('B')}
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
                  Различия версий: {totalHoursDelta.toFixed(2)} ч
                </Typography>
              </Box>
            )}
            <FormControlLabel
              control={
                <Checkbox
                  checked={showOnlyDifferences}
                  disabled={!hasCompareData}
                  onChange={(e) => setShowOnlyDifferences(e.target.checked)}
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
                onChange={(event) => setCompareScenario(event.target.value as CompareScenario)}
              >
                <MenuItem value="base">Базовый</MenuItem>
                <MenuItem value="optimistic">Оптимистичный</MenuItem>
                <MenuItem value="pessimistic">Пессимистичный</MenuItem>
              </Select>
            </FormControl>
          </Stack>

          <Stack direction="row" spacing={1} sx={{ mb: 1.5, flexWrap: 'wrap', alignItems: 'center' }}>
            {(['table', 'chart'] as CompareViewMode[]).map((mode) => (
              <Button
                key={mode}
                size="small"
                variant={viewMode === mode ? 'contained' : 'outlined'}
                onClick={() => setViewMode(mode)}
                sx={{ textTransform: 'none' }}
              >
                {mode === 'table' ? 'Таблица' : 'График'}
              </Button>
            ))}
            {viewMode === 'table' && (
              <>
                <Button
                  size="small"
                  variant="outlined"
                  disabled={!hasCompareData || exporting}
                  onClick={() => void handleExportAllExcel()}
                  startIcon={<FileDownloadIcon />}
                  sx={{ textTransform: 'none' }}
                >
                  Экспорт всех excel
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  disabled={!hasCompareData || !activeGridReady || exporting}
                  onClick={() => void handleExportVisibleExcel()}
                  startIcon={<FileDownloadIcon />}
                  sx={{ textTransform: 'none' }}
                >
                  Экспорт выделенных excel
                </Button>
              </>
            )}
            <Button
              size="small"
              variant="outlined"
              disabled={!hasCompareData || exportingPdf}
              onClick={() => void handleExportPdf()}
              startIcon={<PictureAsPdfIcon />}
              sx={{ textTransform: 'none' }}
            >
              Экспорт PDF
            </Button>
            {viewMode === 'table' && subTab === 'compare-detail' && (
              <Button
                size="small"
                variant="outlined"
                disabled={!hasCompareData || exporting}
                onClick={() => void handleExportSummaryTable()}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Сводная таблица
              </Button>
            )}
          </Stack>

          <Stack
            direction="row"
            alignItems="center"
            sx={{ mb: 1, gap: 1, width: '100%' }}
          >
            <Tabs
              value={subTab}
              onChange={(_, v: CompareSubTab) => setSubTab(v)}
              variant="scrollable"
              scrollButtons="auto"
              allowScrollButtonsMobile
              sx={{ ...projectsPageSubTabsSx, mb: 0, flex: 1, minWidth: 0 }}
            >
              <Tab label="Итого" value="compare-total" />
              <Tab label="По объектам" value="compare-facility" />
              <Tab label="Детально" value="compare-detail" />
            </Tabs>
            <Tooltip title="Свернуть шапку">
              <IconButton
                size="small"
                aria-label="Свернуть шапку"
                data-testid="project-header-toggle"
                onClick={() => setChromeExpanded(false)}
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

          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        </>
      )}

      {!chromeExpanded && (
        <Box
          data-testid="workspace-chrome-toggle-row"
          sx={{
            display: 'flex',
            justifyContent: 'flex-start',
            alignItems: 'center',
            flexShrink: 0,
            py: 0.5,
            mb: 0.5,
          }}
        >
          <Tooltip title="Развернуть шапку">
            <IconButton
              size="small"
              aria-label="Развернуть шапку"
              data-testid="project-header-toggle"
              onClick={() => setChromeExpanded(true)}
              sx={{
                border: '1px solid #D5D8DC',
                bgcolor: '#F8F9FA',
                borderRadius: 1,
                py: 0.25,
                px: 1.5,
                '&:hover': { bgcolor: '#EEF2F5' },
              }}
            >
              <ExpandMoreIcon
                sx={{
                  transform: 'rotate(0deg)',
                  transition: 'transform 0.2s ease',
                  color: '#5D6D7E',
                }}
              />
            </IconButton>
          </Tooltip>
        </Box>
      )}

      <Box sx={{ flex: 1, minHeight: 360, display: 'flex', flexDirection: 'row', gap: 2 }}>
        {viewMode === 'table' && (
          <Box sx={{ flex: 1, minHeight: 360, height: '100%', width: '100%', minWidth: 0 }}>
            {subTab === 'compare-total' && (
              <Box
                sx={{
                  height: '100%',
                  '& .forecast-summary-total-row': {
                    backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
                    fontWeight: 600,
                  },
                }}
              >
                <DataTable<CompareTreeRow>
                  key={`compare-summary-${showOnlyDifferences}-${summaryRows.length}`}
                  rowData={summaryRows}
                  columnDefs={summaryColumns}
                  objectName="VersionComparison"
                  gridId="compare-summary"
                  loading={loading}
                  height="100%"
                  treeData
                  getDataPath={(row) => row.hierarchy}
                  autoGroupColumnDef={summaryAutoGroupColumnDef}
                  showColumnSettings
                  getRowIdValue={compareSummaryRowId}
                  getRowClass={summaryGetRowClass}
                  onGridApiReady={handleGridApiReady}
                />
              </Box>
            )}

            {subTab === 'compare-facility' && (
              <DataTable<VersionCompareFacilityRow>
                key={`compare-facility-${showOnlyDifferences}-${facilityRows.length}`}
                rowData={facilityRows}
                columnDefs={facilityColumns}
                objectName="VersionComparison"
                gridId="compare-facility"
                loading={loading}
                height="100%"
                showColumnSettings
                getRowIdValue={(row) => `${row.facilityCode}|${row.facilityName}`}
                onGridApiReady={handleGridApiReady}
              />
            )}

            {subTab === 'compare-detail' && (
              <DataTable<VersionCompareDetailRow>
                key={`compare-detail-${showOnlyDifferences}-${detailRows.length}`}
                rowData={detailRows}
                columnDefs={detailColumns}
                objectName="VersionComparison"
                gridId="compare-detail"
                loading={loading}
                height="100%"
                enableRowGrouping
                showColumnSettings
                getRowIdValue={(row) =>
                  `${row.facilityCode}|${row.pvCode}|${row.workId}`
                }
                onGridApiReady={handleGridApiReady}
              />
            )}
          </Box>
        )}
        {viewMode === 'chart' && subTab === 'compare-total' && (
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Suspense fallback={<PanelBusyBar open delayMs={0} />}>
              <CompareChartView chart={chartData} height={400} />
            </Suspense>
          </Box>
        )}
      </Box>

      <ProjectVersionPickerDialog
        open={pickerTarget != null}
        title={pickerTarget === 'A' ? 'Выбор версии А' : 'Выбор версии Б'}
        projectId={projectId}
        versions={workspaceVersions}
        directionFilter="any"
        selectedVersionId={pickerTarget === 'A' ? versionAId : versionBId}
        onSelect={handleVersionSelect}
        onClose={() => setPickerTarget(null)}
      />
    </Box>
  );
}
