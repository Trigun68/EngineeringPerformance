import { useEffect } from 'react';
import type { QueryClient } from '@tanstack/react-query';
import type { IicNorm, IicNormHistoryItem } from '../../api/registersApi';
import { fetchIicNormHistory } from '../../api/registersApi';
import { queryKeys } from '../../services/queryKeys';
import { normToHistoryItem } from './iicNormTree';

export function useIicNormDeepLink(params: {
  canRead: boolean;
  loading: boolean;
  rows: IicNorm[];
  queryClient: QueryClient;
  searchParams: URLSearchParams;
  setSearchParams: (next: URLSearchParams, opts: { replace: boolean }) => void;
  openEdit: (row: IicNorm) => void;
  setSelected: (row: IicNorm) => void;
  setHistoryDialogInitial: (item: IicNormHistoryItem | null) => void;
  setSelectedHistory: (item: IicNormHistoryItem | null) => void;
  setHistoryDialogOpen: (open: boolean) => void;
}): void {
  const {
    canRead,
    loading,
    rows,
    queryClient,
    searchParams,
    setSearchParams,
    openEdit,
    setSelected,
    setHistoryDialogInitial,
    setSelectedHistory,
    setHistoryDialogOpen,
  } = params;

  useEffect(() => {
    if (!canRead || loading || rows.length === 0) return;
    const wdRaw = searchParams.get('workDirectionId');
    const normRaw = searchParams.get('normId');
    if (!wdRaw && !normRaw) return;

    const workDirectionId = wdRaw ? Number(wdRaw) : NaN;
    const normId = normRaw ? Number(normRaw) : NaN;

    void (async () => {
      if (Number.isFinite(workDirectionId) && workDirectionId > 0) {
        const history = await queryClient.fetchQuery({
          queryKey: queryKeys.registers.iicNormsHistory(workDirectionId),
          queryFn: () => fetchIicNormHistory(workDirectionId),
        });
        const items = history.map(normToHistoryItem);
        if (Number.isFinite(normId) && normId > 0) {
          const item = items.find((h) => h.id === normId) ?? null;
          setHistoryDialogInitial(item);
          setSelectedHistory(item);
          setHistoryDialogOpen(true);
          const current = rows.find((r) => r.workDirectionId === workDirectionId);
          if (current) setSelected(current);
        } else {
          const current = rows.find((r) => r.workDirectionId === workDirectionId);
          if (current) openEdit(current);
        }
      } else if (Number.isFinite(normId) && normId > 0) {
        const current = rows.find((r) => r.id === normId);
        if (current) openEdit(current);
      }

      const next = new URLSearchParams(searchParams);
      next.delete('workDirectionId');
      next.delete('normId');
      setSearchParams(next, { replace: true });
    })();
  }, [
    canRead,
    loading,
    queryClient,
    rows,
    searchParams,
    setSearchParams,
    openEdit,
    setSelected,
    setHistoryDialogInitial,
    setHistoryDialogOpen,
    setSelectedHistory,
  ]);
}
