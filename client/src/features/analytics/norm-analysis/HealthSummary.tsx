import { useMemo, useState } from 'react';
import { Box, Chip, Popover, Stack, Typography } from '@mui/material';
import type { HealthSummary, NormHealthIssue } from '../../../api/normRegistryApi';

interface Props {
  summary: HealthSummary | null;
  loading?: boolean;
}

function IssueList({ issues }: { issues: NormHealthIssue[] }) {
  if (issues.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        Нет замечаний этого уровня.
      </Typography>
    );
  }
  return (
    <Stack spacing={1.5} sx={{ maxWidth: 420, maxHeight: 320, overflow: 'auto', p: 0.5 }}>
      {issues.map((i) => (
        <Box key={i.id}>
          <Typography variant="subtitle2">
            [{i.ruleCode}] {i.message}
          </Typography>
          {i.recommendation ? (
            <Typography variant="body2" color="text.secondary">
              Что делать: {i.recommendation}
            </Typography>
          ) : null}
        </Box>
      ))}
    </Stack>
  );
}

export function HealthSummaryBadges({ summary, loading }: Props) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const [filter, setFilter] = useState<'Error' | 'Warning' | 'Info' | null>(null);

  const filtered = useMemo(() => {
    if (!summary || !filter) return [];
    return summary.issues.filter((i) => i.severity === filter);
  }, [summary, filter]);

  if (loading && !summary) {
    return (
      <Typography variant="body2" color="text.secondary" data-testid="norm-analysis-health-loading">
        Проверка здоровья…
      </Typography>
    );
  }

  if (!summary) return null;

  const open = (el: HTMLElement, sev: 'Error' | 'Warning' | 'Info') => {
    setAnchor(el);
    setFilter(sev);
  };

  return (
    <>
      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap data-testid="norm-analysis-health-summary">
        <Chip
          icon={<span aria-hidden>⚠</span>}
          label={`Ошибки: ${summary.errorCount}`}
          color={summary.errorCount > 0 ? 'error' : 'default'}
          variant={summary.errorCount > 0 ? 'filled' : 'outlined'}
          data-testid="norm-analysis-badge-errors"
          onClick={(e) => open(e.currentTarget, 'Error')}
        />
        <Chip
          icon={<span aria-hidden>!</span>}
          label={`Предупреждения: ${summary.warningCount}`}
          color={summary.warningCount > 0 ? 'warning' : 'default'}
          variant={summary.warningCount > 0 ? 'filled' : 'outlined'}
          data-testid="norm-analysis-badge-warnings"
          onClick={(e) => open(e.currentTarget, 'Warning')}
        />
        <Chip
          icon={<span aria-hidden>i</span>}
          label={`Инфо: ${summary.infoCount}`}
          color={summary.infoCount > 0 ? 'info' : 'default'}
          variant="outlined"
          data-testid="norm-analysis-badge-info"
          onClick={(e) => open(e.currentTarget, 'Info')}
        />
      </Stack>
      <Popover
        open={Boolean(anchor)}
        anchorEl={anchor}
        onClose={() => {
          setAnchor(null);
          setFilter(null);
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        slotProps={{ paper: { sx: { p: 1.5 } } }}
        data-testid="norm-analysis-health-popover"
      >
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          {filter === 'Error'
            ? 'Ошибки'
            : filter === 'Warning'
              ? 'Предупреждения'
              : 'Информация'}
        </Typography>
        <IssueList issues={filtered} />
      </Popover>
    </>
  );
}
