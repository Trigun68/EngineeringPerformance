import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ColDef } from 'ag-grid-community';
import {
  Alert,
  Box,
  Button,
  Grid,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import DeleteIcon from '@mui/icons-material/Delete';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import type { ImportDataType } from '../../../api/importApi';
import { exportAnalysisExcel, exportAnalysisPdf } from '../../../api/analyticsApi';
import { exportFactDetailPdf, exportFactFacilityPdf, exportFactSummaryPdf } from '../../../api/visualReportsApi';
import { fetchProjectVersions, type ProjectVersion } from '../../../api/projectVersionsApi';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { fetchProjectsPageData } from '../../../api/projectsPageApi';
import { DataTable } from '../../../components/DataTable/DataTable';
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
  createFactDetailColumns,
  createFactFacilityColumns,
  createFactPerformanceColumns,
} from '../gridColumns';
import { importToolbarStackSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import type { ProjectsPageData, ProjectsPageDetailRow, ProjectsPageFacilityRow } from '../types';
import { DataImportDialog } from '../forecast/DataImportDialog';
import { IterationDeleteDialog } from '../forecast/IterationDeleteDialog';
import { FactAnalysisDialog } from './FactAnalysisDialog';
import { useFactDetailContextMenu } from './useFactDetailContextMenu';
import { buildPerformanceTreeRows, type PerformanceTreeRow } from './performanceTree';

type FactSubTab = 'fact-total' | 'fact-facility' | 'fact-detail';
type ImportDialogType = ImportDataType | null;
type DeleteDialogType = ImportDataType | null;

export interface FactPanelProps {
  projectId: number;
}

function performanceRowId(row: PerformanceTreeRow, index: number): string {
  return `perf-${row.workDirectionId}-${index}`;
}

export function FactPanel({ projectId }: FactPanelProps) {
  const { canRead } = usePermission('Assessment');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');

  const [versions, setVersions] = useState<ProjectVersion[]>([]);
  const [factVersionId, setFactVersionId] = useState<number | null>(null);
  const [factVersionLabel, setFactVersionLabel] = useState('');
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

  useEffect(() => {
    setFactVersionId(null);
    setFactVersionLabel('');
    setPageData(null);
    setError(null);
  }, [projectId]);

  const factVersionPrefKey = `projects.${projectId}.factVersionId`;

  const loadVersions = useCallback(async () => {
    const [list, prefs] = await Promise.all([
      fetchProjectVersions(projectId),
      getUserPreferences().catch(() => ({} as Record<string, string>)),
    ]);
    setVersions(list);
    const savedId = Number.parseInt(prefs[factVersionPrefKey] ?? '', 10);
    const saved = Number.isFinite(savedId) ? list.find((v) => v.id === savedId) : undefined;
    const fact = saved
      ?? list.find((v) => v.direction === ANALYSIS_FACT)
      ?? null;
    if (fact) {
      setFactVersionId(fact.id);
      setFactVersionLabel(buildVersionLabel(fact));
    } else {
      setFactVersionId(null);
      setFactVersionLabel('');
    }
  }, [factVersionPrefKey, projectId]);

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
    void loadVersions().catch(() => setError('Не удалось загрузить версии.'));
  }, [loadVersions]);

  useEffect(() => {
    void loadPageData();
  }, [loadPageData]);

  const handleVersionSelect = (version: ProjectVersion) => {
    setFactVersionId(version.id);
    setFactVersionLabel(buildVersionLabel(version));
    void putUserPreferences({ [factVersionPrefKey]: String(version.id) });
  };

  const selectedFactVersion = useMemo(
    () => versions.find((v) => v.id === factVersionId) ?? null,
    [factVersionId, versions],
  );
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);
  const catalogTypeName = useMemo(() => {
    const typeId = selectedFactVersion?.physicalVolumeTypeId;
    if (!typeId) return null;
    return physicalVolumeTypes.find((t) => t.id === typeId)?.name ?? null;
  }, [physicalVolumeTypes, selectedFactVersion?.physicalVolumeTypeId]);
  const catalogTypeMissing = selectedFactVersion != null && !selectedFactVersion.physicalVolumeTypeId;
  const importBlocked = !factVersionId || catalogTypeMissing;

  const handleExportAnalysis = async (type: 'excel' | 'pdf') => {
    if (!factVersionId) return;
    setExporting(true);
    setError(null);
    try {
      if (type === 'excel') {
        await exportAnalysisExcel(projectId, factVersionId);
      } else if (subTab === 'fact-facility') {
        await exportFactFacilityPdf(projectId, factVersionId);
      } else {
        await exportAnalysisPdf(projectId, factVersionId);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт.'));
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
    return [{
      facilityCode: '',
      facilityName: 'ИТОГО',
      laborHours: total,
      zScore: null,
      interpretation: '',
    }];
  }, [facilityRows]);

  const performanceAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Наименование',
      minWidth: 260,
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

  const { contextMenuItems: factDetailContextMenu, contextDialogs: factDetailDialogs } =
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

  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      <Grid container spacing={2} alignItems="center" sx={{ mb: 2 }}>
        <Grid item xs={12} lg={5} xl={4}>
          <Stack direction="row" spacing={2} alignItems="center">
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
                }}
              >
                <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>
                  {factVersionLabel}
                </Typography>
              </Box>
            )}
            {catalogTypeName && (
              <Box
                sx={{
                  backgroundColor: '#FEF9E7',
                  border: '1px solid #F9E79F',
                  px: 1.5,
                  py: 0.5,
                  borderRadius: 1,
                }}
              >
                <Typography variant="body2" sx={{ color: '#7D6608' }}>
                  Справочник ФО: {catalogTypeName}
                </Typography>
              </Box>
            )}
          </Stack>
        </Grid>

        <Grid item xs={12} lg={7} xl={8}>
          {canImport && (
            <Stack direction="row" spacing={1} sx={importToolbarStackSx}>
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
            </Stack>
          )}
        </Grid>
      </Grid>

      {catalogTypeMissing && factVersionId && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Выберите справочник физобъёмов (КФО или БЭР) в параметрах версии проекта перед импортом данных.
        </Alert>
      )}

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        <Tabs
          value={subTab}
          onChange={(_, v: FactSubTab) => setSubTab(v)}
          sx={projectsPageSubTabsSx}
        >
          <Tab label="Итого" value="fact-total" />
          <Tab label="По объектам" value="fact-facility" />
          <Tab label="Детально" value="fact-detail" />
        </Tabs>

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
                onClick={() => void handleExportAnalysis('excel')}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт Excel
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!factVersionId || exporting}
                onClick={() => void handleExportAnalysis('pdf')}
                startIcon={<PictureAsPdfIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт PDF
              </Button>
            </>
          )}
          {subTab === 'fact-detail' && (
            <Button
              variant="outlined"
              size="small"
              disabled={!factVersionId || exporting}
              onClick={async () => {
                if (!factVersionId) return;
                setExporting(true);
                try {
                  await exportFactDetailPdf(projectId, factVersionId);
                } finally {
                  setExporting(false);
                }
              }}
              startIcon={<PictureAsPdfIcon />}
              sx={{ textTransform: 'none' }}
            >
              Экспорт PDF
            </Button>
          )}
        </Stack>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Box sx={{ flex: 1, minHeight: 360 }}>
        {subTab === 'fact-total' && (
          <DataTable<PerformanceTreeRow>
            rowData={performanceRows}
            columnDefs={performanceColumns}
            objectName="FactPerformance"
            gridId={`fact-performance-${projectId}`}
            loading={loading}
            height="100%"
            treeData
            getDataPath={(row) => row.hierarchy}
            autoGroupColumnDef={performanceAutoGroupColumnDef}
            getRowIdValue={performanceRowId}
            showColumnSettings
          />
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
              objectName="FactFacility"
              gridId={`fact-facility-${projectId}`}
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
            objectName="FactDetail"
            gridId={`fact-detail-${projectId}`}
            loading={loading}
            height="100%"
            enableRowGrouping
            showColumnSettings
            contextMenuItems={factDetailContextMenu}
            getRowIdValue={(row, index) =>
              row.rowId != null ? `${row.isEquipment ? 'eq' : 'pv'}-${row.rowId}` : `fact-detail-${index}`
            }
          />
        )}
      </Box>

      <ProjectVersionPickerDialog
        open={versionPickerOpen}
        title="Версии факта"
        projectId={projectId}
        versions={versions}
        directionFilter={ANALYSIS_FACT}
        selectedVersionId={factVersionId}
        onSelect={handleVersionSelect}
        onClose={() => setVersionPickerOpen(false)}
        onVersionsChanged={() => void loadVersions()}
      />

      <FactAnalysisDialog
        open={analysisOpen}
        projectId={projectId}
        versionId={factVersionId}
        onClose={() => setAnalysisOpen(false)}
      />

      {factDetailDialogs}

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
