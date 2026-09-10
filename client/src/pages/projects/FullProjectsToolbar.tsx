import {
  Alert,
  Box,
  Button,
  IconButton,
  Paper,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import type { Project } from '../../types/project';
import { WorkspaceCompactBar } from './WorkspaceCompactBar';

export interface FullProjectsToolbarProps {
  headerExpanded: boolean;
  hasProject: boolean;
  resolvedProject: Project | null;
  deviationPercent: number | null;
  workspaceLoading: boolean;
  currentProjectId: number | null;
  workspaceError: boolean;
  canEdit: boolean;
  error: string | null;
  refreshDisabled: boolean;
  activeMainTabLabel: string;
  onSelectProject: () => void;
  onEditProject: () => void;
  onCharacteristics: () => void;
  onParticipants: () => void;
  onRefresh: () => void;
}

export function FullProjectsToolbar({
  headerExpanded,
  hasProject,
  resolvedProject,
  deviationPercent,
  workspaceLoading,
  currentProjectId,
  workspaceError,
  canEdit,
  error,
  refreshDisabled,
  activeMainTabLabel,
  onSelectProject,
  onEditProject,
  onCharacteristics,
  onParticipants,
  onRefresh,
}: FullProjectsToolbarProps) {
  if (!headerExpanded) {
    return (
      <WorkspaceCompactBar
        projectName={
          hasProject
            ? (resolvedProject!.code
              ? `${resolvedProject!.code} — ${resolvedProject!.name}`
              : resolvedProject!.name)
            : null
        }
        tabLabel={activeMainTabLabel}
      />
    );
  }

  return (
    <Paper elevation={1} sx={{ p: { xs: 1.5, md: 2 }, mb: 0.5, backgroundColor: '#F8F9FA' }}>
      <Box sx={{ mb: 2 }}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Button
            variant="contained"
            size="small"
            onClick={onSelectProject}
            sx={{
              backgroundColor: '#3498DB',
              color: 'white',
              '&:hover': { backgroundColor: '#2980B9' },
            }}
          >
            Выбрать проект
          </Button>

          {hasProject ? (
            <>
              <Box
                sx={{
                  backgroundColor: '#2C3E50',
                  color: 'white',
                  px: 1.5,
                  py: 0.5,
                  borderRadius: 1,
                  fontWeight: 'bold',
                  fontSize: '0.875rem',
                }}
              >
                {resolvedProject!.code}
              </Box>
              <Typography
                variant="h6"
                sx={{
                  color: '#2C3E50',
                  fontWeight: 500,
                  fontSize: { xs: '1rem', md: '1.25rem' },
                  maxWidth: { xs: '100%', md: 'none' },
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                }}
              >
                {resolvedProject!.name}
              </Typography>
              {deviationPercent != null && (
                <Box
                  sx={{
                    backgroundColor: '#FDFEFE',
                    border: '1px solid #D5D8DC',
                    px: 1.5,
                    py: 0.5,
                    borderRadius: 1,
                  }}
                >
                  <Typography variant="body2" sx={{ color: '#616A6B' }}>
                    Отклонение: <strong>{deviationPercent.toFixed(2)}%</strong>
                  </Typography>
                </Box>
              )}
            </>
          ) : workspaceLoading && currentProjectId ? (
            <Typography variant="body1" color="text.secondary">
              Загрузка проекта…
            </Typography>
          ) : currentProjectId && workspaceError ? (
            <Typography variant="body1" color="error">
              Не удалось загрузить проект
            </Typography>
          ) : (
            <Typography variant="body2" sx={{ color: '#95A5A6' }}>
              Проект не выбран
            </Typography>
          )}
        </Stack>
      </Box>

      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        <Button
          variant="outlined"
          size="small"
          disabled={!hasProject || !canEdit}
          onClick={onEditProject}
        >
          Основные данные
        </Button>
        <Button
          variant="outlined"
          size="small"
          disabled={!hasProject}
          onClick={onCharacteristics}
        >
          Характеристики
        </Button>
        <Button
          variant="outlined"
          size="small"
          disabled={!hasProject}
          onClick={onParticipants}
        >
          Участники
        </Button>
        <Tooltip title="Обновить">
          <IconButton onClick={onRefresh} disabled={refreshDisabled} size="small">
            <RefreshIcon />
          </IconButton>
        </Tooltip>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mt: 2 }} onClose={() => {}}>
          {error}
        </Alert>
      )}
    </Paper>
  );
}
