import { create } from 'zustand';
import apiClient from '../services/apiClient';
import {
  DICTIONARY_KEYS,
  type DictionaryItem,
  type DictionaryKey,
  type DictionariesAllResponse,
} from '../types/dictionary';

/**
 * Справочники загружаются один раз при старте приложения (см. AppLayout.tsx)
 * и кэшируются в Zustand до явного invalidate() после правок справочников.
 */

function pickLists(data: DictionariesAllResponse): Record<DictionaryKey, DictionaryItem[]> {
  return {
    physicalVolumeTypes: data.physicalVolumeTypes ?? [],
    physicalVolumes: data.physicalVolumes ?? [],
    equipments: data.equipments ?? [],
    laborIntensities: data.laborIntensities ?? [],
    iicWorkDirections: data.iicWorkDirections ?? [],
    iicWorkDirectionNorms: data.iicWorkDirectionNorms ?? [],
    unitsOfMeasurement: data.unitsOfMeasurement ?? [],
    projectCharacteristics: data.projectCharacteristics ?? [],
  };
}

interface DictionaryState {
  dictionaries: DictionariesAllResponse | null;
  isLoading: boolean;
  isLoaded: boolean;
  error: string | null;
  caches: Record<DictionaryKey, Map<number, DictionaryItem>>;
  lists: Record<DictionaryKey, DictionaryItem[]>;
  /** @alias fetchDictionaries */
  fetchAll: () => Promise<void>;
  fetchDictionaries: () => Promise<void>;
  invalidate: () => Promise<void>;
  getById: (key: DictionaryKey, id: number) => DictionaryItem | undefined;
  getList: (key: DictionaryKey) => DictionaryItem[];
  getName: (key: DictionaryKey, id: number | null | undefined) => string;
  getUnitName: (id: number | null | undefined) => string;
  getDirectionName: (id: number | null | undefined) => string;
  getNormTypeName: (codeOrId: string | number | null | undefined) => string;
}

function buildCaches(data: DictionariesAllResponse): {
  caches: Record<DictionaryKey, Map<number, DictionaryItem>>;
  lists: Record<DictionaryKey, DictionaryItem[]>;
} {
  const caches = {} as Record<DictionaryKey, Map<number, DictionaryItem>>;
  const lists = {} as Record<DictionaryKey, DictionaryItem[]>;

  const listsByKey = pickLists(data);
  for (const key of DICTIONARY_KEYS) {
    const items = listsByKey[key];
    lists[key] = items;
    caches[key] = new Map(items.map((item) => [item.id, item]));
  }

  return { caches, lists };
}

function emptyState(): Pick<DictionaryState, 'caches' | 'lists' | 'dictionaries'> {
  const caches = {} as Record<DictionaryKey, Map<number, DictionaryItem>>;
  const lists = {} as Record<DictionaryKey, DictionaryItem[]>;
  for (const key of DICTIONARY_KEYS) {
    caches[key] = new Map();
    lists[key] = [];
  }
  return { caches, lists, dictionaries: null };
}

export const useDictionaryStore = create<DictionaryState>((set, get) => ({
  ...emptyState(),
  isLoading: false,
  isLoaded: false,
  error: null,

  fetchAll: async () => {
    if (get().isLoaded || get().isLoading) {
      return;
    }
    await get().invalidate();
  },

  fetchDictionaries: async () => {
    await get().fetchAll();
  },

  invalidate: async () => {
    set({ isLoading: true, error: null, isLoaded: false });
    try {
      const { data } = await apiClient.get<DictionariesAllResponse>('/dictionaries/all');
      const { caches, lists } = buildCaches(data);
      set({ dictionaries: data, caches, lists, isLoaded: true });
    } catch {
      set({ error: 'Не удалось загрузить справочники.' });
    } finally {
      set({ isLoading: false });
    }
  },

  getById: (key, id) => get().caches[key].get(id),

  getList: (key) => get().lists[key],

  getName: (key, id) => {
    if (id == null) {
      return '';
    }
    return get().caches[key].get(id)?.name ?? String(id);
  },

  getUnitName: (id) => get().getName('unitsOfMeasurement', id),

  getDirectionName: (id) => get().getName('iicWorkDirections', id),

  getNormTypeName: (codeOrId) => {
    if (codeOrId == null || codeOrId === '') {
      return '';
    }
    const types = get().lists.physicalVolumeTypes;
    if (typeof codeOrId === 'number') {
      return types.find((t) => t.id === codeOrId)?.name ?? String(codeOrId);
    }
    const token = codeOrId.trim();
    const byCode = types.find(
      (t) => t.code?.toLowerCase() === token.toLowerCase() || t.name.toLowerCase() === token.toLowerCase(),
    );
    return byCode?.name ?? token;
  },
}));
