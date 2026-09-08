import { startTransition, useCallback, useDeferredValue, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColDef, GridApi } from 'ag-grid-community';
import {
  Alert,
  Box,
  Button,
  Stack,
  Tab,
  Tabs,
} from '@mui/material';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import type { ImportDataType } from '../../../api/importApi';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import {
  fetchProjectsPageData,
  fetchProjectsPageDetails,
} from '../../../api/projectsPageApi';
import { DataTable } from '../../../components/DataTable/DataTable';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import {
  ANALYSIS_FACT,
  buildVersionLabel,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import {
  LAST_PROJECT_PREF_KEY,
  factVersionPrefKey,
  parsePrefId,
} from '../../../utils/projectWorkspacePrefs';
import {
  createFactDetailColumns,
  createFactFacilityColumns,
  createFactPerformanceColumns,
} from '../gridColumns';
import { projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { PanelNotificationsButton } from '../shared/PanelNotificationsButton';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import type { ProjectsPageData, ProjectsPageDetailRow, ProjectsPageFacilityRow } from '../types';
import { DataImportDialog } from '../forecast/DataImportDialog';
import { IterationDeleteDialog } from '../forecast/IterationDeleteDialog';
import { PlanningParametersDialog } from '../forecast/PlanningParametersDialog';
import { shortCatalogName } from '../forecast/catalogShortName';
import { FactAnalysisDialog } from './FactAnalysisDialog';
import { FactToolbar } from './FactToolbar';
import { useFactExport } from './useFactExport';
import { useProjectDetailContextMenu } from '../shared/useProjectDetailContextMenu';
import { buildPerformanceTreeRows, type PerformanceTreeRow } from './performanceTree';
import type { ProjectVersion } from '../../../api/projectVersionsApi';

type FactSubTab = 'fact-total' | 'fact-facility' | 'fact-detail';
type ImportDialogType = ImportDataType | null;
type DeleteDialogType = ImportDataType | null;

export interface FactPanelProps {
  projectId: number;
  projectName?: string;
  projectCode?: string;
  workspaceVersions: ProjectVersion[];
  workspaceFactVersionId: number | null;
  workspaceFactVersionLabel: string;
}

function performanceRowId(row: PerformanceTreeRow, index: number): string {
  return `perf-${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level}-${index}`;
}

export function FactPanel({
  projectId,
  projectName = '',
  projectCode = '',
  workspaceVersions,
  workspaceFactVersionId,
  workspaceFactVersionLabel,
}: FactPanelProps) {
  const queryClient = useQueryClient();
  const { canRead } = usePermission('Assessment');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { chromeExpanded } = useWorkspaceChrome();

  const [subTab, setSubTab] = usePersistedTabState<FactSubTab>(
    `projects.${projectId}.factSubTab`,
    'fact-total',
  );
  const [actionLoading, setLoading] = useState(false);
  const [actionError, setError] = useState<string | null>(null);
  const [versionPickerOpen, setVersionPickerOpen] = useState(false);
  const [analysisOpen, setAnalysisOpen] = useState(false);
  const [importDialogType, setImportDialogType] = useState<ImportDialogType>(null);
  const [deleteDialogType, setDeleteDialogType] = useState<DeleteDialogType>(null);
  const [planningOpen, setPlanningOpen] = useState(false);
  const detailGridApiRef = useRef<GridApi | null>(null);
  const [detailGridReady, setDetailGridReady] = useState(false);

  // Пользовательский выбор версии (переопределяет workspace по умолчанию)
  const [userFactVersionId, setUserFactVersionId] = useState<number | null>(null);
  const restoredFactPrefForProjectRef = useRef<number | null>(null);
  const factVersionId = userFactVersionId ?? workspaceFactVersionId;
  const factVersionLabel = userFactVersionId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userFactVersionId);
        return v ? buildVersionLabel(v) : '';
      })()
    : workspaceFactVersionLabel;

  useEffect(() => {
    setUserFactVersionId(null);
    setError(null);
    restoredFactPrefForProjectRef.current = null;
  }, [projectId]);

  useEffect(() => {
    if (workspaceVersions.length === 0 || restoredFactPrefForProjectRef.current === projectId) return;
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        restoredFactPrefForProjectRef.current = projectId;
        const saved = parsePrefId(prefs[factVersionPrefKey(projectId)]);
        if (saved != null && workspaceVersions.some((v) => v.id === saved)) {
          setUserFactVersionId(saved);
        }
      })
      .catch(() => {
        restoredFactPrefForProjectRef.current = projectId;
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, workspaceVersions]);

  useEffect(() => {
    setError(null);
  }, [factVersionId]);

  const pageDataQuery = useQuery({
    queryKey: queryKeys.fact.pageData(projectId, factVersionId ?? undefined),
    queryFn: ({ signal }) => fetchProjectsPageData(projectId, factVersionId!, undefined, signal),
    enabled: canRead && factVersionId != null,
  });

  const pageDataHasDetail = (pageDataQuery.data?.fact.detail.length ?? 0) > 0;

  // Ленивая догрузка «Детально»: fetch только на вкладке fact-detail,
  // после успешного page-data и если грид в page-data ещё пустой.
  // subTab в ключе, чтобы кэш не смешивался с «Итого» / «по объектам».
  const detailQuery = useQuery({
    queryKey: queryKeys.fact.detail(projectId, factVersionId ?? undefined, subTab),
    queryFn: ({ signal }) => fetchProjectsPageDetails(projectId, factVersionId!, undefined, signal),
    enabled:
      canRead
      && factVersionId != null
      && subTab === 'fact-detail'
      && pageDataQuery.isSuccess
      && !pageDataHasDetail,
  });

  const pageData = useMemo((): ProjectsPageData | null => {
    const base = pageDataQuery.data;
    if (!base) return null;
    const lazyDetail = detailQuery.data?.factDetail;
    if (lazyDetail === undefined) return base;
    return { ...base, fact: { ...base.fact, detail: lazyDetail } };
  }, [detailQuery.data, pageDataQuery.data]);

  const loading = actionLoading || pageDataQuery.isFetching;
  const detailLoading = detailQuery.isFetching;
  const pageDataError =
    pageDataQuery.isError && !isRequestCanceled(pageDataQuery.error)
      ? getApiErrorMessage(pageDataQuery.error, 'Не удалось загрузить данные оценки факта.')
      : null;
  const detailError =
    detailQuery.isError && !isRequestCanceled(detailQuery.error)
      ? getApiErrorMessage(detailQuery.error, 'Не удалось загрузить детальные данные оценки факта.')
      : null;
  const error = actionError ?? pageDataError ?? detailError;

  const reloadPageData = useCallback(async () => {
    if (!factVersionId || !canRead) return;
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: queryKeys.fact.pageData(projectId, factVersionId),
      }),
      queryClient.invalidateQueries({
        queryKey: queryKeys.fact.detail(projectId, factVersionId),
      }),
    ]);
  }, [canRead, factVersionId, projectId, queryClient]);

  const handleVersionSelect = (version: ProjectVersion) => {
    setUserFactVersionId(version.id);
    void putUserPreferences({
      [LAST_PROJECT_PREF_KEY]: String(projectId),
      [factVersionPrefKey(projectId)]: String(version.id),
    });
  };

  const selectedFactVersion = useMemo(
    () => workspaceVersions.find((v) => v.id === factVersionId) ?? null,
    [workspaceVersions, factVersionId],
  );
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);
  const catalogTypeName = useMemo(() => {
    const typeId = selectedFactVersion?.physicalVolumeTypeId;
    if (!typeId) return null;
    return physicalVolumeTypes.find((t) => t.id === typeId)?.name ?? null;
  }, [physicalVolumeTypes, selectedFactVersion?.physicalVolumeTypeId]);
  const catalogShortLabel = useMemo(() => shortCatalogName(catalogTypeName), [catalogTypeName]);
  const catalogTypeMissing = selectedFactVersion != null && !selectedFactVersion.physicalVolumeTypeId;
  const importBlocked = !factVersionId || catalogTypeMissing;

  const {
    exporting,
    handleExportSummary,
    handleExportFacility,
    handleExportDetail,
    handleExportDetailVisibleExcel,
    handleExportDetailSummaryTable,
  } = useFactExport({
    projectId,
    factVersionId,
    detailGridApiRef,
    setError,
  });

  const handleDetailGridApiReady = useCallback((api: GridApi) => {
    detailGridApiRef.current = api;
    setDetailGridReady(true);
  }, []);

  const performanceColumns = useMemo(
    () => createFactPerformanceColumns() as unknown as ColDef<PerformanceTreeRow>[],
    [],
  );
  const facilityColumns = useMemo(() => createFactFacilityColumns(), []);
  const detailColumns = useMemo(() => createFactDetailColumns(), []);

  const performanceRows = useMemo(
    () => buildPerformanceTreeRows(pageData?.fact.performance ?? []),
    [pageData?.fact.performance],
  );
  const facilityRows = pageData?.fact.facility ?? [];
  const detailRows = pageData?.fact.detail ?? [];
  // Тяжёлый грид «Детально»: не блокируем переключение вкладок свежим rowData.
  const deferredDetailRows = useDeferredValue(detailRows);

  const facilityPinnedBottomRow = useMemo((): ProjectsPageFacilityRow[] => {
    if (facilityRows.length === 0) return [];
    const total = facilityRows.reduce((s, r) => s + (r.laborHours ?? 0), 0);
    const pes = facilityRows.reduce((s, r) => s + (r.pessimisticHours ?? 0), 0);
    const base = facilityRows.reduce((s, r) => s + (r.baseHours ?? 0), 0);
    const opt = facilityRows.reduce((s, r) => s + (r.optimisticHours ?? 0), 0);
    return [{
      facilityCode: '',
      facilityName: 'ИТОГО',
      laborHours: total,
      pessimisticHours: pes,
      baseHours: base,
      optimisticHours: opt,
      zScore: null,
      interpretation: '',
    }];
  }, [facilityRows]);

  const performanceAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Наименование',
      minWidth: 260,
      flex: 1,
      field: 'workDirectionName' as const,
      cellRendererParams: { suppressCount: true },
    }),
    [],
  );

  const facilityGetRowClass = useCallback(
    (params: { node?: { rowPinned?: 'top' | 'bottom' | null } }) =>
      params.node?.rowPinned === 'bottom' ? 'forecast-summary-total-row' : undefined,
    [],
  );

  const performanceGetRowClass = useCallback(
    (params: { data?: PerformanceTreeRow }) =>
      params.data?.isTotal ? 'forecast-summary-total-row' : undefined,
    [],
  );

  const { contextMenuItems: factDetailContextMenu, contextDialogs: factDetailDialogs, ConfirmDialog: ConfirmDialogFact } =
    useProjectDetailContextMenu({
      projectId,
      versionId: factVersionId,
      scope: 'fact',
      setImportDialogType,
      setLoading,
      setError,
    });

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр оценки факта.</Alert>;
  }

  const factNotifications = catalogTypeMissing && factVersionId
    ? ['Выберите справочник физобъёмов (КФО или БЕР) в параметрах версии проекта перед импортом данных.']
    : [];

  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      <WorkingOverlay open={exporting} />
      <PanelBusyBar open={loading || detailLoading} />
      {chromeExpanded && (
        <>
      <FactToolbar
        factVersionId={factVersionId}
        factVersionLabel={factVersionLabel}
        catalogShortLabel={catalogShortLabel}
        catalogTypeMissing={catalogTypeMissing}
        importBlocked={importBlocked}
        canImport={canImport}
        loading={loading}
        onOpenVersionPicker={() => setVersionPickerOpen(true)}
        onOpenPlanning={() => setPlanningOpen(true)}
        onImport={setImportDialogType}
        onDelete={setDeleteDialogType}
      />

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ flex: '1 1 auto', minWidth: 0, flexWrap: 'wrap' }}>
          <Tabs
            value={subTab}
            onChange={(_, v: FactSubTab) => startTransition(() => setSubTab(v))}
            variant="scrollable"
            scrollButtons="auto"
            allowScrollButtonsMobile
            sx={{ ...projectsPageSubTabsSx, width: 'auto', flex: '0 1 auto' }}
          >
            <Tab label="Итого" value="fact-total" />
            <Tab label="Итого по объектам" value="fact-facility" />
            <Tab label="Детально" value="fact-detail" />
          </Tabs>
          <PanelNotificationsButton
            messages={factNotifications}
            title="Предупреждения"
          />
        </Stack>

        <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
          {subTab === 'fact-total' && (
            <>
              <Button
                variant="contained"
                disabled={!factVersionId}
                onClick={() => setAnalysisOpen(true)}
                sx={{ backgroundColor: '#3498DB', color: 'white', height: 36, textTransform: 'none' }}
              >
                📊 Анализ версии
              </Button>
              <ExportActionButton
                kind="excel"
                disabled={!factVersionId}
                loading={exporting}
                onClick={() => void handleExportSummary('excel')}
              />
              <ExportActionButton
                kind="pdf"
                disabled={!factVersionId}
                loading={exporting}
                onClick={() => void handleExportSummary('pdf')}
              />
            </>
          )}
          {subTab === 'fact-facility' && (
            <>
              <ExportActionButton
                kind="excel"
                disabled={!factVersionId}
                loading={exporting}
                onClick={() => void handleExportFacility('excel')}
              />
              <ExportActionButton
                kind="pdf"
                disabled={!factVersionId}
                loading={exporting}
                onClick={() => void handleExportFacility('pdf')}
              />
            </>
          )}
          {subTab === 'fact-detail' && (
            <>
              <ExportActionButton
                kind="excel"
                disabled={!factVersionId}
                loading={exporting}
                onClick={() => void handleExportDetail()}
              />
              <ExportActionButton
                kind="excelSelected"
                disabled={!factVersionId || !detailGridReady}
                loading={exporting}
                onClick={() => void handleExportDetailVisibleExcel()}
              />
              <ExportActionButton
                kind="excelSummary"
                disabled={!factVersionId}
                loading={exporting}
                onClick={() => void handleExportDetailSummaryTable()}
              />
            </>
          )}
        </Stack>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        </>
      )}

      <WorkspaceChromeToggleRow />

      <Box sx={{ flex: 1, minHeight: 360 }}>
        {subTab === 'fact-total' && (
          <Box
            sx={{
              height: '100%',
              '& .forecast-summary-total-row': {
                backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
                fontWeight: 600,
              },
            }}
          >
            <DataTable<PerformanceTreeRow>
              rowData={performanceRows}
              columnDefs={performanceColumns}
              objectName="Assessment"
              gridId="fact-performance"
              projectId={projectId}
              loading={loading}
              height="100%"
              treeData
              getDataPath={(row) => row.hierarchy}
              autoGroupColumnDef={performanceAutoGroupColumnDef}
              getRowIdValue={performanceRowId}
              getRowClass={performanceGetRowClass}
              showColumnSettings
            />
          </Box>
        )}

        {subTab === 'fact-facility' && (
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
              objectName="Assessment"
              gridId="fact-facility"
              projectId={projectId}
              loading={loading}
              height="100%"
              pinnedBottomRowData={facilityPinnedBottomRow}
              getRowClass={facilityGetRowClass}
              showColumnSettings
            />
          </Box>
        )}

        {subTab === 'fact-detail' && (
          <DataTable<ProjectsPageDetailRow>
            rowData={deferredDetailRows}
            columnDefs={detailColumns}
            objectName="Assessment"
            gridId="fact-detail"
            projectId={projectId}
            loading={loading}
            height="100%"
            enableRowGrouping
            showColumnSettings
            contextMenuItems={factDetailContextMenu}
            onGridApiReady={handleDetailGridApiReady}
            getRowIdValue={(row, index) =>
              row.rowId != null ? `${row.isEquipment ? 'eq' : 'pv'}-${row.rowId}` : `fact-detail-${index}`
            }
          />
        )}
      </Box>

      <ProjectVersionPickerDialog
        open={versionPickerOpen}
        title="Выбор версии (факт)"
        projectId={projectId}
        versions={workspaceVersions}
        directionFilter={ANALYSIS_FACT}
        selectedVersionId={factVersionId}
        onSelect={handleVersionSelect}
        onClose={() => setVersionPickerOpen(false)}
      />

      <FactAnalysisDialog
        open={analysisOpen}
        projectId={projectId}
        versionId={factVersionId}
        projectName={projectName}
        projectCode={projectCode}
        versionLabel={factVersionLabel}
        onClose={() => setAnalysisOpen(false)}
      />

      {factDetailDialogs}
      {ConfirmDialogFact}

      <PlanningParametersDialog
        open={planningOpen}
        projectId={projectId}
        versionId={factVersionId}
        showFactColumn
        onClose={() => setPlanningOpen(false)}
        onSaved={() => void reloadPageData()}
      />

      {importDialogType && factVersionId && (
        <DataImportDialog
          open
          dataType={importDialogType}
          projectId={projectId}
          versionId={factVersionId}
          onClose={() => setImportDialogType(null)}
          onSuccess={() => void reloadPageData()}
        />
      )}

      {deleteDialogType && factVersionId && (
        <IterationDeleteDialog
          open
          dataType={deleteDialogType}
          projectId={projectId}
          versionId={factVersionId}
          onClose={() => setDeleteDialogType(null)}
          onSuccess={() => void reloadPageData()}
        />
      )}
    </Box>
  );
}
