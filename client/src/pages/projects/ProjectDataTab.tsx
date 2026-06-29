import { Box, Typography, Button, Stack } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';

interface ProjectDataTabProps {
  projectId: number;
  versionId: number;
}

export function ProjectDataTab({ projectId, versionId }: ProjectDataTabProps) {
  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Данные проекта
      </Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
        <Button variant="outlined" startIcon={<AddIcon />}>
          Добавить физ. объемы
        </Button>
        <Button variant="outlined" startIcon={<AddIcon />}>
          Добавить оборудование
        </Button>
      </Stack>

      <Typography variant="body2" color="text.secondary">
        Управление данными проекта {projectId}, версия {versionId}
      </Typography>
    </Box>
  );
}
