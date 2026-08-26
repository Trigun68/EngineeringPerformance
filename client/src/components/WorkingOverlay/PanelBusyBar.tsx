import { Box, LinearProgress } from '@mui/material';
import { useDelayedBusy } from '../../hooks/useDelayedBusy';

export interface PanelBusyBarProps {
  open: boolean;
  /** Показывать только если загрузка длится дольше указанного времени (мс) — без мигания на быстрых ответах. */
  delayMs?: number;
  'data-testid'?: string;
}

const BAR_HEIGHT = 3;

/**
 * Неблокирующий индикатор загрузки данных внутри панели: остальной интерфейс
 * (шапка, вкладки, соседние наборы данных) остаётся доступным (ISS-018).
 * Блокирующий {@link WorkingOverlay} остаётся только для мутаций и экспорта.
 */
export function PanelBusyBar({ open, delayMs = 300, 'data-testid': testId }: PanelBusyBarProps) {
  const visible = useDelayedBusy(open, delayMs);

  // Место под полосу резервируем всегда, иначе контент дёргается при появлении.
  // data-busy отражает сам факт загрузки, а не задержку показа: E2E ждёт готовности
  // данных по этому атрибуту (см. waitForWorkingOverlayHidden), и на быстрых ответах
  // проверка не должна проскакивать между стартом запроса и появлением полосы.
  return (
    <Box
      sx={{ height: BAR_HEIGHT, flexShrink: 0 }}
      data-testid={testId}
      data-busy={open ? 'true' : 'false'}
    >
      {visible ? <LinearProgress sx={{ height: BAR_HEIGHT }} /> : null}
    </Box>
  );
}
