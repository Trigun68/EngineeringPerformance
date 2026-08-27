import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import {
  Alert,
  Box,
  Chip,
<<<<<<< HEAD
  IconButton,
  LinearProgress,
  Paper,
  Skeleton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import HelpOutlineOutlinedIcon from '@mui/icons-material/HelpOutlineOutlined';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
=======
  Stack,
  Typography,
} from '@mui/material';
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
import type { ColDef } from 'ag-grid-community';
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
<<<<<<< HEAD
  type Confidence,
=======
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
  type FactNormAnalysisResponse,
  type NormComparison,
  type NormSuggestion,
  type ReferenceFormula,
  type Verdict,
} from '../../../api/factNormAnalyticsApi';
import { usePermission } from '../../../hooks/usePermission';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { AccuracyBadge } from './AccuracyBadge';
<<<<<<< HEAD
import { NormRevisionDialog } from './NormRevisionDialog';
import { FactVsNormChart } from './FactVsNormChart';
import { ProjectPickerDialog } from './ProjectPickerDialog';
import { deltaHeatBackground, severityColor, severityLabel, severityRatio } from './severity';
=======
import { CreateNormFromSuggestionDialog } from './CreateNormFromSuggestionDialog';
import { FactVsNormChart } from './FactVsNormChart';
import { ProjectPickerDialog } from './ProjectPickerDialog';
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968

const VERDICT_LABEL: Record<Verdict, string> = {
  ChangeFormulaType: 'Сменить формулу',
  AdjustParameters: 'Сдвиг параметров',
  NormIsActual: 'Норма актуальна',
  InsufficientData: 'Недостаточно данных',
};

const VERDICT_COLOR: Record<Verdict, 'error' | 'warning' | 'success' | 'default'> = {
  ChangeFormulaType: 'error',
  AdjustParameters: 'warning',
  NormIsActual: 'success',
  InsufficientData: 'default',
};

<<<<<<< HEAD
const CONFIDENCE_LABEL: Record<Confidence, string> = {
  Low: 'Низкая',
  Medium: 'Средняя',
  High: 'Высокая',
};

// Требующие внимания вердикты идут первыми, чтобы аналитик сразу видел проблемные группы.
const VERDICT_PRIORITY: Record<Verdict, number> = {
  ChangeFormulaType: 0,
  AdjustParameters: 1,
  InsufficientData: 2,
  NormIsActual: 3,
};

function sortByAttention(comparisons: NormComparison[]): NormComparison[] {
  return [...comparisons].sort((a, b) => VERDICT_PRIORITY[a.verdict] - VERDICT_PRIORITY[b.verdict]);
}

=======
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
const FACT_NORM_PROJECTS_PREF_KEY = 'analytics.factNorm.projectIds.v1';

interface Props {
  dateFrom: string;
  dateTo: string;
  onToolbarExtras?: (node: ReactNode) => void;
}

export function FactNormAnalysisPanel({ dateFrom, dateTo, onToolbarExtras }: Props) {
  const { canEdit: canEditNorms } = usePermission('IicWorkDirectionNorms');
  const [projectIds, setProjectIds] = useState<number[] | null>(null);
  const [prefsReady, setPrefsReady] = useState(false);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<FactNormAnalysisResponse | null>(null);
  const [selected, setSelected] = useState<NormComparison | null>(null);
  const [formulas, setFormulas] = useState<ReferenceFormula[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
<<<<<<< HEAD
  const [dialogRow, setDialogRow] = useState<NormComparison | null>(null);
  const [dialogSuggestion, setDialogSuggestion] = useState<NormSuggestion | null>(null);
  // Клик по KPI-карточке фильтрует таблицу; 'all' — фильтр не применён.
  const [kpiFilter, setKpiFilter] = useState<'all' | 'actual' | 'attention' | 'insufficient'>('all');
=======
  const [dialogSuggestion, setDialogSuggestion] = useState<NormSuggestion | null>(null);
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
  const abortRef = useRef<AbortController | null>(null);

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

  const runAnalysis = useCallback(async () => {
    if (!dateFrom || !dateTo) return;
    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    setLoading(true);
    setError(null);
    try {
      const data = await runFactNormAnalysis(
        {
          periodStart: dateFrom,
          periodEnd: dateTo,
          projectIds,
        },
        ac.signal,
      );
      setResult(data);
<<<<<<< HEAD
      setSelected(sortByAttention(data.comparisons)[0] ?? null);
=======
      setSelected(data.comparisons[0] ?? null);
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
    } catch (e) {
      if ((e as { name?: string })?.name === 'CanceledError' || (e as { code?: string })?.code === 'ERR_CANCELED') {
        return;
      }
      setError(e instanceof Error ? e.message : 'Ошибка анализа');
      setResult(null);
    } finally {
      setLoading(false);
    }
  }, [dateFrom, dateTo, projectIds]);

  useEffect(() => {
    if (!prefsReady) return;
    void runAnalysis();
    return () => abortRef.current?.abort();
  }, [runAnalysis, prefsReady]);

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
        <ToolbarActionButton onClick={() => void runAnalysis()} disabled={loading}>
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
    runAnalysis,
  ]);

  const suggestionById = useMemo(() => {
    const map = new Map<number, NormSuggestion>();
    for (const s of result?.suggestions ?? []) map.set(s.id, s);
    return map;
  }, [result]);

<<<<<<< HEAD
  const sortedComparisons = useMemo(
    () => sortByAttention(result?.comparisons ?? []),
    [result],
  );

  const filteredComparisons = useMemo(() => {
    if (kpiFilter === 'all') return sortedComparisons;
    if (kpiFilter === 'actual') return sortedComparisons.filter((c) => c.verdict === 'NormIsActual');
    if (kpiFilter === 'insufficient') {
      return sortedComparisons.filter((c) => c.verdict === 'InsufficientData');
    }
    return sortedComparisons.filter(
      (c) => c.verdict === 'ChangeFormulaType' || c.verdict === 'AdjustParameters',
    );
  }, [sortedComparisons, kpiFilter]);

  const summary = useMemo(() => {
    const comparisons = result?.comparisons ?? [];
    const total = comparisons.length;
    const actual = comparisons.filter((c) => c.verdict === 'NormIsActual').length;
    const attention = comparisons.filter(
      (c) => c.verdict === 'ChangeFormulaType' || c.verdict === 'AdjustParameters',
    ).length;
    const insufficient = comparisons.filter((c) => c.verdict === 'InsufficientData').length;
    const hasCriticalAttention = comparisons.some((c) => c.verdict === 'ChangeFormulaType');
    return { total, actual, attention, insufficient, hasCriticalAttention };
  }, [result]);

  const kpiCards: Array<{
    key: string;
    filterKey: 'all' | 'actual' | 'attention' | 'insufficient';
    label: string;
    value: number;
    icon: ReactNode;
    color: string;
  }> = [
    {
      key: 'total',
      filterKey: 'all',
      label: 'Групп проверено',
      value: summary.total,
      icon: <AssessmentOutlinedIcon fontSize="small" />,
      color: 'text.secondary',
    },
    {
      key: 'actual',
      filterKey: 'actual',
      label: 'Норма актуальна',
      value: summary.actual,
      icon: <CheckCircleOutlineIcon fontSize="small" />,
      color: 'success.main',
    },
    {
      key: 'attention',
      filterKey: 'attention',
      label: 'Требуют внимания',
      value: summary.attention,
      icon: <WarningAmberOutlinedIcon fontSize="small" />,
      color: summary.hasCriticalAttention ? 'error.main' : 'warning.main',
    },
    {
      key: 'insufficient',
      filterKey: 'insufficient',
      label: 'Недостаточно данных',
      value: summary.insufficient,
      icon: <HelpOutlineOutlinedIcon fontSize="small" />,
      color: 'text.secondary',
    },
  ];

  // Изменить норму можно для любой строки — в т.ч. для «Норма актуальна», на случай
  // ручной коррекции по мнению аналитика. Если у строки есть suggestion — предзаполняем
  // диалог им; иначе открываем ручной редактор формулы реестра норм.
  const openRevisionDialog = useCallback(
    (row: NormComparison | undefined) => {
      if (!row || !canEditNorms) return;
      const s = row.suggestionId ? suggestionById.get(row.suggestionId) ?? null : null;
      setDialogRow(row);
=======
  const openCreateDialog = useCallback(
    (row: NormComparison | undefined) => {
      if (!row?.suggestionId || !canEditNorms) return;
      if (row.verdict === 'NormIsActual') return;
      const s = suggestionById.get(row.suggestionId);
      if (!s) return;
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
      setDialogSuggestion(s);
      setDialogOpen(true);
    },
    [canEditNorms, suggestionById],
  );

  const contextMenuItems = useMemo<ContextMenuItem<NormComparison>[]>(() => {
    if (!canEditNorms) return [];
    return [
      {
<<<<<<< HEAD
        name: 'Изменить норму',
        action: openRevisionDialog,
      },
    ];
  }, [canEditNorms, openRevisionDialog]);
=======
        name: 'Создать норму',
        action: openCreateDialog,
        disabled: (row) =>
          !row?.suggestionId ||
          row.verdict === 'NormIsActual' ||
          row.verdict === 'InsufficientData',
      },
    ];
  }, [canEditNorms, openCreateDialog]);
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968

  const columnDefs = useMemo<ColDef<NormComparison>[]>(
    () => [
      { field: 'groupName', headerName: 'Группа IIC', flex: 1.6, minWidth: 200 },
      {
<<<<<<< HEAD
        colId: 'severity',
        headerName: 'Серьёзность',
        width: 150,
        valueGetter: (p) => (p.data ? severityRatio(p.data) : 0),
        cellRenderer: (p: { data?: NormComparison }) => {
          if (!p.data) return null;
          const ratio = severityRatio(p.data);
          return (
            <Stack direction="row" spacing={0.75} alignItems="center" sx={{ height: '100%' }}>
              <Box
                sx={{
                  width: 9,
                  height: 9,
                  borderRadius: '50%',
                  flexShrink: 0,
                  bgcolor: severityColor(ratio),
                }}
              />
              <Typography variant="body2" noWrap>
                {severityLabel(ratio)}
              </Typography>
            </Stack>
          );
        },
      },
      {
=======
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
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
<<<<<<< HEAD
        width: 140,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
        cellStyle: (p) => ({
          backgroundColor: deltaHeatBackground(p.value, p.data?.normStdDeviation || p.data?.factStdDeviation || 1),
        }),
=======
        width: 130,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
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
<<<<<<< HEAD
        cellStyle: (p) => ({
          backgroundColor: deltaHeatBackground(p.value, p.data?.normStdDeviation || 1),
        }),
=======
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
      },
      {
        field: 'deltaR2',
        headerName: 'ΔR²',
        width: 80,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
<<<<<<< HEAD
        cellStyle: (p) => ({ backgroundColor: deltaHeatBackground(p.value, 0.3) }),
=======
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
      },
      {
        field: 'verdict',
        headerName: 'Вердикт',
        width: 160,
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
<<<<<<< HEAD
      {
        colId: 'actions',
        headerName: '',
        width: 56,
        pinned: 'right' as const,
        sortable: false,
        filter: false,
        resizable: false,
        cellRenderer: (p: { data?: NormComparison }) => {
          if (!p.data || !canEditNorms) return null;
          return (
            <Tooltip title="Изменить норму">
              <IconButton
                size="small"
                aria-label={`Изменить норму: ${p.data.groupName}`}
                onClick={(e) => {
                  e.stopPropagation();
                  openRevisionDialog(p.data);
                }}
              >
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          );
        },
      },
    ],
    [canEditNorms, openRevisionDialog],
=======
    ],
    [],
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
  );

  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}
      data-testid="fact-norm-analysis-panel"
    >
      <Typography variant="body2" color="text.secondary">
        Сравнение <strong>факта</strong> (основные Fact-версии) с действующей нормой. Plan не
        участвует. Таблица — полный список направлений; график — динамика объём → производительность.
      </Typography>

<<<<<<< HEAD
      {loading && result ? <LinearProgress sx={{ borderRadius: 1 }} data-testid="fact-norm-refresh-progress" /> : null}

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }} data-testid="fact-norm-kpi-row">
        {loading && !result
          ? Array.from({ length: 4 }).map((_, i) => (
              <Skeleton
                key={i}
                variant="rounded"
                sx={{ flex: '1 1 220px', minWidth: 200 }}
                height={80}
              />
            ))
          : result && result.comparisons.length > 0
            ? kpiCards.map((card) => {
                const active = kpiFilter === card.filterKey;
                return (
                  <Paper
                    key={card.key}
                    component="button"
                    type="button"
                    variant="outlined"
                    onClick={() => setKpiFilter(active ? 'all' : card.filterKey)}
                    aria-pressed={active}
                    data-testid={`fact-norm-kpi-${card.filterKey}`}
                    sx={{
                      flex: '1 1 220px',
                      minWidth: 200,
                      p: 2,
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1.5,
                      textAlign: 'left',
                      cursor: 'pointer',
                      font: 'inherit',
                      borderColor: active ? 'primary.main' : undefined,
                      borderWidth: active ? 2 : 1,
                      backgroundColor: active ? 'action.selected' : 'background.paper',
                      transition: 'background-color 0.15s, border-color 0.15s',
                      '&:hover': { backgroundColor: 'action.hover' },
                    }}
                  >
                    <Box sx={{ color: card.color, display: 'flex' }}>{card.icon}</Box>
                    <Box>
                      <Typography variant="h6" component="div" sx={{ color: card.color, lineHeight: 1.2 }}>
                        {card.value}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {card.label}
                      </Typography>
                    </Box>
                  </Paper>
                );
              })
            : null}
        </Box>

      {kpiFilter !== 'all' && (
        <Alert severity="info" onClose={() => setKpiFilter('all')} sx={{ py: 0 }}>
          Показаны только строки: {kpiCards.find((c) => c.filterKey === kpiFilter)?.label}. Нажмите
          на карточку ещё раз или на крестик, чтобы вернуться к полному списку.
        </Alert>
      )}

=======
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
      {error && <Alert severity="error">{error}</Alert>}

      {result?.warnings?.length ? (
        <Alert severity="info">{result.warnings.slice(0, 3).join(' ')}</Alert>
      ) : null}

      {!loading && result && result.comparisons.length === 0 && (
        <Alert severity="info" data-testid="fact-norm-empty">
          Нет primary Fact с объёмом и производительностью за период. Выберите другой диапазон или
          проекты.
        </Alert>
      )}

      <Typography variant="subtitle1" component="h2">
        Сравнение по группам IIC
      </Typography>
<<<<<<< HEAD
      <Box
        sx={{
          '& .fact-norm-row-selected': {
            backgroundColor: 'rgba(41, 128, 185, 0.12) !important',
            fontWeight: 600,
          },
        }}
      >
        <DataTable<NormComparison>
          rowData={filteredComparisons}
          columnDefs={columnDefs}
          objectName="GroupTrend"
          gridId="fact-norm-suggestions"
          loading={loading}
          height={Math.max(280, Math.min(560, 48 + filteredComparisons.length * 42))}
          contextMenuItems={contextMenuItems}
          onRowClicked={(row) => setSelected(row)}
          getRowIdValue={(row) => String(row.normId)}
          getRowClass={(p) =>
            p.data && selected && p.data.normId === selected.normId
              ? 'fact-norm-row-selected'
              : undefined
          }
        />
      </Box>

      {selected && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
            <Chip
              size="small"
              color={VERDICT_COLOR[selected.verdict]}
              label={VERDICT_LABEL[selected.verdict]}
              data-testid="fact-norm-selected-verdict"
            />
            <Chip
              size="small"
              variant="outlined"
              label={`Отклонение: ${severityLabel(severityRatio(selected))}`}
              sx={{ borderColor: severityColor(severityRatio(selected)), color: severityColor(severityRatio(selected)) }}
              data-testid="fact-norm-selected-severity"
            />
            {selected.suggestionId && suggestionById.get(selected.suggestionId) ? (
              <Chip
                size="small"
                variant="outlined"
                label={`Уверенность предложения: ${
                  CONFIDENCE_LABEL[suggestionById.get(selected.suggestionId)!.confidence]
                }`}
                data-testid="fact-norm-selected-confidence"
              />
            ) : null}
            {canEditNorms && (
              <Tooltip title="Изменить норму этой группы">
                <IconButton
                  size="small"
                  onClick={() => openRevisionDialog(selected)}
                  aria-label="Изменить норму"
                  data-testid="fact-norm-selected-edit"
                >
                  <EditOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
          <FactVsNormChart
            title={`${selected.groupName}: производительность vs объём`}
            points={selected.chartPoints ?? []}
            currentFormula={selected.currentFormula}
            currentParams={selected.currentParameters}
            proposedFormula={selected.proposedFormula}
            proposedParams={selected.proposedParameters}
            deviationSigma={selected.normStdDeviation}
          />
        </Box>
=======
      <DataTable<NormComparison>
        rowData={result?.comparisons ?? []}
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
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
      )}

      <ProjectPickerDialog
        open={pickerOpen}
        selectedIds={projectIds}
        onClose={() => setPickerOpen(false)}
        onApply={applyProjectIds}
      />

<<<<<<< HEAD
      <NormRevisionDialog
        open={dialogOpen}
        row={dialogRow}
        suggestion={dialogSuggestion}
        formulas={formulas}
=======
      <CreateNormFromSuggestionDialog
        open={dialogOpen}
        suggestion={dialogSuggestion}
        formulas={formulas}
        chartPoints={selected?.chartPoints ?? []}
>>>>>>> 480f11eb88553d5509e14adcaba70049ddca2968
        onClose={() => setDialogOpen(false)}
        onSaved={() => void runAnalysis()}
      />
    </Box>
  );
}
