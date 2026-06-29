import { type FormEvent, type ReactNode, useEffect, useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  Grid,
  Typography,
} from '@mui/material';

interface DirectoryEditDialogProps {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
  onSubmit: () => Promise<void> | void;
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg';
}

export function DirectoryEditDialog({
  open,
  title,
  children,
  onClose,
  onSubmit,
  maxWidth = 'sm',
}: DirectoryEditDialogProps) {
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!open) {
      setError(null);
    }
  }, [open]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await onSubmit();
    } catch {
      setError('Не удалось сохранить изменения.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth={maxWidth} PaperProps={{ sx: { overflow: 'hidden' } }}>
      <Box component="form" onSubmit={handleSubmit}>
        <Box sx={{ bgcolor: '#2C3E50', px: 2, py: 1.5 }}>
          <Typography sx={{ color: '#ECF0F1', fontSize: 16, fontWeight: 600 }}>{title}</Typography>
        </Box>
        <DialogContent sx={{ bgcolor: '#FFFFFF', pt: 2 }}>
          <Grid container spacing={1} columnSpacing={2}>
            {children}
          </Grid>
          {error && (
            <Typography color="error" variant="body2" sx={{ mt: 1 }}>
              {error}
            </Typography>
          )}
        </DialogContent>
        <DialogActions sx={{ bgcolor: '#FFFFFF', px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={submitting} sx={{ minWidth: 100 }}>
            Отмена
          </Button>
          <Button type="submit" variant="contained" disabled={submitting} sx={{ minWidth: 100 }}>
            {submitting ? 'Сохранение...' : 'Сохранить'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

interface FormRowProps {
  label: string;
  children: ReactNode;
}

export function DirectoryFormRow({ label, children }: FormRowProps) {
  return (
    <>
      <Grid item xs={12} sm={3}>
        <Typography sx={{ fontSize: 14, pt: 1 }}>{label}</Typography>
      </Grid>
      <Grid item xs={12} sm={9}>
        {children}
      </Grid>
    </>
  );
}
