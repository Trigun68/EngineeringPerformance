import { Alert, Box, Typography } from '@mui/material';

interface ForecastTabProps {
  projectId: number;
  versionId: number | null;
}

export function ForecastTab({ projectId, versionId }: ForecastTabProps) {
  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Прогнозирование
      </Typography>
      {versionId ? (
        <Typography variant="body2" color="text.secondary">
          Прогнозирование для проекта {projectId}, версия {versionId}
        </Typography>
      ) : (
        <Alert severity="warning">Выберите версию анализа</Alert>
      )}
    </Box>
  );
}
