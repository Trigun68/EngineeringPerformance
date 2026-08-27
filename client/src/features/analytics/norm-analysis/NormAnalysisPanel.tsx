import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import GridViewOutlinedIcon from '@mui/icons-material/GridViewOutlined';
import HelpOutlineOutlinedIcon from '@mui/icons-material/HelpOutlineOutlined';
import ShowChartOutlinedIcon from '@mui/icons-material/ShowChartOutlined';
import TableRowsOutlinedIcon from '@mui/icons-material/TableRowsOutlined';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import {
  exportNormHealthReport,
  exportNormRegistry,
  fetchNormRegistryFilters,
  fetchNormRegistryHealth,
  fetchNormRegistryList,
  fetchNormRegistryTimeline,
  type HealthSummary,
  type NormListItem,
  type NormRegistryFiltersMeta,
  type TimelineResponse,
} from '../../../api/normRegistryApi';
import { runFactNormAnalysis } from '../../../api/factNormAnalyticsApi';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import { usePermission } from '../../../hooks/usePermission';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { AnalyticsExtraAccordion } from '../shared/AnalyticsExtraAccordion';
import { AnalyticsKpiCards } from '../shared/AnalyticsKpiCards';
import { NormFilters, type NormFilterState } from './NormFilters';
import { NormHeatmap } from './NormHeatmap';
import { NormInlineDetail } from './NormInlineDetail';
import { NormRegistryTable } from './NormRegistryTable';
import { NormTimeline } from './NormTimeline';
import {
  coverageChipLabel,
  coverageIssues,
  filterNormsByKpi,
  sortNormsForRegistry,
  summarizeNormKpis,
  type NormKpiFilter,
} from './normAnalysisMetrics';

const NORM_FILTERS_PREF_KEY = 'analytics.normAnalysis.filters.v1';

const DEFAULT_FILTERS: NormFilterState = {
  groupIds: [],
  statuses: [],
  formulaTypes: [],
  onlyWithIssues: false,
  search: '',
  includeHistorical: true,
  includeFuture: true,
};

interface Props {
  dateFrom: string;
  dateTo: string;
  onOpenFactNorms?: (groupId: number) => void;
}

export function NormAnalysisPanel({ dateFrom, dateTo, onOpenFactNorms }: Props) {
  const { canRead, canEdit } = usePermission('IicWorkDirectionNorms');
  const [meta, setMeta] = useState<NormRegistryFiltersMeta | null>(null);
  const [filters, setFilters] = useState<NormFilterState>(DEFAULT_FILTERS);
  const [debounced, setDebounced] = useState<NormFilterState>(DEFAULT_FILTERS);
  const [filtersReady, setFiltersReady] = useState(false);
  const [rows, setRows] = useState<NormListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [health, setHealth] = useState<HealthSummary | null>(null);
  const [timeline, setTimeline] = useState<TimelineResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [kpiFilter, setKpiFilter] = useState<NormKpiFilter>('all');
  const [viewMode, setViewMode] = useState<'table' | 'heatmap'>('table');
  const [meanOffsetPct, setMeanOffsetPct] = useState(0);
  const [sigmaMultiplier, setSigmaMultiplier] = useState(1);
  const [factYs, setFactYs] = useState<number[] | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        const raw = prefs[NORM_FILTERS_PREF_KEY];
        if (raw) {
          try {
            const parsed = JSON.parse(raw) as Partial<NormFilterState>;
            const next: NormFilterState = {
              ...DEFAULT_FILTERS,
              ...parsed,
              groupIds: Array.isArray(parsed.groupIds) ? parsed.groupIds : DEFAULT_FILTERS.groupIds,
              statuses: Array.isArray(parsed.statuses) ? parsed.statuses : DEFAULT_FILTERS.statuses,
              formulaTypes: Array.isArray(parsed.formulaTypes)
                ? parsed.formulaTypes
                : DEFAULT_FILTERS.formulaTypes,
            };
            setFilters(next);
            setDebounced(next);
          } catch {
            // defaults
          }
        }
        setFiltersReady(true);
      })
      .catch(() => {
        if (!cancelled) setFiltersReady(true);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const persistFilters = useCallback((next: NormFilterState) => {
    setFilters(next);
    void putUserPreferences({ [NORM_FILTERS_PREF_KEY]: JSON.stringify(next) });
  }, []);

  useEffect(() => {
    const t = window.setTimeout(() => setDebounced(filters), 400);
    return () => window.clearTimeout(t);
  }, [filters]);

  useEffect(() => {
    if (!canRead) return;
    void fetchNormRegistryFilters()
      .then(setMeta)
      .catch(() => setMeta(null));
  }, [canRead]);

  const apiFilter = useMemo(
    () => ({
      groupIds: debounced.groupIds.length ? debounced.groupIds : null,
      statuses: debounced.statuses.length ? debounced.statuses : null,
      formulaTypes: debounced.formulaTypes.length ? debounced.formulaTypes : null,
      onlyWithIssues: debounced.onlyWithIssues ? true : null,
      search: debounced.search.trim() || null,
      effectiveFrom: dateFrom || null,
      effectiveTo: dateTo || null,
      page: 1,
      pageSize: 200,
      sortBy: 'group',
      sortDesc: false,
    }),
    [debounced, dateFrom, dateTo],
  );

  const load = useCallback(async () => {
    if (!canRead) return;
    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    setLoading(true);
    setError(null);
    try {
      const [list, healthSummary, tl] = await Promise.all([
        fetchNormRegistryList(apiFilter, ac.signal),
        fetchNormRegistryHealth(apiFilter.groupIds, ac.signal),
        fetchNormRegistryTimeline(
          debounced.includeHistorical,
          debounced.includeFuture,
          ac.signal,
        ),
      ]);
      setRows(list.items);
      setTotal(list.totalCount);
      setHealth(healthSummary);
      setTimeline(tl);
    } catch (err: unknown) {
      if ((err as { name?: string })?.name === 'CanceledError' || (err as { code?: string })?.code === 'ERR_CANCELED') {
        return;
      }
      setError(err instanceof Error ? err.message : 'Ошибка загрузки реестра норм');
    } finally {
      setLoading(false);
    }
  }, [apiFilter, canRead, debounced.includeFuture, debounced.includeHistorical]);

  useEffect(() => {
    if (!filtersReady) return;
    void load();
  }, [load, reloadToken, filtersReady]);

  const summary = useMemo(() => summarizeNormKpis(rows), [rows]);
  const filteredRows = useMemo(
    () => sortNormsForRegistry(filterNormsByKpi(rows, kpiFilter, summary.medianCv)),
    [rows, kpiFilter, summary.medianCv],
  );
  const missing = useMemo(() => coverageIssues(health), [health]);
  const groups = meta?.groups ?? [];
  const selectedRow = rows.find((r) => r.id === selectedId) ?? null;

  const kpiCards = [
    {
      key: 'all' as const,
      label: 'Всего норм',
      value: summary.total,
      icon: <AssessmentOutlinedIcon fontSize="small" />,
      color: 'text.secondary',
    },
    {
      key: 'Error' as const,
      label: 'Ошибки',
      value: summary.errors,
      icon: <ErrorOutlineIcon fontSize="small" />,
      color: 'error.main',
    },
    {
      key: 'Warning' as const,
      label: 'Предупреждения',
      value: summary.warnings,
      icon: <WarningAmberOutlinedIcon fontSize="small" />,
      color: 'warning.main',
    },
    {
      key: 'cv' as const,
      label: 'Аномальный разброс (CV)',
      value: summary.cvOutliers,
      icon: <ShowChartOutlinedIcon fontSize="small" />,
      color: '#8E44AD',
    },
    {
      key: 'lowConfidence' as const,
      label: 'Низкая уверенность',
      value: summary.lowConfidence,
      icon: <HelpOutlineOutlinedIcon fontSize="small" />,
      color: '#8E44AD',
    },
    {
      key: 'ok' as const,
      label: 'Без замечаний',
      value: summary.ok,
      icon: <CheckCircleOutlineIcon fontSize="small" />,
      color: 'success.main',
    },
  ];

  useEffect(() => {
    setMeanOffsetPct(0);
    setSigmaMultiplier(1);
    setFactYs(null);
    if (!selectedRow || !dateFrom || !dateTo) return;
    const ac = new AbortController();
    void runFactNormAnalysis(
      {
        periodStart: dateFrom,
        periodEnd: dateTo,
        workDirectionIds: [selectedRow.groupId],
      },
      ac.signal,
    )
      .then((data) => {
        const match = data.comparisons.find((c) => c.workDirectionId === selectedRow.groupId);
        const ys = (match?.chartPoints ?? []).map((p) => p.y).filter((y) => Number.isFinite(y));
        setFactYs(ys);
      })
      .catch(() => setFactYs([]));
    return () => ac.abort();
  }, [selectedRow, dateFrom, dateTo]);

  if (!canRead) {
    return (
      <Alert severity="warning" data-testid="norm-analysis-no-permission">
        Нет права на чтение норм IIC (IicWorkDirectionNorms).
      </Alert>
    );
  }

  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 1280, mx: 'auto' }}
      data-testid="norm-analysis-panel"
    >
      <AnalyticsExtraAccordion
        testId="norm-analysis-filters-extra"
        title="Фильтры и выгрузка"
      >
        <Stack spacing={1.5}>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <ExportActionButton
              kind="excel"
              onClick={() => void exportNormRegistry('xlsx', apiFilter).catch((e: Error) => setError(e.message))}
              data-testid="norm-analysis-export-xlsx"
            />
            <ExportActionButton
              kind="csv"
              onClick={() => void exportNormRegistry('csv', apiFilter).catch((e: Error) => setError(e.message))}
              data-testid="norm-analysis-export-csv"
            />
            <ExportActionButton
              kind="excelHealth"
              onClick={() => void exportNormHealthReport('xlsx').catch((e: Error) => setError(e.message))}
              data-testid="norm-analysis-export-health"
            />
            <ToolbarActionButton
              onClick={() => setReloadToken((x) => x + 1)}
              data-testid="norm-analysis-refresh"
            >
              {ACTION_LABELS.refresh}
            </ToolbarActionButton>
          </Stack>
          <NormFilters meta={meta} value={filters} onChange={persistFilters} />
        </Stack>
      </AnalyticsExtraAccordion>

      {missing.length > 0 && (
        <Alert
          severity="warning"
          icon={<ErrorOutlineIcon fontSize="inherit" />}
          data-testid="norm-analysis-coverage-alert"
        >
          <Typography variant="body2" sx={{ fontWeight: 600, mb: 0.5 }}>
            {missing.length} направления работ без активной нормы — расчёты идут без нормативного
            контроля
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 0.5 }}>
            {missing.map((issue) => (
              <Chip
                key={issue.id}
                size="small"
                label={coverageChipLabel(issue, groups)}
                variant="outlined"
                color="warning"
              />
            ))}
          </Stack>
        </Alert>
      )}

      <AnalyticsKpiCards
        testId="norm-analysis-health-summary"
        cards={kpiCards.map((card) => ({
          ...card,
          active: kpiFilter === card.key,
          onClick: () => setKpiFilter(kpiFilter === card.key && card.key !== 'all' ? 'all' : card.key),
          testId:
            card.key === 'Error'
              ? 'norm-analysis-badge-errors'
              : card.key === 'Warning'
                ? 'norm-analysis-badge-warnings'
                : card.key === 'all'
                  ? 'norm-analysis-kpi-all'
                  : card.key === 'cv'
                    ? 'norm-analysis-kpi-cv'
                    : card.key === 'lowConfidence'
                      ? 'norm-analysis-kpi-confidence'
                      : 'norm-analysis-kpi-ok',
        }))}
      />

      {kpiFilter !== 'all' && (
        <Alert severity="info" onClose={() => setKpiFilter('all')} sx={{ py: 0 }}>
          Показаны только строки: {kpiCards.find((c) => c.key === kpiFilter)?.label}. Нажмите на
          карточку ещё раз или на крестик, чтобы вернуться к полному списку.
        </Alert>
      )}

      {error && (
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <NormTimeline
        timeline={timeline}
        dateFrom={dateFrom}
        dateTo={dateTo}
        issues={health?.issues ?? []}
        selectedId={selectedId}
        onSelectNorm={(id) => setSelectedId(id)}
      />

      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap>
        <Typography variant="subtitle1" component="h3">
          Реестр норм ({filteredRows.length} из {total || rows.length})
        </Typography>
        <ToggleButtonGroup
          exclusive
          size="small"
          value={viewMode}
          onChange={(_, v: 'table' | 'heatmap' | null) => {
            if (v) setViewMode(v);
          }}
          aria-label="Режим отображения реестра"
          data-testid="norm-analysis-view-toggle"
        >
          <ToggleButton value="table" aria-label="Таблица" data-testid="norm-analysis-view-table">
            <TableRowsOutlinedIcon fontSize="small" sx={{ mr: 0.75 }} />
            Таблица
          </ToggleButton>
          <ToggleButton value="heatmap" aria-label="Тепловая карта" data-testid="norm-analysis-view-heatmap">
            <GridViewOutlinedIcon fontSize="small" sx={{ mr: 0.75 }} />
            Тепловая карта
          </ToggleButton>
        </ToggleButtonGroup>
      </Stack>

      {viewMode === 'heatmap' ? (
        <NormHeatmap
          rows={filteredRows}
          issues={health?.issues ?? []}
          groups={groups}
          selectedId={selectedId}
          onSelect={(row) => setSelectedId(row.id)}
        />
      ) : (
        <NormRegistryTable
          rows={filteredRows}
          allRows={rows}
          loading={loading}
          issues={health?.issues ?? []}
          medianCv={summary.medianCv}
          selectedId={selectedId}
          onRowClick={(row) => setSelectedId(row.id)}
        />
      )}

      {selectedRow ? (
        <NormInlineDetail
          row={selectedRow}
          allRows={rows}
          issues={health?.issues ?? []}
          canEdit={canEdit}
          factYs={factYs}
          meanOffsetPct={meanOffsetPct}
          sigmaMultiplier={sigmaMultiplier}
          onMeanOffset={setMeanOffsetPct}
          onSigmaMultiplier={setSigmaMultiplier}
          onResetSim={() => {
            setMeanOffsetPct(0);
            setSigmaMultiplier(1);
          }}
          onArchived={() => setReloadToken((x) => x + 1)}
          onOpenFactNorms={onOpenFactNorms}
        />
      ) : null}
    </Box>
  );
}
