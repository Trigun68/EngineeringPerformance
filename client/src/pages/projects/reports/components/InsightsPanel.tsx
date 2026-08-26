import { Box, Card, CardContent, Chip, Grid, Stack, Typography } from '@mui/material';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import StorageIcon from '@mui/icons-material/Storage';
import type { Insight, InsightKind, PerformanceTreeRow } from '../reportCalc';
import { fmt, INTERPRETATION_COLOR, MUTED_COLOR } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

const KIND_META: Record<InsightKind, { icon: typeof WarningAmberIcon; color: string; label: string }> = {
  risk: { icon: ErrorOutlineIcon, color: INTERPRETATION_COLOR.Critical, label: 'Риск' },
  attention: { icon: WarningAmberIcon, color: INTERPRETATION_COLOR.NeedsAttention, label: 'Контроль' },
  strength: { icon: TrendingDownIcon, color: INTERPRETATION_COLOR.Excellent, label: 'Сильная сторона' },
  data: { icon: StorageIcon, color: MUTED_COLOR, label: 'Качество данных' },
};

interface InsightsPanelProps {
  insights: Insight[];
  keyRows: PerformanceTreeRow[];
  totalHours: number;
}

export function InsightsPanel({ insights, keyRows, totalHours }: InsightsPanelProps) {
  return (
    <Grid container spacing={2}>
      <Grid item xs={12} lg={8}>
        <Card variant="outlined" sx={{ height: '100%' }}>
          <CardContent>
            <Stack spacing={0.5} sx={{ mb: 2 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                Выводы и рекомендации
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Сформированы автоматически по строкам производительности текущей версии.
              </Typography>
            </Stack>

            {insights.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                Существенных отклонений не выявлено.
              </Typography>
            ) : (
              <Stack spacing={2.5}>
                {insights.map((insight) => {
                  const meta = KIND_META[insight.kind];
                  const Icon = meta.icon;
                  return (
                    <Stack direction="row" spacing={1.5} key={insight.title}>
                      <Box
                        sx={{
                          width: 32,
                          height: 32,
                          flexShrink: 0,
                          borderRadius: 1,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          bgcolor: wpColors.pageBg,
                          color: meta.color,
                        }}
                      >
                        <Icon fontSize="small" />
                      </Box>
                      <Stack spacing={0.5}>
                        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {insight.title}
                          </Typography>
                          <Chip
                            size="small"
                            variant="outlined"
                            label={meta.label}
                            sx={{ color: meta.color, borderColor: meta.color, fontSize: 11, height: 20 }}
                          />
                        </Stack>
                        <Typography variant="body2" color="text.secondary">
                          {insight.detail}
                        </Typography>
                      </Stack>
                    </Stack>
                  );
                })}
              </Stack>
            )}
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12} lg={4}>
        <Card variant="outlined" sx={{ height: '100%' }}>
          <CardContent>
            <Stack spacing={0.5} sx={{ mb: 2 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                Ключевые дисциплины
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Наибольший вклад в средневзвешенную Z-оценку по часам поддерева.
              </Typography>
            </Stack>

            <Stack spacing={2}>
              {keyRows.map((row) => {
                const share = totalHours > 0 ? ((row.subtreeHours ?? 0) / totalHours) * 100 : 0;
                return (
                  <Stack spacing={0.5} key={row.workDirectionId}>
                    <Stack direction="row" justifyContent="space-between" alignItems="baseline" spacing={1.5}>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {row.workDirectionName}
                      </Typography>
                      <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.muted }}>
                        {fmt(share, 1)}%
                      </Typography>
                    </Stack>
                    <Box sx={{ height: 8, width: '100%', borderRadius: 1, bgcolor: wpColors.pageBg, overflow: 'hidden' }}>
                      <Box
                        sx={{
                          height: '100%',
                          borderRadius: 1,
                          bgcolor: wpColors.accent,
                          width: `${Math.min(share, 100)}%`,
                        }}
                      />
                    </Box>
                    <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.muted }}>
                      {fmt(row.subtreeHours, 0)} ч
                    </Typography>
                  </Stack>
                );
              })}
            </Stack>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
}
