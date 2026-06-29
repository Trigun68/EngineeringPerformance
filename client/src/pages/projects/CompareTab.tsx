import { Box, Typography } from '@mui/material';

interface CompareTabProps {
  projectId: number;
}

export function CompareTab({ projectId }: CompareTabProps) {
  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Сравнение версий
      </Typography>
      <Typography variant="body2" color="text.secondary">
        Сравнение версий проекта {projectId}
      </Typography>
    </Box>
  );
}
