import { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Box,
  Alert,
  TextField,
  FormControlLabel,
  Checkbox,
} from '@mui/material';

interface ProjectParametersDialogProps {
  open: boolean;
  projectId: number | null;
  onClose: () => void;
}

export function ProjectParametersDialog({
  open,
  projectId,
  onClose,
}: ProjectParametersDialogProps) {
  const [creating, setCreating] = useState(false);
  const [created, setCreated] = useState(false);
  const [parameterName, setParameterName] = useState('');
  const [includeInReports, setIncludeInReports] = useState(true);

  const handleCreateTable = async () => {
    setCreating(true);
    // TODO: API call to create parameters table
    await new Promise((resolve) => setTimeout(resolve, 500));
    setCreated(true);
    setCreating(false);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Параметры проекта</DialogTitle>
      <DialogContent>
        <Box sx={{ py: 2 }}>
          <Typography variant="body1" gutterBottom>
            Project ID: {projectId}
          </Typography>

          {!created ? (
            <>
              <Alert severity="info" sx={{ mb: 2 }}>
                Создание таблицы параметров для проекта. Параметры будут храниться
                в отдельной таблице базы данных.
              </Alert>

              <TextField
                fullWidth
                label="Название параметра"
                value={parameterName}
                onChange={(e) => setParameterName(e.target.value)}
                sx={{ mb: 2 }}
              />

              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeInReports}
                    onChange={(e) => setIncludeInReports(e.target.checked)}
                  />
                }
                label="Включать в отчеты"
              />
            </>
          ) : (
            <Alert severity="success">
              Таблица параметров успешно создана для проекта!
            </Alert>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>
          {created ? 'Закрыть' : 'Отмена'}
        </Button>
        {!created && (
          <Button
            variant="contained"
            onClick={handleCreateTable}
            disabled={creating || !parameterName.trim()}
          >
            {creating ? 'Создание...' : 'Создать таблицу'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
