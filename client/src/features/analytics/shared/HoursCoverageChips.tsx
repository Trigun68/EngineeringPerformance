import { Chip, Stack, Typography } from '@mui/material';
import type { HoursCoverageDto } from '../../../api/projectTrendApi';

function pct(share: number | null): string {
  if (share == null) return '—';
  return `${Math.round(share * 100)}%`;
}

interface Props {
  coverage: HoursCoverageDto;
  testId?: string;
}

/** Чипы покрытия часов: доли считает API/движок. */
export function HoursCoverageChips({ coverage, testId }: Props) {
  const lowPair = (coverage.shareWithBridgePair ?? 0) === 0 && coverage.hoursEligibleForBridge === 0;
  return (
    <Stack spacing={0.5} data-testid={testId}>
      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        <Chip
          size="small"
          variant="outlined"
          label={`Z известен: ${pct(coverage.shareWithZ)} часов`}
          data-testid="trend-coverage-z"
        />
        <Chip
          size="small"
          variant="outlined"
          label={`Пара план/факт: ${pct(coverage.shareWithBridgePair)} часов`}
          data-testid="trend-coverage-pair"
        />
      </Stack>
      {lowPair ? (
        <Typography variant="caption" color="text.secondary" data-testid="trend-coverage-hint">
          Нет строк с объёмом и часами для моста — часто трудоёмкость без направления IIC.
        </Typography>
      ) : null}
    </Stack>
  );
}
