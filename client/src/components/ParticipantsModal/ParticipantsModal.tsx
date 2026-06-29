import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import {
  fetchProjectPermissions,
  saveProjectPermissions,
  type ProjectPermission,
} from '../../api/projectPermissionsApi';
import { fetchUsersLookup, type UserLookup } from '../../api/usersApi';

interface ParticipantRow {
  userId: number;
  displayName: string;
  username: string;
  roleName: string;
  level: number;
}

export interface ParticipantsModalProps {
  open: boolean;
  projectId: number;
  onClose: () => void;
  onSaved: () => void;
}

export function ParticipantsModal({ open, projectId, onClose, onSaved }: ParticipantsModalProps) {
  const [rows, setRows] = useState<ParticipantRow[]>([]);
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [addUserId, setAddUserId] = useState<number | ''>('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      const [permissions, lookup] = await Promise.all([
        fetchProjectPermissions(projectId),
        fetchUsersLookup(),
      ]);
      setUsers(lookup);
      setRows(permissions.map(mapPermission));
    } catch {
      setError('Не удалось загрузить участников.');
    }
  }, [projectId]);

  useEffect(() => {
    if (open) void load();
  }, [open, load]);

  const handleAdd = () => {
    if (!addUserId) return;
    const user = users.find((u) => u.id === addUserId);
    if (!user || rows.some((r) => r.userId === user.id)) return;
    setRows([...rows, {
      userId: user.id,
      displayName: user.displayName,
      username: user.username,
      roleName: user.roleName,
      level: 1,
    }]);
    setAddUserId('');
  };

  const handleRemove = (userId: number) => {
    setRows(rows.filter((r) => r.userId !== userId));
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      await saveProjectPermissions(
        projectId,
        rows.map((r) => ({ userId: r.userId, level: r.level })),
      );
      onSaved();
      onClose();
    } catch {
      setError('Не удалось сохранить участников.');
    } finally {
      setSaving(false);
    }
  };

  const availableUsers = users.filter((u) => !rows.some((r) => r.userId === u.id));

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Участники проекта</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          {rows.map((row) => (
            <Stack key={row.userId} direction="row" spacing={1} alignItems="center">
              <TextField
                label="Пользователь"
                value={`${row.displayName} (${row.username})`}
                fullWidth
                disabled
                size="small"
              />
              <TextField
                select
                label="Уровень"
                value={row.level}
                onChange={(e) => {
                  const level = Number(e.target.value);
                  setRows(rows.map((r) => (r.userId === row.userId ? { ...r, level } : r)));
                }}
                size="small"
                sx={{ minWidth: 120 }}
              >
                <MenuItem value={1}>Чтение</MenuItem>
                <MenuItem value={2}>Редактирование</MenuItem>
              </TextField>
              <Button color="error" onClick={() => handleRemove(row.userId)}>×</Button>
            </Stack>
          ))}
          <Stack direction="row" spacing={1}>
            <TextField
              select
              label="Добавить пользователя"
              value={addUserId}
              onChange={(e) => setAddUserId(Number(e.target.value))}
              fullWidth
              size="small"
            >
              {availableUsers.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.displayName}</MenuItem>
              ))}
            </TextField>
            <Button variant="outlined" onClick={handleAdd} disabled={!addUserId}>+</Button>
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Отмена</Button>
        <Button variant="contained" onClick={() => void handleSave()} disabled={saving}>
          Сохранить
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function mapPermission(p: ProjectPermission): ParticipantRow {
  return {
    userId: p.userId,
    displayName: p.displayName,
    username: p.username,
    roleName: p.roleName,
    level: p.level,
  };
}
