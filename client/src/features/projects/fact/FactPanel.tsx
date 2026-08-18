import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { ColDef, GridApi } from 'ag-grid-community';
import {
  Alert,
  Box,
  Button,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import DeleteIcon from '@mui/icons-material/Delete';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import type { ImportDataType } from '../../../api/importApi';
import { exportFactDetailPdf, exportFactFacilityPdf, exportFactSummaryPdf } from '../../../api/visualReportsApi';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import {
  exportFactDetailExcel,
  exportFactDetailExcelVisible,
  exportFactDetailSummaryTable,
  exportFactFacilityExcel,
  fetchProjectsPageData,
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
import { getApiErrorMessage } from '../../../services/apiError';
import {
  LAST_PROJECT_PREF_KEY,
  factVersionPrefKey,
  parsePrefId,
} from '../../../utils/projectWorkspacePrefs';
import { collectGridExportData } from '../../../utils/directoryExport';
import {
  createFactDetailColumns,
  createFactFacilityColumns,
  createFactPerformanceColumns,
} from '../gridColumns';
import { panelPrimaryToolbarSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { PanelNotificationsButton } from '../shared/PanelNotificationsButton';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import type { ProjectsPageData, ProjectsPageDetailRow, ProjectsPageFacilityRow } from '../types';
import { DataImportDialog } from '../forecast/DataImportDialog';
import { IterationDeleteDialog } from '../forecast/IterationDeleteDialog';
import { PlanningParametersDialog } from '../forecast/PlanningParametersDialog';
import { shortCatalogName } from '../forecast/catalogShortName';
import { FactAnalysisDialog } from './FactAnalysisDialog';
import { useFactDetailContextMenu } from './useFactDetailContextMenu';
import { buildPerformanceTreeRows, type PerformanceTreeRow } from './performanceTree';
import type { ProjectVersion } from '../../../api/projectVersionsApi';

type FactSubTab = 'fact-total' | 'fact-facility' | 'fact-detail';
type ImportDialogType = ImportDataType | null;
type DeleteDialogType = ImportDataType | null;

export interface FactPanelProps {
  projectId: number;
  workspaceVersions: ProjectVersion[];
  workspaceFactVersionId: number | null;
  workspaceFactVersionLabel: string;
}

function performanceRowId(row: PerformanceTreeRow, index: number): string {
  return `perf-${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level}-${index}`;
}

export function FactPanel({ projectId, workspaceVersions, workspaceFactVersionId, workspaceFactVersionLabel }: FactPanelProps) {
  const { canRead } = usePermission('Assessment');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { chromeExpanded } = useWorkspaceChrome();

  const [subTab, setSubTab] = usePersistedTabState<FactSubTab>(
    `projects.${projectId}.factSubTab`,
    'fact-total',
  );
  const [pageData, setPageData] = useState<ProjectsPageData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [versionPickerOpen, setVersionPickerOpen] = useState(false);
  const [analysisOpen, setAnalysisOpen] = useState(false);
  const [exporting, setExporting] = useState(false);
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
    setPageData(null);
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

  const loadPageData = useCallback(async () => {
    if (!factVersionId || !canRead) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchProjectsPageData(projectId, factVersionId);
      setPageData(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить данные оценки факта.'));
      setPageData(null);
    } finally {
      setLoading(false);
    }
  }, [canRead, factVersionId, projectId]);

  useEffect(() => {
    void loadPageData();
  }, [loadPageData]);

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

  const handleExportFacility = async (type: 'excel' | 'pdf') => {
    if (!factVersionId) return;
    setExporting(true);
    setError(null);
    try {
      if (type === 'excel') await exportFactFacilityExcel(projectId, factVersionId);
      else await exportFactFacilityPdf(projectId, factVersionId);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт.'));
    } finally {
      setExporting(false);
    }
  };

  const handleExportDetail = async (type: 'excel' | 'pdf') => {
    if (!factVersionId) return;
    setExporting(true);
    setError(null);
    try {
      if (type === 'excel') await exportFactDetailExcel(projectId, factVersionId);
      else await exportFactDetailPdf(projectId, factVersionId);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт.'));
    } finally {
      setExporting(false);
    }
  };

  const handleDetailGridApiReady = useCallback((api: GridApi) => {
    detailGridApiRef.current = api;
    setDetailGridReady(true);
  }, []);

  const handleExportDetailVisibleExcel = async () => {
    if (!factVersionId) return;
    const api = detailGridApiRef.current;
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
    setExporting(true);
    setError(null);
    try {
      await exportFactDetailExcelVisible(
        projectId,
        factVersionId,
        'Факт_Детально_выделенные',
        headers,
        exportRows,
      );
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт выделенных данных.'));
    } finally {
      setExporting(false);
    }
  };

  const handleExportDetailSummaryTable = async () => {
    if (!factVersionId) return;
    setExporting(true);
    setError(null);
    try {
      await exportFactDetailSummaryTable(projectId, factVersionId);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выгрузить сводную таблицу.'));
    } finally {
      setExporting(false);
    }
  };

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
    useFactDetailContextMenu({
      projectId,
      factVersionId,
      onReload: loadPageData,
      setImportDialogType,
      setLoading,
      setError,
    });

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр оценки факта.</Alert>;
  }

  const factNotifications = catalogTypeMissing && factVersionId
    ? ['Выберите справочник физобъёмов (КФО или БЭР) в параметрах версии проекта перед импортом данных.']
    : [];

  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      <WorkingOverlay open={exporting} />
      <PanelBusyBar open={loading} />
      {chromeExpanded && (
        <>
      <Stack direction="row" sx={panelPrimaryToolbarSx}>
        <Button
          variant="contained"
          onClick={() => setVersionPickerOpen(true)}
          sx={{
            backgroundColor: '#2980B9',
            color: 'white',
            flexShrink: 0,
            '&:hover': { backgroundColor: '#2471A3' },
          }}
        >
          📋 Версия (факт)
        </Button>
        {factVersionLabel && (
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
              {factVersionLabel}
            </Typography>
          </Box>
        )}
        {catalogShortLabel && (
          <Box
            data-testid="fact-catalog-label"
            sx={{
              backgroundColor: '#FEF9E7',
              border: '1px solid #F9E79F',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
            }}
          >
            <Typography variant="body2" sx={{ color: '#7D6608' }} noWrap>
              {catalogShortLabel}
            </Typography>
          </Box>
        )}
        {factVersionId && (
          <Button
            variant="outlined"
            onClick={() => setPlanningOpen(true)}
            sx={{ textTransform: 'none' }}
            data-testid="fact-planning-parameters-button"
          >
            Параметры
          </Button>
        )}
        {canImport && (
          <>
            <Button
              variant="outlined"
              size="small"
              disabled={importBlocked || loading}
              title={catalogTypeMissing ? 'Укажите справочник КФО или БЭР в версии проекта' : undefined}
              onClick={() => setImportDialogType('physical-volumes')}
              startIcon={<FileDownloadIcon />}
            >
              Импорт ФО
            </Button>
            <Button
              variant="outlined"
              size="small"
              disabled={importBlocked || loading}
              onClick={() => setDeleteDialogType('physical-volumes')}
              startIcon={<DeleteIcon />}
              color="error"
            >
              Удалить ФО
            </Button>
            <Button
              variant="outlined"
              size="small"
              disabled={importBlocked || loading}
              onClick={() => setImportDialogType('equipment')}
              startIcon={<FileDownloadIcon />}
            >
              Импорт оборуд.
            </Button>
            <Button
              variant="outlined"
              size="small"
              disabled={importBlocked || loading}
              onClick={() => setDeleteDialogType('equipment')}
              startIcon={<DeleteIcon />}
              color="error"
            >
              Удалить оборуд.
            </Button>
            <Button
              variant="outlined"
              size="small"
              disabled={importBlocked || loading}
              onClick={() => setImportDialogType('labor-intensities')}
              startIcon={<FileDownloadIcon />}
            >
              Импорт труд.
            </Button>
            <Button
              variant="outlined"
              size="small"
              disabled={importBlocked || loading}
              onClick={() => setDeleteDialogType('labor-intensities')}
              startIcon={<DeleteIcon />}
              color="error"
            >
              Удалить труд.
            </Button>
          </>
        )}
      </Stack>

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ flex: '1 1 auto', minWidth: 0, flexWrap: 'wrap' }}>
          <Tabs
            value={subTab}
            onChange={(_, v: FactSubTab) => setSubTab(v)}
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
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || exporting}
                onClick={async () => {
                  if (!factVersionId) return;
                  setExporting(true);
                  try {
                    await exportFactSummaryPdf(projectId, factVersionId);
                  } finally {
                    setExporting(false);
                  }
                }}
                startIcon={<PictureAsPdfIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт PDF
              </Button>
            </>
          )}
          {subTab === 'fact-facility' && (
            <>
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || exporting}
                onClick={() => void handleExportFacility('excel')}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт Excel
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || exporting}
                onClick={() => void handleExportFacility('pdf')}
                startIcon={<PictureAsPdfIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт PDF
              </Button>
            </>
          )}
          {subTab === 'fact-detail' && (
            <>
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || exporting}
                onClick={() => void handleExportDetail('excel')}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт всех excel
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || !detailGridReady || exporting}
                onClick={() => void handleExportDetailVisibleExcel()}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт выделенных excel
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || exporting}
                onClick={() => void handleExportDetail('pdf')}
                startIcon={<PictureAsPdfIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт PDF
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || exporting}
                onClick={() => void handleExportDetailSummaryTable()}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Сводная таблица
              </Button>
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
            rowData={detailRows}
            columnDefs={detailColumns}
            objectName="Assessment"
            gridId="fact-detail"
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
        onSaved={() => void loadPageData()}
      />

      {importDialogType && factVersionId && (
        <DataImportDialog
          open
          dataType={importDialogType}
          projectId={projectId}
          versionId={factVersionId}
          onClose={() => setImportDialogType(null)}
          onSuccess={() => void loadPageData()}
        />
      )}

      {deleteDialogType && factVersionId && (
        <IterationDeleteDialog
          open
          dataType={deleteDialogType}
          projectId={projectId}
          versionId={factVersionId}
          onClose={() => setDeleteDialogType(null)}
          onSuccess={() => void loadPageData()}
        />
      )}
    </Box>
  );
}
