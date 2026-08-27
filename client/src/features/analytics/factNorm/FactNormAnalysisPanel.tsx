import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import {
  Alert,
  Box,
  Chip,
  Stack,
  Typography,
} from '@mui/material';
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
  type FactNormAnalysisResponse,
  type NormComparison,
  type NormSuggestion,
  type ReferenceFormula,
  type Verdict,
} from '../../../api/factNormAnalyticsApi';
import { usePermission } from '../../../hooks/usePermission';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { AccuracyBadge } from './AccuracyBadge';
import { CreateNormFromSuggestionDialog } from './CreateNormFromSuggestionDialog';
import { FactVsNormChart } from './FactVsNormChart';
import { ProjectPickerDialog } from './ProjectPickerDialog';

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
  const [dialogSuggestion, setDialogSuggestion] = useState<NormSuggestion | null>(null);
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
      setSelected(data.comparisons[0] ?? null);
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

  const columnDefs = useMemo<ColDef<NormComparison>[]>(
    () => [
      { field: 'groupName', headerName: 'Группа IIC', flex: 1.6, minWidth: 200 },
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
        width: 130,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
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
      },
      {
        field: 'deltaR2',
        headerName: 'ΔR²',
        width: 80,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
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
        участвует. Таблица — полный список направлений; график — динамика объём → производительность.
      </Typography>

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
        onSaved={() => void runAnalysis()}
      />
    </Box>
  );
}
