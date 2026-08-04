import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Stack,
  Typography,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { DataTable } from '../../../components/DataTable/DataTable';
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

interface Props {
  dateFrom: string;
  dateTo: string;
}

export function FactNormAnalysisPanel({ dateFrom, dateTo }: Props) {
  const { canEdit: canEditNorms } = usePermission('IicWorkDirectionNorms');
  const [projectIds, setProjectIds] = useState<number[] | null>(null);
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
    void runAnalysis();
    return () => abortRef.current?.abort();
  }, [runAnalysis]);

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
      { field: 'groupName', headerName: 'Группа', flex: 1.4, minWidth: 180 },
      { field: 'currentFormula', headerName: 'Тек.тип', width: 120 },
      { field: 'proposedFormula', headerName: 'Факт.тип', width: 120 },
      {
        field: 'deltaR2',
        headerName: 'ΔR²',
        width: 90,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(3) : ''),
      },
      {
        field: 'deltaMean',
        headerName: 'Δmean',
        width: 100,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(2) : ''),
      },
      {
        field: 'deltaSigma',
        headerName: 'Δσ',
        width: 90,
        valueFormatter: (p) => (p.value != null ? Number(p.value).toFixed(2) : ''),
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

  const accuracy: Accuracy | null = result?.accuracy ?? null;
  const projectLabel =
    projectIds == null
      ? 'Все проекты'
      : projectIds.length === 1
        ? `1 проект`
        : `${projectIds.length} проектов`;

  return (
    <Box
      sx={{ display: 'flex', flexDirection: 'column', gap: 2, minHeight: 0 }}
      data-testid="fact-norm-analysis-panel"
    >
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems="center" flexWrap="wrap">
        <Typography variant="h6" component="h2" sx={{ flexGrow: 1 }}>
          Анализ факта и норм
        </Typography>
        <AccuracyBadge accuracy={accuracy} />
        <Button
          size="small"
          variant="outlined"
          onClick={() => setPickerOpen(true)}
          data-testid="fact-norm-project-scope"
        >
          Проекты: {projectLabel}
        </Button>
        <Button size="small" variant="outlined" onClick={() => void runAnalysis()} disabled={loading}>
          Обновить
        </Button>
        <Button
          size="small"
          variant="outlined"
          disabled={!result || loading}
          onClick={() =>
            void exportFactNormAnalysis(
              { periodStart: dateFrom, periodEnd: dateTo, projectIds },
              'xlsx',
            )
          }
          data-testid="fact-norm-export"
        >
          Экспорт
        </Button>
      </Stack>

      {accuracy?.level === 'Low' && accuracy.hint && (
        <Alert severity="warning" data-testid="fact-norm-low-accuracy-hint">
          {accuracy.hint}
        </Alert>
      )}

      {error && <Alert severity="error">{error}</Alert>}

      {result?.warnings?.length ? (
        <Alert severity="info">{result.warnings.slice(0, 3).join(' ')}</Alert>
      ) : null}

      {!loading && result && result.comparisons.length === 0 && (
        <Alert severity="info" data-testid="fact-norm-empty">
          Нет данных за период. Выберите другой диапазон или проекты.
        </Alert>
      )}

      <Box sx={{ minHeight: 280 }}>
        <DataTable<NormComparison>
          rowData={result?.comparisons ?? []}
          columnDefs={columnDefs}
          objectName="GroupTrend"
          gridId="fact-norm-suggestions"
          loading={loading}
          height={320}
          contextMenuItems={contextMenuItems}
          onRowClicked={(row) => setSelected(row)}
          getRowIdValue={(row) => String(row.normId)}
        />
      </Box>

      {selected && (
        <FactVsNormChart
          title={selected.groupName}
          points={selected.chartPoints ?? []}
          currentFormula={selected.currentFormula}
          currentParams={null}
          proposedFormula={selected.proposedFormula}
          proposedParams={selected.proposedParameters}
        />
      )}

      <ProjectPickerDialog
        open={pickerOpen}
        selectedIds={projectIds}
        onClose={() => setPickerOpen(false)}
        onApply={setProjectIds}
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
