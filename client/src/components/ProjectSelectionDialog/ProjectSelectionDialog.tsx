import { useMemo, useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  TextField,
  Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import AddIcon from '@mui/icons-material/Add';
import type { Project } from '../../types/project';

interface ProjectSelectionDialogProps {
  open: boolean;
  projects: Project[];
  onSelect: (projectId: number) => void;
  onClose: () => void;
  onCreateNew: () => void;
  onEditSelected?: (project: Project) => void;
  onCopySelected?: (project: Project) => void;
}

export function ProjectSelectionDialog({
  open,
  projects,
  onSelect,
  onClose,
  onCreateNew,
  onEditSelected,
  onCopySelected,
}: ProjectSelectionDialogProps) {
  const [search, setSearch] = useState('');
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const filteredProjects = useMemo(() => {
    if (!search.trim()) return projects;
    const query = search.toLowerCase();
    return projects.filter((p) =>
      [
        p.code,
        p.name,
        p.description,
        p.workNumber,
        p.contractNumber,
        p.customer,
        p.contractor,
        p.gip,
        p.workStatus,
        p.program,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(query),
    );
  }, [projects, search]);

  const confirmProject = (projectId: number) => {
    onSelect(projectId);
    setSelectedId(null);
    setSearch('');
  };

  const handleSelect = () => {
    if (selectedId) confirmProject(selectedId);
  };

  const handleClose = () => {
    setSelectedId(null);
    setSearch('');
    onClose();
  };

  const selectedProject = projects.find((p) => p.id === selectedId);

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Выбор проекта</Typography>
          <IconButton onClick={handleClose} size="small">
            <CloseIcon />
          </IconButton>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Box sx={{ mb: 2 }}>
          <TextField
            fullWidth
            size="small"
            placeholder="Поиск проекта по названию или коду..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </Box>

        <Box sx={{ display: 'flex', gap: 2, height: 400 }}>
          {/* Project List */}
          <Box sx={{ flex: 1, border: 1, borderColor: 'divider', borderRadius: 1, overflow: 'auto' }}>
            <List dense sx={{ p: 0 }}>
              {filteredProjects.map((project) => (
                <ListItem key={project.id} disablePadding>
                  <ListItemButton
                    selected={selectedId === project.id}
                    onClick={() => setSelectedId(project.id)}
                    onDoubleClick={() => confirmProject(project.id)}
                  >
                    <ListItemText
                      primary={`${project.code} — ${project.name}`}
                      secondary={project.description}
                    />
                  </ListItemButton>
                </ListItem>
              ))}
              {filteredProjects.length === 0 && (
                <ListItem>
                  <ListItemText
                    primary="Нет проектов"
                    secondary={search ? 'Попробуйте изменить поисковый запрос' : 'Создайте новый проект'}
                  />
                </ListItem>
              )}
            </List>
          </Box>

          {/* Selected Project Details */}
          <Box sx={{ width: 280, border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
            {selectedProject ? (
              <>
                <Typography variant="subtitle2" gutterBottom>
                  Выбранный проект
                </Typography>
                <Typography variant="body1" gutterBottom>
                  {selectedProject.code}
                </Typography>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  {selectedProject.name}
                </Typography>
                {selectedProject.description && (
                  <Typography variant="caption" color="text.secondary">
                    {selectedProject.description}
                  </Typography>
                )}
              </>
            ) : (
              <Typography variant="body2" color="text.secondary" align="center" sx={{ mt: 4 }}>
                Выберите проект из списка
              </Typography>
            )}
          </Box>
        </Box>
      </DialogContent>

      <DialogActions>
        <Button onClick={onCreateNew} startIcon={<AddIcon />}>
          Создать новый
        </Button>
        <Button
          onClick={() => selectedProject && onEditSelected?.(selectedProject)}
          disabled={!selectedProject || !onEditSelected}
        >
          Редактировать
        </Button>
        <Button
          onClick={() => selectedProject && onCopySelected?.(selectedProject)}
          disabled={!selectedProject || !onCopySelected}
        >
          Копировать
        </Button>
        <Box sx={{ flex: 1 }} />
        <Button onClick={handleClose}>Отмена</Button>
        <Button
          variant="contained"
          onClick={handleSelect}
          disabled={!selectedId}
        >
          Выбрать
        </Button>
      </DialogActions>
    </Dialog>
  );
}
