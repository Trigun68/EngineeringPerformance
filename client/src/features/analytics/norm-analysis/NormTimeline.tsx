import { useMemo } from 'react';
import { Box, Typography } from '@mui/material';
import type { TimelineResponse } from '../../../api/normRegistryApi';

interface Props {
  timeline: TimelineResponse | null;
  onSelectNorm?: (normId: number) => void;
}

export function NormTimeline({ timeline, onSelectNorm }: Props) {
  const { minMs, maxMs, rows } = useMemo(() => {
    const bars = timeline?.bars ?? [];
    if (bars.length === 0) {
      return { minMs: 0, maxMs: 1, rows: [] as typeof bars };
    }
    const today = Date.now();
    let min = Infinity;
    let max = -Infinity;
    for (const b of bars) {
      const from = new Date(b.effectiveFrom).getTime();
      const to = b.effectiveTo ? new Date(b.effectiveTo).getTime() : today;
      min = Math.min(min, from);
      max = Math.max(max, to);
    }
    if (!Number.isFinite(min) || !Number.isFinite(max) || max <= min) {
      max = min + 86400000;
    }
    return { minMs: min, maxMs: max, rows: bars };
  }, [timeline]);

  if (!timeline || rows.length === 0) {
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

  const span = maxMs - minMs;
  const byGroup = new Map<number, typeof rows>();
  for (const bar of rows) {
    const list = byGroup.get(bar.groupId) ?? [];
    list.push(bar);
    byGroup.set(bar.groupId, list);
  }

  return (
    <Box data-testid="norm-analysis-timeline" role="region" aria-label="Таймлайн периодов норм по направлениям IIC">
      <Typography variant="subtitle1" component="h3" sx={{ mb: 0.5 }}>
        Таймлайн действия норм по группам IIC
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        Горизонтальные полосы — периоды EffectiveFrom–EffectiveTo. Красная обводка означает наличие
        health-issue.
      </Typography>
      <Box
        sx={{
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1,
          p: 1.5,
        }}
      >
      {[...byGroup.entries()].map(([groupId, bars]) => {
        const name = bars[0]?.groupName ?? `#${groupId}`;
        return (
          <Box key={groupId} sx={{ mb: 1.5 }}>
            <Typography variant="caption" sx={{ fontWeight: 600, display: 'block', mb: 0.5 }}>
              {name}
            </Typography>
            <Box sx={{ position: 'relative', height: 28, bgcolor: 'action.hover', borderRadius: 0.5 }}>
              {bars.map((bar) => {
                const from = new Date(bar.effectiveFrom).getTime();
                const to = bar.effectiveTo ? new Date(bar.effectiveTo).getTime() : Date.now();
                const left = ((from - minMs) / span) * 100;
                const width = Math.max(0.8, ((to - from) / span) * 100);
                return (
                  <Box
                    key={bar.normId}
                    title={`${bar.label} (${bar.status})`}
                    onClick={() => onSelectNorm?.(bar.normId)}
                    sx={{
                      position: 'absolute',
                      left: `${left}%`,
                      width: `${width}%`,
                      top: 4,
                      height: 20,
                      bgcolor: bar.color,
                      opacity: bar.hasIssue ? 1 : 0.85,
                      border: bar.hasIssue ? '2px solid #b91c1c' : '1px solid transparent',
                      borderRadius: 0.5,
                      cursor: onSelectNorm ? 'pointer' : 'default',
                      '&:hover': { opacity: 1, filter: 'brightness(1.05)' },
                    }}
                    data-testid={`norm-timeline-bar-${bar.normId}`}
                  />
                );
              })}
            </Box>
          </Box>
        );
      })}
      </Box>
    </Box>
  );
}
