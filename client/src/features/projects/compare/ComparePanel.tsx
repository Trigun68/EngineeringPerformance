import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  FormControlLabel,
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
import {
  fetchCompareChart,
  fetchVersionCompare,
  type CompareChartResult,
  type VersionCompareDetailRow,
  type VersionCompareFacilityRow,
  type VersionCompareResult,
} from '../../../api/analyticsApi';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { fetchProjectVersions, type ProjectVersion } from '../../../api/projectVersionsApi';
import { DataTable } from '../../../components/DataTable/DataTable';
import {
  buildVersionLabel,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { getApiErrorMessage } from '../../../services/apiError';
import {
  createCompareDetailColumns,
  createCompareFacilityColumns,
  createCompareSummaryColumns,
} from '../gridColumns';
import { importToolbarStackSx, projectsPageSubTabsSx } from '../shared/projectsPageTabStyles';
import {
  filterCompareDetailRows,
  filterCompareFacilityRows,
  filterCompareSummaryRows,
} from './compareFilters';
import { buildCompareTreeRows, type CompareTreeRow } from './compareTree';
import { exportComparePdf, type ComparePdfSection } from '../../../api/visualReportsApi';
import { DataImportDialog } from '../forecast/DataImportDialog';
import { IterationDeleteDialog } from '../forecast/IterationDeleteDialog';
import { CompareChartView } from './CompareChartView';

type CompareSubTab = 'compare-total' | 'compare-facility' | 'compare-detail';
type CompareViewMode = 'table' | 'chart' | 'split';

export interface ComparePanelProps {
  projectId: number;
}

function compareSummaryRowId(row: CompareTreeRow, index: number): string {
  return `cmp-${row.workDirectionId}-${index}`;
}

export function ComparePanel({ projectId }: ComparePanelProps) {
  const { canRead } = usePermission('VersionComparison');
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');

  const [versions, setVersions] = useState<ProjectVersion[]>([]);
  const [versionAId, setVersionAId] = useState<number | null>(null);
  const [versionBId, setVersionBId] = useState<number | null>(null);
  const [versionALabel, setVersionALabel] = useState('');
  const [versionBLabel, setVersionBLabel] = useState('');
  const [versionAName, setVersionAName] = useState('Версия А');
  const [versionBName, setVersionBName] = useState('Версия Б');
  const [pickerTarget, setPickerTarget] = useState<'A' | 'B' | null>(null);
  const [showOnlyDifferences, setShowOnlyDifferences] = useState(false);
  const [subTab, setSubTab] = usePersistedTabState<CompareSubTab>(
    `projects.${projectId}.compareSubTab`,
    'compare-total',
  );
  const [compareData, setCompareData] = useState<VersionCompareResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [importOpen, setImportOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [viewMode, setViewMode] = useState<CompareViewMode>('table');
  const [chartData, setChartData] = useState<CompareChartResult | null>(null);
  const [exportingPdf, setExportingPdf] = useState(false);

  const comparePdfSection = useMemo((): ComparePdfSection => {
    if (subTab === 'compare-facility') return 'facility';
    if (subTab === 'compare-detail') return 'detail';
    return 'summary';
  }, [subTab]);

  const handleExportPdf = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    setExportingPdf(true);
    try {
      await exportComparePdf(projectId, versionAId, versionBId, {
        section: comparePdfSection,
        showOnlyDifferences,
      });
    } catch {
      setError('Не удалось экспортировать PDF.');
    } finally {
      setExportingPdf(false);
    }
  }, [comparePdfSection, projectId, showOnlyDifferences, versionAId, versionBId]);

  useEffect(() => {
    setVersionAId(null);
    setVersionBId(null);
    setVersionALabel('');
    setVersionBLabel('');
    setCompareData(null);
    setError(null);
  }, [projectId]);

  const versionAPrefKey = `projects.${projectId}.compareVersionAId`;
  const versionBPrefKey = `projects.${projectId}.compareVersionBId`;

  const loadVersions = useCallback(async () => {
    const [list, prefs] = await Promise.all([
      fetchProjectVersions(projectId),
      getUserPreferences().catch(() => ({} as Record<string, string>)),
    ]);
    setVersions(list);

    const savedA = Number.parseInt(prefs[versionAPrefKey] ?? '', 10);
    const savedB = Number.parseInt(prefs[versionBPrefKey] ?? '', 10);

    const resolveA = Number.isFinite(savedA) && list.some((v) => v.id === savedA)
      ? savedA
      : list[0]?.id ?? null;
    const verA = list.find((v) => v.id === resolveA);
    if (verA) {
      setVersionAId(verA.id);
      setVersionALabel(buildVersionLabel(verA));
      setVersionAName(verA.versionName);
    } else {
      setVersionAId(null);
      setVersionALabel('');
      setVersionAName('Версия А');
    }

    const fallbackB = list.length > 1 ? list[1].id : list[0]?.id ?? null;
    const resolveB = Number.isFinite(savedB) && list.some((v) => v.id === savedB)
      ? savedB
      : fallbackB;
    const verB = list.find((v) => v.id === resolveB);
    if (verB) {
      setVersionBId(verB.id);
      setVersionBLabel(buildVersionLabel(verB));
      setVersionBName(verB.versionName);
    } else {
      setVersionBId(null);
      setVersionBLabel('');
      setVersionBName('Версия Б');
    }
  }, [projectId, versionAPrefKey, versionBPrefKey]);

  const loadCompare = useCallback(async () => {
    if (!versionAId || !versionBId || !canRead) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchVersionCompare(projectId, versionAId, versionBId);
      setCompareData(data);
      const chart = await fetchCompareChart(projectId, versionAId, versionBId, 'iic', 'zScore', 20);
      setChartData(chart);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить сравнение версий.'));
      setCompareData(null);
    } finally {
      setLoading(false);
    }
  }, [canRead, projectId, versionAId, versionBId]);

  useEffect(() => {
    void loadVersions().catch(() => setError('Не удалось загрузить версии.'));
  }, [loadVersions]);

  useEffect(() => {
    void loadCompare();
  }, [loadCompare]);

  const handleVersionSelect = (version: ProjectVersion) => {
    if (pickerTarget === 'A') {
      setVersionAId(version.id);
      setVersionALabel(buildVersionLabel(version));
      setVersionAName(version.versionName);
      void putUserPreferences({ [versionAPrefKey]: String(version.id) });
    } else if (pickerTarget === 'B') {
      setVersionBId(version.id);
      setVersionBLabel(buildVersionLabel(version));
      setVersionBName(version.versionName);
      void putUserPreferences({ [versionBPrefKey]: String(version.id) });
    }
    setPickerTarget(null);
  };

  const summaryRows = useMemo(() => {
    const rows = compareData?.summary ?? [];
    const filtered = filterCompareSummaryRows(rows, showOnlyDifferences);
    return buildCompareTreeRows(filtered);
  }, [compareData?.summary, showOnlyDifferences]);

  const facilityRows = useMemo(
    () => filterCompareFacilityRows(compareData?.facility ?? [], showOnlyDifferences),
    [compareData?.facility, showOnlyDifferences],
  );

  const detailRows = useMemo(
    () => filterCompareDetailRows(compareData?.detail ?? [], showOnlyDifferences),
    [compareData?.detail, showOnlyDifferences],
  );

  const summaryAutoGroupColumnDef = useMemo(
    () => ({
      headerName: 'Направление',
      minWidth: 260,
      field: 'name' as const,
      cellRendererParams: { suppressCount: true },
    }),
    [],
  );

  const summaryColumns = useMemo(
    () => createCompareSummaryColumns(versionAName, versionBName) as unknown as import('ag-grid-community').ColDef<CompareTreeRow>[],
    [versionAName, versionBName],
  );
  const facilityColumns = useMemo(
    () => createCompareFacilityColumns(versionAName, versionBName),
    [versionAName, versionBName],
  );
  const detailColumns = useMemo(() => createCompareDetailColumns(), []);

  const hasCompareData = Boolean(compareData && versionAId && versionBId);

  const kpiStrip = useMemo(() => {
    if (!compareData) return null;
    const totalHoursDelta = compareData.facility.reduce((s, r) => s + (r.hoursDelta ?? 0), 0);
    const zRows = compareData.summary.filter((r) => r.zDelta != null);
    const weightedZDelta = zRows.length > 0
      ? zRows.reduce((s, r) => s + (r.zDelta ?? 0), 0) / zRows.length
      : null;
    return { totalHoursDelta, weightedZDelta };
  }, [compareData]);

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр сравнения версий.</Alert>;
  }

  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      <Grid container spacing={2} alignItems="center" sx={{ mb: 2 }}>
        <Grid item xs={12} md={6}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Button
              variant="contained"
              onClick={() => setPickerTarget('A')}
              sx={{ backgroundColor: '#2980B9', color: 'white', '&:hover': { backgroundColor: '#2471A3' } }}
            >
              📋 Версия А
            </Button>
            {versionALabel && (
              <Box sx={{ backgroundColor: '#EBF5FB', border: '1px solid #AED6F1', px: 1.5, py: 0.5, borderRadius: 1 }}>
                <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>{versionALabel}</Typography>
              </Box>
            )}
          </Stack>
        </Grid>
        <Grid item xs={12} md={6}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Button
              variant="contained"
              onClick={() => setPickerTarget('B')}
              sx={{ backgroundColor: '#8E44AD', color: 'white', '&:hover': { backgroundColor: '#7D3C98' } }}
            >
              📋 Версия Б
            </Button>
            {versionBLabel && (
              <Box sx={{ backgroundColor: '#F5EEF8', border: '1px solid #D7BDE2', px: 1.5, py: 0.5, borderRadius: 1 }}>
                <Typography variant="body2" sx={{ color: '#6C3483' }} noWrap>{versionBLabel}</Typography>
              </Box>
            )}
          </Stack>
        </Grid>
      </Grid>

      {canImport && versionAId && (
        <Stack direction="row" spacing={1} sx={{ ...importToolbarStackSx, mb: 2, justifyContent: 'flex-start' }}>
          <Button
            variant="outlined"
            size="small"
            disabled={loading}
            onClick={() => setImportOpen(true)}
            startIcon={<FileDownloadIcon />}
          >
            Импорт оборуд.
          </Button>
          <Button
            variant="outlined"
            size="small"
            disabled={loading}
            onClick={() => setDeleteOpen(true)}
            startIcon={<DeleteIcon />}
            color="error"
          >
            Удалить оборуд.
          </Button>
        </Stack>
      )}

      <FormControlLabel
        control={
          <Checkbox
            checked={showOnlyDifferences}
            disabled={!hasCompareData}
            onChange={(e) => setShowOnlyDifferences(e.target.checked)}
          />
        }
        label="Показывать различия"
        sx={{ mb: 1 }}
      />

      {kpiStrip && (
        <Stack direction="row" spacing={2} sx={{ mb: 2, flexWrap: 'wrap' }}>
          <Typography variant="body2" sx={{ bgcolor: '#EBF5FB', px: 1.5, py: 0.5, borderRadius: 1 }}>
            Δ трудоёмкость (объекты): {kpiStrip.totalHoursDelta.toFixed(2)} ч
          </Typography>
          {kpiStrip.weightedZDelta != null && (
            <Typography variant="body2" sx={{ bgcolor: '#F5EEF8', px: 1.5, py: 0.5, borderRadius: 1 }}>
              Средн. Δ Z: {kpiStrip.weightedZDelta.toFixed(4)}
            </Typography>
          )}
        </Stack>
      )}

      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap', alignItems: 'center' }}>
        {(['table', 'chart', 'split'] as CompareViewMode[]).map((mode) => (
          <Button
            key={mode}
            size="small"
            variant={viewMode === mode ? 'contained' : 'outlined'}
            onClick={() => setViewMode(mode)}
            sx={{ textTransform: 'none' }}
          >
            {mode === 'table' ? 'Таблица' : mode === 'chart' ? 'График' : 'Split'}
          </Button>
        ))}
        <Button
          size="small"
          variant="outlined"
          disabled={!hasCompareData || exportingPdf}
          onClick={() => void handleExportPdf()}
          startIcon={<PictureAsPdfIcon />}
          sx={{ textTransform: 'none' }}
        >
          Экспорт PDF
        </Button>
      </Stack>

      <Tabs
        value={subTab}
        onChange={(_, v: CompareSubTab) => setSubTab(v)}
        sx={{ ...projectsPageSubTabsSx, mb: 2 }}
      >
        <Tab label="Итого" value="compare-total" />
        <Tab label="По объектам" value="compare-facility" />
        <Tab label="Детально" value="compare-detail" />
      </Tabs>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Box sx={{ flex: 1, minHeight: 360, display: 'flex', flexDirection: viewMode === 'split' ? 'column' : 'row', gap: 2 }}>
        {(viewMode === 'table' || viewMode === 'split') && (
          <Box sx={{ flex: viewMode === 'split' ? 1 : 1, minHeight: 0, width: '100%' }}>
        {subTab === 'compare-total' && (
          <DataTable<CompareTreeRow>
            rowData={summaryRows}
            columnDefs={summaryColumns}
            objectName="CompareSummary"
            gridId={`compare-summary-${projectId}`}
            loading={loading}
            height="100%"
            treeData
            getDataPath={(row) => row.hierarchy}
            autoGroupColumnDef={summaryAutoGroupColumnDef}
            showColumnSettings
            getRowIdValue={compareSummaryRowId}
          />
        )}

        {subTab === 'compare-facility' && (
          <DataTable<VersionCompareFacilityRow>
            rowData={facilityRows}
            columnDefs={facilityColumns}
            objectName="CompareFacility"
            gridId={`compare-facility-${projectId}`}
            loading={loading}
            height="100%"
            showColumnSettings
            getRowIdValue={(row, index) => `${row.facilityCode}-${row.facilityName}-${index}`}
          />
        )}

        {subTab === 'compare-detail' && (
          <DataTable<VersionCompareDetailRow>
            rowData={detailRows}
            columnDefs={detailColumns}
            objectName="CompareDetail"
            gridId={`compare-detail-${projectId}`}
            loading={loading}
            height="100%"
            enableRowGrouping
            showColumnSettings
            getRowIdValue={(row, index) =>
              `${row.facilityCode}-${row.pvCode}-${row.workId}-${index}`
            }
          />
        )}
          </Box>
        )}
        {(viewMode === 'chart' || viewMode === 'split') && subTab === 'compare-total' && (
          <Box sx={{ flex: viewMode === 'split' ? 1 : 1, minWidth: 0 }}>
            <CompareChartView chart={chartData} height={viewMode === 'split' ? 280 : 400} />
          </Box>
        )}
      </Box>

      <ProjectVersionPickerDialog
        open={pickerTarget != null}
        title={pickerTarget === 'A' ? 'Выбор версии А' : 'Выбор версии Б'}
        projectId={projectId}
        versions={versions}
        directionFilter="any"
        selectedVersionId={pickerTarget === 'A' ? versionAId : versionBId}
        onSelect={handleVersionSelect}
        onClose={() => setPickerTarget(null)}
        onVersionsChanged={() => void loadVersions()}
      />

      {importOpen && versionAId && (
        <DataImportDialog
          open
          dataType={'equipment' satisfies ImportDataType}
          projectId={projectId}
          versionId={versionAId}
          onClose={() => setImportOpen(false)}
          onSuccess={() => void loadCompare()}
        />
      )}

      {deleteOpen && versionAId && (
        <IterationDeleteDialog
          open
          dataType={'equipment' satisfies ImportDataType}
          projectId={projectId}
          versionId={versionAId}
          onClose={() => setDeleteOpen(false)}
          onSuccess={() => void loadCompare()}
        />
      )}
    </Box>
  );
}
