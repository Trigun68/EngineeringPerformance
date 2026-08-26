import { Link } from 'react-router-dom';
import { Alert, Box, Button, Typography } from '@mui/material';

export function GroupTrendTab({ projectId }: { projectId: number }) {
  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h6" sx={{ mb: 1 }}>Тренд по группам</Typography>
      <Alert severity="info" sx={{ mb: 2 }}>
        Тренд оценки факта (Z) и тренд производительности нормы вынесены на страницу «Аналитика».
      </Alert>
      <Button component={Link} to={`/analytics?projectId=${projectId}`} variant="contained">
        Открыть аналитику
      </Button>
    </Box>
  );
}
