import { Backdrop, CircularProgress, Stack, Typography } from '@mui/material';
import { useDelayedBusy } from '../../hooks/useDelayedBusy';

export interface WorkingOverlayProps {
  open: boolean;
  /** Текст поверх индикатора. */
  message?: string;
  /**
   * z-index поверх модалок (по умолчанию modal + 2).
   * Нужен, когда операция идёт внутри Dialog.
   */
  aboveModals?: boolean;
  /**
   * Показывать оверлей только если `open` дольше указанного времени (мс).
   * 0 — сразу (мутации/экспорт). Для загрузки данных обычно 1000.
   */
  delayMs?: number;
}

/**
 * Полноэкранный индикатор длительных операций («Работаю…»).
 */
export function WorkingOverlay({
  open,
  message = 'Работаю…',
  aboveModals = true,
  delayMs = 0,
}: WorkingOverlayProps) {
  const visible = useDelayedBusy(open, delayMs);

  return (
    <Backdrop
      open={visible}
      data-testid="working-overlay"
      sx={{
        color: '#ECF0F1',
        zIndex: (theme) => (aboveModals ? theme.zIndex.modal + 2 : theme.zIndex.drawer + 1),
        bgcolor: 'rgba(44, 62, 80, 0.55)',
      }}
    >
      <Stack alignItems="center" spacing={2}>
        <CircularProgress color="inherit" size={40} />
        <Typography variant="h6" component="p" sx={{ fontWeight: 500, letterSpacing: 0.3 }}>
          {message}
        </Typography>
      </Stack>
    </Backdrop>
  );
}
