import { Box, Card, CardContent, Grid, Stack, Typography } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import RemoveIcon from '@mui/icons-material/Remove';
import type { ProjectVersionAnalysisSummary } from '../../../../api/analyticsApi';
import { fmt, fmtSigned, getInterpretation, INTERPRETATION_COLOR, INTERPRETATION_RU, MUTED_COLOR } from '../reportCalc';

interface SummaryCardsProps {
  summary: ProjectVersionAnalysisSummary;
  previousZScore: number | null;
  accuracyScore: number | null;
}

export function SummaryCards({ summary, previousZScore, accuracyScore }: SummaryCardsProps) {
  const z = summary.weightedZScore;
  const interpretation = getInterpretation(z);
  const color = interpretation ? INTERPRETATION_COLOR[interpretation] : MUTED_COLOR;

  const delta = z !== null && previousZScore !== null ? z - previousZScore : null;
  const isFlat = delta === null || Math.abs(delta) < 0.005;
  // Меньший Z = лучше, поэтому отрицательная дельта — улучшение.
  const trendColor = isFlat ? MUTED_COLOR : delta! < 0 ? INTERPRETATION_COLOR.Excellent : INTERPRETATION_COLOR.Critical;
  const TrendIcon = isFlat ? RemoveIcon : delta! < 0 ? ArrowDownwardIcon : ArrowUpwardIcon;

  const totalFactLaborHours = summary.totalFactLaborHours ?? 0;
  const unallocatedLaborHours = summary.unallocatedLaborHours ?? 0;
  const unallocatedShare = totalFactLaborHours > 0 ? (unallocatedLaborHours / totalFactLaborHours) * 100 : 0;

  const cards = [
    {
      label: 'Средневзвешенная Z-оценка',
      value: fmtSigned(z),
      valueColor: color,
      footer: (
        <Stack spacing={0.5}>
          <Typography variant="body2" sx={{ color }}>
            {interpretation ? INTERPRETATION_RU[interpretation] : 'Нет данных'}
          </Typography>
          {previousZScore !== null && (
            <Stack direction="row" spacing={0.5} alignItems="center">
              <TrendIcon sx={{ fontSize: 14, color: trendColor }} />
              <Typography variant="caption" sx={{ color: trendColor }}>
                {fmtSigned(delta)} к предыдущей версии
              </Typography>
            </Stack>
          )}
        </Stack>
      ),
      borderColor: color,
    },
    {
      label: 'Перцентиль производительности',
      value: summary.weightedZScorePercentile || 'Н/Д',
      footer: (
        <Typography variant="body2" color="text.secondary">
          Положение проекта на нормативной шкале ИИЦ
        </Typography>
      ),
    },
    {
      label: 'Итого трудоёмкость факта',
      value: (
        <>
          {fmt(summary.totalFactLaborHours, 0)}
          <Typography component="span" variant="body1" sx={{ ml: 0.5, fontWeight: 400, color: 'text.secondary' }}>
            ч
          </Typography>
        </>
      ),
      footer: (
        <Typography variant="body2" color="text.secondary">
          Не отнесено к направлениям: {fmt(summary.unallocatedLaborHours, 0)} ч ({fmt(unallocatedShare, 1)}%)
        </Typography>
      ),
    },
    {
      label: 'Точность сравнения',
      value:
        accuracyScore === null ? (
          'Н/Д'
        ) : (
          <>
            {fmt(accuracyScore, 1)}
            <Typography component="span" variant="body1" sx={{ ml: 0.5, fontWeight: 400, color: 'text.secondary' }}>
              %
            </Typography>
          </>
        ),
      footer: (
        <Typography variant="body2" color="text.secondary">
          Средняя PD% по характеристикам проекта
        </Typography>
      ),
    },
  ];

  return (
    <Grid container spacing={2}>
      {cards.map((card) => (
        <Grid item xs={12} sm={6} xl={3} key={card.label}>
          <Card
            variant="outlined"
            sx={card.borderColor ? { borderLeft: 4, borderLeftColor: card.borderColor } : undefined}
          >
            <CardContent>
              <Stack spacing={1}>
                <Typography
                  variant="caption"
                  sx={{ color: 'text.secondary', letterSpacing: 0.4, textTransform: 'uppercase', fontWeight: 600 }}
                >
                  {card.label}
                </Typography>
                <Box
                  sx={{
                    fontFamily: 'monospace',
                    fontSize: 28,
                    fontWeight: 600,
                    lineHeight: 1,
                    color: card.valueColor,
                  }}
                >
                  {card.value}
                </Box>
                {card.footer}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}
