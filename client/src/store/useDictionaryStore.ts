import { create } from 'zustand';
import apiClient from '../services/apiClient';
import {
  DICTIONARY_KEYS,
  type DictionaryItem,
  type DictionaryKey,
  type DictionariesAllResponse,
} from '../types/dictionary';
import { formatIisNorm } from '../utils/formatIisNorm';
import { resolveIicDirectionUnitName } from '../utils/resolveIicDirectionUnit';

const DICTIONARIES_STORAGE_KEY = 'ep.dictionaries.v1';

interface StoredDictionaries {
  etag: string;
  data: DictionariesAllResponse;
}

function readStoredDictionaries(): StoredDictionaries | null {
  try {
    const raw = sessionStorage.getItem(DICTIONARIES_STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as Partial<StoredDictionaries>;
    if (typeof parsed.etag === 'string' && parsed.data) {
      return { etag: parsed.etag, data: parsed.data };
    }
  } catch {
    /* private mode / quota */
  }
  return null;
}

function writeStoredDictionaries(etag: string, data: DictionariesAllResponse): void {
  try {
    sessionStorage.setItem(DICTIONARIES_STORAGE_KEY, JSON.stringify({ etag, data }));
  } catch {
    /* quota */
  }
}

function clearStoredDictionaries(): void {
  try {
    sessionStorage.removeItem(DICTIONARIES_STORAGE_KEY);
  } catch {
    /* ignore */
  }
}

async function requestDictionaries(useHttpCache: boolean): Promise<DictionariesAllResponse> {
  const stored = useHttpCache ? readStoredDictionaries() : null;
  const response = await apiClient.get<DictionariesAllResponse>('/dictionaries/all', {
    headers: stored ? { 'If-None-Match': stored.etag } : undefined,
    validateStatus: (status) => status === 200 || status === 304,
  });
  if (response.status === 304 && stored) {
    return stored.data;
  }
  const etag = response.headers.etag;
  if (typeof etag === 'string' && etag.length > 0) {
    writeStoredDictionaries(etag, response.data);
  }
  return response.data;
}

/**
 * Справочники загружаются один раз при старте приложения (см. AppLayout.tsx)
 * и кэшируются в Zustand до явного invalidate() после правок справочников.
 */

function pickLists(data: DictionariesAllResponse): Record<DictionaryKey, DictionaryItem[]> {
  return {
    physicalVolumeTypes: data.physicalVolumeTypes ?? [],
    physicalVolumes: data.physicalVolumes ?? [],
    physicalVolumesKfo: data.physicalVolumesKfo ?? [],
    physicalVolumesBer: data.physicalVolumesBer ?? [],
    equipments: data.equipments ?? [],
    laborIntensities: data.laborIntensities ?? [],
    iicWorkDirections: data.iicWorkDirections ?? [],
    iicWorkDirectionNorms: data.iicWorkDirectionNorms ?? [],
    unitsOfMeasurement: data.unitsOfMeasurement ?? [],
    docSets: data.docSets ?? [],
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
    set({ isLoading: true, error: null });
    try {
      const data = await requestDictionaries(true);
      const { caches, lists } = buildCaches(data);
      set({ dictionaries: data, caches, lists, isLoaded: true });
    } catch {
      set({ error: 'Не удалось загрузить справочники.' });
    } finally {
      set({ isLoading: false });
    }
  },

  fetchDictionaries: async () => {
    await get().fetchAll();
  },

  invalidate: async () => {
    clearStoredDictionaries();
    set({ isLoading: true, error: null, isLoaded: false });
    try {
      const data = await requestDictionaries(false);
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

  getDirectionName: (id) => {
    if (id == null) {
      return '';
    }

    const directions = get().lists.iicWorkDirections;
    const direction = get().caches.iicWorkDirections.get(id);
    if (!direction) {
      return String(id);
    }

    return formatIisNorm({
      code: direction.code,
      name: direction.name,
      unitName: resolveIicDirectionUnitName(id, directions, get().getUnitName),
    });
  },

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
