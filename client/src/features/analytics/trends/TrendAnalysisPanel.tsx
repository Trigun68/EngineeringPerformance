import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  FormControl,
  FormControlLabel,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  Typography,
} from '@mui/material';
import {
  fetchZTrendFilters,
  fetchZTrends,
  type ZTrendFilters,
  type ZTrendResponse,
  type ZTrendWeightMode,
} from '../../../api/zTrendApi';
import { NormLegend } from './NormLegend';
import { ZTrendChart } from './ZTrendChart';

interface Props {
  dateFrom: string;
  dateTo: string;
}

export function TrendAnalysisPanel({ dateFrom, dateTo }: Props) {
  const [filters, setFilters] = useState<ZTrendFilters | null>(null);
  const [result, setResult] = useState<ZTrendResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPlan, setShowPlan] = useState(true);
  const [showFact, setShowFact] = useState(true);
  const [weightMode, setWeightMode] = useState<ZTrendWeightMode>('basisHours');
  const abortRef = useRef<AbortController | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    let cancelled = false;
    void fetchZTrendFilters()
      .then((f) => {
        if (!cancelled) setFilters(f);
      })
      .catch(() => {
        if (!cancelled) setFilters({ projects: [], groups: [], weightModes: ['basisHours', 'uniform'] });
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const load = useCallback(() => {
    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    setLoading(true);
    setError(null);

    void fetchZTrends(
      {
        periodStart: dateFrom || null,
        periodEnd: dateTo || null,
        weightMode,
      },
      ac.signal,
    )
      .then((data) => {
        if (!ac.signal.aborted) {
          setResult(data);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (ac.signal.aborted) return;
        const message = err instanceof Error ? err.message : 'Не удалось загрузить тренды';
        setError(message);
        setLoading(false);
      });
  }, [dateFrom, dateTo, weightMode]);

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => load(), 400);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      abortRef.current?.abort();
    };
  }, [load]);

  const hasSegments = useMemo(() => {
    if (!result) return false;
    const o = result.overall;
    return o.plan.segments.length > 0 || o.fact.segments.length > 0 || result.byGroup.length > 0;
  }, [result]);

  return (
    <Stack spacing={2} data-testid="ztrend-panel">
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="ztrend-weight-label">Вес</InputLabel>
          <Select
            labelId="ztrend-weight-label"
            label="Вес"
            value={weightMode}
            onChange={(e) => setWeightMode(e.target.value as ZTrendWeightMode)}
            data-testid="ztrend-weight-mode"
          >
            {(filters?.weightModes ?? ['basisHours', 'uniform']).map((m) => (
              <MenuItem key={m} value={m}>
                {m === 'uniform' ? 'Равный (uniform)' : 'Нормативные часы (basisHours)'}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControlLabel
          control={
            <Switch
              checked={showPlan}
              onChange={(_, v) => setShowPlan(v)}
              data-testid="ztrend-toggle-plan"
            />
          }
          label="План"
        />
        <FormControlLabel
          control={
            <Switch
              checked={showFact}
              onChange={(_, v) => setShowFact(v)}
              data-testid="ztrend-toggle-fact"
            />
          }
          label="Факт"
        />
      </Stack>

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress size={32} data-testid="ztrend-loading" />
        </Box>
      )}

      {error && (
        <Alert severity="error" data-testid="ztrend-error">
          {error}
        </Alert>
      )}

      {!loading && result && result.warnings.length > 0 && (
        <Stack direction="row" flexWrap="wrap" gap={1} data-testid="ztrend-warnings">
          {result.warnings.slice(0, 8).map((w) => (
            <Alert key={w} severity="info" sx={{ py: 0 }}>
              {w}
            </Alert>
          ))}
        </Stack>
      )}

      {!loading && result && !hasSegments && (
        <Alert severity="info" data-testid="ztrend-empty">
          Нет основных версий (isPrimary) с рассчитанным Z в выбранном периоде.
        </Alert>
      )}

      {!loading && result && hasSegments && (
        <>
          <Typography variant="h6" component="h2">
            Общий тренд Z
          </Typography>
          <NormLegend legend={result.legend} />
          <ZTrendChart
            group={result.overall}
            showPlan={showPlan}
            showFact={showFact}
            height={360}
            title="Средневзвешенный Z по проектам"
            testId="ztrend-overall-chart"
          />

          {result.byGroup.length > 0 && (
            <>
              <Typography variant="h6" component="h2" sx={{ mt: 1 }}>
                По направлениям ИИС
              </Typography>
              <Grid container spacing={2} data-testid="ztrend-groups-grid">
                {result.byGroup.map((g) => (
                  <Grid item xs={12} md={6} lg={4} key={g.groupId}>
                    <ZTrendChart
                      group={g}
                      showPlan={showPlan}
                      showFact={showFact}
                      height={260}
                      testId={`ztrend-group-${g.groupId}`}
                    />
                  </Grid>
                ))}
              </Grid>
            </>
          )}
        </>
      )}
    </Stack>
  );
}
