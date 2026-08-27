import { createContext, useContext, useMemo, type ReactNode } from 'react';

export interface AnalyticsChromeContextValue {
  chromeExpanded: boolean;
  setChromeExpanded: (expanded: boolean) => void;
}

const AnalyticsChromeContext = createContext<AnalyticsChromeContextValue | null>(null);

export function AnalyticsChromeProvider({
  chromeExpanded,
  setChromeExpanded,
  children,
}: {
  chromeExpanded: boolean;
  setChromeExpanded: (expanded: boolean) => void;
  children: ReactNode;
}) {
  const value = useMemo(
    () => ({ chromeExpanded, setChromeExpanded }),
    [chromeExpanded, setChromeExpanded],
  );
  return (
    <AnalyticsChromeContext.Provider value={value}>{children}</AnalyticsChromeContext.Provider>
  );
}

export function useAnalyticsChrome(): AnalyticsChromeContextValue {
  const ctx = useContext(AnalyticsChromeContext);
  if (!ctx) {
    return { chromeExpanded: true, setChromeExpanded: () => undefined };
  }
  return ctx;
}
