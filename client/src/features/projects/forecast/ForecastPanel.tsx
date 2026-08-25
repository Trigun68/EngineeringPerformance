import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import type { ImportDataType } from '../../../api/importApi';
import { deleteEquipment, deletePhysicalVolume, createEquipment, createPhysicalVolume, fetchEquipmentById, fetchPhysicalVolumeById } from '../../../api/projectDataApi';
import {
  exportForecastDetailExcel,
  exportForecastDetailExcelVisible,
  exportForecastDetailPdf,
  exportForecastDetailSummaryTable,
  exportForecastFacilityExcel,
  exportForecastFacilityPdf,
  exportForecastSummaryExcel,
  exportForecastSummaryPdf,
  fetchProjectsPageData,
  fetchProjectsPageDetails,
} from '../../../api/projectsPageApi';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { DataTable, type ContextMenuItem } from '../../../components/DataTable/DataTable';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import {
  ANALYSIS_PLAN,
  buildVersionLabel,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { usePermission } from '../../../hooks/usePermission';
import { useConfirmation } from '../../../hooks/useConfirmation';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import {
  LAST_PROJECT_PREF_KEY,
  parsePrefId,
  planVersionPrefKey,
} from '../../../utils/projectWorkspacePrefs';
import { collectGridExportData } from '../../../utils/directoryExport';
import type { GridApi } from 'ag-grid-community';
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
import { shortCatalogName } from './catalogShortName';
import { panelPrimaryToolbarSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import { PanelNotificationsButton } from '../shared/PanelNotificationsButton';
import { useWorkspaceChrome } from '../shared/workspaceChromeContext';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import { ProjectEquipmentQuickEditDialog } from './ProjectEquipmentQuickEditDialog';
import { ProjectPhysicalVolumeQuickEditDialog } from './ProjectPhysicalVolumeQuickEditDialog';
import { ForecastDetailCatalogDialog } from './ForecastDetailCatalogDialog';
import { ForecastDetailNormDialog } from './ForecastDetailNormDialog';
import { collectForecastVolumeWarnings } from './normFallbackWarning';
import type { ProjectVersion } from '../../../api/projectVersionsApi';

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
  workspaceVersions: ProjectVersion[];
  workspacePlanVersionId: number | null;
  workspacePlanVersionLabel: string;
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

export function ForecastPanel({ projectId, projectName = '', projectCode = '', workspaceVersions, workspacePlanVersionId, workspacePlanVersionLabel }: ForecastPanelProps) {
  const { canRead } = usePermission('Projects');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { canEdit: canEditPpv } = usePermission('ProjectPhysicalVolumes');
  const { canEdit: canEditPeq } = usePermission('ProjectEquipments');
  const { canEdit: canEditPvCatalog } = usePermission('PhysicalVolumes');
  const { canEdit: canEditEquipmentCatalog } = usePermission('Equipment');
  const { canEdit: canEditNorms } = usePermission('IicWorkDirectionNorms');
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();
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
  const [pageData, setPageData] = useState<ProjectsPageData | null>(null);
  const [loading, setLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [versionPickerOpen, setVersionPickerOpen] = useState(false);
  const [reportOpen, setReportOpen] = useState(false);
  const [planningOpen, setPlanningOpen] = useState(false);
  const detailGridApiRef = useRef<GridApi | null>(null);
  const [detailGridReady, setDetailGridReady] = useState(false);
  const planVersionIdRef = useRef<number | null>(null);
  const pageDataLoadGenRef = useRef(0);
  const pageDataAbortRef = useRef<AbortController | null>(null);
  planVersionIdRef.current = planVersionId;
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
    setPageData(null);
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

  const loadPageData = useCallback(async () => {
    if (!planVersionId || !canRead) return;
    const requestedVersionId = planVersionId;
    const loadGen = ++pageDataLoadGenRef.current;
    pageDataAbortRef.current?.abort();
    const controller = new AbortController();
    pageDataAbortRef.current = controller;
    setPageData((prev) =>
      prev ? { ...prev, forecast: { ...prev.forecast, detail: [] } } : prev,
    );
    setLoading(true);
    setError(null);
    try {
      const data = await fetchProjectsPageData(projectId, requestedVersionId, undefined, controller.signal);
      if (loadGen !== pageDataLoadGenRef.current || requestedVersionId !== planVersionIdRef.current) {
        return;
      }
      setPageData((prev) => {
        if (prev != null && prev.forecast.detail.length > 0 && data.forecast.detail.length === 0) {
          return { ...data, forecast: { ...data.forecast, detail: prev.forecast.detail } };
        }
        return data;
      });
    } catch (err) {
      if (isRequestCanceled(err) || loadGen !== pageDataLoadGenRef.current) return;
      setError(getApiErrorMessage(err, 'Не удалось загрузить данные прогнозирования.'));
      setPageData(null);
    } finally {
      if (loadGen === pageDataLoadGenRef.current) {
        setLoading(false);
      }
    }
  }, [canRead, planVersionId, projectId]);

  useEffect(() => {
    void loadPageData();
  }, [loadPageData]);

  const forecastDetailLength = pageData?.forecast.detail.length ?? 0;
  const hasPageData = pageData != null;

  // Детально догружается отдельно. Не завязывать эффект на объект pageData и не
  // глушить fetch в cleanup — иначе prefs версии плана оставляют грид пустым.
  useEffect(() => {
    if (subTab !== 'forecast-detail' || !planVersionId || !canRead || !hasPageData) {
      return;
    }
    if (forecastDetailLength > 0) {
      return;
    }
    const requestedVersionId = planVersionId;
    const loadGen = pageDataLoadGenRef.current;
    const controller = new AbortController();
    setDetailLoading(true);
    void fetchProjectsPageDetails(projectId, requestedVersionId, undefined, controller.signal)
      .then((details) => {
        if (requestedVersionId !== planVersionIdRef.current) {
          return;
        }
        if (loadGen !== pageDataLoadGenRef.current) {
          return;
        }
        setPageData((prev) =>
          prev ? { ...prev, forecast: { ...prev.forecast, detail: details.forecastDetail } } : prev,
        );
      })
      .catch((err) => {
        if (isRequestCanceled(err) || requestedVersionId !== planVersionIdRef.current) {
          return;
        }
        setError(getApiErrorMessage(err, 'Не удалось загрузить детальные данные прогнозирования.'));
      })
      .finally(() => {
        if (requestedVersionId === planVersionIdRef.current && loadGen === pageDataLoadGenRef.current) {
          setDetailLoading(false);
        }
      });
    return () => {
      if (planVersionIdRef.current !== requestedVersionId) {
        controller.abort();
      }
      setDetailLoading(false);
    };
  }, [subTab, planVersionId, canRead, hasPageData, forecastDetailLength, projectId]);

  const handleVersionSelect = (version: ProjectVersion) => {
    setUserPlanVersionId(version.id);
    void putUserPreferences({
      [LAST_PROJECT_PREF_KEY]: String(projectId),
      [planVersionPrefKey(projectId)]: String(version.id),
    });
  };

  const handleExportSummary = async (type: 'excel' | 'pdf') => {
    if (!planVersionId) return;
    setExporting(true);
    setError(null);
    try {
      if (type === 'excel') {
        await exportForecastSummaryExcel(projectId, planVersionId);
      } else {
        await exportForecastSummaryPdf(projectId, planVersionId);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт.'));
    } finally {
      setExporting(false);
    }
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

  const handleDetailGridApiReady = useCallback((api: GridApi) => {
    detailGridApiRef.current = api;
    setDetailGridReady(true);
  }, []);

  const handleExportDetailVisibleExcel = async () => {
    if (!planVersionId) return;
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
      await exportForecastDetailExcelVisible(
        projectId,
        planVersionId,
        'Прогноз_Детально_выделенные',
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
    if (!(await confirm({
      title: 'Удалить строку?',
      description: `Удалить строку «${label}»?`,
    }))) return;

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
        name: 'Редактировать норму IIC',
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
  const facilityRows = pageData?.forecast.facility ?? [];
  const detailRows = pageData?.forecast.detail ?? [];
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
      <Stack direction="row" sx={panelPrimaryToolbarSx}>
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
            data-testid="plan-version-label"
            sx={{
              backgroundColor: '#EBF5FB',
              border: '1px solid #AED6F1',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
              maxWidth: { xs: '100%', sm: 320 },
            }}
          >
            <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>
              {planVersionLabel}
            </Typography>
          </Box>
        )}
        {catalogShortLabel && (
          <Box
            data-testid="plan-catalog-label"
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
        {planVersionId && (
          <Button
            variant="outlined"
            onClick={() => setPlanningOpen(true)}
            sx={{ textTransform: 'none' }}
            data-testid="forecast-planning-parameters-button"
          >
            Параметры
          </Button>
        )}
        {canImport && (
          <>
            <ToolbarActionButton
              disabled={!planVersionId || loading}
              onClick={() => setImportDialogType('physical-volumes')}
              startIcon={<UploadFileIcon />}
            >
              {ACTION_LABELS.importPhysicalVolumes}
            </ToolbarActionButton>
            <ToolbarActionButton
              disabled={!planVersionId || loading}
              onClick={() => setDeleteDialogType('physical-volumes')}
              startIcon={<DeleteIcon />}
              color="error"
            >
              {ACTION_LABELS.deletePhysicalVolumes}
            </ToolbarActionButton>
            <ToolbarActionButton
              disabled={!planVersionId || loading}
              onClick={() => setImportDialogType('equipment')}
              startIcon={<UploadFileIcon />}
            >
              {ACTION_LABELS.importEquipment}
            </ToolbarActionButton>
            <ToolbarActionButton
              disabled={!planVersionId || loading}
              onClick={() => setDeleteDialogType('equipment')}
              startIcon={<DeleteIcon />}
              color="error"
            >
              {ACTION_LABELS.deleteEquipment}
            </ToolbarActionButton>
          </>
        )}
      </Stack>

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ flex: '1 1 auto', minWidth: 0, flexWrap: 'wrap' }}>
          <Tabs
            value={subTab}
            onChange={(_, v: ForecastSubTab) => setSubTab(v)}
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
              <ExportActionButton
                kind="excel"
                disabled={!planVersionId}
                loading={exporting}
                onClick={() => void handleExportSummary('excel')}
                data-testid="forecast-summary-export-excel"
              />
              <ExportActionButton
                kind="pdf"
                disabled={!planVersionId}
                loading={exporting}
                onClick={() => void handleExportSummary('pdf')}
                data-testid="forecast-summary-export-pdf"
              />
            </>
          )}
          {subTab === 'forecast-facility' && (
            <>
              <ExportActionButton
                kind="excel"
                disabled={!planVersionId}
                loading={exporting}
                onClick={() => void handleExportFacility('excel')}
                data-testid="forecast-facility-export-excel"
              />
              <ExportActionButton
                kind="pdf"
                disabled={!planVersionId}
                loading={exporting}
                onClick={() => void handleExportFacility('pdf')}
                data-testid="forecast-facility-export-pdf"
              />
            </>
          )}
          {subTab === 'forecast-detail' && (
            <>
              <ExportActionButton
                kind="excel"
                disabled={!planVersionId}
                loading={exporting}
                onClick={() => void handleExportDetail('excel')}
              />
              <ExportActionButton
                kind="excelSelected"
                disabled={!planVersionId || !detailGridReady}
                loading={exporting}
                onClick={() => void handleExportDetailVisibleExcel()}
              />
              <ExportActionButton
                kind="pdf"
                disabled={!planVersionId}
                loading={exporting}
                onClick={() => void handleExportDetail('pdf')}
                data-testid="forecast-detail-export-pdf"
              />
              <ExportActionButton
                kind="excelSummary"
                disabled={!planVersionId}
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
            rowData={detailRows}
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
      {ConfirmDialog}
    </Box>
  );
}
