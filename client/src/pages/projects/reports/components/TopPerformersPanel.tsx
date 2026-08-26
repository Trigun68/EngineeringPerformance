import { Box, Card, CardContent, Stack, Typography } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import type { PerformanceTreeRow } from '../reportCalc';
import { fmt, fmtSigned, interpretationColor } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

const TOP_COUNT = 5;

interface TopPerformersPanelProps {
  leaves: PerformanceTreeRow[];
}

interface RankedRow {
  row: PerformanceTreeRow;
  rank: number;
}

function rank(rows: PerformanceTreeRow[]): RankedRow[] {
  return rows.map((row, index) => ({ row, rank: index + 1 }));
}

function RankedList({
  title,
  description,
  icon,
  accentColor,
  rows,
  emptyLabel,
}: {
  title: string;
  description: string;
  icon: React.ReactNode;
  accentColor: string;
  rows: RankedRow[];
  emptyLabel: string;
}) {
  return (
    <Card variant="outlined" sx={{ height: '100%', borderLeft: 4, borderLeftColor: accentColor }}>
      <CardContent sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
          {icon}
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            {title}
          </Typography>
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {description}
        </Typography>

        {rows.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {emptyLabel}
          </Typography>
        ) : (
          <Stack spacing={1} sx={{ flex: 1 }}>
            {rows.map(({ row, rank: position }) => {
              const color = interpretationColor(row.zScore);
              return (
                <Stack
                  key={row.workDirectionId}
                  direction="row"
                  spacing={1.5}
                  alignItems="center"
                  sx={{
                    py: 0.75,
                    px: 1,
                    borderRadius: 1,
                    bgcolor: wpColors.pageBg,
                  }}
                >
                  <Box
                    sx={{
                      width: 22,
                      height: 22,
                      borderRadius: '50%',
                      bgcolor: color,
                      color: '#FFFFFF',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: 11,
                      fontWeight: 700,
                      flexShrink: 0,
                    }}
                  >
                    {position}
                  </Box>
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="body2" noWrap title={row.workDirectionName} sx={{ fontWeight: 500 }}>
                      {row.workDirectionName}
                    </Typography>
                    <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.muted }}>
                      {row.workDirectionCode} · {fmt(row.subtreeHours ?? row.factLaborHours, 0)} ч
                    </Typography>
                  </Box>
                  <Typography
                    variant="body2"
                    sx={{ flexShrink: 0, textAlign: 'right', fontFamily: 'monospace', fontWeight: 700, color }}
                  >
                    {fmtSigned(row.zScore)}
                  </Typography>
                </Stack>
              );
            })}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Компактный ориентир перед подробной диаграммой DirectionBars: явный топ-5 лучших
 * (наименьший Z — опережение нормы) и топ-5 худших (наибольший Z — перерасход) листовых
 * направлений работ (групп физобъёмов ИИЦ), отсортированных по Z-оценке.
 */
export function TopPerformersPanel({ leaves }: TopPerformersPanelProps) {
  const scored = leaves.filter((r) => r.zScore !== null);
  const best = rank([...scored].sort((a, b) => a.zScore! - b.zScore!).slice(0, TOP_COUNT));
  const worst = rank(
    [...scored]
      .sort((a, b) => b.zScore! - a.zScore!)
      .slice(0, TOP_COUNT)
      .filter((r) => !best.some((b) => b.row.workDirectionId === r.workDirectionId)),
  );

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2 }}>
      <RankedList
        title="Топ-5 лучших направлений"
        description="Наибольшее опережение нормы ИИЦ — наименьшая Z-оценка."
        icon={<ArrowDownwardIcon sx={{ fontSize: 18, color: '#1E8449' }} />}
        accentColor="#1E8449"
        rows={best}
        emptyLabel="Недостаточно данных для оценки."
      />
      <RankedList
        title="Топ-5 худших направлений"
        description="Наибольший перерасход трудозатрат относительно нормы ИИЦ."
        icon={<ArrowUpwardIcon sx={{ fontSize: 18, color: '#B5544C' }} />}
        accentColor="#B5544C"
        rows={worst}
        emptyLabel="Недостаточно данных для оценки."
      />
    </Box>
  );
}
