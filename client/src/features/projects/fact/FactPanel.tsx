import { startTransition, useCallback, useDeferredValue, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Box, Stack, Tab, Tabs } from '@mui/material';
import type { GridApi } from 'ag-grid-community';
import { emptyRows } from '../../../components/DataTable/emptyRows';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { PanelNotificationsButton } from '../shared/PanelNotificationsButton';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import { FactToolbar, type FactToolbarSubTab } from './FactToolbar';
import { FactPerformanceGrid } from './FactPerformanceGrid';
import { FactFacilityGrid } from './FactFacilityGrid';
import { FactDetailGrid } from './FactDetailGrid';
import { useFactExport } from './useFactExport';
import { useFactPanelDialogs } from './useFactPanelDialogs';
import { useFactVersion } from './useFactVersion';
import { useFactPageData } from './useFactPageData';
import { buildPerformanceTreeRows } from './performanceTree';
import type { ProjectVersion } from '../../../api/projectVersionsApi';

type FactSubTab = FactToolbarSubTab;

export interface FactPanelProps {
  projectId: number;
  projectName?: string;
  projectCode?: string;
  workspaceVersions: ProjectVersion[];
  workspaceFactVersionId: number | null;
  workspaceFactVersionLabel: string;
}

export function FactPanel({
  projectId,
  projectName = '',
  projectCode = '',
  workspaceVersions,
  workspaceFactVersionId,
  workspaceFactVersionLabel,
}: FactPanelProps) {
  const { canRead } = usePermission('Assessment');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { chromeExpanded } = useWorkspaceChrome();

  const [subTab, setSubTab] = usePersistedTabState<FactSubTab>(
    `projects.${projectId}.factSubTab`,
    'fact-total',
  );
  const [actionLoading, setLoading] = useState(false);
  const [actionError, setError] = useState<string | null>(null);
  const detailGridApiRef = useRef<GridApi | null>(null);
  const [detailGridReady, setDetailGridReady] = useState(false);

  const {
    factVersionId,
    factVersionLabel,
    catalogShortLabel,
    catalogTypeMissing,
    importBlocked,
    handleVersionSelect,
  } = useFactVersion({
    projectId,
    workspaceVersions,
    workspaceFactVersionId,
    workspaceFactVersionLabel,
  });

  useEffect(() => {
    setError(null);
  }, [projectId, factVersionId]);

  const {
    pageData,
    isFetching,
    detailLoading,
    pageDataError,
    detailError,
    reloadPageData,
  } = useFactPageData({
    projectId,
    factVersionId,
    subTab,
    canRead,
  });

  const loading = actionLoading || isFetching;
  const error = actionError ?? pageDataError ?? detailError;

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

  const performanceRows = useMemo(
    () => buildPerformanceTreeRows(pageData?.fact.performance ?? emptyRows()),
    [pageData?.fact.performance],
  );
  const facilityRows = pageData?.fact.facility ?? emptyRows();
  const detailRows = pageData?.fact.detail ?? emptyRows();
  // Тяжёлый грид «Детально»: не блокируем переключение вкладок свежим rowData.
  const deferredDetailRows = useDeferredValue(detailRows);

  const {
    contextMenuItems: factDetailContextMenu,
    dialogs,
    onOpenVersionPicker,
    onOpenPlanning,
    onOpenAnalysis,
    onImport,
    onDelete,
  } = useFactPanelDialogs({
    projectId,
    projectName,
    projectCode,
    workspaceVersions,
    factVersionId,
    factVersionLabel,
    onVersionSelect: handleVersionSelect,
    onReload: () => { void reloadPageData(); },
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
        exporting={exporting}
        detailGridReady={detailGridReady}
        subTab={subTab}
        onOpenVersionPicker={onOpenVersionPicker}
        onOpenPlanning={onOpenPlanning}
        onOpenAnalysis={onOpenAnalysis}
        onImport={onImport}
        onDelete={onDelete}
        onExportSummary={handleExportSummary}
        onExportFacility={handleExportFacility}
        onExportDetail={handleExportDetail}
        onExportDetailVisibleExcel={handleExportDetailVisibleExcel}
        onExportDetailSummaryTable={handleExportDetailSummaryTable}
      >
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
      </FactToolbar>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        </>
      )}

      <WorkspaceChromeToggleRow />

      <Box sx={{ flex: 1, minHeight: 360 }}>
        {subTab === 'fact-total' && (
          <FactPerformanceGrid rowData={performanceRows} projectId={projectId} loading={loading} />
        )}
        {subTab === 'fact-facility' && (
          <FactFacilityGrid rowData={facilityRows} projectId={projectId} loading={loading} />
        )}
        {subTab === 'fact-detail' && (
          <FactDetailGrid
            rowData={deferredDetailRows}
            projectId={projectId}
            loading={loading}
            contextMenuItems={factDetailContextMenu}
            onGridApiReady={handleDetailGridApiReady}
          />
        )}
      </Box>

      {dialogs}
    </Box>
  );
}
