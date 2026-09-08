import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Chip,
  Stack,
  Typography,
} from '@mui/material';
import type { CellClassParams, ColDef, ICellRendererParams } from 'ag-grid-community';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import HelpOutlineOutlinedIcon from '@mui/icons-material/HelpOutlineOutlined';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import { DataTable } from '../../../components/DataTable/DataTable';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import type { ContextMenuItem } from '../../../components/DataTable/DataTable';
import {
  exportFactNormAnalysis,
  fetchReferenceFormulas,
  runFactNormAnalysis,
  type Accuracy,
  type NormComparison,
  type NormSuggestion,
  type ReferenceFormula,
  type Verdict,
} from '../../../api/factNormAnalyticsApi';
import { usePermission } from '../../../hooks/usePermission';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { getApiErrorMessage, isRequestCanceled } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import { AnalyticsKpiCards } from '../shared/AnalyticsKpiCards';
import { AccuracyBadge } from './AccuracyBadge';
import { CreateNormFromSuggestionDialog } from './CreateNormFromSuggestionDialog';
import { FactVsNormChart } from './FactVsNormChart';
import { ProjectPickerDialog } from './ProjectPickerDialog';
import {
  deltaHeatBackground,
  severityColor,
  severityLabel,
  severityRatio,
} from './severity';

const VERDICT_LABEL: Record<Verdict, string> = {
  ChangeFormulaType: 'Изменить тип формулы',
  AdjustParameters: 'Скорректировать параметры',
  NormIsActual: 'Норма актуальна',
  InsufficientData: 'Недостаточно данных',
};

const VERDICT_COLOR: Record<Verdict, 'error' | 'warning' | 'success' | 'default'> = {
  ChangeFormulaType: 'error',
  AdjustParameters: 'warning',
  NormIsActual: 'success',
  InsufficientData: 'default',
};

const FACT_NORM_PROJECTS_PREF_KEY = 'analytics.factNorm.projectIds.v1';

/** Стабильный scopeKey: порядок id в UI не должен плодить отдельные записи кэша. */
function factNormScopeKey(
  projectIds: number[] | null,
  workDirectionIds: number[] | null | undefined,
): string {
  const sortedProjects = projectIds == null ? null : [...projectIds].sort((a, b) => a - b);
  const sortedDirections = workDirectionIds?.length
    ? [...workDirectionIds].sort((a, b) => a - b)
    : null;
  return JSON.stringify({ projectIds: sortedProjects, workDirectionIds: sortedDirections });
}

type KpiFilter = 'all' | 'actual' | 'attention' | 'insufficient';

interface Props {
  dateFrom: string;
  dateTo: string;
  onToolbarExtras?: (node: ReactNode) => void;
  workDirectionIds?: number[] | null;
}

export function FactNormAnalysisPanel({
  dateFrom,
  dateTo,
  onToolbarExtras,
  workDirectionIds,
}: Props) {
  const queryClient = useQueryClient();
  const { canEdit: canEditNorms } = usePermission('IicWorkDirectionNorms');
  const [projectIds, setProjectIds] = useState<number[] | null>(null);
  const [prefsReady, setPrefsReady] = useState(false);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [selected, setSelected] = useState<NormComparison | null>(null);
  const [formulas, setFormulas] = useState<ReferenceFormula[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [dialogSuggestion, setDialogSuggestion] = useState<NormSuggestion | null>(null);
  const [kpiFilter, setKpiFilter] = useState<KpiFilter>('all');

  useEffect(() => {
    void fetchReferenceFormulas().then(setFormulas).catch(() => setFormulas([]));
  }, []);

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        const raw = prefs[FACT_NORM_PROJECTS_PREF_KEY];
        if (raw) {
          try {
            const parsed = JSON.parse(raw) as unknown;
            if (parsed === null) {
              setProjectIds(null);
            } else if (Array.isArray(parsed)) {
              setProjectIds(
                parsed.filter((id): id is number => typeof id === 'number' && Number.isFinite(id)),
              );
            }
          } catch {
            // defaults: all projects
          }
        }
        setPrefsReady(true);
      })
      .catch(() => {
        if (!cancelled) setPrefsReady(true);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const applyProjectIds = useCallback((ids: number[] | null) => {
    setProjectIds(ids);
    void putUserPreferences({ [FACT_NORM_PROJECTS_PREF_KEY]: JSON.stringify(ids) });
  }, []);

  const scopeKey = useMemo(
    () => factNormScopeKey(projectIds, workDirectionIds),
    [projectIds, workDirectionIds],
  );
  const analysisQueryKey = queryKeys.analytics.factNorm(dateFrom, dateTo, scopeKey);

  const {
    data,
    isFetching,
    isError,
    error: queryError,
    refetch,
  } = useQuery({
    queryKey: analysisQueryKey,
    queryFn: ({ signal }) =>
      runFactNormAnalysis(
        {
          periodStart: dateFrom,
          periodEnd: dateTo,
          projectIds,
          workDirectionIds: workDirectionIds?.length ? workDirectionIds : null,
        },
        signal,
      ),
    enabled: prefsReady && !!dateFrom && !!dateTo,
    placeholderData: keepPreviousData,
  });

  const result = data ?? null;
  const loading = isFetching;
  const error =
    isError && !isRequestCanceled(queryError)
      ? getApiErrorMessage(queryError, 'Ошибка анализа')
      : null;

  // Как раньше: после успешного ответа выделяем первую строку сравнения.
  useEffect(() => {
    if (!result) return;
    setSelected(result.comparisons[0] ?? null);
  }, [result]);

  const accuracy: Accuracy | null = result?.accuracy ?? null;
  const projectLabel =
    projectIds == null
      ? 'Все проекты'
      : projectIds.length === 1
        ? `1 проект`
        : `${projectIds.length} проектов`;

  useEffect(() => {
    if (!onToolbarExtras) return;
    const shortPeriod =
      accuracy != null && accuracy.periodLengthDays > 0 && accuracy.periodLengthDays < 90;
    onToolbarExtras(
      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
        <AccuracyBadge accuracy={accuracy} />
        {shortPeriod ? (
          <Chip
            size="small"
            color="warning"
            variant="outlined"
            label="Короткий период"
            title={accuracy?.hint ?? `${accuracy?.periodLengthDays ?? 0} дн.`}
            data-testid="fact-norm-short-period"
          />
        ) : null}
        <ToolbarActionButton
          onClick={() => setPickerOpen(true)}
          data-testid="fact-norm-project-scope"
        >
          {projectLabel}
        </ToolbarActionButton>
        <ToolbarActionButton onClick={() => void refetch()} disabled={loading}>
          {ACTION_LABELS.refresh}
        </ToolbarActionButton>
        <ExportActionButton
          kind="excel"
          disabled={!result || loading}
          onClick={() =>
            void exportFactNormAnalysis(
              { periodStart: dateFrom, periodEnd: dateTo, projectIds },
              'xlsx',
            )
          }
          data-testid="fact-norm-export"
        />
      </Stack>,
    );
    return () => onToolbarExtras(null);
  }, [
    onToolbarExtras,
    accuracy,
    projectLabel,
    loading,
    result,
    dateFrom,
    dateTo,
    projectIds,
    refetch,
  ]);

  const suggestionById = useMemo(() => {
    const map = new Map<number, NormSuggestion>();
    for (const s of result?.suggestions ?? []) map.set(s.id, s);
    return map;
  }, [result]);

  const openCreateDialog = useCallback(
    (row: NormComparison | undefined) => {
      if (!row?.suggestionId || !canEditNorms) return;
      if (row.verdict === 'NormIsActual') return;
      const s = suggestionById.get(row.suggestionId);
      if (!s) return;
      setDialogSuggestion(s);
      setDialogOpen(true);
    },
    [canEditNorms, suggestionById],
  );

  const contextMenuItems = useMemo<ContextMenuItem<NormComparison>[]>(() => {
    if (!canEditNorms) return [];
    return [
      {
        name: 'Создать норму',
        action: openCreateDialog,
        disabled: (row) =>
          !row?.suggestionId ||
          row.verdict === 'NormIsActual' ||
          row.verdict === 'InsufficientData',
      },
    ];
  }, [canEditNorms, openCreateDialog]);

  const comparisons = result?.comparisons ?? [];
  const summary = useMemo(() => {
    const actual = comparisons.filter((c) => c.verdict === 'NormIsActual').length;
    const attention = comparisons.filter(
      (c) => c.verdict === 'AdjustParameters' || c.verdict === 'ChangeFormulaType',
    ).length;
    const insufficient = comparisons.filter((c) => c.verdict === 'InsufficientData').length;
    const hasCriticalAttention = comparisons.some(
      (c) => c.verdict === 'ChangeFormulaType' || severityRatio(c) >= 2,
    );
    return {
      total: comparisons.length,
      actual,
      attention,
      insufficient,
      hasCriticalAttention,
    };
  }, [comparisons]);

  const filteredComparisons = useMemo(() => {
    if (kpiFilter === 'all') return comparisons;
    if (kpiFilter === 'actual') return comparisons.filter((c) => c.verdict === 'NormIsActual');
    if (kpiFilter === 'insufficient') {
      return comparisons.filter((c) => c.verdict === 'InsufficientData');
    }
    return comparisons.filter(
      (c) => c.verdict === 'ChangeFormulaType' || c.verdict === 'AdjustParameters',
    );
  }, [comparisons, kpiFilter]);

  const columnDefs = useMemo<ColDef<NormComparison>[]>(
    () => [
      { field: 'groupName', headerName: 'Группа IIC', flex: 1.6, minWidth: 200 },
      {
        headerName: 'Серьёзность',
        width: 180,
        valueGetter: (p) => (p.data ? severityRatio(p.data) : 0),
        cellRenderer: (p: ICellRendererParams<NormComparison>) => {
          if (!p.data) return null;
          const ratio = severityRatio(p.data);
          return (
            <Stack direction="row" spacing={0.75} alignItems="center" sx={{ height: '100%' }}>
              <Box
                sx={{
                  width: 9,
                  height: 9,
                  borderRadius: '50%',
                  bgcolor: severityColor(ratio),
                  flexShrink: 0,
                }}
              />
              <Typography variant="body2" sx={{ color: severityColor(ratio), fontWeight: 600 }}>
                {severityLabel(ratio)}
              </Typography>
            </Stack>
          );
        },
      },
      {
        field: 'normMeanPerformance',
        headerName: 'μ нормы',
        width: 100,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : '—'),
      },
      {
        field: 'factMeanPerformance',
        headerName: 'Среднее факт',
        width: 120,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : '—'),
      },
      {
        field: 'deltaMean',
        headerName: 'Δμ (факт−норма)',
        width: 140,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
        cellStyle: (p: CellClassParams<NormComparison>) => ({
          backgroundColor: deltaHeatBackground(
            Number(p.value ?? 0),
            p.data?.normStdDeviation || 1,
          ),
        }),
      },
      {
        field: 'normStdDeviation',
        headerName: 'σ нормы',
        width: 90,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : '—'),
      },
      {
        field: 'factStdDeviation',
        headerName: 'σ факт',
        width: 90,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : '—'),
      },
      {
        field: 'deltaSigma',
        headerName: 'Δσ',
        width: 80,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
        cellStyle: (p: CellClassParams<NormComparison>) => ({
          backgroundColor: deltaHeatBackground(
            Number(p.value ?? 0),
            p.data?.normStdDeviation || 1,
          ),
        }),
      },
      {
        field: 'deltaR2',
        headerName: 'ΔR²',
        width: 80,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
        cellStyle: (p: CellClassParams<NormComparison>) => ({
          backgroundColor: deltaHeatBackground(Number(p.value ?? 0), 0.3),
        }),
      },
      {
        field: 'verdict',
        headerName: 'Вердикт',
        width: 200,
        cellRenderer: (p: { value?: Verdict }) => {
          const v = p.value ?? 'NormIsActual';
          return (
            <Chip
              size="small"
              color={VERDICT_COLOR[v]}
              label={VERDICT_LABEL[v]}
              aria-label={VERDICT_LABEL[v]}
            />
          );
        },
      },
    ],
    [],
  );

  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}
      data-testid="fact-norm-analysis-panel"
    >
      <Typography variant="body2" color="text.secondary">
        Сравнение <strong>факта</strong> (основные Fact-версии) с действующей нормой. Plan не
        участвует. Карточки фильтруют таблицу; график — динамика объём → производительность выбранной
        группы.
      </Typography>

      {error && <Alert severity="error">{error}</Alert>}

      {result?.warnings?.length ? (
        <Alert severity="info">{result.warnings.slice(0, 3).join(' ')}</Alert>
      ) : null}

      <AnalyticsKpiCards
        testId="fact-norm-kpi"
        cards={[
          {
            key: 'all',
            label: 'Групп проверено',
            value: summary.total,
            icon: <AssessmentOutlinedIcon fontSize="small" />,
            color: 'text.secondary',
            active: kpiFilter === 'all',
            onClick: () => setKpiFilter('all'),
            testId: 'fact-norm-kpi-all',
          },
          {
            key: 'actual',
            label: 'Норма актуальна',
            value: summary.actual,
            icon: <CheckCircleOutlineIcon fontSize="small" />,
            color: 'success.main',
            active: kpiFilter === 'actual',
            onClick: () => setKpiFilter(kpiFilter === 'actual' ? 'all' : 'actual'),
            testId: 'fact-norm-kpi-actual',
          },
          {
            key: 'attention',
            label: 'Требуют внимания',
            value: summary.attention,
            icon: <WarningAmberOutlinedIcon fontSize="small" />,
            color: summary.hasCriticalAttention ? 'error.main' : 'warning.main',
            active: kpiFilter === 'attention',
            onClick: () => setKpiFilter(kpiFilter === 'attention' ? 'all' : 'attention'),
            testId: 'fact-norm-kpi-attention',
          },
          {
            key: 'insufficient',
            label: 'Недостаточно данных',
            value: summary.insufficient,
            icon: <HelpOutlineOutlinedIcon fontSize="small" />,
            color: 'text.secondary',
            active: kpiFilter === 'insufficient',
            onClick: () => setKpiFilter(kpiFilter === 'insufficient' ? 'all' : 'insufficient'),
            testId: 'fact-norm-kpi-insufficient',
          },
        ]}
      />

      {kpiFilter !== 'all' && (
        <Alert severity="info" onClose={() => setKpiFilter('all')} sx={{ py: 0 }} data-testid="fact-norm-kpi-filter-hint">
          Показаны только строки выбранного фильтра. Нажмите карточку ещё раз, чтобы вернуть полный
          список.
        </Alert>
      )}

      {!loading && result && result.comparisons.length === 0 && (
        <Alert severity="info" data-testid="fact-norm-empty">
          Нет primary Fact с объёмом и производительностью за период. Выберите другой диапазон или
          проекты.
        </Alert>
      )}

      <Typography variant="subtitle1" component="h2">
        Сравнение по группам IIC
      </Typography>
      <DataTable<NormComparison>
        rowData={filteredComparisons}
        columnDefs={columnDefs}
        objectName="GroupTrend"
        gridId="fact-norm-suggestions"
        loading={loading}
        height={Math.max(280, Math.min(560, 48 + (result?.comparisons.length ?? 0) * 42))}
        contextMenuItems={contextMenuItems}
        onRowClicked={(row) => setSelected(row)}
        getRowIdValue={(row) => String(row.normId)}
      />

      {selected && (
        <FactVsNormChart
          title={`${selected.groupName}: производительность vs объём`}
          points={selected.chartPoints ?? []}
          currentFormula={selected.currentFormula}
          currentParams={selected.currentParameters}
          proposedFormula={selected.proposedFormula}
          proposedParams={selected.proposedParameters}
        />
      )}

      <ProjectPickerDialog
        open={pickerOpen}
        selectedIds={projectIds}
        onClose={() => setPickerOpen(false)}
        onApply={applyProjectIds}
      />

      <CreateNormFromSuggestionDialog
        open={dialogOpen}
        suggestion={dialogSuggestion}
        formulas={formulas}
        chartPoints={selected?.chartPoints ?? []}
        onClose={() => setDialogOpen(false)}
        onSaved={() => {
          void queryClient.invalidateQueries({ queryKey: analysisQueryKey });
        }}
      />
    </Box>
  );
}
