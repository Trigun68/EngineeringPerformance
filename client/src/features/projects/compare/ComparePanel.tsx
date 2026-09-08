import { startTransition, useCallback, useDeferredValue, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
  IconButton,
  Tooltip,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import {
  fetchVersionCompare,
  fetchVersionCompareDetail,
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
import { queryKeys } from '../../../services/queryKeys';
import {
  compareScenarioPrefKey,
  compareShowDiffPrefKey,
  compareVersionAPrefKey,
  compareVersionBPrefKey,
  parsePrefId,
} from '../../../utils/projectWorkspacePrefs';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import {
  createCompareDetailColumns,
  createCompareFacilityColumns,
  createCompareSummaryColumns,
  type CompareScenario,
} from '../gridColumns';
import { shortCatalogName } from '../forecast/catalogShortName';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { compareTabPlannedHoursDelta } from './compareColumns';
import { buildCompareFacilityPinnedTotal } from './compareFacilityTotals';
import {
  filterCompareDetailRows,
  filterCompareFacilityRows,
  filterCompareSummaryRows,
} from './compareFilters';
import { buildCompareTreeRows, type CompareTreeRow } from './compareTree';
import { CompareToolbar } from './CompareToolbar';
import { useCompareExport, type CompareSubTab } from './useCompareExport';

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

  // Пользовательский выбор версии (переопределяет workspace по умолчанию)
  const [userVersionAId, setUserVersionAId] = useState<number | null>(null);
  const [userVersionBId, setUserVersionBId] = useState<number | null>(null);
  const restoredComparePrefForProjectRef = useRef<number | null>(null);
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

  const {
    actionError,
    setError,
    exporting,
    exportingPdf,
    activeGridReady,
    handleGridApiReady,
    handleExportPdf,
    handleExportVisibleExcel,
    handleExportAllExcel,
    handleExportSummaryTable,
  } = useCompareExport({
    projectId,
    versionAId,
    versionBId,
    subTab,
    showOnlyDifferences,
  });

  useEffect(() => {
    setUserVersionAId(null);
    setUserVersionBId(null);
    setError(null);
    restoredComparePrefForProjectRef.current = null;
  }, [projectId, setError]);

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

  // Перезагрузка — invalidateQueries по queryKeys.compare.pageData / .detail
  // (префикс detail без subKey сбрасывает ленивый кэш). Смена A/B сама меняет ключ.
  const pageDataQuery = useQuery({
    queryKey: queryKeys.compare.pageData(projectId, versionAId ?? undefined, versionBId ?? undefined),
    queryFn: ({ signal }) => fetchVersionCompare(projectId, versionAId!, versionBId!, signal),
    enabled: canRead && versionAId != null && versionBId != null,
  });

  const pageDataHasDetail = (pageDataQuery.data?.detail.length ?? 0) > 0;

  // Ленивая догрузка «Детально»: fetch только на вкладке compare-detail,
  // после успешного page-data и если грид в page-data ещё пустой.
  // subTab в ключе, чтобы кэш не смешивался с «Итого» / «по объектам».
  const detailQuery = useQuery({
    queryKey: queryKeys.compare.detail(projectId, versionAId ?? undefined, versionBId ?? undefined, subTab),
    queryFn: ({ signal }) => fetchVersionCompareDetail(projectId, versionAId!, versionBId!, signal),
    enabled:
      canRead
      && versionAId != null
      && versionBId != null
      && subTab === 'compare-detail'
      && pageDataQuery.isSuccess
      && !pageDataHasDetail,
  });

  const compareData = useMemo((): VersionCompareResult | null => {
    const base = pageDataQuery.data;
    if (!base) return null;
    const lazyDetail = detailQuery.data;
    if (lazyDetail === undefined) return base;
    return { ...base, detail: lazyDetail };
  }, [detailQuery.data, pageDataQuery.data]);

  const loading = pageDataQuery.isFetching;
  const detailLoading = detailQuery.isFetching;
  const pageDataError =
    pageDataQuery.isError && !isRequestCanceled(pageDataQuery.error)
      ? getApiErrorMessage(pageDataQuery.error, 'Не удалось загрузить сравнение версий.')
      : null;
  const detailError =
    detailQuery.isError && !isRequestCanceled(detailQuery.error)
      ? getApiErrorMessage(detailQuery.error, 'Не удалось загрузить детальное сравнение версий.')
      : null;
  const error = actionError ?? pageDataError ?? detailError;

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
  // Тяжёлый грид «Детально»: не блокируем переключение вкладок свежим rowData.
  const deferredDetailRows = useDeferredValue(detailRows);

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
          <CompareToolbar
            versionALabel={versionALabel}
            versionBLabel={versionBLabel}
            catalogLabelA={catalogLabelA}
            catalogLabelB={catalogLabelB}
            showOnlyDifferences={showOnlyDifferences}
            hasCompareData={hasCompareData}
            compareScenario={compareScenario}
            totalHoursDelta={totalHoursDelta}
            subTab={subTab}
            exporting={exporting}
            exportingPdf={exportingPdf}
            activeGridReady={activeGridReady}
            onPickVersionA={() => setPickerTarget('A')}
            onPickVersionB={() => setPickerTarget('B')}
            onShowDiffChange={(checked) => setShowDiffRaw(checked ? '1' : '0')}
            onScenarioChange={setCompareScenario}
            onSubTabChange={(tab) => startTransition(() => setSubTab(tab))}
            onExportExcel={() => void handleExportAllExcel()}
            onExportExcelSelected={() => void handleExportVisibleExcel()}
            onExportPdf={() => void handleExportPdf()}
            onExportExcelSummary={() => void handleExportSummaryTable()}
            onCollapseChrome={() => setChromeExpanded(false)}
          />

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
              rowData={deferredDetailRows}
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
