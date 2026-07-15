import { useCallback, useEffect, useRef, useState } from 'react';
import { getUserPreferences, putUserPreferences } from '../api/userPreferencesApi';

/**
 * Сохраняет значение вкладки/состояния UI в user preferences.
 * Persist только при явном update() — не в useEffect(value), иначе риск цикла
 * Maximum update depth при смене prefKey / Tabs.
 */
export function usePersistedTabState<T extends string>(
  prefKey: string,
  defaultValue: T,
): [T, (value: T) => void] {
  const [value, setValue] = useState<T>(defaultValue);
  const prefKeyRef = useRef(prefKey);
  prefKeyRef.current = prefKey;

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled || prefKeyRef.current !== prefKey) return;
        const raw = prefs[prefKey];
        if (typeof raw === 'string' && raw.length > 0) {
          setValue(raw as T);
        } else {
          setValue(defaultValue);
        }
      })
      .catch(() => {
        if (!cancelled && prefKeyRef.current === prefKey) {
          setValue(defaultValue);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [prefKey, defaultValue]);

  const update = useCallback((next: T) => {
    setValue(next);
    void putUserPreferences({ [prefKeyRef.current]: next });
  }, []);

  return [value, update];
}
