import { useEffect, useState } from 'react';

/**
 * Возвращает true только если `busy` непрерывно true дольше `delayMs`.
 * При сбросе busy — мгновенно false (без задержки скрытия).
 */
export function useDelayedBusy(busy: boolean, delayMs = 1000): boolean {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (!busy) {
      setVisible(false);
      return;
    }
    if (delayMs <= 0) {
      setVisible(true);
      return;
    }
    const timer = window.setTimeout(() => setVisible(true), delayMs);
    return () => window.clearTimeout(timer);
  }, [busy, delayMs]);

  return visible;
}
