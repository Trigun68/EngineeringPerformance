import { type FormEvent, useEffect, useState } from 'react';
import {
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  FormControlLabel,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import type { AppUser, RoleOption, UserFormData } from '../../types/user';

interface UserEditDialogProps {
  open: boolean;
  title: string;
  roles: RoleOption[];
  initial: UserFormData;
  onClose: () => void;
  onSubmit: (form: UserFormData) => Promise<void>;
  isEditMode: boolean;
}

export function UserEditDialog({
  open,
  title,
  roles,
  initial,
  onClose,
  onSubmit,
  isEditMode,
}: UserEditDialogProps) {
  const [form, setForm] = useState<UserFormData>(initial);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open) {
      setForm(initial);
      setError(null);
    }
  }, [open, initial]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!form.username.trim() || !form.displayName.trim()) {
      setError('Логин и имя обязательны');
      return;
    }
    if (!form.roleId) {
      setError('Выберите роль');
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await onSubmit(form);
      onClose();
    } catch {
      setError('Не удалось сохранить пользователя');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <Box component="form" onSubmit={handleSubmit}>
        <Box sx={{ bgcolor: '#2C3E50', px: 2, py: 1.5 }}>
          <Typography sx={{ color: '#ECF0F1', fontSize: 16, fontWeight: 600 }}>{title}</Typography>
        </Box>
        <DialogContent>
          <TextField
            label="Логин"
            value={form.username}
            onChange={(e) => setForm((prev) => ({ ...prev, username: e.target.value }))}
            required
            fullWidth
            size="small"
            sx={{ mb: 2 }}
          />
          <TextField
            label="Имя"
            value={form.displayName}
            onChange={(e) => setForm((prev) => ({ ...prev, displayName: e.target.value }))}
            required
            fullWidth
            size="small"
            sx={{ mb: 2 }}
          />
          <TextField
            label="Пароль"
            type="password"
            value={form.password}
            onChange={(e) => setForm((prev) => ({ ...prev, password: e.target.value }))}
            fullWidth
            size="small"
            placeholder={isEditMode ? 'Оставьте пустым, чтобы не менять' : ''}
            sx={{ mb: 2 }}
          />
          <TextField
            select
            label="Роль"
            value={form.roleId}
            onChange={(e) => setForm((prev) => ({ ...prev, roleId: Number(e.target.value) }))}
            required
            fullWidth
            size="small"
            sx={{ mb: 2 }}
          >
            {roles.map((role) => (
              <MenuItem key={role.id} value={role.id}>
                {role.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Windows"
            value={form.windowsUsername}
            onChange={(e) => setForm((prev) => ({ ...prev, windowsUsername: e.target.value }))}
            fullWidth
            size="small"
            sx={{ mb: 1 }}
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={form.isActive}
                onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
              />
            }
            label="Активный"
          />
          {error && (
            <Typography color="error" sx={{ mt: 1, fontSize: 14 }}>
              {error}
            </Typography>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={submitting}>
            Отмена
          </Button>
          <Button type="submit" variant="contained" disabled={submitting}>
            Сохранить
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

export function userToForm(user: AppUser): UserFormData {
  return {
    username: user.username,
    displayName: user.displayName,
    password: '',
    roleId: user.roleId,
    windowsUsername: user.windowsUsername ?? '',
    isActive: user.isActive,
  };
}

export function emptyUserForm(defaultRoleId: number | '' = ''): UserFormData {
  return {
    username: '',
    displayName: '',
    password: '',
    roleId: defaultRoleId,
    windowsUsername: '',
    isActive: true,
  };
}
