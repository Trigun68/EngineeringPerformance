import { Box, Typography } from '@mui/material';

interface ReportsTabProps {
  projectId: number;
  versionId: number;
}

export function ReportsTab({ projectId, versionId }: ReportsTabProps) {
  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Отчеты
      </Typography>
      <Typography variant="body2" color="text.secondary">
        Отчеты для проекта {projectId}, версия {versionId}
      </Typography>
    </Box>
  );
}
