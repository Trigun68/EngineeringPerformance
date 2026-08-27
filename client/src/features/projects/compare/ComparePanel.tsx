import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
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
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import type { GridApi } from 'ag-grid-community';
import {
  exportCompareDetailExcel,
  exportCompareDetailSummaryTable,
  exportCompareExcelVisible,
  fetchVersionCompare,
  fetchVersionCompareDetail,
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
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import {
  compareScenarioPrefKey,
  compareShowDiffPrefKey,
  compareVersionAPrefKey,
  compareVersionBPrefKey,
  parsePrefId,
} from '../../../utils/projectWorkspacePrefs';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import { collectGridExportData } from '../../../utils/directoryExport';
import { formatGridNumber } from '../../../utils/formatGridNumber';
import {
  createCompareDetailColumns,
  createCompareFacilityColumns,
  createCompareSummaryColumns,
  type CompareScenario,
} from '../gridColumns';
import { shortCatalogName } from '../forecast/catalogShortName';
import { panelPrimaryToolbarSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { compareTabPlannedHoursDelta } from './compareColumns';
import { buildCompareFacilityPinnedTotal } from './compareFacilityTotals';
import {
  filterCompareDetailRows,
  filterCompareFacilityRows,
  filterCompareSummaryRows,
} from './compareFilters';
import { buildCompareTreeRows, type CompareTreeRow } from './compareTree';
import { exportComparePdf, type ComparePdfSection } from '../../../api/visualReportsApi';

type CompareSubTab = 'compare-total' | 'compare-facility' | 'compare-detail';

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
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [exportingPdf, setExportingPdf] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [pickerTarget, setPickerTarget] = useState<'A' | 'B' | null>(null);
  const [showDiffRaw, setShowDiffRaw] = usePersistedTabState<'0' | '1'>(
    compareShowDiffPrefKey(projectId),
    '0',
  );
  const showOnlyDifferences = showDiffRaw === '1';
  const [compareScenario, setCompareScenario] = usePersistedTabState<CompareScenario>(
    compareScenarioPrefKey(projectId),
    'base',
  );
  const [activeGridReady, setActiveGridReady] = useState(false);
  const gridApiRef = useRef<GridApi | null>(null);

  // Пользовательский выбор версии (переопределяет workspace по умолчанию)
  const [userVersionAId, setUserVersionAId] = useState<number | null>(null);
  const [userVersionBId, setUserVersionBId] = useState<number | null>(null);
  const restoredComparePrefForProjectRef = useRef<number | null>(null);
  const versionAId = userVersionAId ?? workspaceCompareVersionAId;
  const versionBId = userVersionBId ?? workspaceCompareVersionBId;
  const versionAIdRef = useRef<number | null>(null);
  const versionBIdRef = useRef<number | null>(null);
  const projectIdRef = useRef(projectId);
  const compareLoadGenRef = useRef(0);
  const compareAbortRef = useRef<AbortController | null>(null);
  const loadedComparePairRef = useRef<string | null>(null);
  versionAIdRef.current = versionAId;
  versionBIdRef.current = versionBId;
  projectIdRef.current = projectId;
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
  }, [subTab]);

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
        versionAId,
        versionBId,
        true,
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
        await exportCompareExcelVisible(
          projectId,
          section,
          `${tabLabel}_все`,
          headers,
          exportRows,
          versionAId,
          versionBId,
          false,
        );
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
    const requestedA = versionAId;
    const requestedB = versionBId;
    const requestedPair = `${requestedA}:${requestedB}`;
    const loadGen = ++compareLoadGenRef.current;
    compareAbortRef.current?.abort();
    const controller = new AbortController();
    compareAbortRef.current = controller;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchVersionCompare(projectId, requestedA, requestedB, controller.signal);
      if (
        loadGen !== compareLoadGenRef.current
        || requestedA !== versionAIdRef.current
        || requestedB !== versionBIdRef.current
      ) {
        return;
      }
      setCompareData((prev) => {
        const keepDetail =
          prev != null
          && prev.detail.length > 0
          && data.detail.length === 0
          && loadedComparePairRef.current === requestedPair;
        loadedComparePairRef.current = requestedPair;
        return keepDetail ? { ...data, detail: prev.detail } : data;
      });
    } catch (err) {
      if (isRequestCanceled(err) || loadGen !== compareLoadGenRef.current) return;
      setError(getApiErrorMessage(err, 'Не удалось загрузить сравнение версий.'));
      setCompareData(null);
    } finally {
      if (loadGen === compareLoadGenRef.current) {
        setLoading(false);
      }
    }
  }, [canRead, projectId, versionAId, versionBId]);

  useEffect(() => {
    compareAbortRef.current?.abort();
    compareLoadGenRef.current += 1;
    setUserVersionAId(null);
    setUserVersionBId(null);
    setCompareData(null);
    setError(null);
    restoredComparePrefForProjectRef.current = null;
    loadedComparePairRef.current = null;
  }, [projectId]);

  useEffect(() => {
    if (workspaceVersions.length === 0 || restoredComparePrefForProjectRef.current === projectId) return;
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        restoredComparePrefForProjectRef.current = projectId;
        const savedA = parsePrefId(prefs[compareVersionAPrefKey(projectId)]);
        const savedB = parsePrefId(prefs[compareVersionBPrefKey(projectId)]);
        if (savedA != null && workspaceVersions.some((v) => v.id === savedA)) {
          setUserVersionAId(savedA);
        }
        if (savedB != null && workspaceVersions.some((v) => v.id === savedB)) {
          setUserVersionBId(savedB);
        }
      })
      .catch(() => {
        restoredComparePrefForProjectRef.current = projectId;
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, workspaceVersions]);

  useEffect(() => {
    void loadCompare();
  }, [loadCompare]);

  const compareDetailLength = compareData?.detail.length ?? 0;
  const hasComparePayload = compareData != null;

  // Детально догружается отдельно. Не завязывать эффект на объект compareData и не
  // глушить fetch в cleanup — иначе prefs версий A/B оставляют грид пустым.
  useEffect(() => {
    if (subTab !== 'compare-detail' || !versionAId || !versionBId || !canRead || !hasComparePayload) {
      return;
    }
    if (compareDetailLength > 0) {
      return;
    }
    const requestedA = versionAId;
    const requestedB = versionBId;
    const requestedProjectId = projectId;
    const loadGen = compareLoadGenRef.current;
    const controller = new AbortController();
    setDetailLoading(true);
    void fetchVersionCompareDetail(projectId, requestedA, requestedB, controller.signal)
      .then((detail) => {
        if (
          requestedProjectId !== projectIdRef.current
          || requestedA !== versionAIdRef.current
          || requestedB !== versionBIdRef.current
        ) {
          return;
        }
        if (loadGen !== compareLoadGenRef.current) {
          return;
        }
        setCompareData((prev) => (prev ? { ...prev, detail } : prev));
      })
      .catch((err) => {
        if (
          isRequestCanceled(err)
          || requestedA !== versionAIdRef.current
          || requestedB !== versionBIdRef.current
        ) {
          return;
        }
        setError(getApiErrorMessage(err, 'Не удалось загрузить детальное сравнение версий.'));
      })
      .finally(() => {
        if (
          requestedA === versionAIdRef.current
          && requestedB === versionBIdRef.current
          && loadGen === compareLoadGenRef.current
        ) {
          setDetailLoading(false);
        }
      });
    return () => {
      if (
        versionAIdRef.current !== requestedA
        || versionBIdRef.current !== requestedB
        || projectIdRef.current !== requestedProjectId
      ) {
        controller.abort();
      }
      setDetailLoading(false);
    };
  }, [subTab, versionAId, versionBId, canRead, hasComparePayload, compareDetailLength, projectId]);

  const handleVersionSelect = (version: ProjectVersion) => {
    if (pickerTarget === 'A') {
      setUserVersionAId(version.id);
      void putUserPreferences({ [compareVersionAPrefKey(projectId)]: String(version.id) });
    } else if (pickerTarget === 'B') {
      setUserVersionBId(version.id);
      void putUserPreferences({ [compareVersionBPrefKey(projectId)]: String(version.id) });
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
  const facilityPinnedBottomRow = useMemo(
    () => buildCompareFacilityPinnedTotal(facilityRows),
    [facilityRows],
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

  const totalHoursDelta = useMemo(
    () => compareTabPlannedHoursDelta(subTab, compareData, compareScenario),
    [compareData, compareScenario, subTab],
  );

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
      <PanelBusyBar open={loading || detailLoading} />
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
            <FormControlLabel
              control={
                <Checkbox
                  checked={showOnlyDifferences}
                  disabled={!hasCompareData}
                  onChange={(e) => setShowDiffRaw(e.target.checked ? '1' : '0')}
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
                onChange={(event) =>
                  setCompareScenario(event.target.value as CompareScenario)
                }
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
              onChange={(_, v: CompareSubTab) => setSubTab(v)}
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
                onClick={() => void handleExportAllExcel()}
              />
              <ExportActionButton
                kind="excelSelected"
                disabled={!hasCompareData || !activeGridReady}
                loading={exporting}
                onClick={() => void handleExportVisibleExcel()}
              />
              <ExportActionButton
                kind="pdf"
                disabled={!hasCompareData}
                loading={exportingPdf}
                onClick={() => void handleExportPdf()}
              />
              {subTab === 'compare-detail' && (
                <ExportActionButton
                  kind="excelSummary"
                  disabled={!hasCompareData}
                  loading={exporting}
                  onClick={() => void handleExportSummaryTable()}
                />
              )}
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
          </Box>

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
                projectId={projectId}
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
              projectId={projectId}
              loading={loading}
              height="100%"
              showColumnSettings
              pinnedBottomRowData={facilityPinnedBottomRow}
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
              projectId={projectId}
              loading={loading || detailLoading}
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
