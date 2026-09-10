import { render, renderHook, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { DictionariesAllResponse } from '../../types/dictionary';
import { DICTIONARY_KEYS } from '../../types/dictionary';
import { queryKeys } from '../../services/queryKeys';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { useDictionaries } from '../useDictionaries';

const fetchDictionariesAll = vi.fn();

vi.mock('../../store/useDictionaryStore', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../store/useDictionaryStore')>();
  return {
    ...actual,
    fetchDictionariesAll: (...args: unknown[]) => fetchDictionariesAll(...args),
  };
});

function emptyDictionaries(): DictionariesAllResponse {
  return {
    physicalVolumeTypes: [{ id: 1, name: 'КФО', code: 'KFO' }],
    physicalVolumes: [],
    physicalVolumesKfo: [],
    physicalVolumesBer: [],
    equipments: [],
    laborIntensities: [],
    iicWorkDirections: [],
    iicWorkDirectionNorms: [],
    unitsOfMeasurement: [],
    docSets: [],
    projectCharacteristics: [],
  };
}

function createWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: 5 * 60_000 } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  return { client, wrapper };
}

function DualConsumers() {
  useDictionaries();
  useDictionaries();
  return <div data-testid="dual-ready" />;
}

describe('useDictionaries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useDictionaryStore.setState({
      dictionaries: null,
      isLoaded: false,
      isLoading: false,
      error: null,
    });
    fetchDictionariesAll.mockResolvedValue(emptyDictionaries());
  });

  it('fetches GET dictionaries/all once for two consumers', async () => {
    const { wrapper } = createWrapper();
    render(<DualConsumers />, { wrapper });
    await waitFor(() => {
      expect(screen.getByTestId('dual-ready')).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(fetchDictionariesAll).toHaveBeenCalledTimes(1);
    });
    expect(fetchDictionariesAll.mock.calls[0][0]).toBeInstanceOf(AbortSignal);
  });

  it('hydrates Zustand lookup layer from query data', async () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useDictionaries(), { wrapper });
    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });
    expect(result.current.data?.physicalVolumeTypes[0]?.name).toBe('КФО');
    expect(useDictionaryStore.getState().lists.physicalVolumeTypes[0]?.name).toBe('КФО');
    expect(queryKeys.dictionaries.all()).toEqual(['dictionaries', 'all']);
    for (const key of DICTIONARY_KEYS) {
      expect(Array.isArray(useDictionaryStore.getState().lists[key])).toBe(true);
    }
  });
});
