import { type FormEvent, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  TextField,
  Typography,
} from '@mui/material';
import { changeOwnPassword } from '../../api/usersMeApi';
import { wpColors } from '../../theme/wpColors';

interface ChangePasswordDialogProps {
  open: boolean;
  onClose: () => void;
}

interface FormState {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

const EMPTY_FORM: FormState = {
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
};

function validateForm(form: FormState): string | null {
  if (!form.oldPassword.trim()) return 'Введите текущий пароль';
  if (form.newPassword.length < 8) return 'Новый пароль должен содержать не менее 8 символов';
  if (form.newPassword !== form.confirmPassword) return 'Новый пароль и подтверждение не совпадают';
  if (form.newPassword === form.oldPassword) return 'Новый пароль должен отличаться от текущего';
  return null;
}

export function ChangePasswordDialog({ open, onClose }: ChangePasswordDialogProps) {
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open) {
      setForm(EMPTY_FORM);
      setError(null);
    }
  }, [open]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const validationError = validateForm(form);
    if (validationError) {
      setError(validationError);
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await changeOwnPassword(form.oldPassword, form.newPassword, form.confirmPassword);
      onClose();
    } catch {
      setError('Не удалось сменить пароль. Проверьте текущий пароль.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <Box component="form" onSubmit={handleSubmit}>
        <Box sx={{ bgcolor: wpColors.sidebar, px: 2, py: 1.5 }}>
          <Typography sx={{ color: '#ECF0F1', fontSize: 16, fontWeight: 600 }}>
            Смена пароля
          </Typography>
        </Box>
        <DialogContent>
          <StackFields form={form} setForm={setForm} />
          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={submitting}>
            Отмена
          </Button>
          <Button type="submit" variant="contained" disabled={submitting}>
            {submitting ? 'Сохранение...' : 'Сохранить'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

function StackFields({
  form,
  setForm,
}: {
  form: FormState;
  setForm: React.Dispatch<React.SetStateAction<FormState>>;
}) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
      <TextField
        label="Текущий пароль"
        type="password"
        value={form.oldPassword}
        onChange={(e) => setForm((prev) => ({ ...prev, oldPassword: e.target.value }))}
        required
        fullWidth
        size="small"
        autoComplete="current-password"
      />
      <TextField
        label="Новый пароль"
        type="password"
        value={form.newPassword}
        onChange={(e) => setForm((prev) => ({ ...prev, newPassword: e.target.value }))}
        required
        fullWidth
        size="small"
        autoComplete="new-password"
        helperText="Минимум 8 символов"
      />
      <TextField
        label="Подтверждение пароля"
        type="password"
        value={form.confirmPassword}
        onChange={(e) => setForm((prev) => ({ ...prev, confirmPassword: e.target.value }))}
        required
        fullWidth
        size="small"
        autoComplete="new-password"
      />
    </Box>
  );
}
