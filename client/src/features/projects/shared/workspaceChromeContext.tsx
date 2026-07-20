import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

export interface WorkspaceChromeContextValue {
  /** false — свёрнут весь chrome до таблицы. */
  chromeExpanded: boolean;
  setChromeExpanded: (expanded: boolean) => void;
  /** Подпись версии для компактной строки (план / факт). */
  versionSummary: string | null;
  setVersionSummary: (label: string | null) => void;
}

const WorkspaceChromeContext = createContext<WorkspaceChromeContextValue | null>(null);

export function WorkspaceChromeProvider({
  chromeExpanded,
  setChromeExpanded,
  children,
}: {
  chromeExpanded: boolean;
  setChromeExpanded: (expanded: boolean) => void;
  children: ReactNode;
}) {
  const [versionSummary, setVersionSummaryState] = useState<string | null>(null);
  const setVersionSummary = useCallback((label: string | null) => {
    setVersionSummaryState(label);
  }, []);

  const value = useMemo(
    () => ({
      chromeExpanded,
      setChromeExpanded,
      versionSummary,
      setVersionSummary,
    }),
    [chromeExpanded, setChromeExpanded, versionSummary, setVersionSummary],
  );

  return (
    <WorkspaceChromeContext.Provider value={value}>
      {children}
    </WorkspaceChromeContext.Provider>
  );
}

export function useWorkspaceChrome(): WorkspaceChromeContextValue {
  const ctx = useContext(WorkspaceChromeContext);
  if (!ctx) {
    return {
      chromeExpanded: true,
      setChromeExpanded: () => undefined,
      versionSummary: null,
      setVersionSummary: () => undefined,
    };
  }
  return ctx;
}
