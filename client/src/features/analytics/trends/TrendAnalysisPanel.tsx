import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import {
  Alert,
  Box,
  Chip,
  Grid,
  IconButton,
  LinearProgress,
  Popover,
  Skeleton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
import DateRangeOutlinedIcon from '@mui/icons-material/DateRangeOutlined';
import FunctionsOutlinedIcon from '@mui/icons-material/FunctionsOutlined';
import {
  fetchZTrends,
  type ZTrendContribution,
  type ZTrendResponse,
  type ZTrendWeightMode,
} from '../../../api/zTrendApi';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { NormLegend } from './NormLegend';
import { ZTrendChart } from './ZTrendChart';
import { ZTrendKpiCards } from './ZTrendKpiCards';
import { computeZTrendKpi } from './zTrendKpi';

const WEIGHT_MODE_LABEL: Record<ZTrendWeightMode, string> = {
  basisHours: 'Нормативные часы (basisHours)',
};

function formatPeriodChip(dateFrom: string, dateTo: string): string {
  const fmt = (iso: string) => {
    const d = new Date(iso);
    return Number.isFinite(d.getTime())
      ? d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
      : '';
  };
  const from = dateFrom ? fmt(dateFrom) : '';
  const to = dateTo ? fmt(dateTo) : '';
  if (from && to) return `${from} — ${to}`;
  if (from) return `с ${from}`;
  if (to) return `по ${to}`;
  return 'Весь период';
}

interface Props {
  dateFrom: string;
  dateTo: string;
  weightMode: ZTrendWeightMode;
  showPlan: boolean;
  showFact: boolean;
  onToolbarExtras?: (node: ReactNode) => void;
}

function collectContributions(result: ZTrendResponse | null): ZTrendContribution[] {
  if (!result) return [];
  const map = new Map<string, ZTrendContribution>();
  const visit = (points: { contributions?: ZTrendContribution[] | null }[]) => {
    for (const p of points) {
      for (const c of p.contributions ?? []) {
        const key = `${c.projectId}-${c.versionId}`;
        if (!map.has(key)) map.set(key, c);
      }
    }
  };
  for (const seg of [...result.overall.plan.segments, ...result.overall.fact.segments]) {
    visit(seg.points);
  }
  for (const g of result.byGroup) {
    for (const seg of [...g.plan.segments, ...g.fact.segments]) visit(seg.points);
  }
  return [...map.values()].sort((a, b) =>
    `${a.projectCode} ${a.versionName}`.localeCompare(`${b.projectCode} ${b.versionName}`, 'ru'),
  );
}

export function TrendAnalysisPanel({
  dateFrom,
  dateTo,
  weightMode,
  showPlan,
  showFact,
  onToolbarExtras,
}: Props) {
  const [result, setResult] = useState<ZTrendResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [infoAnchor, setInfoAnchor] = useState<HTMLElement | null>(null);
  const [infoPinned, setInfoPinned] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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

  const contributions = useMemo(() => collectContributions(result), [result]);

  useEffect(() => {
    if (!onToolbarExtras) return;
    onToolbarExtras(
      <Tooltip title="Версии в выборке периода">
        <IconButton
          size="small"
          data-testid="ztrend-versions-info"
          aria-label="Версии в выборке периода"
          onMouseEnter={(e) => {
            if (!infoPinned) setInfoAnchor(e.currentTarget);
          }}
          onMouseLeave={() => {
            if (!infoPinned) setInfoAnchor(null);
          }}
          onClick={(e) => {
            setInfoPinned((p) => !p);
            setInfoAnchor(e.currentTarget);
          }}
        >
          <InfoOutlinedIcon fontSize="small" />
        </IconButton>
      </Tooltip>,
    );
    return () => onToolbarExtras(null);
  }, [onToolbarExtras, infoPinned]);

  const hasSegments = useMemo(() => {
    if (!result) return false;
    const o = result.overall;
    return o.plan.segments.length > 0 || o.fact.segments.length > 0 || result.byGroup.length > 0;
  }, [result]);

  const kpi = useMemo(() => (result ? computeZTrendKpi(result.overall) : null), [result]);

  return (
    <Stack spacing={2} data-testid="ztrend-panel">
      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
        <Chip
          size="small"
          icon={<FunctionsOutlinedIcon sx={{ fontSize: 15 }} />}
          label={`Вес: ${WEIGHT_MODE_LABEL[weightMode] ?? weightMode}`}
          data-testid="ztrend-weight-chip"
          sx={{ bgcolor: '#F1F5F9', color: '#475569', fontWeight: 500, cursor: 'default' }}
        />
        <Chip
          size="small"
          icon={<DateRangeOutlinedIcon sx={{ fontSize: 15 }} />}
          label={formatPeriodChip(dateFrom, dateTo)}
          data-testid="ztrend-period-chip"
          sx={{ bgcolor: '#F1F5F9', color: '#475569', fontWeight: 500, cursor: 'default' }}
        />
      </Stack>

      <Popover
        open={Boolean(infoAnchor)}
        anchorEl={infoAnchor}
        onClose={() => {
          setInfoPinned(false);
          setInfoAnchor(null);
        }}
        disableRestoreFocus
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        slotProps={{ paper: { sx: { p: 1.5, maxWidth: 420, maxHeight: 360, overflow: 'auto' } } }}
        data-testid="ztrend-versions-popover"
      >
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Версии в выборке периода
        </Typography>
        {contributions.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Нет версий с рассчитанным Z в текущем ответе.
          </Typography>
        ) : (
          <Stack component="ul" spacing={0.5} sx={{ m: 0, pl: 2 }}>
            {contributions.map((c) => (
              <Typography key={`${c.projectId}-${c.versionId}`} component="li" variant="body2">
                {c.projectCode} — {c.projectName}: {c.versionName}
                {c.zScore != null ? ` (Z=${c.zScore.toFixed(2)})` : ''}
              </Typography>
            ))}
          </Stack>
        )}
        {infoPinned ? (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
            Закреплено — нажмите ⓘ ещё раз, чтобы закрыть.
          </Typography>
        ) : null}
      </Popover>

      {loading && !result && (
        <Box data-testid="ztrend-loading" sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5 }}>
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton
                key={i}
                variant="rounded"
                height={96}
                sx={{ flex: '1 1 200px', minWidth: 200, borderRadius: 2.5 }}
              />
            ))}
          </Box>
          <Skeleton variant="rounded" height={360} sx={{ borderRadius: 2.5 }} />
        </Box>
      )}

      {loading && result ? (
        <LinearProgress sx={{ borderRadius: 1 }} data-testid="ztrend-refresh-progress" />
      ) : null}

      {error && (
        <Alert severity="error" data-testid="ztrend-error">
          {error}
        </Alert>
      )}

      {result && result.warnings.length > 0 && (
        <Alert severity="info" data-testid="ztrend-warnings" sx={{ py: 0.5 }}>
          {result.warnings.slice(0, 3).join(' ')}
        </Alert>
      )}

      {result && !hasSegments && (
        <Alert severity="info" data-testid="ztrend-empty">
          Нет основных версий (isPrimary) с рассчитанным Z в выбранном периоде.
        </Alert>
      )}

      {result && hasSegments && kpi && (
        <>
          <ZTrendKpiCards kpi={kpi} />

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
                По направлениям IIC
              </Typography>
              <Grid container spacing={2} data-testid="ztrend-groups-grid">
                {result.byGroup.map((g) => (
                  <Grid item xs={12} md={6} lg={4} key={g.groupId}>
                    <ZTrendChart
                      group={{
                        ...g,
                        groupName: formatIisNorm({ name: g.groupName }),
                      }}
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
