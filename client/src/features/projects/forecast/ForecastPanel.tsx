import { useCallback, useEffect, useMemo, useState } from 'react';
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
import { deleteEquipment, deletePhysicalVolume, createEquipment, createPhysicalVolume, fetchEquipmentById, fetchPhysicalVolumeById } from '../../../api/projectDataApi';
import { fetchProjectVersions, type ProjectVersion } from '../../../api/projectVersionsApi';
import {
  exportForecastDetailExcel,
  exportForecastDetailPdf,
  exportForecastDetailSummaryTable,
  exportForecastFacilityExcel,
  exportForecastFacilityPdf,
  fetchProjectsPageData,
} from '../../../api/projectsPageApi';
import { exportForecastSummaryPdf } from '../../../api/visualReportsApi';
import { DataTable, type ContextMenuItem } from '../../../components/DataTable/DataTable';
import {
  ANALYSIS_PLAN,
  buildVersionLabel,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { getApiErrorMessage } from '../../../services/apiError';
import {
  createForecastDetailColumns,
  createForecastFacilityColumns,
  createForecastSummaryColumns,
} from '../gridColumns';
import type { ProjectsPageDetailRow, ProjectsPageData, ProjectsPageFacilityRow, ProjectsPageForecastSummaryRow } from '../types';
import { DataImportDialog } from './DataImportDialog';
import { ForecastReportDialog } from './ForecastReportDialog';
import { IterationDeleteDialog } from './IterationDeleteDialog';
import { PlanningParametersDialog } from './PlanningParametersDialog';
import { importToolbarStackSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { ProjectEquipmentQuickEditDialog } from './ProjectEquipmentQuickEditDialog';
import { ProjectPhysicalVolumeQuickEditDialog } from './ProjectPhysicalVolumeQuickEditDialog';
import { ForecastDetailCatalogDialog } from './ForecastDetailCatalogDialog';
import { ForecastDetailNormDialog } from './ForecastDetailNormDialog';
import { collectNormFallbackWarnings } from './normFallbackWarning';

type ForecastSubTab = 'forecast-total' | 'forecast-facility' | 'forecast-detail';
type ImportDialogType = Extract<ImportDataType, 'physical-volumes' | 'equipment'> | null;
type DeleteDialogType = Extract<ImportDataType, 'physical-volumes' | 'equipment'> | null;

interface QuickEditState {
  rowId: number;
  nomenclature: string;
  quantity: number;
}

export interface ForecastPanelProps {
  projectId: number;
  projectName?: string;
  projectCode?: string;
}

function summaryRowId(row: ProjectsPageForecastSummaryRow, index: number): string {
  return `${row.workDirectionId}-${row.isTotal ? 'total' : 'row'}-${row.level}-${index}`;
}

function detailNomenclature(row: ProjectsPageDetailRow): string {
  return (
    (row.isEquipment
      ? row.itemName || row.catalogName || row.pvName
      : row.pvName || row.catalogName || row.workName) ?? ''
  );
}

export function ForecastPanel({ projectId, projectName = '', projectCode = '' }: ForecastPanelProps) {
  const { canRead } = usePermission('Projects');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { canEdit: canEditPpv } = usePermission('ProjectPhysicalVolumes');
  const { canEdit: canEditPeq } = usePermission('ProjectEquipments');
  const { canEdit: canEditPvCatalog } = usePermission('PhysicalVolumes');
  const { canEdit: canEditEquipmentCatalog } = usePermission('Equipment');
  const { canEdit: canEditNorms } = usePermission('IicWorkDirectionNorms');

  const [versions, setVersions] = useState<ProjectVersion[]>([]);
  const [planVersionId, setPlanVersionId] = useState<number | null>(null);
  const [planVersionLabel, setPlanVersionLabel] = useState('');
  const [subTab, setSubTab] = usePersistedTabState<ForecastSubTab>(
    `projects.${projectId}.forecastSubTab`,
    'forecast-total',
  );
  const [pageData, setPageData] = useState<ProjectsPageData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [versionPickerOpen, setVersionPickerOpen] = useState(false);
  const [reportOpen, setReportOpen] = useState(false);
  const [planningOpen, setPlanningOpen] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [importDialogType, setImportDialogType] = useState<ImportDialogType>(null);
  const [deleteDialogType, setDeleteDialogType] = useState<DeleteDialogType>(null);
  const [ppvEdit, setPpvEdit] = useState<QuickEditState | null>(null);
  const [peqEdit, setPeqEdit] = useState<QuickEditState | null>(null);
  const [catalogEdit, setCatalogEdit] = useState<{
    resource: string;
    entryId: number;
    title: string;
  } | null>(null);
  const [normEdit, setNormEdit] = useState<{
    normId: number;
    workDirectionId: number;
    groupLabel: string;
    unitLabel: string;
  } | null>(null);

  useEffect(() => {
    setPlanVersionId(null);
    setPlanVersionLabel('');
    setPageData(null);
    setError(null);
  }, [projectId]);

  const loadVersions = useCallback(async () => {
    const list = await fetchProjectVersions(projectId);
    setVersions(list);
    let chosen: ProjectVersion | null = null;
    setPlanVersionId((prev) => {
      if (prev != null) {
        const existing = list.find((v) => v.id === prev);
        if (existing) {
          chosen = existing;
          return prev;
        }
      }
      chosen = list.find((v) => v.direction === ANALYSIS_PLAN) ?? list[0] ?? null;
      return chosen?.id ?? null;
    });
    setPlanVersionLabel(chosen ? buildVersionLabel(chosen) : '');
  }, [projectId]);

  const loadPageData = useCallback(async () => {
    if (!planVersionId || !canRead) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchProjectsPageData(projectId, planVersionId);
      setPageData(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить данные прогнозирования.'));
      setPageData(null);
    } finally {
      setLoading(false);
    }
  }, [canRead, planVersionId, projectId]);

  useEffect(() => {
    void loadVersions().catch(() => setError('Не удалось загрузить версии.'));
  }, [loadVersions]);

  useEffect(() => {
    void loadPageData();
  }, [loadPageData]);

  const handleVersionSelect = (version: ProjectVersion) => {
    setPlanVersionId(version.id);
    setPlanVersionLabel(buildVersionLabel(version));
  };

  const handleExportFacility = async (type: 'excel' | 'pdf') => {
    if (!planVersionId) return;
    setExporting(true);
    setError(null);
    try {
      if (type === 'excel') {
        await exportForecastFacilityExcel(projectId, planVersionId);
      } else {
        await exportForecastFacilityPdf(projectId, planVersionId);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт.'));
    } finally {
      setExporting(false);
    }
  };

  const handleExportDetail = async (type: 'excel' | 'pdf') => {
    if (!planVersionId) return;
    setExporting(true);
    setError(null);
    try {
      if (type === 'excel') {
        await exportForecastDetailExcel(projectId, planVersionId);
      } else {
        await exportForecastDetailPdf(projectId, planVersionId);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт.'));
    } finally {
      setExporting(false);
    }
  };

  const handleExportDetailSummaryTable = async () => {
    if (!planVersionId) return;
    setExporting(true);
    setError(null);
    try {
      await exportForecastDetailSummaryTable(projectId, planVersionId);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт сводной таблицы.'));
    } finally {
      setExporting(false);
    }
  };

  const handleDeleteDetailRow = useCallback(async (row: ProjectsPageDetailRow) => {
    if (!planVersionId || !row.rowId) return;
    const label = detailNomenclature(row);
    if (!window.confirm(`Удалить строку «${label}»?`)) return;

    setLoading(true);
    setError(null);
    try {
      if (row.isEquipment) {
        if (!canEditPeq) return;
        await deleteEquipment(projectId, planVersionId, row.rowId);
      } else {
        if (!canEditPpv) return;
        await deletePhysicalVolume(projectId, planVersionId, row.rowId);
      }
      await loadPageData();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось удалить строку.'));
    } finally {
      setLoading(false);
    }
  }, [canEditPeq, canEditPpv, loadPageData, planVersionId, projectId]);

  const handleCopyDetailRow = useCallback(async (row: ProjectsPageDetailRow) => {
    if (!row.rowId || !planVersionId) return;
    setLoading(true);
    setError(null);
    try {
      if (row.isEquipment) {
        if (!canEditPeq) return;
        const src = await fetchEquipmentById(projectId, planVersionId, row.rowId);
        if (!src) throw new Error('Строка оборудования не найдена.');
        await createEquipment(projectId, planVersionId, {
          equipmentId: src.equipmentId,
          quantity: src.quantity,
          quantityInWork: src.quantityInWork,
        });
      } else {
        if (!canEditPpv) return;
        const src = await fetchPhysicalVolumeById(projectId, planVersionId, row.rowId);
        if (!src) throw new Error('Строка физ. объёма не найдена.');
        await createPhysicalVolume(projectId, planVersionId, {
          physicalVolumeId: src.physicalVolumeId,
          quantity: src.quantity,
          facilityCode: src.facilityCode,
          facilityName: src.facilityName,
          workName: src.workName,
          laborHoursPerUnit: src.laborHoursPerUnit,
        });
      }
      await loadPageData();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось скопировать строку.'));
    } finally {
      setLoading(false);
    }
  }, [canEditPeq, canEditPpv, loadPageData, planVersionId, projectId]);

  const detailContextMenuItems = useMemo((): ContextMenuItem<ProjectsPageDetailRow>[] => {
    const items: ContextMenuItem<ProjectsPageDetailRow>[] = [];

    if (canEditPpv || canEditPeq) {
      items.push({
        name: 'Редактировать физ. объём',
        disabled: (row) => !row?.rowId || row.isEquipment,
        action: (row) => {
          if (!row?.rowId || row.isEquipment) return;
          if (!canEditPpv) return;
          setPpvEdit({
            rowId: row.rowId,
            nomenclature: detailNomenclature(row),
            quantity: row.quantity,
          });
        },
      });
      items.push({
        name: 'Редактировать оборудование',
        disabled: (row) => !row?.rowId || !row.isEquipment,
        action: (row) => {
          if (!row?.rowId || !row.isEquipment) return;
          if (!canEditPeq) return;
          setPeqEdit({
            rowId: row.rowId,
            nomenclature: detailNomenclature(row),
            quantity: row.quantity,
          });
        },
      });
      items.push({
        name: 'Редактировать номенклатуру физ. объёма',
        disabled: (row) => !canEditPvCatalog || !row?.catalogVolumeId || !row.catalogResource || row.isEquipment,
        action: (row) => {
          if (!canEditPvCatalog || !row?.catalogVolumeId || !row.catalogResource || row.isEquipment) return;
          setCatalogEdit({
            resource: row.catalogResource,
            entryId: row.catalogVolumeId,
            title: 'Редактировать номенклатуру физ. объёма',
          });
        },
      });
      items.push({
        name: 'Редактировать номенклатуру оборудования',
        disabled: (row) => !canEditEquipmentCatalog || !row?.catalogEquipmentId || !row.isEquipment,
        action: (row) => {
          if (!canEditEquipmentCatalog || !row?.catalogEquipmentId || !row.isEquipment) return;
          setCatalogEdit({
            resource: 'equipment',
            entryId: row.catalogEquipmentId,
            title: 'Редактировать номенклатуру оборудования',
          });
        },
      });
      items.push({
        name: 'Редактировать норму EAC',
        disabled: (row) => !canEditNorms || !row?.normId || !row.classifierId,
        action: (row) => {
          if (!canEditNorms || !row?.normId || !row.classifierId) return;
          setNormEdit({
            normId: row.normId,
            workDirectionId: row.classifierId,
            groupLabel: row.iicGroupName,
            unitLabel: row.iicUnitName,
          });
        },
      });
      items.push({
        name: 'Удалить выбранную строку',
        disabled: (row) => !row?.rowId,
        action: (row) => {
          if (row) void handleDeleteDetailRow(row);
        },
      });
      items.push({
        name: 'Добавить строку копированием выбранной',
        disabled: (row) => !row?.rowId,
        action: (row) => {
          if (row) void handleCopyDetailRow(row);
        },
      });
      items.push({
        name: 'Добавить строку',
        action: (row) => {
          setImportDialogType(row?.isEquipment ? 'equipment' : 'physical-volumes');
        },
      });
    } else if (canImport) {
      items.push({
        name: 'Добавить (импорт)',
        action: (row) => {
          setImportDialogType(row?.isEquipment ? 'equipment' : 'physical-volumes');
        },
      });
    }

    return items;
  }, [
    canEditEquipmentCatalog,
    canEditNorms,
    canEditPeq,
    canEditPpv,
    canEditPvCatalog,
    canImport,
    handleCopyDetailRow,
    handleDeleteDetailRow,
  ]);

  const summaryColumns = useMemo(() => createForecastSummaryColumns(), []);
  const facilityColumns = useMemo(() => createForecastFacilityColumns(), []);
  const summaryRows = pageData?.forecast.summary ?? [];
  const normFallbackWarnings = useMemo(
    () =>
      collectNormFallbackWarnings(
        summaryRows
          .filter((r) => !r.isTotal)
          .map((r) => ({
            workDirectionId: r.workDirectionId,
            name: r.displayName,
            factPhysicalVolume: r.factPhysicalVolume,
            normProductivity: r.normProductivity,
            baseHours: r.baseHours,
          })),
      ),
    [summaryRows],
  );
  const facilityRows = pageData?.forecast.facility ?? [];
  const detailRows = pageData?.forecast.detail ?? [];

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
      <Grid container spacing={2} alignItems="center" sx={{ mb: 2 }}>
        <Grid item xs={12} md={6}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Button
              variant="contained"
              onClick={() => setVersionPickerOpen(true)}
              sx={{
                backgroundColor: '#2980B9',
                color: 'white',
                '&:hover': { backgroundColor: '#2471A3' },
              }}
            >
              📋 Версия (план)
            </Button>
            {planVersionLabel && (
              <Box
                sx={{
                  backgroundColor: '#EBF5FB',
                  border: '1px solid #AED6F1',
                  px: 1.5,
                  py: 0.5,
                  borderRadius: 1,
                }}
              >
                <Typography variant="body2" sx={{ color: '#1A5276' }}>
                  {planVersionLabel}
                </Typography>
              </Box>
            )}
            {planVersionId && (
              <Button
                variant="outlined"
                onClick={() => setPlanningOpen(true)}
                sx={{ textTransform: 'none' }}
              >
                Параметры планирования
              </Button>
            )}
          </Stack>
        </Grid>

        <Grid item xs={12} md={6}>
          <Stack direction="row" spacing={1} sx={importToolbarStackSx}>
            {canImport && (
              <>
                <Button
                  variant="outlined"
                  size="small"
                  disabled={!planVersionId || loading}
                  onClick={() => setImportDialogType('physical-volumes')}
                  startIcon={<FileDownloadIcon />}
                >
                  Импорт ФО
                </Button>
                <Button
                  variant="outlined"
                  size="small"
                  disabled={!planVersionId || loading}
                  onClick={() => setDeleteDialogType('physical-volumes')}
                  startIcon={<DeleteIcon />}
                  color="error"
                >
                  Удалить ФО
                </Button>
                <Button
                  variant="outlined"
                  size="small"
                  disabled={!planVersionId || loading}
                  onClick={() => setImportDialogType('equipment')}
                  startIcon={<FileDownloadIcon />}
                >
                  Импорт оборуд.
                </Button>
                <Button
                  variant="outlined"
                  size="small"
                  disabled={!planVersionId || loading}
                  onClick={() => setDeleteDialogType('equipment')}
                  startIcon={<DeleteIcon />}
                  color="error"
                >
                  Удалить оборуд.
                </Button>
              </>
            )}
          </Stack>
        </Grid>
      </Grid>

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        <Tabs
          value={subTab}
          onChange={(_, v: ForecastSubTab) => setSubTab(v)}
          sx={projectsPageSubTabsSx}
        >
          <Tab label="Итого" value="forecast-total" />
          <Tab label="Итого по объекту" value="forecast-facility" />
          <Tab label="Детально" value="forecast-detail" />
        </Tabs>

        <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
          {subTab === 'forecast-total' && (
            <>
              <Button
                variant="contained"
                disabled={!planVersionId}
                onClick={() => setReportOpen(true)}
                sx={{ backgroundColor: '#27AE60', color: 'white', height: 36, textTransform: 'none' }}
              >
                📊 Прогноз
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!planVersionId || exporting}
                onClick={async () => {
                  if (!planVersionId) return;
                  setExporting(true);
                  try {
                    await exportForecastSummaryPdf(projectId, planVersionId);
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
          {subTab === 'forecast-facility' && (
            <>
              <Button
                variant="outlined"
                size="small"
                disabled={!planVersionId || exporting}
                onClick={() => void handleExportFacility('excel')}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт Excel
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!planVersionId || exporting}
                onClick={() => void handleExportFacility('pdf')}
                startIcon={<PictureAsPdfIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт PDF
              </Button>
            </>
          )}
          {subTab === 'forecast-detail' && (
            <>
              <Button
                variant="outlined"
                size="small"
                disabled={!planVersionId || exporting}
                onClick={() => void handleExportDetail('excel')}
                startIcon={<FileDownloadIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт Excel
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!planVersionId || exporting}
                onClick={() => void handleExportDetail('pdf')}
                startIcon={<PictureAsPdfIcon />}
                sx={{ textTransform: 'none' }}
              >
                Экспорт PDF
              </Button>
              <Button
                variant="outlined"
                size="small"
                disabled={!planVersionId || exporting}
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

      {normFallbackWarnings.length > 0 && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {normFallbackWarnings.map((w) => (
            <div key={w}>{w}</div>
          ))}
        </Alert>
      )}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Box sx={{ flex: 1, minHeight: 360 }}>
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
              objectName="ForecastSummary"
              gridId={`forecast-summary-${projectId}`}
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
              objectName="ForecastFacility"
              gridId={`forecast-facility-${projectId}`}
              loading={loading}
              height="100%"
              pinnedBottomRowData={facilityPinnedBottomRow}
              getRowClass={facilityGetRowClass}
            />
          </Box>
        )}

        {subTab === 'forecast-detail' && (
          <DataTable<ProjectsPageDetailRow>
            rowData={detailRows}
            columnDefs={detailColumns}
            objectName="ForecastDetail"
            gridId={`forecast_details_columns-${projectId}`}
            loading={loading}
            height="100%"
            enableRowGrouping
            showColumnSettings
            contextMenuItems={detailContextMenuItems}
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
        versions={versions}
        directionFilter={ANALYSIS_PLAN}
        selectedVersionId={planVersionId}
        onSelect={handleVersionSelect}
        onClose={() => setVersionPickerOpen(false)}
        onVersionsChanged={() => void loadVersions()}
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
        onSaved={() => void loadPageData()}
      />

      {importDialogType && planVersionId && (
        <DataImportDialog
          open
          dataType={importDialogType}
          projectId={projectId}
          versionId={planVersionId}
          onClose={() => setImportDialogType(null)}
          onSuccess={() => void loadPageData()}
        />
      )}

      {deleteDialogType && planVersionId && (
        <IterationDeleteDialog
          open
          dataType={deleteDialogType}
          projectId={projectId}
          versionId={planVersionId}
          onClose={() => setDeleteDialogType(null)}
          onSuccess={() => void loadPageData()}
        />
      )}

      {ppvEdit && planVersionId && (
        <ProjectPhysicalVolumeQuickEditDialog
          open
          projectId={projectId}
          versionId={planVersionId}
          rowId={ppvEdit.rowId}
          nomenclature={ppvEdit.nomenclature}
          quantity={ppvEdit.quantity}
          onClose={() => setPpvEdit(null)}
          onSaved={() => void loadPageData()}
        />
      )}

      {peqEdit && planVersionId && (
        <ProjectEquipmentQuickEditDialog
          open
          projectId={projectId}
          versionId={planVersionId}
          rowId={peqEdit.rowId}
          nomenclature={peqEdit.nomenclature}
          quantity={peqEdit.quantity}
          onClose={() => setPeqEdit(null)}
          onSaved={() => void loadPageData()}
        />
      )}

      <ForecastDetailCatalogDialog
        open={catalogEdit != null}
        resource={catalogEdit?.resource ?? ''}
        entryId={catalogEdit?.entryId ?? null}
        title={catalogEdit?.title ?? ''}
        onClose={() => setCatalogEdit(null)}
        onSaved={() => void loadPageData()}
      />

      <ForecastDetailNormDialog
        open={normEdit != null}
        normId={normEdit?.normId ?? null}
        workDirectionId={normEdit?.workDirectionId ?? null}
        groupLabel={normEdit?.groupLabel ?? ''}
        unitLabel={normEdit?.unitLabel ?? ''}
        onClose={() => setNormEdit(null)}
        onSaved={() => void loadPageData()}
      />
    </Box>
  );
}
