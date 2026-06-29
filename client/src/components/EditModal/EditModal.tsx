import { type FormEvent, type ReactNode, useEffect, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
} from '@mui/material';

export interface EditModalProps {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
  onSubmit: () => Promise<void> | void;
  submitLabel?: string;
  cancelLabel?: string;
  isSubmitting?: boolean;
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
}

export function EditModal({
  open,
  title,
  children,
  onClose,
  onSubmit,
  submitLabel = 'Сохранить',
  cancelLabel = 'Отмена',
  isSubmitting = false,
  maxWidth = 'sm',
}: EditModalProps) {
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setError(null);
    }
  }, [open]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    try {
      await onSubmit();
    } catch {
      setError('Не удалось сохранить изменения.');
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth={maxWidth}>
      <form onSubmit={handleSubmit}>
        <DialogTitle>{title}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {children}
            {error && (
              <span style={{ color: '#d32f2f', fontSize: '0.875rem' }}>{error}</span>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={isSubmitting}>
            {cancelLabel}
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {isSubmitting ? 'Сохранение...' : submitLabel}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
