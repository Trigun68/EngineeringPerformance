import { startTransition, useCallback, useDeferredValue, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Box, Stack, Tab, Tabs } from '@mui/material';
import type { GridApi } from 'ag-grid-community';
import { emptyRows } from '../../../components/DataTable/emptyRows';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { ForecastToolbar, type ForecastToolbarSubTab } from './ForecastToolbar';
import { ForecastSummaryGrid } from './ForecastSummaryGrid';
import { ForecastFacilityGrid } from './ForecastFacilityGrid';
import { ForecastDetailGrid } from './ForecastDetailGrid';
import { useForecastExport } from './useForecastExport';
import { useForecastPlanVersion } from './useForecastPlanVersion';
import { useForecastPanelDialogs } from './useForecastPanelDialogs';
import { useForecastPageData } from './useForecastPageData';
import { projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { PanelNotificationsButton } from '../shared/PanelNotificationsButton';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import { collectForecastVolumeWarnings } from './normFallbackWarning';

type ForecastSubTab = ForecastToolbarSubTab;

export interface ForecastPanelProps {
  projectId: number;
  projectName?: string;
  projectCode?: string;
  workspaceVersions: ProjectVersion[];
  workspacePlanVersionId: number | null;
  workspacePlanVersionLabel: string;
}

export function ForecastPanel({
  projectId,
  projectName = '',
  projectCode = '',
  workspaceVersions,
  workspacePlanVersionId,
  workspacePlanVersionLabel,
}: ForecastPanelProps) {
  const { canRead } = usePermission('Projects');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { chromeExpanded } = useWorkspaceChrome();

  const { planVersionId, planVersionLabel, catalogShortLabel, handleVersionSelect } = useForecastPlanVersion({
    projectId,
    workspaceVersions,
    workspacePlanVersionId,
    workspacePlanVersionLabel,
  });

  const [subTab, setSubTab] = usePersistedTabState<ForecastSubTab>(
    `projects.${projectId}.forecastSubTab`,
    'forecast-total',
  );
  const [actionLoading, setLoading] = useState(false);
  const [actionError, setError] = useState<string | null>(null);
  const detailGridApiRef = useRef<GridApi | null>(null);
  const [detailGridReady, setDetailGridReady] = useState(false);

  useEffect(() => {
    setError(null);
  }, [projectId, planVersionId]);

  const {
    pageData,
    isFetching,
    detailLoading,
    pageDataError,
    detailError,
    reloadPageData,
  } = useForecastPageData({
    projectId,
    planVersionId,
    subTab,
    canRead,
  });

  const loading = actionLoading || isFetching;
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

  const handleDetailGridApiReady = useCallback((api: GridApi) => {
    detailGridApiRef.current = api;
    setDetailGridReady(true);
  }, []);

  const summaryRows = pageData?.forecast.summary ?? emptyRows();
  const facilityRows = pageData?.forecast.facility ?? emptyRows();
  const detailRows = pageData?.forecast.detail ?? emptyRows();
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
            // Own физобъём; exclusive remap factPhysicalVolume в предупреждения не подставляем.
            factPhysicalVolume: r.physicalVolume,
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

  const {
    contextMenuItems: detailContextMenuItems,
    dialogs,
    onOpenVersionPicker,
    onOpenPlanning,
    onOpenReport,
    onImportPhysicalVolumes,
    onDeletePhysicalVolumes,
    onImportEquipment,
    onDeleteEquipment,
  } = useForecastPanelDialogs({
    projectId,
    projectName,
    projectCode,
    workspaceVersions,
    planVersionId,
    planVersionLabel,
    summaryRows,
    onVersionSelect: handleVersionSelect,
    onReload: () => { void reloadPageData(); },
    setLoading,
    setError,
  });

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
        onOpenVersionPicker={onOpenVersionPicker}
        onOpenPlanning={onOpenPlanning}
        onOpenReport={onOpenReport}
        onImportPhysicalVolumes={onImportPhysicalVolumes}
        onDeletePhysicalVolumes={onDeletePhysicalVolumes}
        onImportEquipment={onImportEquipment}
        onDeleteEquipment={onDeleteEquipment}
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
          <ForecastSummaryGrid rowData={summaryRows} projectId={projectId} loading={loading} />
        )}
        {subTab === 'forecast-facility' && (
          <ForecastFacilityGrid rowData={facilityRows} projectId={projectId} loading={loading} />
        )}
        {subTab === 'forecast-detail' && (
          <ForecastDetailGrid
            rowData={deferredDetailRows}
            projectId={projectId}
            loading={loading || detailLoading}
            contextMenuItems={detailContextMenuItems}
            onGridApiReady={handleDetailGridApiReady}
          />
        )}
      </Box>

      {dialogs}
    </Box>
  );
}
