import { useEffect, useState } from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField } from '@mui/material';

interface MfaVerifyModalProps {
  open: boolean;
  isLoading: boolean;
  error: string | null;
  onVerify: (code: string) => Promise<void>;
}

export function MfaVerifyModal({ open, isLoading, error, onVerify }: MfaVerifyModalProps) {
  const [code, setCode] = useState('');

  useEffect(() => {
    if (code.length !== 6 || isLoading) return;
    void onVerify(code).catch(() => undefined);
  }, [code, isLoading, onVerify]);

  useEffect(() => {
    if (!open) setCode('');
  }, [open]);

  return (
    <Dialog open={open} maxWidth="xs" fullWidth>
      <DialogTitle>Подтверждение MFA</DialogTitle>
      <DialogContent>
        <TextField
          autoFocus
          fullWidth
          label="Шестизначный код"
          value={code}
          onChange={(event) => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
          inputProps={{ inputMode: 'numeric', pattern: '[0-9]*', maxLength: 6 }}
          sx={{ mt: 1 }}
        />
        {error ? <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert> : null}
      </DialogContent>
      <DialogActions>
        <Button
          disabled={code.length !== 6 || isLoading}
          onClick={() => void onVerify(code).catch(() => undefined)}
        >
          Подтвердить
        </Button>
      </DialogActions>
    </Dialog>
  );
}
