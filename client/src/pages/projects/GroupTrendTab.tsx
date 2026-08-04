import { Link } from 'react-router-dom';
import { Alert, Box, Button, Typography } from '@mui/material';

export function GroupTrendTab({ projectId }: { projectId: number }) {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h6" sx={{ mb: 1 }}>Тренд по группам</Typography>
      <Alert severity="info" sx={{ mb: 2 }}>
        Трендовый анализ перенесён на отдельную страницу аналитики с графиками факта, прогноза и накопительного итога.
      </Alert>
      <Button component={Link} to={`/analytics?projectId=${projectId}`} variant="contained">
        Открыть трендовый анализ
      </Button>
    </Box>
  );
}
