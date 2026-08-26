import { useCallback, useEffect, useRef, useState } from 'react';
import { getUserPreferences, putUserPreferences } from '../api/userPreferencesApi';

/**
 * Сохраняет значение вкладки/состояния UI в user preferences.
 * Persist только при явном update() — не в useEffect(value), иначе риск цикла
 * Maximum update depth при смене prefKey / Tabs.
 *
 * Загрузка prefs не должна перетирать уже сделанный пользователем update()
 * (гонка: click → put prefs, затем late getUserPreferences → setValue(default)).
 */
export function usePersistedTabState<T extends string>(
  prefKey: string,
  defaultValue: T,
): [T, (value: T) => void] {
  const [value, setValue] = useState<T>(defaultValue);
  const prefKeyRef = useRef(prefKey);
  prefKeyRef.current = prefKey;
  const userTouchedRef = useRef(false);

  useEffect(() => {
    let cancelled = false;
    userTouchedRef.current = false;
    setValue(defaultValue);

    void getUserPreferences()
      .then((prefs) => {
        if (cancelled || prefKeyRef.current !== prefKey || userTouchedRef.current) return;
        const raw = prefs[prefKey];
        if (typeof raw === 'string' && raw.length > 0) {
          setValue(raw as T);
        }
      })
      .catch(() => {
        // оставляем defaultValue, уже выставленный выше
      });
    return () => {
      cancelled = true;
    };
  }, [prefKey, defaultValue]);

  const update = useCallback((next: T) => {
    userTouchedRef.current = true;
    setValue(next);
    void putUserPreferences({ [prefKeyRef.current]: next });
  }, []);

  return [value, update];
}
