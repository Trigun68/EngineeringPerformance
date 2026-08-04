import { Chip, Stack, Typography } from '@mui/material';
import type { HealthSummary } from '../../../api/normRegistryApi';

interface Props {
  summary: HealthSummary | null;
  loading?: boolean;
}

export function HealthSummaryBadges({ summary, loading }: Props) {
  if (loading && !summary) {
    return (
      <Typography variant="body2" color="text.secondary" data-testid="norm-analysis-health-loading">
        Проверка здоровья…
      </Typography>
    );
  }

  if (!summary) return null;

  return (
    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap data-testid="norm-analysis-health-summary">
      <Chip
        icon={<span aria-hidden>⚠</span>}
        label={`Ошибки: ${summary.errorCount}`}
        color={summary.errorCount > 0 ? 'error' : 'default'}
        variant={summary.errorCount > 0 ? 'filled' : 'outlined'}
        data-testid="norm-analysis-badge-errors"
      />
      <Chip
        icon={<span aria-hidden>!</span>}
        label={`Предупреждения: ${summary.warningCount}`}
        color={summary.warningCount > 0 ? 'warning' : 'default'}
        variant={summary.warningCount > 0 ? 'filled' : 'outlined'}
        data-testid="norm-analysis-badge-warnings"
      />
      <Chip
        icon={<span aria-hidden>i</span>}
        label={`Инфо: ${summary.infoCount}`}
        color={summary.infoCount > 0 ? 'info' : 'default'}
        variant="outlined"
        data-testid="norm-analysis-badge-info"
      />
    </Stack>
  );
}
