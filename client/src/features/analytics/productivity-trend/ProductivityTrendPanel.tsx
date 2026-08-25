import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  Grid,
  Stack,
  Typography,
} from '@mui/material';
import type { ColDef, ValueFormatterParams } from 'ag-grid-community';
import { DataTable } from '../../../components/DataTable/DataTable';
import {
  fetchProductivityTrend,
  type ProductivityTrendResponse,
  type ProductivityTrendTransition,
  type ProductivityVolumeMode,
} from '../../../api/productivityTrendApi';
import { FormulaTypeLegend } from './FormulaTypeLegend';
import { ProductivityTrendChart } from './ProductivityTrendChart';

interface Props {
  dateFrom: string;
  dateTo: string;
  volumeMode: ProductivityVolumeMode;
}

const VERDICT_LABEL: Record<string, string> = {
  tightening: 'Ужесточение',
  loosening: 'Ослабление',
  ambiguous: 'Неоднозначно',
  insufficient: 'Недостаточно данных',
  stable: 'Без изменения',
};

const FORMULA_LABEL: Record<string, string> = {
  Linear: 'Линейная',
  Logarithmic: 'Логарифмическая',
  ShiftedLog: 'Сдвинутая логарифмическая',
  Wright: 'Уайт',
  Power: 'Степенная',
  ExponentialDecay: 'Эксп. затухание',
  PiecewiseLinear: 'Кусочно-линейная',
  Polynomial: 'Полиномиальная',
  Constant: 'Постоянная',
  Hyperbolic: 'Гиперболическая',
  Exponential: 'Экспоненциальная',
};

function formatNum(value: number | null | undefined, digits = 2): string {
  if (value == null || Number.isNaN(value)) return '—';
  return value.toLocaleString('ru-RU', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });
}

function pair(oldValue: number | null, newValue: number | null): string {
  return `${formatNum(oldValue)} → ${formatNum(newValue)}`;
}

function formulaLabel(type: string): string {
  return FORMULA_LABEL[type] ?? type;
}

export function ProductivityTrendPanel({ dateFrom, dateTo, volumeMode }: Props) {
  const [result, setResult] = useState<ProductivityTrendResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const load = useCallback(() => {
    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    setLoading(true);
    setError(null);

    void fetchProductivityTrend(
      {
        periodStart: dateFrom || null,
        periodEnd: dateTo || null,
        volumeMode,
      },
      ac.signal,
    )
      .then((data) => {
        if (ac.signal.aborted) return;
        setResult(data);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (ac.signal.aborted) return;
        const message = err instanceof Error ? err.message : 'Не удалось загрузить тренд производительности';
        setError(message);
        setLoading(false);
      });
  }, [dateFrom, dateTo, volumeMode]);

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => load(), 400);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      abortRef.current?.abort();
    };
  }, [load]);

  const columnDefs = useMemo<ColDef<ProductivityTrendTransition>[]>(
    () => [
      { field: 'groupName', headerName: 'Группа IIC', flex: 1, minWidth: 220 },
      {
        field: 'effectiveFrom',
        headerName: 'Дата смены',
        width: 120,
        valueFormatter: (p: ValueFormatterParams<ProductivityTrendTransition>) => {
          const raw = p.value as string | undefined;
          if (!raw) return '—';
          const d = new Date(raw);
          return Number.isNaN(d.getTime()) ? raw : d.toLocaleDateString('ru-RU');
        },
      },
      {
        headerName: 'Формула',
        width: 220,
        valueGetter: (p) =>
          p.data
            ? `${formulaLabel(p.data.oldFormulaType)} → ${formulaLabel(p.data.newFormulaType)}`
            : '',
      },
      {
        headerName: 'Часы P10',
        width: 140,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldP10, p.data.hoursNewP10) : ''),
      },
      {
        headerName: 'Часы медиана',
        width: 150,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldMedian, p.data.hoursNewMedian) : ''),
      },
      {
        headerName: 'Часы P90',
        width: 140,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldP90, p.data.hoursNewP90) : ''),
      },
      {
        field: 'standardEffectHours',
        headerName: 'Эффект нормы',
        width: 130,
        valueFormatter: (p) => formatNum(p.value as number | null),
      },
      {
        field: 'mixEffectHours',
        headerName: 'Эффект объёма',
        width: 130,
        valueFormatter: (p) => formatNum(p.value as number | null),
      },
      {
        field: 'verdict',
        headerName: 'Вердикт',
        width: 150,
        valueFormatter: (p) => VERDICT_LABEL[String(p.value ?? '')] ?? String(p.value ?? '—'),
      },
      {
        field: 'curvesCross',
        headerName: 'Пересечение',
        width: 120,
        valueFormatter: (p) => (p.value ? 'Да' : 'Нет'),
      },
      {
        field: 'logVolumeLtOne',
        headerName: 'ln V<1',
        width: 100,
        valueFormatter: (p) => (p.value ? 'Да' : 'Нет'),
      },
    ],
    [],
  );

  const hasSeries = Boolean(
    result &&
      (result.overall.segments.some((s) => s.points.length > 0) || result.byGroup.length > 0),
  );

  return (
    <Stack spacing={2} data-testid="prod-trend-panel">
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress size={32} data-testid="prod-trend-loading" />
        </Box>
      )}

      {error && (
        <Alert severity="error" data-testid="prod-trend-error">
          {error}
        </Alert>
      )}

      {!loading && result && result.warnings.length > 0 && (
        <Alert severity="info" data-testid="prod-trend-warnings" sx={{ py: 0.5 }}>
          {result.warnings.slice(0, 3).join(' ')}
        </Alert>
      )}

      {!loading && result && !hasSeries && (
        <Alert severity="info" data-testid="prod-trend-empty">
          Нет норм IIC, пересекающих выбранный период.
        </Alert>
      )}

      {!loading && result && hasSeries && (
        <>
          <Typography variant="h6" component="h2">
            Индекс часов при опорном объёме V*
          </Typography>
          <FormulaTypeLegend legend={result.legend} />
          <ProductivityTrendChart
            group={result.overall}
            metric="index"
            height={320}
            title="Индекс Σ N(V*)·V* относительно начала периода"
            testId="prod-trend-overall-chart"
          />

          {result.byGroup.length > 0 && (
            <>
              <Typography variant="h6" component="h2" sx={{ mt: 1 }}>
                Часы N(V*) по группам IIC
              </Typography>
              <Grid container spacing={2} data-testid="prod-trend-groups-grid">
                {result.byGroup.map((g) => (
                  <Grid item xs={12} md={6} lg={4} key={g.groupId}>
                    <ProductivityTrendChart
                      group={g}
                      metric="hours"
                      height={260}
                      title={`${g.groupName}${g.vStar != null ? ` · V*=${formatNum(g.vStar, 1)}` : ''}`}
                      testId={`prod-trend-group-${g.groupId}`}
                    />
                  </Grid>
                ))}
              </Grid>
            </>
          )}

          <Typography variant="h6" component="h2" sx={{ mt: 1 }}>
            Смены формулы
          </Typography>
          <Box sx={{ height: 360 }} data-testid="prod-trend-transitions">
            <DataTable
              rowData={result.transitions}
              columnDefs={columnDefs}
              objectName="GroupTrend"
              gridId="analytics-productivity-trend-transitions"
              getRowIdValue={(row) => `${row.groupId}-${row.oldNormId}-${row.newNormId}`}
              gridTestId="prod-trend-transitions-grid"
              height="100%"
            />
          </Box>
        </>
      )}
    </Stack>
  );
}
