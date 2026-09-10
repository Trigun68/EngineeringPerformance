import { startTransition, useDeferredValue, useEffect, useMemo, useState } from 'react';
import { Alert, Box } from '@mui/material';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { emptyRows } from '../../../components/DataTable/emptyRows';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import { ProjectVersionPickerDialog } from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import {
  compareScenarioPrefKey,
  compareShowDiffPrefKey,
} from '../../../utils/projectWorkspacePrefs';
import { type CompareScenario } from '../gridColumns';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { compareTabPlannedHoursDelta } from './compareColumns';
import {
  filterCompareDetailRows,
  filterCompareFacilityRows,
  filterCompareSummaryRows,
} from './compareFilters';
import { buildCompareTreeRows } from './compareTree';
import { CompareToolbar } from './CompareToolbar';
import { CompareSummaryGrid } from './CompareSummaryGrid';
import { CompareFacilityGrid } from './CompareFacilityGrid';
import { CompareDetailGrid } from './CompareDetailGrid';
import { CompareCollapsedChrome } from './CompareCollapsedChrome';
import { useCompareExport, type CompareSubTab } from './useCompareExport';
import { useCompareVersions } from './useCompareVersions';
import { useComparePageData } from './useComparePageData';

export interface ComparePanelProps {
  projectId: number;
  workspaceVersions: ProjectVersion[];
  workspaceCompareVersionAId: number | null;
  workspaceCompareVersionBId: number | null;
}

export function ComparePanel({
  projectId,
  workspaceVersions,
  workspaceCompareVersionAId,
  workspaceCompareVersionBId,
}: ComparePanelProps) {
  const { canRead } = usePermission('VersionComparison');
  const { chromeExpanded, setChromeExpanded } = useWorkspaceChrome();

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

  const {
    versionAId,
    versionBId,
    versionALabel,
    versionBLabel,
    catalogLabelA,
    catalogLabelB,
    applyVersion,
  } = useCompareVersions({
    projectId,
    workspaceVersions,
    workspaceCompareVersionAId,
    workspaceCompareVersionBId,
  });

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
    setError(null);
  }, [projectId, setError]);

  const {
    compareData,
    isFetching,
    detailLoading,
    pageDataError,
    detailError,
  } = useComparePageData({
    projectId,
    versionAId,
    versionBId,
    subTab,
    canRead,
  });

  const loading = isFetching;
  const error = actionError ?? pageDataError ?? detailError;

  const handleVersionSelect = (version: ProjectVersion) => {
    if (pickerTarget === 'A' || pickerTarget === 'B') {
      applyVersion(pickerTarget, version);
    }
    setPickerTarget(null);
  };

  const summaryRows = useMemo(() => {
    const rows = compareData?.summary ?? emptyRows();
    const filtered = filterCompareSummaryRows(rows, showOnlyDifferences, compareScenario);
    return buildCompareTreeRows(filtered);
  }, [compareData?.summary, compareScenario, showOnlyDifferences]);

  const facilityRows = useMemo(
    () => filterCompareFacilityRows(compareData?.facility ?? emptyRows(), showOnlyDifferences, compareScenario),
    [compareData?.facility, compareScenario, showOnlyDifferences],
  );

  const detailRows = useMemo(
    () => filterCompareDetailRows(compareData?.detail ?? emptyRows(), showOnlyDifferences, compareScenario),
    [compareData?.detail, compareScenario, showOnlyDifferences],
  );
  // Тяжёлый грид «Детально»: не блокируем переключение вкладок свежим rowData.
  const deferredDetailRows = useDeferredValue(detailRows);

  const hasCompareData = Boolean(compareData && versionAId && versionBId);
  const totalHoursDelta = useMemo(
    () => compareTabPlannedHoursDelta(subTab, compareData, compareScenario),
    [compareData, compareScenario, subTab],
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
        <CompareCollapsedChrome onExpand={() => setChromeExpanded(true)} />
      )}

      <Box sx={{ flex: 1, minHeight: 360, display: 'flex', flexDirection: 'row', gap: 2 }}>
        <Box sx={{ flex: 1, minHeight: 360, height: '100%', width: '100%', minWidth: 0 }}>
          {subTab === 'compare-total' && (
            <CompareSummaryGrid
              rowData={summaryRows}
              projectId={projectId}
              loading={loading}
              showOnlyDifferences={showOnlyDifferences}
              compareScenario={compareScenario}
              onGridApiReady={handleGridApiReady}
            />
          )}
          {subTab === 'compare-facility' && (
            <CompareFacilityGrid
              rowData={facilityRows}
              projectId={projectId}
              loading={loading}
              showOnlyDifferences={showOnlyDifferences}
              compareScenario={compareScenario}
              onGridApiReady={handleGridApiReady}
            />
          )}
          {subTab === 'compare-detail' && (
            <CompareDetailGrid
              rowData={deferredDetailRows}
              keyRowCount={detailRows.length}
              projectId={projectId}
              loading={loading || detailLoading}
              showOnlyDifferences={showOnlyDifferences}
              compareScenario={compareScenario}
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
