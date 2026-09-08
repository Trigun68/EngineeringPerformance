import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getApiErrorMessage } from '../services/apiError';
import { queryKeys } from '../services/queryKeys';
import { fetchDictionariesAll, useDictionaryStore } from '../store/useDictionaryStore';

/**
 * Единственный fetch GET /api/dictionaries/all. Lookup'и остаются в Zustand:
 * хук гидрирует store.setData, чтобы getDirectionName и списки не дублировались.
 */
export function useDictionaries(enabled = true) {
  const setData = useDictionaryStore((s) => s.setData);

  const query = useQuery({
    queryKey: queryKeys.dictionaries.all(),
    queryFn: ({ signal }) => fetchDictionariesAll(signal),
    staleTime: 5 * 60_000,
    enabled,
  });

  useEffect(() => {
    if (query.data) {
      setData(query.data);
    }
  }, [query.data, setData]);

  useEffect(() => {
    if (!enabled) {
      useDictionaryStore.setState({ isLoading: false });
      return;
    }
    if (query.isError) {
      useDictionaryStore.setState({
        isLoading: false,
        error: getApiErrorMessage(query.error, 'Не удалось загрузить справочники.'),
      });
      return;
    }
    // isLoading — только первичная загрузка, чтобы импорт не блокировался на refetch.
    useDictionaryStore.setState({
      isLoading: query.isPending,
      error: null,
    });
  }, [enabled, query.error, query.isError, query.isPending]);

  return query;
}
