import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  TextField,
  Typography,
} from '@mui/material';
import { fetchProjects } from '../../../api/projectsApi';
import type { Project } from '../../../types/project';

interface Props {
  open: boolean;
  selectedIds: number[] | null;
  onClose: () => void;
  onApply: (projectIds: number[] | null) => void;
}

export function ProjectPickerDialog({ open, selectedIds, onClose, onApply }: Props) {
  const [projects, setProjects] = useState<Project[]>([]);
  const [filter, setFilter] = useState('');
  const [localSelected, setLocalSelected] = useState<number[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setLocalSelected(selectedIds ?? []);
    setFilter('');
    setLoading(true);
    void fetchProjects(false)
      .then(setProjects)
      .finally(() => setLoading(false));
  }, [open, selectedIds]);

  const filtered = useMemo(() => {
    const q = filter.trim().toLowerCase();
    if (!q) return projects;
    return projects.filter(
      (p) =>
        p.name.toLowerCase().includes(q) ||
        p.code.toLowerCase().includes(q) ||
        (p.workNumber ?? '').toLowerCase().includes(q),
    );
  }, [projects, filter]);

  const toggle = (id: number) => {
    setLocalSelected((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm" data-testid="project-picker-dialog">
      <DialogTitle>Выбор проектов</DialogTitle>
      <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        <TextField
          autoFocus
          label="Фильтр"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Начните вводить название или шифр…"
          size="small"
          fullWidth
          inputProps={{ 'data-testid': 'project-picker-filter' }}
        />
        <Typography variant="body2" color="text.secondary">
          {loading
            ? 'Загрузка…'
            : `Показано ${filtered.length} из ${projects.length}. Пустой выбор = все проекты за период.`}
        </Typography>
        <List dense sx={{ maxHeight: 360, overflow: 'auto', border: 1, borderColor: 'divider' }}>
          {filtered.map((p) => (
            <ListItem key={p.id} disablePadding>
              <ListItemButton onClick={() => toggle(p.id)} dense>
                <ListItemIcon sx={{ minWidth: 36 }}>
                  <Checkbox
                    edge="start"
                    checked={localSelected.includes(p.id)}
                    tabIndex={-1}
                    disableRipple
                  />
                </ListItemIcon>
                <ListItemText primary={`${p.code} — ${p.name}`} secondary={p.workNumber ?? undefined} />
              </ListItemButton>
            </ListItem>
          ))}
        </List>
      </DialogContent>
      <DialogActions>
        <Button
          onClick={() => {
            setLocalSelected([]);
            onApply(null);
            onClose();
          }}
          data-testid="project-picker-all"
        >
          Все проекты за период
        </Button>
        <Button onClick={onClose}>Отмена</Button>
        <Button
          variant="contained"
          onClick={() => {
            onApply(localSelected.length === 0 ? null : localSelected);
            onClose();
          }}
          data-testid="project-picker-apply"
        >
          Применить
        </Button>
      </DialogActions>
    </Dialog>
  );
}
