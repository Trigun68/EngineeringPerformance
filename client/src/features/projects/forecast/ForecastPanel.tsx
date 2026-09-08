import { startTransition, useCallback, useDeferredValue, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Stack,
  Tab,
  Tabs,
} from '@mui/material';
import type { ImportDataType } from '../../../api/importApi';
import {
  fetchProjectsPageData,
  fetchProjectsPageDetails,
} from '../../../api/projectsPageApi';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { DataTable } from '../../../components/DataTable/DataTable';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import {
  ANALYSIS_PLAN,
  buildVersionLabel,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import {
  LAST_PROJECT_PREF_KEY,
  parsePrefId,
  planVersionPrefKey,
} from '../../../utils/projectWorkspacePrefs';
import type { GridApi } from 'ag-grid-community';
import {
  createForecastDetailColumns,
  createForecastFacilityColumns,
  createForecastSummaryColumns,
} from '../gridColumns';
import type { ProjectsPageDetailRow, ProjectsPageData, ProjectsPageFacilityRow, ProjectsPageForecastSummaryRow } from '../types';
import { DataImportDialog } from './DataImportDialog';
import { ForecastReportDialog } from './ForecastReportDialog';
import { ForecastToolbar } from './ForecastToolbar';
import { IterationDeleteDialog } from './IterationDeleteDialog';
import { PlanningParametersDialog } from './PlanningParametersDialog';
import { shortCatalogName } from './catalogShortName';
import { useForecastExport } from './useForecastExport';
import { projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { PanelNotificationsButton } from '../shared/PanelNotificationsButton';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import { useProjectDetailContextMenu } from '../shared/useProjectDetailContextMenu';
import { collectForecastVolumeWarnings } from './normFallbackWarning';
import type { ProjectVersion } from '../../../api/projectVersionsApi';

type ForecastSubTab = 'forecast-total' | 'forecast-facility' | 'forecast-detail';
type ImportDialogType = Extract<ImportDataType, 'physical-volumes' | 'equipment'> | null;
type DeleteDialogType = Extract<ImportDataType, 'physical-volumes' | 'equipment'> | null;

export interface ForecastPanelProps {
  projectId: number;
  projectName?: string;
  projectCode?: string;
  workspaceVersions: ProjectVersion[];
  workspacePlanVersionId: number | null;
  workspacePlanVersionLabel: string;
}

function summaryRowId(row: ProjectsPageForecastSummaryRow, index: number): string {
  return `${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level}-${index}`;
}

export function ForecastPanel({ projectId, projectName = '', projectCode = '', workspaceVersions, workspacePlanVersionId, workspacePlanVersionLabel }: ForecastPanelProps) {
  const queryClient = useQueryClient();
  const { canRead } = usePermission('Projects');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { chromeExpanded } = useWorkspaceChrome();

  // Пользовательский выбор версии (переопределяет workspace по умолчанию)
  const [userPlanVersionId, setUserPlanVersionId] = useState<number | null>(null);
  const restoredPlanPrefForProjectRef = useRef<number | null>(null);
  const planVersionId = userPlanVersionId ?? workspacePlanVersionId;
  const planVersionLabel = userPlanVersionId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userPlanVersionId);
        return v ? buildVersionLabel(v) : '';
      })()
    : workspacePlanVersionLabel;
  const [subTab, setSubTab] = usePersistedTabState<ForecastSubTab>(
    `projects.${projectId}.forecastSubTab`,
    'forecast-total',
  );
  const [actionLoading, setLoading] = useState(false);
  const [actionError, setError] = useState<string | null>(null);
  const [versionPickerOpen, setVersionPickerOpen] = useState(false);
  const [reportOpen, setReportOpen] = useState(false);
  const [planningOpen, setPlanningOpen] = useState(false);
  const detailGridApiRef = useRef<GridApi | null>(null);
  const [detailGridReady, setDetailGridReady] = useState(false);
  const [importDialogType, setImportDialogType] = useState<ImportDialogType>(null);
  const [deleteDialogType, setDeleteDialogType] = useState<DeleteDialogType>(null);

  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);

  const selectedPlanVersion = useMemo(
    () => workspaceVersions.find((v) => v.id === planVersionId) ?? null,
    [workspaceVersions, planVersionId],
  );

  const catalogShortLabel = useMemo(() => {
    const typeId = selectedPlanVersion?.physicalVolumeTypeId;
    if (!typeId) return null;
    const full = physicalVolumeTypes.find((t) => t.id === typeId)?.name
      ?? physicalVolumeTypes.find((t) => t.id === typeId)?.description
      ?? null;
    return shortCatalogName(full);
  }, [physicalVolumeTypes, selectedPlanVersion?.physicalVolumeTypeId]);

  useEffect(() => {
    setUserPlanVersionId(null);
    setError(null);
    restoredPlanPrefForProjectRef.current = null;
  }, [projectId]);

  useEffect(() => {
    if (workspaceVersions.length === 0 || restoredPlanPrefForProjectRef.current === projectId) return;
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        restoredPlanPrefForProjectRef.current = projectId;
        const saved = parsePrefId(prefs[planVersionPrefKey(projectId)]);
        if (saved != null && workspaceVersions.some((v) => v.id === saved)) {
          setUserPlanVersionId(saved);
        }
      })
      .catch(() => {
        restoredPlanPrefForProjectRef.current = projectId;
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, workspaceVersions]);

  useEffect(() => {
    setError(null);
  }, [planVersionId]);

  const pageDataQuery = useQuery({
    queryKey: queryKeys.forecast.pageData(projectId, planVersionId ?? undefined),
    queryFn: ({ signal }) => fetchProjectsPageData(projectId, planVersionId!, undefined, signal),
    enabled: canRead && planVersionId != null,
  });

  const pageDataHasDetail = (pageDataQuery.data?.forecast.detail.length ?? 0) > 0;

  // Ленивая догрузка «Детально»: fetch только на вкладке forecast-detail,
  // после успешного page-data и если грид в page-data ещё пустой.
  // subTab в ключе, чтобы кэш не смешивался с «Итого» / «по объектам».
  const detailQuery = useQuery({
    queryKey: queryKeys.forecast.detail(projectId, planVersionId ?? undefined, subTab),
    queryFn: ({ signal }) => fetchProjectsPageDetails(projectId, planVersionId!, undefined, signal),
    enabled:
      canRead
      && planVersionId != null
      && subTab === 'forecast-detail'
      && pageDataQuery.isSuccess
      && !pageDataHasDetail,
  });

  const pageData = useMemo((): ProjectsPageData | null => {
    const base = pageDataQuery.data;
    if (!base) return null;
    const lazyDetail = detailQuery.data?.forecastDetail;
    if (lazyDetail === undefined) return base;
    return { ...base, forecast: { ...base.forecast, detail: lazyDetail } };
  }, [detailQuery.data, pageDataQuery.data]);

  const loading = actionLoading || pageDataQuery.isFetching;
  const detailLoading = detailQuery.isFetching;
  const pageDataError =
    pageDataQuery.isError && !isRequestCanceled(pageDataQuery.error)
      ? getApiErrorMessage(pageDataQuery.error, 'Не удалось загрузить данные прогнозирования.')
      : null;
  const detailError =
    detailQuery.isError && !isRequestCanceled(detailQuery.error)
      ? getApiErrorMessage(detailQuery.error, 'Не удалось загрузить детальные данные прогнозирования.')
      : null;
  const {
    exporting,
    exportError,
    handleExportSummary,
    handleExportFacility,
    handleExportDetail,
    handleExportDetailVisibleExcel,
    handleExportDetailSummaryTable,
  } = useForecastExport({
    projectId,
    planVersionId,
    detailGridApiRef,
  });

  const error = actionError ?? exportError ?? pageDataError ?? detailError;

  const reloadPageData = useCallback(async () => {
    if (!planVersionId || !canRead) return;
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: queryKeys.forecast.pageData(projectId, planVersionId),
      }),
      queryClient.invalidateQueries({
        queryKey: queryKeys.forecast.detail(projectId, planVersionId),
      }),
    ]);
  }, [canRead, planVersionId, projectId, queryClient]);

  const handleVersionSelect = (version: ProjectVersion) => {
    setUserPlanVersionId(version.id);
    void putUserPreferences({
      [LAST_PROJECT_PREF_KEY]: String(projectId),
      [planVersionPrefKey(projectId)]: String(version.id),
    });
  };

  const handleDetailGridApiReady = useCallback((api: GridApi) => {
    detailGridApiRef.current = api;
    setDetailGridReady(true);
  }, []);

  const { contextMenuItems: detailContextMenuItems, contextDialogs: detailContextDialogs, ConfirmDialog } =
    useProjectDetailContextMenu({
      projectId,
      versionId: planVersionId,
      scope: 'forecast',
      setImportDialogType,
      setLoading,
      setError,
    });

  const summaryColumns = useMemo(() => createForecastSummaryColumns(), []);
  const facilityColumns = useMemo(() => createForecastFacilityColumns(), []);
  const summaryRows = pageData?.forecast.summary ?? [];
  const facilityRows = pageData?.forecast.facility ?? [];
  const detailRows = pageData?.forecast.detail ?? [];
  // Тяжёлый грид «Детально»: не блокируем переключение вкладок свежим rowData.
  const deferredDetailRows = useDeferredValue(detailRows);
  const normFallbackWarnings = useMemo(
    () =>
      collectForecastVolumeWarnings(
        summaryRows
          .filter((r) => !r.isTotal)
          .map((r) => ({
            workDirectionId: r.workDirectionId,
            name: r.displayName,
            factPhysicalVolume: r.factPhysicalVolume,
            normProductivity: r.normProductivity,
            baseHours: r.baseHours,
          })),
        detailRows.map((r) => ({
          isAssessment: r.isAssessment,
          classifierId: r.classifierId,
          quantity: r.quantity,
        })),
      ),
    [summaryRows, detailRows],
  );

  const facilityPinnedBottomRow = useMemo((): ProjectsPageFacilityRow[] => {
    if (facilityRows.length === 0) return [];
    const base = facilityRows.reduce((s, r) => s + (r.baseHours ?? r.laborHours ?? 0), 0);
    const opt = facilityRows.reduce((s, r) => s + (r.optimisticHours ?? 0), 0);
    const pes = facilityRows.reduce((s, r) => s + (r.pessimisticHours ?? 0), 0);
    return [{
      facilityCode: '',
      facilityName: 'ИТОГО',
      laborHours: base,
      zScore: null,
      interpretation: '',
      baseHours: base,
      optimisticHours: opt,
      pessimisticHours: pes,
    }];
  }, [facilityRows]);

  const detailColumns = useMemo(() => createForecastDetailColumns(), []);

  const summaryAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Направление',
      minWidth: 260,
      flex: 1,
      field: 'displayName' as const,
      cellRendererParams: { suppressCount: true },
    }),
    [],
  );

  const summaryGetRowClass = useCallback(
    (params: { data?: ProjectsPageForecastSummaryRow }) =>
      params.data?.isTotal ? 'forecast-summary-total-row' : undefined,
    [],
  );

  const facilityGetRowClass = useCallback(
    (params: { node?: { rowPinned?: 'top' | 'bottom' | null } }) =>
      params.node?.rowPinned === 'bottom' ? 'forecast-summary-total-row' : undefined,
    [],
  );

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр прогнозирования.</Alert>;
  }

  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      <WorkingOverlay open={exporting} />
      <PanelBusyBar open={loading || detailLoading} />
      {chromeExpanded && (
        <>
      <ForecastToolbar
        planVersionLabel={planVersionLabel}
        catalogLabel={catalogShortLabel}
        hasPlanVersion={planVersionId != null}
        canImport={canImport}
        loading={loading}
        exporting={exporting}
        detailGridReady={detailGridReady}
        subTab={subTab}
        onOpenVersionPicker={() => setVersionPickerOpen(true)}
        onOpenPlanning={() => setPlanningOpen(true)}
        onOpenReport={() => setReportOpen(true)}
        onImportPhysicalVolumes={() => setImportDialogType('physical-volumes')}
        onDeletePhysicalVolumes={() => setDeleteDialogType('physical-volumes')}
        onImportEquipment={() => setImportDialogType('equipment')}
        onDeleteEquipment={() => setDeleteDialogType('equipment')}
        onExportSummary={handleExportSummary}
        onExportFacility={handleExportFacility}
        onExportDetail={handleExportDetail}
        onExportDetailVisibleExcel={handleExportDetailVisibleExcel}
        onExportDetailSummaryTable={handleExportDetailSummaryTable}
      >
        <Stack direction="row" spacing={1} alignItems="center" sx={{ flex: '1 1 auto', minWidth: 0, flexWrap: 'wrap' }}>
          <Tabs
            value={subTab}
            onChange={(_, v: ForecastSubTab) => startTransition(() => setSubTab(v))}
            variant="scrollable"
            scrollButtons="auto"
            allowScrollButtonsMobile
            sx={{ ...projectsPageSubTabsSx, width: 'auto', flex: '0 1 auto' }}
          >
            <Tab label="Итого" value="forecast-total" />
            <Tab label="Итого по объектам" value="forecast-facility" />
            <Tab label="Детально" value="forecast-detail" />
          </Tabs>
          <PanelNotificationsButton
            messages={normFallbackWarnings}
            title="Предупреждения по нормам"
          />
        </Stack>
      </ForecastToolbar>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        </>
      )}

      <WorkspaceChromeToggleRow />

      <Box sx={{ flex: 1, minHeight: { xs: 240, md: 360 }, display: 'flex', flexDirection: 'column' }}>
        {subTab === 'forecast-total' && (
          <Box
            sx={{
              height: '100%',
              '& .forecast-summary-total-row': {
                backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
                fontWeight: 600,
              },
            }}
          >
            <DataTable<ProjectsPageForecastSummaryRow>
              rowData={summaryRows}
              columnDefs={summaryColumns}
              objectName="Planning"
              gridId="forecast-summary"
              projectId={projectId}
              loading={loading}
              height="100%"
              treeData
              getDataPath={(row) => row.hierarchy ?? [row.displayName]}
              autoGroupColumnDef={summaryAutoGroupColumnDef}
              getRowClass={summaryGetRowClass}
              getRowIdValue={summaryRowId}
            />
          </Box>
        )}

        {subTab === 'forecast-facility' && (
          <Box
            sx={{
              height: '100%',
              '& .forecast-summary-total-row': {
                backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
                fontWeight: 600,
              },
            }}
          >
            <DataTable<ProjectsPageFacilityRow>
              rowData={facilityRows}
              columnDefs={facilityColumns}
              objectName="Planning"
              gridId="forecast-facility"
              projectId={projectId}
              loading={loading}
              height="100%"
              pinnedBottomRowData={facilityPinnedBottomRow}
              getRowClass={facilityGetRowClass}
            />
          </Box>
        )}

        {subTab === 'forecast-detail' && (
          <DataTable<ProjectsPageDetailRow>
            rowData={deferredDetailRows}
            columnDefs={detailColumns}
            objectName="Planning"
            gridId="forecast_details_columns"
            projectId={projectId}
            loading={loading || detailLoading}
            height="100%"
            enableRowGrouping
            showColumnSettings
            contextMenuItems={detailContextMenuItems}
            onGridApiReady={handleDetailGridApiReady}
            getRowIdValue={(row, index) =>
              row.rowId != null ? `${row.isEquipment ? 'eq' : 'pv'}-${row.rowId}` : `detail-${index}`
            }
          />
        )}
      </Box>

      <ProjectVersionPickerDialog
        open={versionPickerOpen}
        title="Выбор версии (план)"
        projectId={projectId}
        versions={workspaceVersions}
        directionFilter={ANALYSIS_PLAN}
        selectedVersionId={planVersionId}
        onSelect={handleVersionSelect}
        onClose={() => setVersionPickerOpen(false)}
      />

      <ForecastReportDialog
        open={reportOpen}
        projectId={projectId}
        versionId={planVersionId}
        projectName={projectName}
        projectCode={projectCode}
        versionLabel={planVersionLabel}
        summaryRows={summaryRows}
        onClose={() => setReportOpen(false)}
      />

      <PlanningParametersDialog
        open={planningOpen}
        projectId={projectId}
        versionId={planVersionId}
        onClose={() => setPlanningOpen(false)}
        onSaved={() => void reloadPageData()}
      />

      {importDialogType && planVersionId && (
        <DataImportDialog
          open
          dataType={importDialogType}
          projectId={projectId}
          versionId={planVersionId}
          onClose={() => setImportDialogType(null)}
          onSuccess={() => void reloadPageData()}
        />
      )}

      {deleteDialogType && planVersionId && (
        <IterationDeleteDialog
          open
          dataType={deleteDialogType}
          projectId={projectId}
          versionId={planVersionId}
          onClose={() => setDeleteDialogType(null)}
          onSuccess={() => void reloadPageData()}
        />
      )}

      {detailContextDialogs}
      {ConfirmDialog}
    </Box>
  );
}
