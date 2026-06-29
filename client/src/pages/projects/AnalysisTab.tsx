import { Box, Typography } from '@mui/material';

interface AnalysisTabProps {
  projectId: number;
  versionId: number;
}

export function AnalysisTab({ projectId, versionId }: AnalysisTabProps) {
  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Анализ факта
      </Typography>
      <Typography variant="body2" color="text.secondary">
        Анализ фактических данных для проекта {projectId}, версия {versionId}
      </Typography>
    </Box>
  );
}
