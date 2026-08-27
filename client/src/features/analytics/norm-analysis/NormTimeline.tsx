import { useMemo } from 'react';
import { Box, Paper, Stack, Tooltip, Typography } from '@mui/material';
import type { NormHealthIssue, TimelineResponse } from '../../../api/normRegistryApi';
import { SEVERITY_COLOR, SEVERITY_LABEL, STATUS_LABEL, toSeverityKey } from './normAnalysisMetrics';
import {
  SEGMENT_GAP_PX,
  buildTimelineAxis,
  filterBarsToWindow,
  formatTimelineDate,
  groupTimelineBars,
  layoutSegments,
  parsePeriodWindow,
} from './normTimelineLayout';

interface Props {
  timeline: TimelineResponse | null;
  dateFrom?: string;
  dateTo?: string;
  issues?: NormHealthIssue[];
  selectedId?: number | null;
  onSelectNorm?: (normId: number) => void;
}

const LEGEND_KEYS = ['Error', 'Warning', 'Info', 'None'] as const;
const BLOCK_OUTLINE = '#2C3E50';
const GRID_COLUMNS = 'minmax(220px, 32%) 1fr';

export function NormTimeline({
  timeline,
  dateFrom,
  dateTo,
  issues = [],
  selectedId,
  onSelectNorm,
}: Props) {
  const { axis, groups } = useMemo(() => {
    const window = parsePeriodWindow(dateFrom, dateTo);
    const bars = filterBarsToWindow(timeline?.bars ?? [], window);
    return {
      axis: buildTimelineAxis(bars, Date.now(), window),
      groups: groupTimelineBars(bars),
    };
  }, [timeline, dateFrom, dateTo]);

  if (!timeline || groups.length === 0) {
    return (
      <Box
        sx={{ p: 2, border: '1px dashed', borderColor: 'divider', borderRadius: 1 }}
        data-testid="norm-analysis-timeline-empty"
      >
        <Typography variant="body2" color="text.secondary">
          Нет периодов для таймлайна
        </Typography>
      </Box>
    );
  }

  const severityByGroup = new Map<number, ReturnType<typeof toSeverityKey>>();
  for (const issue of issues) {
    if (issue.groupId == null) continue;
    const next = toSeverityKey(issue.severity);
    const prev = severityByGroup.get(issue.groupId) ?? 'None';
    const rank = { Error: 3, Warning: 2, Info: 1, None: 0 };
    if (rank[next] > rank[prev]) severityByGroup.set(issue.groupId, next);
  }

  return (
    <Paper
      variant="outlined"
      sx={{ p: 2 }}
      data-testid="norm-analysis-timeline"
      role="region"
      aria-label="Таймлайн периодов норм по направлениям IIC"
    >
      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap>
        <Typography variant="subtitle1" component="h3">
          Таймлайн действия норм по группам IIC
        </Typography>
        <Stack direction="row" spacing={2} alignItems="center">
          {LEGEND_KEYS.map((s) => (
            <Stack key={s} direction="row" spacing={0.5} alignItems="center">
              <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: SEVERITY_COLOR[s] }} />
              <Typography variant="caption" color="text.secondary">
                {SEVERITY_LABEL[s]}
              </Typography>
            </Stack>
          ))}
          {axis.todayVisible ? (
            <Stack direction="row" spacing={0.5} alignItems="center">
              <Box sx={{ width: 2, height: 12, bgcolor: 'text.primary' }} />
              <Typography variant="caption" color="text.secondary">
                Сегодня
              </Typography>
            </Stack>
          ) : null}
        </Stack>
      </Stack>
      <Stack spacing={1} sx={{ mt: 2 }}>
        {groups.map((row) => {
          const sevKey = severityByGroup.get(row.groupId) ?? 'None';
          const isSelectedRow = row.bars.some((b) => b.normId === selectedId);
          const segments = layoutSegments(row.bars, axis);
          return (
            <Box
              key={row.groupId}
              data-testid={`norm-timeline-group-${row.groupId}`}
              sx={{
                display: 'grid',
                gridTemplateColumns: GRID_COLUMNS,
                columnGap: 1.5,
                alignItems: 'center',
              }}
            >
              <Typography
                variant="caption"
                sx={{
                  fontWeight: isSelectedRow ? 700 : 600,
                  lineHeight: 1.35,
                  pr: 0.5,
                }}
              >
                {row.groupName}
              </Typography>
              <Box
                sx={{
                  position: 'relative',
                  height: 22,
                  bgcolor: 'action.hover',
                  borderRadius: 0.5,
                  overflow: 'hidden',
                }}
              >
                {axis.todayVisible ? (
                  <Box
                    sx={{
                      position: 'absolute',
                      left: `${axis.todayLeftPct}%`,
                      top: 0,
                      bottom: 0,
                      width: '2px',
                      bgcolor: 'text.primary',
                      opacity: 0.55,
                      zIndex: 2,
                      pointerEvents: 'none',
                    }}
                  />
                ) : null}
                {segments.map((seg) => {
                  const isSelected = seg.bar.normId === selectedId;
                  const fromLabel = formatTimelineDate(new Date(seg.bar.effectiveFrom).getTime());
                  const toLabel = seg.bar.effectiveTo
                    ? formatTimelineDate(new Date(seg.bar.effectiveTo).getTime())
                    : 'н.в.';
                  const status = STATUS_LABEL[seg.bar.status] ?? seg.bar.status;
                  const tip =
                    sevKey !== 'None'
                      ? `${SEVERITY_LABEL[sevKey]}: ${seg.bar.label} (${fromLabel} – ${toLabel})`
                      : `${status}, ${fromLabel} – ${toLabel}, без замечаний`;
                  return (
                    <Tooltip key={seg.bar.normId} title={tip}>
                      <Box
                        onClick={() => onSelectNorm?.(seg.bar.normId)}
                        data-testid={`norm-timeline-bar-${seg.bar.normId}`}
                        aria-label={`${row.groupName}: ${status} ${fromLabel} – ${toLabel}`}
                        sx={{
                          position: 'absolute',
                          left: `calc(${seg.leftPct}% + ${SEGMENT_GAP_PX / 2}px)`,
                          width: `max(4px, calc(${seg.widthPct}% - ${SEGMENT_GAP_PX}px))`,
                          top: 2,
                          height: 18,
                          bgcolor: SEVERITY_COLOR[sevKey],
                          opacity: seg.bar.status === 'Historical' ? 0.55 : 0.9,
                          border: isSelected ? '2px solid' : '1px solid',
                          borderColor: isSelected ? 'text.primary' : BLOCK_OUTLINE,
                          borderRadius: 0.5,
                          cursor: onSelectNorm ? 'pointer' : 'default',
                          zIndex: 1,
                          '&:hover': { opacity: 1, filter: 'brightness(1.08)' },
                        }}
                      />
                    </Tooltip>
                  );
                })}
              </Box>
            </Box>
          );
        })}
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: GRID_COLUMNS,
            columnGap: 1.5,
          }}
        >
          <Box />
          <Box sx={{ position: 'relative', minHeight: 18 }}>
            <Typography variant="caption" color="text.secondary">
              {formatTimelineDate(axis.minMs)}
            </Typography>
            {axis.todayVisible ? (
              <Typography
                variant="caption"
                color="text.secondary"
                data-testid="norm-timeline-today"
                sx={{
                  position: 'absolute',
                  left: `${axis.todayLeftPct}%`,
                  transform: 'translateX(-50%)',
                  whiteSpace: 'nowrap',
                }}
              >
                сегодня
              </Typography>
            ) : null}
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ position: 'absolute', right: 0 }}
            >
              {formatTimelineDate(axis.maxMs)}
            </Typography>
          </Box>
        </Box>
      </Stack>
    </Paper>
  );
}
