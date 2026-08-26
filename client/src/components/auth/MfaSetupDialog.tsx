import { useEffect, useState } from 'react';
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField, Typography } from '@mui/material';
import { beginMfaSetup, completeMfaSetup } from '../../api/authApi';

interface MfaSetupDialogProps {
  open: boolean;
  onCompleted: () => void;
  onDismiss: () => void;
}

export function MfaSetupDialog({ open, onCompleted, onDismiss }: MfaSetupDialogProps) {
  const [secret, setSecret] = useState<string | null>(null);
  const [qrCodeUri, setQrCodeUri] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!open) {
      setSecret(null);
      setQrCodeUri(null);
      setCode('');
      setError(null);
      return;
    }

    if (secret) return;

    setIsLoading(true);
    void beginMfaSetup()
      .then((setup) => {
        setSecret(setup.secret);
        setQrCodeUri(setup.qrCodeUri);
      })
      .catch(() => setError('Не удалось подготовить настройку MFA.'))
      .finally(() => setIsLoading(false));
  }, [open, secret]);

  const verify = async () => {
    if (!secret || code.length !== 6) return;

    setIsLoading(true);
    setError(null);
    try {
      await completeMfaSetup(code, secret);
      onCompleted();
    } catch {
      setError('Неверный код подтверждения.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={open} maxWidth="sm" fullWidth onClose={onDismiss}>
      <DialogTitle>Рекомендуется настроить MFA</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography>
            Добавьте ключ в приложение-аутентификатор, затем введите шестизначный код. Настройку можно выполнить позднее.
          </Typography>
          {secret ? (
            <>
              <Box>
                <Typography variant="subtitle2">Секретный ключ</Typography>
                <Typography sx={{ fontFamily: 'monospace', overflowWrap: 'anywhere' }}>{secret}</Typography>
              </Box>
              <Box>
                <Typography variant="subtitle2">URI для приложения-аутентификатора</Typography>
                <Typography sx={{ fontFamily: 'monospace', fontSize: 12, overflowWrap: 'anywhere' }}>
                  {qrCodeUri}
                </Typography>
              </Box>
            </>
          ) : (
            <Typography>Подготовка ключа...</Typography>
          )}
          <TextField
            label="Шестизначный код"
            value={code}
            onChange={(event) => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
            inputProps={{ inputMode: 'numeric', pattern: '[0-9]*', maxLength: 6 }}
            disabled={!secret || isLoading}
          />
          {error ? <Alert severity="error">{error}</Alert> : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button data-testid="mfa-setup-skip" onClick={onDismiss}>
          Настроить позднее
        </Button>
        <Button disabled={!secret || code.length !== 6 || isLoading} onClick={() => void verify()}>
          Подтвердить и включить
        </Button>
      </DialogActions>
    </Dialog>
  );
}
