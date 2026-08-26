import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Stack,
  Typography,
} from '@mui/material';
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
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import { usePermission } from '../../../hooks/usePermission';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { HealthSummaryBadges } from './HealthSummary';
import { NormCard } from './NormCard';
import { NormFilters, type NormFilterState } from './NormFilters';
import { NormRegistryTable } from './NormRegistryTable';
import { NormTimeline } from './NormTimeline';

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
}

export function NormAnalysisPanel({ dateFrom, dateTo }: Props) {
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
  const [cardOpen, setCardOpen] = useState(false);
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

  if (!canRead) {
    return (
      <Alert severity="warning" data-testid="norm-analysis-no-permission">
        Нет права на чтение норм IIC (IicWorkDirectionNorms).
      </Alert>
    );
  }

  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}
      data-testid="norm-analysis-panel"
    >
      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap spacing={1}>
        <Typography variant="body2" color="text.secondary">
          Период анализа применяется к пересечению EffectiveFrom–EffectiveTo норм. Ниже — фильтры,
          таймлайн действия и реестр.
        </Typography>
        <Stack direction="row" spacing={1}>
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
      </Stack>

      <NormFilters meta={meta} value={filters} onChange={persistFilters} />
      <HealthSummaryBadges summary={health} loading={loading} />

      {error && (
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Typography variant="body2" color="text.secondary">
        Найдено: {total}
      </Typography>

      <NormTimeline
        timeline={timeline}
        onSelectNorm={(id) => {
          setSelectedId(id);
          setCardOpen(true);
        }}
      />

      <Typography variant="subtitle1" component="h3">
        Реестр норм
      </Typography>
      <NormRegistryTable
        rows={rows}
        loading={loading}
        issues={health?.issues ?? []}
        onRowClick={(row) => {
          setSelectedId(row.id);
          setCardOpen(true);
        }}
      />

      <NormCard
        open={cardOpen}
        normId={selectedId}
        canEdit={canEdit}
        onClose={() => setCardOpen(false)}
        onArchived={() => setReloadToken((x) => x + 1)}
      />
    </Box>
  );
}
