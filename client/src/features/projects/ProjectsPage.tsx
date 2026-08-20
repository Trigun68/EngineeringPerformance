import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link as RouterLink, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { isAxiosError } from 'axios';
import { getApiErrorMessage } from '../../services/apiError';
import { fetchProjectVersions, type ProjectVersion } from '../../api/projectVersionsApi';
import {
  exportProjectsPageExcel,
  exportProjectsPagePdf,
  fetchProjectsPageData,
  fetchProjectsPageDetails,
} from '../../api/projectsPageApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';
import { usePermission } from '../../hooks/usePermission';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { buildProjectsPageContextMenu } from './buildContextMenu';
import {
  createCompareDetailColumns,
  createCompareFacilityColumns,
  createCompareSummaryColumns,
  createDetailColumns,
  createFacilityColumns,
  createForecastFacilityColumns,
  createForecastSummaryColumns,
  createPerformanceColumns,
} from './gridColumns';
import { GRID_TABS, type GridTabId, type ProjectsPageData } from './types';

export function ProjectsPage() {
  const { projectId: projectIdParam } = useParams<{ projectId: string }>();
  const projectId = Number(projectIdParam);
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();

  const { canRead, hasAccess: canEditProjects } = usePermission('Projects');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { hasAccess: canCopyVersion } = usePermission('ProjectAnalysisVersions');

  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const getDirectionName = useDictionaryStore((s) => s.getDirectionName);

  const [versions, setVersions] = useState<ProjectVersion[]>([]);
  const [versionId, setVersionId] = useState<number | null>(null);
  const [compareVersionId, setCompareVersionId] = useState<number | null>(null);
  const [activeTab, setActiveTab] = useState<GridTabId>('forecast-summary');
  const [pageData, setPageData] = useState<ProjectsPageData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [exportError, setExportError] = useState<string | null>(null);
  const [exporting, setExporting] = useState<'pdf' | 'excel' | null>(null);
  const detailRequestedKeyRef = useRef<string | null>(null);

  const formatters = useMemo(
    () => ({ getUnitName, getDirectionName }),
    [getDirectionName, getUnitName],
  );

  useEffect(() => {
    if (!projectId) return;
    void fetchProjectVersions(projectId).then(setVersions).catch(() => setVersions([]));
  }, [projectId]);

  useEffect(() => {
    const fromQuery = Number(searchParams.get('versionId'));
    if (fromQuery && versions.some((v) => v.id === fromQuery)) {
      setVersionId(fromQuery);
      return;
    }
    if (!versionId && versions[0]) {
      setVersionId(versions[0].id);
    }
  }, [versions, searchParams, versionId]);

  const loadPageData = useCallback(async () => {
    if (!projectId || !versionId || !canRead) return;
    detailRequestedKeyRef.current = null;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchProjectsPageData(projectId, versionId, compareVersionId);
      setPageData(data);
    } catch {
      setError('Не удалось загрузить данные гридов.');
      setPageData(null);
    } finally {
      setLoading(false);
    }
  }, [projectId, versionId, compareVersionId, canRead]);

  useEffect(() => {
    void loadPageData();
  }, [loadPageData]);

  useEffect(() => {
    const isDetailTab =
      activeTab === 'forecast-detail' || activeTab === 'fact-detail' || activeTab === 'compare-detail';
    if (!isDetailTab || !projectId || !versionId || !canRead || !pageData) {
      return;
    }
    const needsForecast = activeTab === 'forecast-detail' && pageData.forecast.detail.length === 0;
    const needsFact = activeTab === 'fact-detail' && pageData.fact.detail.length === 0;
    const needsCompare = activeTab === 'compare-detail' && (pageData.compare?.detail.length ?? 0) === 0;
    if (!needsForecast && !needsFact && !needsCompare) {
      return;
    }
    const key = `${projectId}:${versionId}:${compareVersionId ?? 'none'}:${activeTab}`;
    if (detailRequestedKeyRef.current === key) {
      return;
    }
    detailRequestedKeyRef.current = key;
    let cancelled = false;
    void fetchProjectsPageDetails(projectId, versionId, compareVersionId)
      .then((details) => {
        if (cancelled) {
          return;
        }
        setPageData((prev) => {
          if (!prev) {
            return prev;
          }
          return {
            ...prev,
            forecast: { ...prev.forecast, detail: details.forecastDetail },
            fact: { ...prev.fact, detail: details.factDetail },
            compare: prev.compare
              ? { ...prev.compare, detail: details.compareDetail ?? prev.compare.detail }
              : prev.compare,
          };
        });
      })
      .catch(() => {
        if (!cancelled) {
          detailRequestedKeyRef.current = null;
          setError('Не удалось загрузить детальные данные гридов.');
        }
      });
    return () => {
      cancelled = true;
    };
  }, [activeTab, projectId, versionId, compareVersionId, canRead, pageData]);

  const handleExport = useCallback(
    async (type: 'pdf' | 'excel') => {
      if (!projectId || !versionId || !canRead) return;
      setExportError(null);
      setExporting(type);
      try {
        if (type === 'pdf') {
          await exportProjectsPagePdf(projectId, versionId, compareVersionId);
        } else {
          await exportProjectsPageExcel(projectId, versionId, compareVersionId);
        }
      } catch (err) {
        if (isAxiosError(err)) {
          setExportError(getApiErrorMessage(err, 'Не удалось скачать файл экспорта.'));
        } else {
          setExportError('Не удалось скачать файл экспорта.');
        }
      } finally {
        setExporting(null);
      }
    },
    [canRead, compareVersionId, projectId, versionId],
  );

  const contextMenuItems = useMemo(
    () =>
      buildProjectsPageContextMenu(
        {
          canEditData: canEditProjects,
          canCopyVersion,
          canImport,
          canExport: canRead,
          canDelete: canEditProjects,
        },
        {
          onEdit: () => navigate(`/projects/${projectId}`),
          onCopyVersion: () => navigate(`/projects/${projectId}`),
          onImport: () => navigate(`/projects/${projectId}`),
          onExportExcel: () => void handleExport('excel'),
          onExportPdf: () => void handleExport('pdf'),
          onDelete: () => window.alert('Удаление строки — в разработке'),
        },
      ),
    [canCopyVersion, canEditProjects, canImport, canRead, handleExport, navigate, projectId, versionId],
  );

  const { rowData, columnDefs, gridId } = useMemo(() => {
    if (!pageData) {
      return { rowData: [], columnDefs: [] as ColDef[], gridId: 'empty' };
    }

    switch (activeTab) {
      case 'forecast-summary':
        return {
          rowData: pageData.forecast.summary,
          columnDefs: createForecastSummaryColumns(),
          gridId: 'pp-forecast-summary',
        };
      case 'forecast-facility':
        return {
          rowData: pageData.forecast.facility,
          columnDefs: createForecastFacilityColumns(),
          gridId: 'pp-forecast-facility',
        };
      case 'forecast-detail':
        return {
          rowData: pageData.forecast.detail,
          columnDefs: createDetailColumns(),
          gridId: 'pp-forecast-detail',
        };
      case 'fact-performance':
        return {
          rowData: pageData.fact.performance,
          columnDefs: createPerformanceColumns(formatters),
          gridId: 'pp-fact-performance',
        };
      case 'fact-facility':
        return {
          rowData: pageData.fact.facility,
          columnDefs: createFacilityColumns(),
          gridId: 'pp-fact-facility',
        };
      case 'fact-detail':
        return {
          rowData: pageData.fact.detail,
          columnDefs: createDetailColumns(),
          gridId: 'pp-fact-detail',
        };
      case 'compare-summary':
        return {
          rowData: pageData.compare?.summary ?? [],
          columnDefs: createCompareSummaryColumns('base'),
          gridId: 'pp-compare-summary',
        };
      case 'compare-facility':
        return {
          rowData: pageData.compare?.facility ?? [],
          columnDefs: createCompareFacilityColumns('base'),
          gridId: 'pp-compare-facility',
        };
      case 'compare-detail':
        return {
          rowData: pageData.compare?.detail ?? [],
          columnDefs: createCompareDetailColumns('base'),
          gridId: 'pp-compare-detail',
        };
      default:
        return { rowData: [], columnDefs: [] as ColDef[], gridId: 'empty' };
    }
  }, [activeTab, formatters, pageData]);

  if (!canRead) {
    return <Alert severity="info">Нет прав на просмотр проектов.</Alert>;
  }

  return (
    <Box>
      <WorkingOverlay open={loading} delayMs={1000} />
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h5">Данные проекта #{projectId}</Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          component={RouterLink}
          to={`/projects/${projectId}`}
        >
          ← Карточка проекта
        </Typography>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <FormControl size="small" sx={{ minWidth: 260 }}>
          <InputLabel>Версия</InputLabel>
          <Select
            label="Версия"
            value={versionId ?? ''}
            onChange={(e) => {
              const id = Number(e.target.value);
              setVersionId(id);
              setSearchParams({ versionId: String(id) });
            }}
          >
            {versions.map((v) => (
              <MenuItem key={v.id} value={v.id}>
                {v.versionName} — {v.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <FormControl size="small" sx={{ minWidth: 260 }}>
          <InputLabel>Версия для сравнения</InputLabel>
          <Select
            label="Версия для сравнения"
            value={compareVersionId ?? ''}
            displayEmpty
            onChange={(e) => {
              const raw = e.target.value;
              setCompareVersionId(raw === '' ? null : Number(raw));
            }}
          >
            <MenuItem value="">—</MenuItem>
            {versions
              .filter((v) => v.id !== versionId)
              .map((v) => (
                <MenuItem key={v.id} value={v.id}>
                  {v.versionName} — {v.name}
                </MenuItem>
              ))}
          </Select>
        </FormControl>

        <Stack direction="row" spacing={1} alignItems="center" sx={{ ml: 'auto' }}>
          <Button
            variant="outlined"
            size="small"
            disabled={!versionId || exporting !== null}
            onClick={() => void handleExport('pdf')}
          >
            {exporting === 'pdf' ? 'Экспорт PDF…' : 'Экспорт PDF'}
          </Button>
          <Button
            variant="outlined"
            size="small"
            disabled={!versionId || exporting !== null}
            onClick={() => void handleExport('excel')}
          >
            {exporting === 'excel' ? 'Экспорт Excel…' : 'Экспорт Excel'}
          </Button>
        </Stack>
      </Stack>

      {exportError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setExportError(null)}>
          {exportError}
        </Alert>
      )}

      <Tabs
        value={activeTab}
        onChange={(_, value: GridTabId) => setActiveTab(value)}
        variant="scrollable"
        scrollButtons="auto"
        sx={{ mb: 2 }}
      >
        {GRID_TABS.map((tab) => (
          <Tab key={tab.id} value={tab.id} label={tab.label} />
        ))}
      </Tabs>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {loading && !pageData ? (
        <Stack spacing={1}>
          <Skeleton variant="rectangular" height={48} />
          <Skeleton variant="rectangular" height={400} />
        </Stack>
      ) : (
        <DataTable
          rowData={rowData}
          columnDefs={columnDefs}
          objectName="Projects"
          gridId={gridId}
          loading={loading}
          pagination
          paginationPageSize={50}
          rowHeight={40}
          height={560}
          contextMenuItems={contextMenuItems}
        />
      )}
    </Box>
  );
}
