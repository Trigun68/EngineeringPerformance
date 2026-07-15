import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { fetchSystemSettings, updateSystemSettings } from '../../api/systemSettingsApi';
import { getApiErrorMessage } from '../../services/apiError';
import { wpColors } from '../../theme/wpColors';

export function SystemSettingsPage() {
  const [minutes, setMinutes] = useState('1440');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    void fetchSystemSettings()
      .then((s) => setMinutes(String(s.sessionExpirationMinutes)))
      .catch((err) => setError(getApiErrorMessage(err, 'Не удалось загрузить настройки.')))
      .finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    const value = Number(minutes);
    if (!Number.isFinite(value) || value < 5 || value > 10080) {
      setError('Длительность сессии должна быть от 5 до 10080 минут.');
      return;
    }
    setSaving(true);
    setError(null);
    setSuccess(false);
    try {
      await updateSystemSettings({ sessionExpirationMinutes: value });
      setSuccess(true);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить настройки.'));
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress size={28} />
      </Box>
    );
  }

  return (
    <Box sx={{ maxWidth: 520 }}>
      <Typography variant="h6" sx={{ mb: 2, color: wpColors.heading }}>
        Общие настройки системы
      </Typography>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }}>Настройки сохранены.</Alert>}
      <Stack spacing={2}>
        <TextField
          label="Длительность сессии (минуты)"
          type="number"
          value={minutes}
          onChange={(e) => setMinutes(e.target.value)}
          helperText="По умолчанию 1440 минут (24 часа). Применяется к новым токенам при входе и обновлении сессии."
          inputProps={{ min: 5, max: 10080 }}
          fullWidth
        />
        <Button variant="contained" onClick={() => void handleSave()} disabled={saving}>
          {saving ? 'Сохранение...' : 'Сохранить'}
        </Button>
      </Stack>
    </Box>
  );
}
