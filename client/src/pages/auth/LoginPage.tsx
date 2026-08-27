import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { fetchWindowsIdentityHint } from '../../api/authApi';
import { MfaVerifyModal } from '../../components/auth/MfaVerifyModal';
import { useAuthStore } from '../../store/useAuthStore';

const LOGO_SRC = '/assets/logo.png';
const LOGIN_PANEL_BG = '#0D1B2A';

export function LoginPage() {
  const login = useAuthStore((state) => state.login);
  const isLoading = useAuthStore((state) => state.isLoading);
  const error = useAuthStore((state) => state.error);
  const clearError = useAuthStore((state) => state.clearError);
  const authStatus = useAuthStore((state) => state.status);
  const verifyMfa = useAuthStore((state) => state.verifyMfa);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [identityHint, setIdentityHint] = useState<{
    displayName: string | null;
    windowsAccount: string | null;
  } | null>(null);
  const [hintLoaded, setHintLoaded] = useState(false);

  useEffect(() => {
    void fetchWindowsIdentityHint()
      .then((hint) => {
        if (hint.source !== 'proxy') {
          setIdentityHint({ displayName: null, windowsAccount: null });
          return;
        }
        const displayName = hint.displayName?.trim() || null;
        const windowsAccount = hint.windowsUsername?.trim() || null;
        setIdentityHint({ displayName, windowsAccount });
      })
      .catch(() => setIdentityHint({ displayName: null, windowsAccount: null }))
      .finally(() => setHintLoaded(true));
  }, []);

  const welcomeName = useMemo(() => {
    if (!identityHint) return null;
    return identityHint.displayName || identityHint.windowsAccount || null;
  }, [identityHint]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    clearError();
    await login({ username, password });
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        bgcolor: LOGIN_PANEL_BG,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 2,
      }}
    >
      <Paper
        elevation={8}
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', sm: 'row' },
          width: '100%',
          maxWidth: 700,
          minHeight: { xs: 'auto', sm: 480 },
          overflow: 'hidden',
          borderRadius: 3,
        }}
      >
        <Box
          sx={{
            width: { xs: '100%', sm: 280 },
            flexShrink: 0,
            bgcolor: LOGIN_PANEL_BG,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            p: 4,
          }}
        >
          <Stack alignItems="center" spacing={2} sx={{ maxWidth: 220 }}>
            <Box
              component="img"
              src={LOGO_SRC}
              alt="Engineering Performance"
              sx={{ width: 180, height: 180, objectFit: 'contain' }}
            />
            <Typography sx={{ color: '#FFFFFF', fontSize: 18, fontWeight: 700, textAlign: 'center' }}>
              Engineering Performance
            </Typography>
            <Typography sx={{ color: '#8899AA', fontSize: 12, textAlign: 'center' }}>
              Система управления эффективностью
            </Typography>
          </Stack>
        </Box>

        <Box
          sx={{
            flex: 1,
            bgcolor: '#F7F8FA',
            display: 'flex',
            alignItems: 'center',
            p: { xs: 3, sm: 5 },
          }}
        >
          <Box sx={{ width: '100%' }}>
            <Typography component="h1" sx={{ fontSize: 24, fontWeight: 700, color: LOGIN_PANEL_BG, mb: 1 }}>
              Авторизация
            </Typography>
            {hintLoaded && welcomeName ? (
              <Typography sx={{ fontSize: 13, color: '#6B7280', mb: 3.5 }}>
                Добро пожаловать, {welcomeName}
              </Typography>
            ) : hintLoaded ? (
              <Typography sx={{ fontSize: 13, color: '#6B7280', mb: 3.5 }}>
                Введите учетные данные
              </Typography>
            ) : (
              <Typography sx={{ fontSize: 13, color: '#6B7280', mb: 3.5 }}>
                Загрузка...
              </Typography>
            )}
            <Box component="form" onSubmit={handleSubmit}>
              <Stack spacing={2}>
                <TextField
                  label="Имя пользователя"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  required
                  autoFocus
                  fullWidth
                  size="small"
                />
                <TextField
                  label="Пароль"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  fullWidth
                  size="small"
                />
                {error && <Alert severity="error">{error}</Alert>}
                <Button
                  type="submit"
                  variant="contained"
                  disabled={isLoading}
                  fullWidth
                  size="large"
                  sx={{
                    mt: 1,
                    bgcolor: LOGIN_PANEL_BG,
                    textTransform: 'none',
                    fontWeight: 600,
                    boxShadow: 'none',
                    '&:hover': { bgcolor: '#1B2838', boxShadow: 'none' },
                    '&:disabled': { bgcolor: '#4A5568', color: '#A0AEC0' },
                  }}
                >
                  {isLoading ? 'Вход...' : 'Войти'}
                </Button>
              </Stack>
            </Box>
          </Box>
        </Box>
      </Paper>
      <MfaVerifyModal
        open={authStatus === 'mfa_required'}
        isLoading={isLoading}
        error={error}
        onVerify={verifyMfa}
      />
    </Box>
  );
}
