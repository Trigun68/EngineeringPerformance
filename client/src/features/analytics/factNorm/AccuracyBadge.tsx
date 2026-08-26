import { Chip, Tooltip } from '@mui/material';
import type { Accuracy, AccuracyLevel } from '../../../api/factNormAnalyticsApi';

const LABELS: Record<AccuracyLevel, string> = {
  Low: 'Низкая',
  Medium: 'Средняя',
  High: 'Высокая',
};

const COLORS: Record<AccuracyLevel, 'error' | 'warning' | 'success'> = {
  Low: 'error',
  Medium: 'warning',
  High: 'success',
};

const ICONS: Record<AccuracyLevel, string> = {
  Low: '●',
  Medium: '●',
  High: '●',
};

interface Props {
  accuracy: Accuracy | null;
}

export function AccuracyBadge({ accuracy }: Props) {
  if (!accuracy) return null;
  const label = `${ICONS[accuracy.level]} ${LABELS[accuracy.level]}`;
  const tip =
    accuracy.hint ??
    `Точек: ${accuracy.dataPointsCount}, дней: ${accuracy.periodLengthDays}`;
  return (
    <Tooltip title={tip}>
      <Chip
        size="small"
        color={COLORS[accuracy.level]}
        label={`Точность: ${label}`}
        data-testid="fact-norm-accuracy-badge"
        aria-label={`Точность анализа: ${LABELS[accuracy.level]}`}
      />
    </Tooltip>
  );
}
